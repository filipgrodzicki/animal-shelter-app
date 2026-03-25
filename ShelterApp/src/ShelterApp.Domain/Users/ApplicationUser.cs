using Microsoft.AspNetCore.Identity;

namespace ShelterApp.Domain.Users;

/// <summary>
/// Application user extending ASP.NET Core Identity user with additional profile data.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime? DateOfBirth { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    // Parameterless constructor for EF Core
    private ApplicationUser() { }

    public static ApplicationUser Create(
        string email,
        string firstName,
        string lastName,
        string? phoneNumber = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        return user;
    }

    public string FullName => $"{FirstName} {LastName}";

    public void UpdateProfile(
        string firstName,
        string lastName,
        string? phoneNumber,
        DateTime? dateOfBirth,
        string? address,
        string? city,
        string? postalCode)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        DateOfBirth = dateOfBirth;
        Address = address;
        City = city;
        PostalCode = postalCode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt, string? ipAddress = null)
    {
        var refreshToken = RefreshToken.Create(Id, token, expiresAt, ipAddress);
        RefreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeAllRefreshTokens(string? reason = null)
    {
        foreach (var token in RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoke(reason);
        }
    }

    /// <summary>
    /// Anonimizuje dane użytkownika zgodnie z RODO (prawo do bycia zapomnianym).
    /// Zachowuje rekord w bazie dla integralności relacji, ale usuwa dane osobowe.
    /// </summary>
    public void Anonymize()
    {
        var anonymousId = Id.ToString()[..8];

        Email = $"deleted_{anonymousId}@anonymous.local";
        NormalizedEmail = Email.ToUpperInvariant();
        UserName = Email;
        NormalizedUserName = Email.ToUpperInvariant();
        FirstName = "Użytkownik";
        LastName = "Usunięty";
        PhoneNumber = null;
        DateOfBirth = null;
        Address = null;
        City = null;
        PostalCode = null;
        AvatarUrl = null;
        PasswordHash = null;
        SecurityStamp = Guid.NewGuid().ToString();
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RevokeAllRefreshTokens("Konto zostało usunięte (RODO)");
    }
}
