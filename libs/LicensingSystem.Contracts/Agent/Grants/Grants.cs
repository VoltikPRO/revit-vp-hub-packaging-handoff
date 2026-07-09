namespace LicensingSystem.Contracts.Agent.Grants;

/// <summary>
/// Public key used to verify publisher-signed license grants.
/// </summary>
public sealed record PublisherPublicKeyDto(
    Guid PublisherId,
    string Kid,
    string Algorithm,
    string PublicKeyPem,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? RevokedAtUtc);

/// <summary>
/// Canonical payload that a publisher signs. Signature is stored separately.
/// </summary>
public sealed record LicenseGrantPayloadDto(
    Guid GrantId,
    Guid PublisherId,
    Guid CustomerOrgId,
    string ProductCode,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidUntilUtc,
    int SeatLimit,
    string Channel,
    string? AudienceServerId,
    DateTimeOffset IssuedAtUtc,
    string Kid);

/// <summary>
/// Signed license grant. Plugins should validate <see cref="SignatureBase64"/> over <see cref="Payload"/>.
/// </summary>
public sealed record SignedLicenseGrantDto(
    LicenseGrantPayloadDto Payload,
    string SignatureBase64);

/// <summary>
/// Response returned to plugins for a canRun check. Plugins must verify the publisher signature on the grant,
/// validate tenant binding, and check freshness (nonce/timestamp echo).
/// </summary>
public sealed record CanRunProofDto(
    string RequestNonce,
    DateTimeOffset RequestTimestampUtc,
    string ProductCode,
    string? PluginVersion,
    bool Allowed,
    string Reason,
    SignedLicenseGrantDto? Grant,
    PublisherPublicKeyDto? PublisherKey);

public sealed record GrantsBundleResponse(
    Guid CustomerOrgId,
    string? AudienceServerId,
    IReadOnlyList<SignedLicenseGrantDto> Grants,
    IReadOnlyList<PublisherPublicKeyDto> PublisherKeys,
    DateTimeOffset ServerTime);

