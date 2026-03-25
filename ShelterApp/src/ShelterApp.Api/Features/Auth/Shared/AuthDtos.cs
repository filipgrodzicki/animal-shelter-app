namespace ShelterApp.Api.Features.Auth.Shared;

/// <summary>
/// User information returned in auth responses.
/// </summary>
public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    IList<string> Roles
);

/// <summary>
/// Authentication response with tokens and user info.
/// </summary>
public record AuthResponse(
    UserDto User,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);

/// <summary>
/// Token refresh response.
/// </summary>
public record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);

/// <summary>
/// Simple success response.
/// </summary>
public record SuccessResponse(string Message);
