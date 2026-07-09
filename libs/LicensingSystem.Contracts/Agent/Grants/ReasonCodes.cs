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

    // Transitional aliases (backward compatibility)
    public const string OfflineExpiredAlias = "OFFLINE_EXPIRED";
    public const string SeatLimitReachedAlias = "SEAT_LIMIT_REACHED";

    /// <summary>
    /// Normalizes legacy aliases to canonical reason codes.
    /// Returns trimmed input for unknown codes so callers can preserve extensibility.
    /// </summary>
    public static string Normalize(string? reason)
    {
        var r = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        return r switch
        {
            OfflineExpiredAlias => OfflineGraceExpired,
            SeatLimitReachedAlias => LeaseExhausted,
            _ => r
        };
    }
}
