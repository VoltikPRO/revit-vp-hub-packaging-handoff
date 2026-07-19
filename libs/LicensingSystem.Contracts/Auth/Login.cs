namespace LicensingSystem.Contracts.Auth;

/// <param name="Intent">
/// Optional login surface: <c>agent</c>, <c>admin</c>, or <c>portal</c>.
/// Drives session audience and JWT <c>aud</c> on the Worker. Omitted/unknown → agent.
/// </param>
public sealed record LoginRequest(string Email, string Password, string? Intent = null);

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
    bool MustAcceptTerms = false,
    /// <summary>When <see cref="OrgId"/> is set: <c>Member</c> or <c>OrgAdmin</c>.</summary>
    string? OrgRole = null);

