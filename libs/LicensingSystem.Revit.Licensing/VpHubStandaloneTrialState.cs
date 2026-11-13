using System.IO;
using LicensingSystem.Agent.Ipc;

namespace LicensingSystem.Revit.Licensing;

/// <summary>
/// Local per-machine state for standalone (offline, no-agent) trials — see
/// docs/architecture/canrun-contract-versioning.md §1c. Two independent, additive restrictions
/// on top of <see cref="RevitLicenseProofVerifier.TryVerifyStandaloneTrialGrant"/>, which only
/// checks what the signed grant itself can prove:
///
/// 1. <b>Permanent disablement once the agent has ever licensed this product here.</b> This is
///    the actual security boundary: it is what stops "kill the agent, fall back to a fresh free
///    trial" from being a real bypass. A paying customer whose agent is briefly unreachable still
///    gets denied, exactly as before this existed — the agent having succeeded once is permanent
///    for this machine.
/// 2. <b>First-use trial window.</b> An embedded trial grant's own signed validity window may be
///    wider than the intended per-user trial length (see the CI re-signing design in the
///    architecture doc) — this narrows it to "N days since *this machine* first used it," purely
///    as a UX improvement. This is <i>not</i> a security control: local state can be cleared by a
///    determined user to reset the clock. The signed grant's own expiry is what still applies
///    regardless — this can only make the effective trial shorter, never longer.
///
/// Never throws; a failure to read or write this state fails closed for restriction 1 (treated as
/// "already used/disabled" — see <see cref="IsStandaloneTrialPermanentlyDisabled"/>) and fails
/// open for restriction 2 only in the sense that a lost first-use timestamp starts a fresh local
/// window, bounded by whatever the signed grant's own expiry already allows.
/// </summary>
public static class VpHubStandaloneTrialState
{
    private static readonly object Gate = new();

    public static string GetStateDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VP-Hub",
            "trial-state");

    public static string GetStateFilePath(string productCode) =>
        Path.Combine(GetStateDirectory(), VpHubPluginFileLog.SanitizeProductCodeForFileName(productCode) + ".json");

    /// <summary>
    /// Call after every AGENT-path license success (<see cref="RevitLicenseProofVerifier.TryVerifyProof"/>
    /// returning true) for this product. Idempotent; the first-success timestamp is sticky.
    /// </summary>
    public static void RecordAgentGrantSuccess(string productCode)
    {
        try
        {
            lock (Gate)
            {
                var state = ReadState(productCode);
                if (state.AgentGrantEverSucceeded)
                    return;

                state.AgentGrantEverSucceeded = true;
                state.AgentGrantFirstSucceededUtc = DateTimeOffset.UtcNow;
                WriteState(productCode, state);
            }
        }
        catch
        {
            // Never break Revit because of local trial-state bookkeeping. Worst case here is a
            // missed permanent-disablement write; see the deny-by-default framing in
            // IsStandaloneTrialPermanentlyDisabled for why this does not weaken the boundary in
            // the common case (the write happens on every agent success, so a single missed write
            // is very unlikely to matter, and the file being unreadable at all is treated as
            // disabled below, not as "never used").
        }
    }

    /// <summary>
    /// True if the agent has ever successfully licensed this product on this machine — in which
    /// case standalone trial must be refused unconditionally, before ever attempting to verify an
    /// embedded grant at all. Fails closed: if the state file cannot be read (corrupt, permissions,
    /// unexpected content), this returns true rather than false, since "cannot prove the agent
    /// never succeeded" must not be treated the same as "confirmed it never did".
    /// </summary>
    public static bool IsStandaloneTrialPermanentlyDisabled(string productCode)
    {
        try
        {
            var path = GetStateFilePath(productCode);
            if (!File.Exists(path))
                return false;

            return ReadState(productCode).AgentGrantEverSucceeded;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Records this machine's first standalone-trial use for <paramref name="productCode"/> if
    /// none is recorded yet, then reports whether "now" still falls within <paramref name="maxDays"/>
    /// of that first use. Call only after <see cref="RevitLicenseProofVerifier.TryVerifyStandaloneTrialGrant"/>
    /// has already succeeded and <see cref="IsStandaloneTrialPermanentlyDisabled"/> is false — this
    /// narrows further, it never grants anything by itself.
    /// </summary>
    public static bool TryContinueTrialWindow(string productCode, int maxDays, out DateTimeOffset firstUseUtc)
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            lock (Gate)
            {
                var state = ReadState(productCode);
                if (state.TrialFirstUseUtc is not { } existing)
                {
                    state.TrialFirstUseUtc = now;
                    WriteState(productCode, state);
                    firstUseUtc = now;
                    return true;
                }

                firstUseUtc = existing;
                return (now - existing) <= TimeSpan.FromDays(maxDays);
            }
        }
        catch
        {
            // Lost/unreadable local state: start a fresh window rather than fail closed here —
            // restriction 2 is a UX narrowing, not the security boundary (that is restriction 1
            // above, which does fail closed). The signed grant's own ValidUntilUtc still applies
            // regardless of what happens in this method.
            firstUseUtc = now;
            return true;
        }
    }

    /// <summary>
    /// Read-only peek at the local first-use timestamp — unlike <see cref="TryContinueTrialWindow"/>,
    /// never writes state, so it is safe to call from UI code (e.g. an About/status window) without
    /// starting the trial clock just by displaying it. Returns <c>true</c> with a <c>null</c>
    /// <paramref name="firstUseUtc"/> when no standalone trial has been used here yet (not an error);
    /// returns <c>false</c> only if the state file exists but could not be read.
    /// </summary>
    public static bool TryGetTrialFirstUseUtc(string productCode, out DateTimeOffset? firstUseUtc)
    {
        try
        {
            var path = GetStateFilePath(productCode);
            if (!File.Exists(path))
            {
                firstUseUtc = null;
                return true;
            }

            firstUseUtc = ReadState(productCode).TrialFirstUseUtc;
            return true;
        }
        catch
        {
            firstUseUtc = null;
            return false;
        }
    }

    private sealed class TrialState
    {
        public bool AgentGrantEverSucceeded { get; set; }
        public DateTimeOffset? AgentGrantFirstSucceededUtc { get; set; }
        public DateTimeOffset? TrialFirstUseUtc { get; set; }
    }

    private static TrialState ReadState(string productCode)
    {
        var path = GetStateFilePath(productCode);
        if (!File.Exists(path))
            return new TrialState();

        var json = File.ReadAllText(path);
        // IpcJsonCodec: STJ first, Newtonsoft fallback on AppDomain STJ/Encodings conflicts
        // (Revit 2023-2024 shared AppDomain — another add-in's incompatible STJ can otherwise
        // throw MissingMethodException even for a purely-local file read like this one).
        return IpcJsonCodec.Deserialize<TrialState>(json) ?? new TrialState();
    }

    private static void WriteState(string productCode, TrialState state)
    {
        var path = GetStateFilePath(productCode);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, IpcJsonCodec.Serialize(state));
    }
}
