namespace LicensingSystem.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User);

public sealed record UserDto(
    Guid Id,
    string Email,
    Guid? OrgId,
    bool IsSuperAdmin,
    bool MustChangePassword = false,
    /// <summary>When <see cref="OrgId"/> is set: <c>Member</c> or <c>OrgAdmin</c>.</summary>
    string? OrgRole = null);

