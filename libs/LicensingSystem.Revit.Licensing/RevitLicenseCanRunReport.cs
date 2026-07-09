using System.Diagnostics;
using System.Text;
using LicensingSystem.Agent.Ipc;
using LicensingSystem.Contracts.Agent;
using LicensingSystem.Contracts.Agent.Grants;

namespace LicensingSystem.Revit.Licensing;

/// <summary>
/// Calls the local agent <c>canRun</c> over the named pipe, verifies publisher-signed proof material, and formats a support-oriented status report.
/// Caches a verified-OK report for <see cref="DefaultVerifiedOkCacheTtl"/> (same policy as the License Probe template).
/// </summary>
public static class RevitLicenseCanRunReport
{
    public static readonly TimeSpan DefaultVerifiedOkCacheTtl = TimeSpan.FromSeconds(60);

    private static readonly object CacheLock = new();
    private static string? _cachedProductCode;
    private static DateTimeOffset _cachedGoodUntilUtc;
    private static string? _cachedVerifiedMessage;

    /// <param name="pinning">Pinned publisher identity and product (from add-in constants).</param>
    /// <param name="pluginVersion">Typically <c>Assembly.GetExecutingAssembly().GetName().Version</c> string.</param>
    /// <param name="revitVersion">Optional Revit version label for diagnostics.</param>
    /// <param name="verifiedOkCacheTtl">TTL for in-memory cache after full cryptographic OK; pass <c>TimeSpan.Zero</c> to disable caching.</param>
    /// <param name="ipcTimeout">Timeout for the canRun IPC round-trip (linked with <paramref name="cancellationToken"/>).</param>
    /// <param name="trace">Optional diagnostic lines (e.g. file log).</param>
    public static async Task<string> BuildAsync(
        RevitLicensePinning pinning,
        string pluginVersion,
        string? revitVersion = null,
        TimeSpan? verifiedOkCacheTtl = null,
        TimeSpan? ipcTimeout = null,
        Action<string>? trace = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var ttl = verifiedOkCacheTtl ?? DefaultVerifiedOkCacheTtl;
        if (ttl > TimeSpan.Zero)
        {
            lock (CacheLock)
            {
                if (string.Equals(_cachedProductCode, pinning.ProductCode, StringComparison.OrdinalIgnoreCase)
                    && DateTimeOffset.UtcNow < _cachedGoodUntilUtc
                    && !string.IsNullOrEmpty(_cachedVerifiedMessage))
                {
                    TraceLine(trace, "BuildAsync: cache hit (verified OK still within TTL).");
                    return _cachedVerifiedMessage!;
                }
            }
        }

        var sw = Stopwatch.StartNew();
        TraceLine(
            trace,
            $"BuildAsync: start add-inVersion={pluginVersion} product={pinning.ProductCode}");

        var nonce = Guid.NewGuid().ToString("N");
        var ts = DateTimeOffset.UtcNow;
        var pipeName = NamedPipeNames.ForCurrentUser();
        TraceLine(trace, $"BuildAsync: IPC canRun pipe={pipeName}");

        CanRunProofDto proof;
        long canRunMs;
        try
        {
            var ipc = new NamedPipeAgentClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ipcTimeout ?? TimeSpan.FromSeconds(4));
            proof = await ipc
                .CallAsync<CanRunProofDto>(
                    new
                    {
                        type = "canRun",
                        productCode = pinning.ProductCode,
                        pluginVersion,
                        nonce,
                        timestampUtc = ts,
                        correlationId,
                    },
                    timeoutCts.Token)
                .ConfigureAwait(false);
            canRunMs = sw.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            TraceLine(
                trace,
                $"BuildAsync: canRun IPC failed after {sw.ElapsedMilliseconds}ms {ex.GetType().Name}: {ex.Message}");
            var sbFail = new StringBuilder();
            sbFail.AppendLine(FormatIpcFailureSummary(ex));
            sbFail.AppendLine();
            sbFail.AppendLine("Details");
            sbFail.AppendLine($"- Revit: {revitVersion ?? "(unknown)"}");
            sbFail.AppendLine($"- Product: {pinning.ProductCode}");
            sbFail.AppendLine($"- Add-in version: {pluginVersion}");
            sbFail.AppendLine($"- CorrelationId: {correlationId ?? "(none)"}");
            sbFail.AppendLine($"- Pipe: {pipeName}");
            sbFail.AppendLine($"- RequestNonce: {nonce}");
            sbFail.AppendLine($"- RequestTimestampUtc: {ts:O}");
            sbFail.AppendLine($"- Error: {ex.GetType().Name}: {ex.Message}");
            sbFail.AppendLine();
            if (!IsMissingPluginDependency(ex))
            {
                sbFail.AppendLine($"Override: set {NamedPipeNames.PipeNameOverrideEnvVar}");
                sbFail.AppendLine();
            }

            sbFail.AppendLine(FormatIpcFailureNextSteps(ex));
            return sbFail.ToString().TrimEnd();
        }

