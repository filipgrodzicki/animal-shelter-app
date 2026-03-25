using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Infrastructure.Persistence;
using Xunit;

namespace ShelterApp.Tests.Integration;

/// <summary>
/// Base class for integration tests
/// </summary>
[Collection("PostgreSql")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlFixture PostgreSqlFixture;
    protected ShelterWebApplicationFactory Factory = null!;
    protected HttpClient Client = null!;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected IntegrationTestBase(PostgreSqlFixture fixture)
    {
        PostgreSqlFixture = fixture;
    }

    public async Task InitializeAsync()
    {
        Factory = new ShelterWebApplicationFactory(PostgreSqlFixture.ConnectionString);
        await Factory.InitializeDatabaseAsync();
        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
    }

    #region Authentication Helpers

    protected async Task<AuthTokens> RegisterUserAsync(
        string email = "user@test.com",
        string password = "Test123!",
        string firstName = "Test",
        string lastName = "User")
    {
        var request = new
        {
            email,
            password,
            confirmPassword = password,
            firstName,
            lastName,
            phoneNumber = "+48123456789"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return new AuthTokens(result!.AccessToken, result.RefreshToken);
    }

    protected async Task<AuthTokens> LoginAsync(string email, string password)
    {
        var request = new { email, password };
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return new AuthTokens(result!.AccessToken, result.RefreshToken);
    }

    protected async Task<AuthTokens> RegisterStaffUserAsync(string email = "staff@test.com")
    {
        var tokens = await RegisterUserAsync(email, "Staff123!", "Staff", "Member");

        // Add Staff role directly via database
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ShelterApp.Domain.Users.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        await userManager.AddToRoleAsync(user!, ShelterApp.Domain.Users.AppRoles.Staff);

        // Re-login to get new token with role
        return await LoginAsync(email, "Staff123!");
    }

    protected async Task<AuthTokens> RegisterAdminUserAsync(string email = "admin@test.com")
    {
        var tokens = await RegisterUserAsync(email, "Admin123!", "Admin", "User");

        // Add Admin role directly via database
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ShelterApp.Domain.Users.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        await userManager.AddToRoleAsync(user!, ShelterApp.Domain.Users.AppRoles.Admin);

        // Re-login to get new token with role
        return await LoginAsync(email, "Admin123!");
    }

    protected void SetAuthToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthToken()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    #endregion

    #region Animal Helpers

    protected async Task<Guid> CreateTestAnimalAsync(string name = "Burek", bool makeAvailable = true)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

        var animal = Animal.Create(
            registrationNumber: $"SCH/DOG/{DateTime.UtcNow:yyyy}/{Guid.NewGuid().ToString()[..4].ToUpper()}",
            species: Species.Dog,
            breed: "Labrador",
            name: name,
            gender: Gender.Male,
            size: Size.Large,
            color: "Czarny",
            admissionCircumstances: "Znaleziony na ulicy",
            ageInMonths: 24,
            chipNumber: Guid.NewGuid().ToString()[..15],
            description: "Przyjazny pies do adopcji"
        );

        if (makeAvailable)
        {
            animal.ChangeStatus(
                AnimalStatusTrigger.DopuszczenieDoAdopcji,
                "system");
        }

        dbContext.Animals.Add(animal);
        await dbContext.SaveChangesAsync();

        return animal.Id;
    }

    protected async Task<Animal?> GetAnimalAsync(Guid animalId)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        return await dbContext.Animals.FindAsync(animalId);
    }

    #endregion

    #region Visit Slot Helpers

    protected async Task CreateVisitSlotsAsync(DateTime date, int count = 5)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

        var dateOnly = DateOnly.FromDateTime(date);
        for (int i = 0; i < count; i++)
        {
            var startTime = new TimeOnly(10 + i, 0);
            var endTime = startTime.AddMinutes(30);
            var slotResult = ShelterApp.Domain.Appointments.VisitSlot.Create(
                dateOnly,
                startTime,
                endTime,
                maxCapacity: 2
            );
            if (slotResult.IsSuccess)
            {
                dbContext.VisitSlots.Add(slotResult.Value);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    #endregion

    #region Response Types

    protected record AuthResponse(
        UserDto User,
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt);

    protected record UserDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string? AvatarUrl,
        IList<string> Roles);

    protected record AuthTokens(string AccessToken, string RefreshToken);

    protected record AdoptionApplicationResponse(
        Guid Id,
        string ApplicationNumber,
        string Status,
        AnimalDto Animal);

    protected record AnimalDto(
        Guid Id,
        string Name,
        string Status);

    protected record SuccessResponse(string Message);

    protected record ProblemDetails(
        string Type,
        string Title,
        int Status,
        string? Detail);

    #endregion
}
