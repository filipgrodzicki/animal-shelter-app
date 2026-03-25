namespace ShelterApp.Domain.Users;

/// <summary>
/// Token service interface for JWT token generation and validation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the given user.
    /// </summary>
    Task<TokenResult> GenerateAccessTokenAsync(ApplicationUser user);

    /// <summary>
    /// Generates a refresh token string.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT access token and returns the user ID if valid.
    /// </summary>
    Task<Guid?> ValidateAccessTokenAsync(string token);

    /// <summary>
    /// Gets the expiration time for access tokens.
    /// </summary>
    TimeSpan AccessTokenExpiration { get; }

    /// <summary>
    /// Gets the expiration time for refresh tokens.
    /// </summary>
    TimeSpan RefreshTokenExpiration { get; }
}

/// <summary>
/// Result of token generation.
/// </summary>
public record TokenResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);

/// <summary>
/// Authentication result returned after successful login/register.
/// </summary>
public record AuthResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    IList<string> Roles,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);
