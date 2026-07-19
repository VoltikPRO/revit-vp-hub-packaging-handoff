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

        if (!TryVerifyPublisherPinned(proof.PublisherKey, payload, pinning, out error))
        {
            reasonCode = ReasonCodes.RunProofInvalid;
            return false;
        }

        if (!TryVerifySignedGrantSignature(proof.Grant, pinning.ProductCode, pinning, out error))
        {
            reasonCode = ReasonCodes.SignatureInvalid;
            return false;
        }

        return true;
    }

    private static bool TryVerifyPublisherPinned(
        PublisherPublicKeyDto key,
        LicenseGrantPayloadDto payload,
        RevitLicensePinning pinning,
        out string? error)
    {
        error = null;

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

        if (!string.Equals(key.Kid, pinning.ExpectedKid, StringComparison.Ordinal))
        {
            error = "Kid not trusted.";
            return false;
        }

        var responsePem = NormalizePemWhitespace(key.PublicKeyPem);
        var pinnedPem = NormalizePemWhitespace(pinning.ExpectedPublisherPublicKeyPem);
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
            ecdsa.ImportParameters(pinning.P256PublicKeyParameters);
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
