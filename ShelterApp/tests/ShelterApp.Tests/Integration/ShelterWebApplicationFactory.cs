using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;

namespace ShelterApp.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class ShelterWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ShelterWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration for JWT and other settings
            var testConfiguration = new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "TestSecretKey123456789012345678901234567890",
                ["Jwt:Issuer"] = "ShelterApp.Tests",
                ["Jwt:Audience"] = "ShelterApp.Tests",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Shelter:Name"] = "Test Shelter",
                ["Shelter:Address"] = "Test Address",
                ["Shelter:Phone"] = "+48123456789",
                ["Shelter:Email"] = "test@shelter.com",
                ["Shelter:Website"] = "https://test.shelter.com",
                ["Email:EnableSending"] = "false",
                ["Email:Provider"] = "Smtp",
                ["Email:FromEmail"] = "test@shelter.com",
                ["Email:FromName"] = "Test Shelter"
            };

            config.AddInMemoryCollection(testConfiguration);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<ShelterDbContext>>();
            services.RemoveAll<ShelterDbContext>();

            // Add DbContext with test database (no retry strategy for tests to support user-initiated transactions)
            services.AddDbContext<ShelterDbContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });

            // Disable background services for testing
            services.RemoveAll<IHostedService>();

            // Configure email to not send actual emails
            services.Configure<EmailSettings>(options =>
            {
                options.EnableSending = false;
            });

            // Override JWT settings - this is needed because AddAuth captures config at startup
            services.Configure<JwtSettings>(options =>
            {
                options.Secret = "TestSecretKey123456789012345678901234567890";
                options.Issuer = "ShelterApp.Tests";
                options.Audience = "ShelterApp.Tests";
                options.AccessTokenExpirationMinutes = 60;
                options.RefreshTokenExpirationDays = 7;
            });

            // Reconfigure JWT Bearer authentication with test settings
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TestSecretKey123456789012345678901234567890"));
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = "ShelterApp.Tests",
                    ValidateAudience = true,
                    ValidAudience = "ShelterApp.Tests",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        // Seed roles
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<ShelterApp.Domain.Users.ApplicationRole>>();
        foreach (var roleName in ShelterApp.Domain.Users.AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ShelterApp.Domain.Users.ApplicationRole(roleName, $"Role: {roleName}"));
            }
        }
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

        // Clear all data except roles
        dbContext.AdoptionApplications.RemoveRange(dbContext.AdoptionApplications);
        dbContext.Adopters.RemoveRange(dbContext.Adopters);
        dbContext.Animals.RemoveRange(dbContext.Animals);
        dbContext.Volunteers.RemoveRange(dbContext.Volunteers);
        dbContext.VisitBookings.RemoveRange(dbContext.VisitBookings);
        dbContext.VisitSlots.RemoveRange(dbContext.VisitSlots);
        dbContext.EmailQueue.RemoveRange(dbContext.EmailQueue);

        // Clear users (except for any seeded admin)
        var users = dbContext.Users.ToList();
        dbContext.Users.RemoveRange(users);

        await dbContext.SaveChangesAsync();
    }

    public ShelterDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
    }
}
