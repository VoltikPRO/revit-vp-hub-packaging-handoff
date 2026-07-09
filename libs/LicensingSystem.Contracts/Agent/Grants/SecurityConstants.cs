namespace LicensingSystem.Contracts.Agent.Grants;

/// <summary>
/// Canonical licensing security constants shared across implementations.
/// Keep values aligned with docs/architecture/security-constants.md.
/// </summary>
public static class SecurityConstants
{
    /// <summary>Allowed absolute skew between request and echoed proof timestamp.</summary>
    public const int ClockSkewSeconds = 120;

    /// <summary>Default TTL for future per-run signed proof artifacts.</summary>
    public const int RunProofTtlSeconds = 600;

    /// <summary>Maximum replay window for future replay-cache enforcement.</summary>
    public const int MaxReplayWindowSeconds = 1800;

    /// <summary>Nonce raw byte length (GUID).</summary>
    public const int NonceLengthBytes = 16;

    /// <summary>Nonce rendered hex length for GUID "N" format.</summary>
    public const int NonceLengthHexChars = 32;
}
