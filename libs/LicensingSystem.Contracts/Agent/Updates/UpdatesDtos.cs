namespace LicensingSystem.Contracts.Agent.Updates;

public sealed record UpdateManifestItemDto(
    string ProductCode,
    string Version,
    string Sha256Hex,
    string RelativePath,
    string FileName,
    string Channel,
    string? SignatureAlgorithm,
    string? SignatureBase64,
    /// <summary><c>all</c> (default) or <c>advanced</c> — advanced-only packages for Developer mode.</summary>
    string? Availability = null);

public sealed record UpdatesManifestResponse(string Channel, IReadOnlyList<UpdateManifestItemDto> Items);

public sealed record UpdateDownloadTokenRequest(string ProductCode, string Version, bool IncludeAdvanced = false);

public sealed record UpdateDownloadTokenResponse(string Token, DateTimeOffset ExpiresAtUtc);
