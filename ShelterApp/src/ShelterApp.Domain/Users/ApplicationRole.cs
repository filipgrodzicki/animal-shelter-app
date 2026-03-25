using Microsoft.AspNetCore.Identity;

namespace ShelterApp.Domain.Users;

/// <summary>
/// Application role extending ASP.NET Core Identity role.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationRole() : base()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
        CreatedAt = DateTime.UtcNow;
    }

    public ApplicationRole(string roleName, string description) : base(roleName)
    {
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Predefined roles for the shelter application.
/// </summary>
public static class AppRoles
{
    public const string User = "User";
    public const string Volunteer = "Volunteer";
    public const string Staff = "Staff";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = new[]
    {
        User,
        Volunteer,
        Staff,
        Admin
    };

    public static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        { User, "Zarejestrowany użytkownik aplikacji" },
        { Volunteer, "Wolontariusz z ograniczonymi uprawnieniami do pomocy w schronisku" },
        { Staff, "Pracownik schroniska z uprawnieniami do zarządzania zwierzętami i adopcjami" },
        { Admin, "Administrator systemu z pełnymi uprawnieniami" }
    };
}
