namespace LicensingSystem.Contracts.Auth;

public sealed record RefreshRequest(string RefreshToken);

public sealed record RefreshResponse(string AccessToken, string RefreshToken);