        sw.Stop();
        TraceLine(
            trace,
            $"BuildAsync: canRun OK in {sw.ElapsedMilliseconds}ms allowed={proof.Allowed} reason={TruncateForLog(proof.Reason, 240)} grant={(proof.Grant is not null)} publisherKey={(proof.PublisherKey is not null)}");

        var sb = new StringBuilder();
        sb.AppendLine($"Product: {pinning.ProductCode}");
        sb.AppendLine($"Add-in version: {pluginVersion}");
        sb.AppendLine();

        if (!proof.Allowed)
        {
            TraceLine(trace, "BuildAsync: policy denied (Allowed=false); building user message.");
            sb.AppendLine("This product is not licensed for this machine.");
            sb.AppendLine(CanRunReasonMessages.FormatWithCode(proof.Reason));
            AppendGrantHint(sb, proof, pinning);
            AppendDetails(
                sb,
                revitVersion,
                correlationId,
                pipeName,
                pluginVersion,
                nonce,
                ts,
                proof,
                canRunMs,
                verificationOk: false,
                verificationError: "Denied by policy (Allowed=false).");
            return sb.ToString().TrimEnd();
        }

        if (proof.Grant is null || proof.PublisherKey is null)
        {
            TraceLine(
                trace,
                "BuildAsync: Allowed=true but missing grant or publisher key; deny.");
            sb.AppendLine("License verification failed: the agent response did not include a signed grant.");
            sb.AppendLine("Do not treat this session as licensed. Please contact support.");
            AppendDetails(
                sb,
                revitVersion,
                correlationId,
                pipeName,
                pluginVersion,
                nonce,
                ts,
                proof,
                canRunMs,
                verificationOk: false,
                verificationError: "Allowed=true but missing Grant and/or PublisherKey.");
            return sb.ToString().TrimEnd();
        }

        TraceLine(trace, "BuildAsync: verifying publisher-signed grant (nonce/timestamp/product binding).");
        if (!RevitLicenseProofVerifier.TryVerifyProof(proof, pinning, nonce, ts, out var proofError))
        {
            TraceLine(
                trace,
                $"BuildAsync: cryptographic verification failed: {TruncateForLog(proofError ?? "unknown", 400)}");
            sb.AppendLine("License verification failed; please contact support.");
            sb.AppendLine(proofError ?? "Unknown proof error.");
            AppendDetails(
                sb,
                revitVersion,
                correlationId,
                pipeName,
                pluginVersion,
                nonce,
                ts,
                proof,
                canRunMs,
                verificationOk: false,
                verificationError: proofError ?? "Unknown proof error.");
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine("License: OK (publisher-signed grant verified for this build).");
        AppendDetails(
            sb,
            revitVersion,
            correlationId,
            pipeName,
            pluginVersion,
            nonce,
            ts,
            proof,
            canRunMs,
            verificationOk: true,
            verificationError: null);
        var text = sb.ToString().TrimEnd();
        if (ttl > TimeSpan.Zero)
        {
            lock (CacheLock)
            {
                _cachedProductCode = pinning.ProductCode;
                _cachedVerifiedMessage = text;
                _cachedGoodUntilUtc = DateTimeOffset.UtcNow.Add(ttl);
            }

            TraceLine(
                trace,
                $"BuildAsync: verification succeeded; cached OK for {(int)ttl.TotalSeconds}s.");
        }

        return text;
    }

    internal static string FormatIpcFailureSummary(Exception ex)
    {
        if (IsMissingPluginDependency(ex))
        {
            return "Plugin dependency missing (for example System.Text.Json). Reinstall or update this product from VP-Hub.";
        }

        if (ex is TimeoutException
            || ex.InnerException is TimeoutException
            || ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "Licensing agent is not running or not signed in.";
        }

        return "Licensing agent is not running or not signed in.";
    }

    internal static string FormatIpcFailureNextSteps(Exception ex)
    {
        if (IsMissingPluginDependency(ex))
        {
            return "In VP-Hub, open Products, run Install / update for this product, then restart Revit. If the problem continues, export diagnostics and contact support.";
        }

        return "Start the VP-Hub / LicensingSystem agent, sign in, and sync entitlements.";
    }

