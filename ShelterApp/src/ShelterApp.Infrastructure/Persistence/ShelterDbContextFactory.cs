using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShelterApp.Infrastructure.Persistence;

/// <summary>
/// DbContext factory used by EF Core during migration generation
/// </summary>
public class ShelterDbContextFactory : IDesignTimeDbContextFactory<ShelterDbContext>
{
    public ShelterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShelterDbContext>();

        // Connection string for design-time purposes (migration generation)
        // At runtime, the connection string from configuration is used
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=shelter;Username=shelter_user;Password=shelter_pass",
            b => b.MigrationsAssembly(typeof(ShelterDbContext).Assembly.FullName));

        return new ShelterDbContext(optionsBuilder.Options);
    }
}
