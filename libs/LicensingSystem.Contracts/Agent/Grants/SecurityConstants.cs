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

    /// <summary>
    /// Hard cap enforced by <c>RevitLicenseProofVerifier</c> on any grant signed by a pinned
    /// *trial* key (see <c>RevitLicensePinning.ExpectedTrialKid</c>), regardless of what the
    /// grant payload itself claims. Bounds the blast radius of a compromised trial key to a
    /// small number of free seats rather than an arbitrary license.
    /// </summary>
    public const int TrialGrantMaxSeatLimit = 5;

    /// <summary>
    /// Hard cap on (ValidUntilUtc - ValidFromUtc) for any grant signed by a pinned trial key.
    /// Same rationale as <see cref="TrialGrantMaxSeatLimit"/>: bounds a compromised trial key to
    /// short-lived trials, not perpetual access.
    /// </summary>
    public const int TrialGrantMaxValidityDays = 14;
}
