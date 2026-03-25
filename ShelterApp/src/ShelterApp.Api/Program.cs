using ShelterApp.Api.Common;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// Services Configuration
// ===========================================

builder.Services
    .AddApiServices()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddIdentityServices(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors();

// Health Checks
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString!,
        name: "postgresql",
        tags: new[] { "db", "sql", "postgresql" });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ShelterApp API",
        Version = "v1",
        Description = "API for Animal Shelter Management System"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===========================================
// Database Migration & Seeding (skip in Testing environment)
// ===========================================
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.ApplyMigrationsAsync();
    await app.SeedRolesAsync();
    await app.SeedUsersAsync();
    await app.SeedSampleDataAsync();
}

// ===========================================
// HTTP Pipeline Configuration
// ===========================================
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ShelterApp API v1");
    });
}

app.UseHttpsRedirection();

// CORS for frontend
app.UseCors(builder => builder
    .WithOrigins("http://localhost:3000", "https://localhost:3000")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
