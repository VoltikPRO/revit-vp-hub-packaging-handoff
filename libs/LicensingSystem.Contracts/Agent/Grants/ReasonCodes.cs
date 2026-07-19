namespace LicensingSystem.Contracts.Agent.Grants;

/// <summary>
/// Canonical licensing reason codes.
/// Keep these values aligned with docs/architecture/reason-codes.md.
/// </summary>
public static class ReasonCodes
{
    // Canonical codes (current target)
    public const string Ok = "OK";
    public const string NotEntitled = "NOT_ENTITLED";
    public const string OfflineGraceExpired = "OFFLINE_GRACE_EXPIRED";
    public const string LeaseExhausted = "LEASE_EXHAUSTED";
    public const string AgentUnavailable = "AGENT_UNAVAILABLE";
    public const string SignatureInvalid = "SIGNATURE_INVALID";
    public const string ProofMissing = "PROOF_MISSING";
    public const string DeviceCertInvalid = "DEVICE_CERT_INVALID";
    public const string RunProofInvalid = "RUN_PROOF_INVALID";
    public const string ReplayDetected = "REPLAY_DETECTED";
    public const string TimestampOutOfRange = "TIMESTAMP_OUT_OF_RANGE";

    // Transitional aliases (backward compatibility)
    public const string OfflineExpiredAlias = "OFFLINE_EXPIRED";
    public const string SeatLimitReachedAlias = "SEAT_LIMIT_REACHED";

    /// <summary>All canonical codes in stable display order (includes OK).</summary>
    public static readonly IReadOnlyList<string> AllCanonical =
    [
        Ok,
        NotEntitled,
        OfflineGraceExpired,
        LeaseExhausted,
        AgentUnavailable,
        SignatureInvalid,
        ProofMissing,
        DeviceCertInvalid,
        RunProofInvalid,
        ReplayDetected,
        TimestampOutOfRange,
    ];

    /// <summary>
    /// Normalizes legacy aliases to canonical reason codes.
    /// Returns trimmed input for unknown codes so callers can preserve extensibility.
    /// </summary>
    public static string Normalize(string? reason)
    {
        var r = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        if (r.Length == 0)
            return r;

        var upper = r.ToUpperInvariant();
        if (upper == OfflineExpiredAlias)
            return OfflineGraceExpired;
        if (upper == SeatLimitReachedAlias)
            return LeaseExhausted;

        foreach (var canonical in AllCanonical)
        {
            if (string.Equals(canonical, r, StringComparison.OrdinalIgnoreCase))
                return canonical;
        }

        return r;
    }
}
