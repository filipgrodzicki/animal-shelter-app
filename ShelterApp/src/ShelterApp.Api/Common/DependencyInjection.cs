using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Emails;
using ShelterApp.Domain.Users;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;
using ShelterApp.Infrastructure.Services.Chatbot;

namespace ShelterApp.Api.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ShelterDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(
                    configuration.GetValue<int>("Database:CommandTimeout", 30));
            });
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ShelterDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Domain services
        services.AddScoped<IRegistrationNumberGenerator, RegistrationNumberGenerator>();
        services.AddScoped<IContractGeneratorService, ContractGeneratorService>();

        // Email services
        services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailSenderService, EmailSenderService>();
        services.AddScoped<IEmailService, EmailService>();

        // Configuration options
        services.Configure<ShelterOptions>(
            configuration.GetSection(ShelterOptions.SectionName));
        services.Configure<ReservationTimeoutOptions>(
            configuration.GetSection(ReservationTimeoutOptions.SectionName));
        services.Configure<EmailSettings>(
            configuration.GetSection(EmailSettings.SectionName));

        // SMS services (WF-29)
        services.Configure<SmsSettings>(
            configuration.GetSection(SmsSettings.SectionName));
        services.AddScoped<ISmsService, SmsService>();

        // Escalation settings (WF-31)
        services.Configure<EscalationSettings>(
            configuration.GetSection(EscalationSettings.SectionName));

        // Background services
        services.AddHostedService<ReservationTimeoutService>();
        services.AddHostedService<EmailQueueProcessorService>();
        services.AddHostedService<EscalationService>(); // WF-31: 48h escalation alerts

        // Chatbot services
        services.Configure<ChatbotSettings>(
            configuration.GetSection(ChatbotSettings.SectionName));

        // Load chatbot configuration from JSON files
        var systemPromptPath = Path.Combine(AppContext.BaseDirectory, "system-prompt.json");
        var matchingWeightsPath = Path.Combine(AppContext.BaseDirectory, "matching-weights.json");

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
        };

        // Dynamiczny provider - czyta konfigurację przy każdym żądaniu
        services.AddSingleton<ISystemPromptProvider>(new FileSystemPromptProvider(systemPromptPath));

        if (File.Exists(matchingWeightsPath))
        {
            var matchingWeightsJson = File.ReadAllText(matchingWeightsPath);
            var matchingWeightsConfig = System.Text.Json.JsonSerializer.Deserialize<MatchingWeightsConfig>(
                matchingWeightsJson, jsonOptions)
                ?? new MatchingWeightsConfig();
            services.AddSingleton(matchingWeightsConfig);
        }
        else
        {
            services.AddSingleton(new MatchingWeightsConfig());
        }

        // Register chatbot services
        services.AddSingleton<IChatSessionManager, InMemoryChatSessionManager>();
        services.AddScoped<IAnimalMatchingService, AnimalMatchingService>();
        services.AddScoped<IAdoptionMatchingService, AdoptionMatchingService>();
        services.AddScoped<IChatbotRagService, ChatbotRagService>();
        services.AddHttpClient<IOpenAiChatService, OpenAiChatService>();

        return services;
    }

    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure JWT settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Add Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password settings
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Sign-in settings
            options.SignIn.RequireConfirmedEmail = false; // Set to true for production
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ShelterDbContext>()
        .AddDefaultTokenProviders();

        // Add Token Service
        services.AddScoped<ITokenService, JwtTokenService>();

        // Configure JWT Authentication
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Add Authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdmin", policy =>
                policy.RequireRole(AppRoles.Admin))
            .AddPolicy("RequireStaff", policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Staff))
            .AddPolicy("RequireVolunteer", policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Staff, AppRoles.Volunteer))
            .AddPolicy("RequireUser", policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Staff, AppRoles.Volunteer, AppRoles.User));

        return services;
    }

    public static async Task SeedRolesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var description = AppRoles.Descriptions.TryGetValue(roleName, out var desc) ? desc : null;
                var result = await roleManager.CreateAsync(new ApplicationRole(roleName, description ?? string.Empty));

                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    logger.LogWarning("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    public static async Task SeedUsersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Default user accounts
        var defaultUsers = new[]
        {
            new { Email = "admin@shelter.pl", Password = "Admin123!", FirstName = "Administrator", LastName = "Systemu", Role = AppRoles.Admin },
            new { Email = "pracownik@shelter.pl", Password = "Pracownik123!", FirstName = "Jan", LastName = "Kowalski", Role = AppRoles.Staff },
            new { Email = "wolontariusz@shelter.pl", Password = "Wolontariusz123!", FirstName = "Anna", LastName = "Nowak", Role = AppRoles.Volunteer },
            new { Email = "uzytkownik@shelter.pl", Password = "Uzytkownik123!", FirstName = "Piotr", LastName = "Wisniewski", Role = AppRoles.User },
        };

        foreach (var userData in defaultUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(userData.Email);
            if (existingUser is null)
            {
                var user = ApplicationUser.Create(
                    email: userData.Email,
                    firstName: userData.FirstName,
                    lastName: userData.LastName,
                    phoneNumber: "+48123456789");

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    // Confirm email
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    await userManager.ConfirmEmailAsync(user, token);

                    await userManager.AddToRoleAsync(user, userData.Role);
                    logger.LogInformation("Created default user: {Email} with role {Role}", userData.Email, userData.Role);
                }
                else
                {
                    logger.LogWarning("Failed to create user {Email}: {Errors}",
                        userData.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:EnableAutoMigration", false))
            return;

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }
}
