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
/// <param name="SchemaVersion">
/// Canonical wire-format version — see <see cref="LicenseGrantPayloadCanonical"/> and
/// docs/architecture/canrun-contract-versioning.md.
///
/// The default here is deliberately the fixed literal <c>1</c>, never "current version": this
/// default is what <c>System.Text.Json</c> substitutes when deserializing a JSON payload that
/// has no <c>schemaVersion</c> property at all — i.e. every grant issued before this field
/// existed. Binding it to "current" instead would misclassify old, already-signed v1 grants as
/// whatever the newest version becomes, and their signatures would stop verifying. Code that
/// mints a *new* grant should pass <see cref="LicenseGrantPayloadCanonical.CurrentSchemaVersion"/>
/// explicitly rather than rely on this default.
/// </param>
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
    string Kid,
    int SchemaVersion = 1);

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

