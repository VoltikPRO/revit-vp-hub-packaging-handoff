namespace LicensingSystem.Contracts.Agent.Updates;

public sealed record UpdateManifestItemDto(
    string ProductCode,
    string Version,
    string Sha256Hex,
    string RelativePath,
    string FileName,
    string Channel,
    string? SignatureAlgorithm,
    string? SignatureBase64);

public sealed record UpdatesManifestResponse(string Channel, IReadOnlyList<UpdateManifestItemDto> Items);

public sealed record UpdateDownloadTokenRequest(string ProductCode, string Version);

public sealed record UpdateDownloadTokenResponse(string Token, DateTimeOffset ExpiresAtUtc);