    private static bool IsMissingPluginDependency(Exception ex)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is FileNotFoundException or FileLoadException)
            {
                var name = cur.Message;
                if (name.Contains("System.Text.Json", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("LicensingSystem.Agent.Ipc", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("LicensingSystem.Contracts", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void TraceLine(Action<string>? trace, string message) => trace?.Invoke(message);

    private static string TruncateForLog(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        var s = value!;
        return s.Length <= maxLen ? s : s.Substring(0, maxLen) + "…";
    }

    private static void AppendGrantHint(StringBuilder sb, CanRunProofDto proof, RevitLicensePinning pinning)
    {
        if (proof.Grant is null || proof.PublisherKey is null)
            return;

        sb.AppendLine();
        if (RevitLicenseProofVerifier.TryVerifyProof(
                proof,
                pinning,
                proof.RequestNonce,
                proof.RequestTimestampUtc,
                out var err))
        {
            sb.AppendLine(
                "Note: a signed grant is present and matches pinned publisher settings, but entitlement or seat policy denied execution.");
        }
        else
        {
            sb.AppendLine("Grant present but not cryptographically trusted: " + (err ?? ""));
        }
    }

    private static void AppendDetails(
        StringBuilder sb,
        string? revitVersion,
        string? correlationId,
        string pipeName,
        string addinVersion,
        string requestNonce,
        DateTimeOffset requestTimestampUtc,
        CanRunProofDto proof,
        long canRunMs,
        bool verificationOk,
        string? verificationError)
    {
        sb.AppendLine();
        sb.AppendLine("Details");

        sb.AppendLine($"- Revit: {revitVersion ?? "(unknown)"}");
        sb.AppendLine($"- CorrelationId: {correlationId ?? "(none)"}");
        sb.AppendLine($"- Pipe: {pipeName}");
        sb.AppendLine($"- canRun duration: {canRunMs} ms");

        sb.AppendLine($"- RequestNonce: {requestNonce}");
        sb.AppendLine($"- RequestTimestampUtc: {requestTimestampUtc:O}");
        sb.AppendLine($"- ProofNonce: {proof.RequestNonce}");
        sb.AppendLine($"- ProofTimestampUtc: {proof.RequestTimestampUtc:O}");
        sb.AppendLine($"- ProofProductCode: {proof.ProductCode}");
        sb.AppendLine($"- ProofPluginVersion: {proof.PluginVersion ?? "(null)"}");

        sb.AppendLine($"- Allowed: {proof.Allowed}");
        sb.AppendLine($"- Reason: {ReasonCodes.Normalize(proof.Reason)}");
        sb.AppendLine($"- Verification: {(verificationOk ? "OK" : "FAILED")}" + (verificationOk ? "" : $" ({verificationError ?? "unknown"})"));

        if (proof.Grant is not null)
        {
            var p = proof.Grant.Payload;
            sb.AppendLine("- Grant:");
            sb.AppendLine($"  - GrantId: {p.GrantId}");
            sb.AppendLine($"  - PublisherId: {p.PublisherId}");
            sb.AppendLine($"  - CustomerOrgId: {p.CustomerOrgId}");
            sb.AppendLine($"  - ProductCode: {p.ProductCode}");
            sb.AppendLine($"  - Channel: {p.Channel}");
            sb.AppendLine($"  - SeatLimit: {p.SeatLimit}");
            sb.AppendLine($"  - ValidFromUtc: {p.ValidFromUtc:O}");
            sb.AppendLine($"  - ValidUntilUtc: {(p.ValidUntilUtc.HasValue ? p.ValidUntilUtc.Value.ToString("O") : "(null)")}");
            sb.AppendLine($"  - IssuedAtUtc: {p.IssuedAtUtc:O}");
            sb.AppendLine($"  - AudienceServerId: {p.AudienceServerId ?? "(null)"}");
            sb.AppendLine($"  - Kid: {p.Kid}");
            sb.AppendLine($"  - Signature: present (length {proof.Grant.SignatureBase64?.Length ?? 0})");
        }
        else
        {
            sb.AppendLine("- Grant: (null)");
        }

        if (proof.PublisherKey is not null)
        {
            var k = proof.PublisherKey;
            sb.AppendLine("- Publisher key:");
            sb.AppendLine($"  - PublisherId: {k.PublisherId}");
            sb.AppendLine($"  - Kid: {k.Kid}");
            sb.AppendLine($"  - Algorithm: {k.Algorithm}");
            sb.AppendLine($"  - CreatedAtUtc: {k.CreatedAtUtc:O}");
            sb.AppendLine($"  - RevokedAtUtc: {(k.RevokedAtUtc.HasValue ? k.RevokedAtUtc.Value.ToString("O") : "(null)")}");
            sb.AppendLine("  - PublicKeyPem: (redacted)");
        }
        else
        {
            sb.AppendLine("- Publisher key: (null)");
        }
    }
}
