using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LicensingSystem.Contracts.Agent.Grants;

namespace LicensingSystem.Revit.Licensing;

/// <summary>Publisher-signed <see cref="CanRunProofDto"/> verification (never trust <c>Allowed</c> alone).</summary>
public static class RevitLicenseProofVerifier
{
    /// <summary>
    /// Normalizes PEM formatting so pasted keys from admin / DB (\r\n, leading indentation) still match pinned constants.
    /// </summary>
    private static string NormalizePemWhitespace(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
            return string.Empty;

        var oneNl = pem.Replace("\r\n", "\n").Replace("\r", "\n");
        var sb = new StringBuilder(oneNl.Length);
        foreach (var segment in oneNl.Split('\n'))
        {
            var line = segment.Trim();
            if (line.Length == 0)
                continue;

            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(line);
        }

        return sb.ToString();
    }

    public static bool TryVerifyProof(
        CanRunProofDto proof,
        RevitLicensePinning pinning,
        string expectedNonce,
        DateTimeOffset expectedTimestampUtc,
        out string? error) =>
        TryVerifyProof(proof, pinning, expectedNonce, expectedTimestampUtc, out error, out _);

    /// <param name="reasonCode">Canonical <see cref="ReasonCodes"/> value when verification fails; null on success.</param>
    public static bool TryVerifyProof(
        CanRunProofDto proof,
        RevitLicensePinning pinning,
        string expectedNonce,
        DateTimeOffset expectedTimestampUtc,
        out string? error,
        out string? reasonCode)
    {
        error = null;
        reasonCode = null;

        if (!string.Equals(proof.ProductCode, pinning.ProductCode, StringComparison.OrdinalIgnoreCase))
        {
            error = "ProductCode mismatch.";
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        if (!string.Equals(proof.RequestNonce, expectedNonce, StringComparison.Ordinal))
        {
            error = "Nonce mismatch (possible replay or wrong response).";
            reasonCode = ReasonCodes.ReplayDetected;
            return false;
        }

        var skew = (proof.RequestTimestampUtc - expectedTimestampUtc).Duration();
        if (skew > TimeSpan.FromSeconds(SecurityConstants.ClockSkewSeconds))
        {
            error = $"Timestamp skew too large: {skew.TotalSeconds:F0}s.";
            reasonCode = ReasonCodes.TimestampOutOfRange;
            return false;
        }

        if (proof.Grant is null || proof.PublisherKey is null)
        {
            error = "Missing grant or publisher key in response.";
            reasonCode = ReasonCodes.ProofMissing;
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (proof.PublisherKey.RevokedAtUtc is { } revokedAt && revokedAt <= now)
        {
            error = "Publisher key is revoked.";
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        var payload = proof.Grant.Payload;

        // Explicit check ahead of the crypto path: an unrecognized schema version would also
        // fail signature verification (LicenseGrantPayloadCanonical throws for unknown versions),
        // but a dedicated reason code here gives support a clear "update the plugin/agent" signal
        // instead of a generic "signature invalid".
        if (!LicenseGrantPayloadCanonical.KnownSchemaVersions.Contains(payload.SchemaVersion))
        {
            error = $"Unsupported grant schema version {payload.SchemaVersion}.";
            reasonCode = ReasonCodes.UnsupportedSchemaVersion;
            return false;
        }

        if (payload.ValidFromUtc > now)
        {
            error = "Grant not yet valid.";
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        if (payload.ValidUntilUtc is { } until && until <= now)
        {
            error = "Grant expired.";
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        if (!TryVerifyPublisherPinned(proof.PublisherKey, payload, pinning, out error, out var isTrialKey))
        {
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        if (!TryVerifySignedGrantSignature(proof.Grant, pinning.ProductCode, pinning, isTrialKey, out error))
        {
            reasonCode = ReasonCodes.SignatureInvalid;
            return false;
        }

        // Hard caps on anything signed by the trial key, enforced here regardless of what the
        // payload itself claims — this is what bounds a compromised trial key to a small number
        // of short-lived free seats instead of an arbitrary license. Checked after signature
        // verification so these fields are known-authentic (actually signed), not attacker-supplied.
        if (isTrialKey && !TryVerifyTrialGrantConstraints(payload, out error))
        {
            reasonCode = ReasonCodes.TrialGrantConstraintViolated;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Verifies a standalone (offline, no-agent) trial grant embedded with the plugin package —
    /// see docs/architecture/canrun-contract-versioning.md §1c. There is no live IPC round-trip
    /// for an embedded artifact, so nonce/timestamp freshness (which exists to catch a replayed
    /// or stale *response*) does not apply here the way it does in <see cref="TryVerifyProof"/>;
    /// what replaces it is that this path only ever accepts a grant signed by the pinned *trial*
    /// key. A grant signed by the production key is refused here even if otherwise perfectly
    /// valid — production licensing must always go through the agent, never this path. Callers
    /// are still responsible for the two checks that cannot live in a pure verification function:
    /// "has the agent ever successfully licensed this product on this machine" (if so, standalone
    /// trial must be permanently refused, before ever calling this) and the local first-use
    /// trial-window tracking (see the caller-side state helper referenced in the architecture
    /// doc) — this method only checks what the signed bytes themselves can prove.
    /// </summary>
    public static bool TryVerifyStandaloneTrialGrant(
        SignedLicenseGrantDto grant,
        PublisherPublicKeyDto publisherKey,
        RevitLicensePinning pinning,
        out string? error,
        out string? reasonCode)
    {
        error = null;
        reasonCode = null;

        var now = DateTimeOffset.UtcNow;
        if (publisherKey.RevokedAtUtc is { } revokedAt && revokedAt <= now)
        {
            error = "Publisher key is revoked.";
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        var payload = grant.Payload;

        if (!LicenseGrantPayloadCanonical.KnownSchemaVersions.Contains(payload.SchemaVersion))
        {
            error = $"Unsupported grant schema version {payload.SchemaVersion}.";
            reasonCode = ReasonCodes.UnsupportedSchemaVersion;
            return false;
        }

        if (payload.ValidFromUtc > now)
        {
            error = "Grant not yet valid.";
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        if (payload.ValidUntilUtc is { } until && until <= now)
        {
            error = "Grant expired.";
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        if (!TryVerifyPublisherPinned(publisherKey, payload, pinning, out error, out var isTrialKey))
        {
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        if (!isTrialKey)
        {
            error = "Standalone trial grants must be signed by the pinned trial key, not the production key.";
            reasonCode = ReasonCodes.StandaloneTrialUnavailable;
            return false;
        }

        if (!TryVerifySignedGrantSignature(grant, pinning.ProductCode, pinning, isTrialKey: true, out error))
        {
            reasonCode = ReasonCodes.SignatureInvalid;
            return false;
        }

        if (!TryVerifyTrialGrantConstraints(payload, out error))
        {
            reasonCode = ReasonCodes.TrialGrantConstraintViolated;
            return false;
        }

        return true;
    }

    private static bool TryVerifyTrialGrantConstraints(LicenseGrantPayloadDto payload, out string? error)
    {
        error = null;

        if (payload.SeatLimit > SecurityConstants.TrialGrantMaxSeatLimit)
        {
            error = $"Trial grant seat limit {payload.SeatLimit} exceeds the maximum of {SecurityConstants.TrialGrantMaxSeatLimit}.";
            return false;
        }

        if (payload.ValidUntilUtc is not { } until)
        {
            error = "Trial grant must have a validUntilUtc (open-ended trials are not allowed).";
            return false;
        }

        var validityDays = (until - payload.ValidFromUtc).TotalDays;
        if (validityDays > SecurityConstants.TrialGrantMaxValidityDays)
        {
            error = $"Trial grant validity of {validityDays:F1} days exceeds the maximum of {SecurityConstants.TrialGrantMaxValidityDays} days.";
            return false;
        }

        return true;
    }

    /// <param name="isTrialKey">True when the response's key matched the pinned *trial* key rather than the production key.</param>
    private static bool TryVerifyPublisherPinned(
        PublisherPublicKeyDto key,
        LicenseGrantPayloadDto payload,
        RevitLicensePinning pinning,
        out string? error,
        out bool isTrialKey)
    {
        error = null;
        isTrialKey = false;

        if (!Guid.TryParse(pinning.ExpectedPublisherId, out var expectedPublisherId) || expectedPublisherId == Guid.Empty
            || string.IsNullOrWhiteSpace(pinning.ExpectedKid)
            || string.IsNullOrWhiteSpace(pinning.ExpectedPublisherPublicKeyPem))
        {
            error = "Publisher key not pinned (ExpectedPublisherId / ExpectedKid / PEM).";
            return false;
        }

        if (key.PublisherId != expectedPublisherId)
        {
            error = "PublisherId not trusted.";
            return false;
        }

        // A response key must match exactly one pinned kid — production, or trial if one is
        // pinned. Matching neither is untrusted; the two pinned kids are guaranteed distinct by
        // RevitLicensePinning's constructor, so this is never ambiguous.
        var responsePem = NormalizePemWhitespace(key.PublicKeyPem);
        string pinnedPem;
        if (string.Equals(key.Kid, pinning.ExpectedKid, StringComparison.Ordinal))
        {
            pinnedPem = NormalizePemWhitespace(pinning.ExpectedPublisherPublicKeyPem);
            isTrialKey = false;
        }
        else if (pinning.ExpectedTrialKid is { } trialKid && string.Equals(key.Kid, trialKid, StringComparison.Ordinal))
        {
            pinnedPem = NormalizePemWhitespace(pinning.ExpectedTrialPublisherPublicKeyPem!);
            isTrialKey = true;
        }
        else
        {
            error = "Kid not trusted.";
            return false;
        }

        if (!string.Equals(responsePem, pinnedPem, StringComparison.Ordinal))
        {
            error = "Publisher public key does not match pinned key.";
            return false;
        }

        if (!string.Equals(key.Algorithm, "es256", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Unsupported algorithm '{key.Algorithm}'.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(pinning.ExpectedAudienceServerId))
        {
            if (!string.Equals(payload.AudienceServerId ?? "", pinning.ExpectedAudienceServerId, StringComparison.Ordinal))
            {
                error = "AudienceServerId mismatch.";
                return false;
            }
        }

        return true;
    }

    private static bool TryVerifySignedGrantSignature(
        SignedLicenseGrantDto signed,
        string expectedProductCode,
        RevitLicensePinning pinning,
        bool isTrialKey,
        out string? error)
    {
        error = null;
        try
        {
            byte[] sig;
            try
            {
                sig = Convert.FromBase64String(signed.SignatureBase64);
            }
            catch
            {
                error = "Invalid signatureBase64.";
                return false;
            }

            var data = LicenseGrantPayloadCanonical.ToUtf8Bytes(signed.Payload);
            using var ecdsa = ECDsa.Create();
            // PEM already matched pinned text — use embedded P-256 point (avoids PEM APIs on net48).
            var keyParameters = isTrialKey
                ? pinning.TrialP256PublicKeyParameters
                    ?? throw new InvalidOperationException("Trial key matched but no trial ECParameters pinned.")
                : pinning.P256PublicKeyParameters;
            ecdsa.ImportParameters(keyParameters);
            // Match worker-api grantVerify: WebCrypto ES256 is raw R||S (64 bytes); many .NET tools emit ASN.1 DER.
            if (!TryVerifyEs256Signature(ecdsa, data, sig, out var verifyError))
            {
                error = verifyError ?? "Grant signature invalid.";
                return false;
            }
        }
        catch (Exception ex)
        {
            error = $"Signature verification error: {ex.Message}";
            return false;
        }

        if (!string.Equals(signed.Payload.ProductCode, expectedProductCode, StringComparison.OrdinalIgnoreCase))
        {
            error = "Grant productCode mismatch.";
            return false;
        }

        return true;
    }

    private static bool TryVerifyEs256Signature(ECDsa ecdsa, byte[] data, byte[] sig, out string? error)
    {
        error = null;
#if NETFRAMEWORK
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(data);
            if (sig.Length == 64)
            {
                var der = Es256DerEncoding.IeeeP1363ToDer(sig);
                if (ecdsa.VerifyHash(hash, der))
                    return true;
            }

            if (ecdsa.VerifyHash(hash, sig))
                return true;
        }
#else
        if (sig.Length == 64 &&
            ecdsa.VerifyData(data, sig, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            return true;

        if (ecdsa.VerifyData(data, sig, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
            return true;
#endif

        error = "Grant signature invalid (expected ES256: raw 64-byte R||S or ASN.1 DER).";
        return false;
    }
}
