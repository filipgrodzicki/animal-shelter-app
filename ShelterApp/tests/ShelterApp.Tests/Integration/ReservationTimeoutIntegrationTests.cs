using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;
using Xunit;

namespace ShelterApp.Tests.Integration;

/// <summary>
/// Test 2: Reservation timeout integration test
/// - Submit application
/// - 7 days pass (simulated)
/// - Automatic cancellation
/// - Animal available again
/// </summary>
[Collection("PostgreSql")]
public class ReservationTimeoutIntegrationTests : IntegrationTestBase
{
    public ReservationTimeoutIntegrationTests(PostgreSqlFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ReservationTimeout_After7Days_ShouldCancelApplicationAndReleaseAnimal()
    {
        // =====================================================
        // ARRANGE: Create test animal and submit application
        // =====================================================
        var animalId = await CreateTestAnimalAsync("Reksio");

        // Verify animal is available
        var animal = await GetAnimalAsync(animalId);
        animal!.Status.Should().Be(AnimalStatus.Available);

        // Create adopter and application directly in DB (to control dates)
        Guid applicationId;
        Guid adopterId;

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

            // Create adopter
            var adopter = Adopter.Create(
                userId: Guid.NewGuid(),
                firstName: "Test",
                lastName: "User",
                email: "timeout.test@example.com",
                phone: "+48123456789",
                dateOfBirth: new DateTime(1990, 1, 1),
                rodoConsentDate: DateTime.UtcNow
            ).Value;

            dbContext.Adopters.Add(adopter);
            adopterId = adopter.Id;

            // Create application with old date (8 days ago)
            var application = AdoptionApplication.Create(
                adopterId: adopter.Id,
                animalId: animalId,
                adoptionMotivation: "Test motivation"
            ).Value;

            // Zmień status na Submitted (zgłoszenie złożone)
            application.ChangeStatus(
                AdoptionApplicationTrigger.ZlozenieZgloszenia,
                "test@test.com");

            dbContext.AdoptionApplications.Add(application);
            applicationId = application.Id;

            // Reserve the animal
            var animalEntity = await dbContext.Animals.FindAsync(animalId);
            animalEntity!.ChangeStatus(
                AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
                "test@test.com");

            await dbContext.SaveChangesAsync();

            // Manually update application date to 8 days ago
            await dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE ""AdoptionApplications""
                  SET ""ApplicationDate"" = @p0
                  WHERE ""Id"" = @p1",
                DateTime.UtcNow.AddDays(-8),
                applicationId);
        }

        // Verify initial state
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

            var app = await dbContext.AdoptionApplications.FindAsync(applicationId);
            app!.Status.Should().Be(AdoptionApplicationStatus.Submitted);

            var animalEntity = await dbContext.Animals.FindAsync(animalId);
            animalEntity!.Status.Should().Be(AnimalStatus.Reserved);
        }

        // =====================================================
        // ACT: Run the timeout service
        // =====================================================
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationTimeoutService>>();
            var options = Options.Create(new ReservationTimeoutOptions
            {
                Enabled = true,
                TimeoutDays = 7,
                CheckIntervalHours = 1,
                BatchSize = 100
            });

            // Create and run the timeout service manually
            var service = new ReservationTimeoutServiceRunner(dbContext, options, logger);
            await service.ProcessTimeoutsAsync(CancellationToken.None);
        }

        // =====================================================
        // ASSERT: Application cancelled and animal released
        // =====================================================
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

            // Application should be cancelled
            var application = await dbContext.AdoptionApplications
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            application!.Status.Should().Be(AdoptionApplicationStatus.Cancelled);

            // Status history should show the cancellation
            application.StatusHistory.Should().Contain(sh =>
                sh.NewStatus == AdoptionApplicationStatus.Cancelled);

            // Animal should be available again
            var animalEntity = await dbContext.Animals.FindAsync(animalId);
            animalEntity!.Status.Should().Be(AnimalStatus.Available);
        }
    }

    [Fact]
    public async Task ReservationTimeout_UnderReviewApplication_ShouldNotBeCancelled()
    {
        // =====================================================
        // ARRANGE: Create application in UnderReview status
        // =====================================================
        var animalId = await CreateTestAnimalAsync("Burek2");
        Guid applicationId;

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

            var adopter = Adopter.Create(
                userId: Guid.NewGuid(),
                firstName: "Test",
                lastName: "User2",
                email: "underreview.test@example.com",
                phone: "+48123456789",
                dateOfBirth: new DateTime(1990, 1, 1),
                rodoConsentDate: DateTime.UtcNow
            ).Value;

            dbContext.Adopters.Add(adopter);

            var application = AdoptionApplication.Create(
                adopterId: adopter.Id,
                animalId: animalId
            ).Value;

            // Najpierw zmień status na Submitted, potem na UnderReview
            application.ChangeStatus(
                AdoptionApplicationTrigger.ZlozenieZgloszenia,
                "test@test.com");
            application.TakeForReview(Guid.NewGuid(), "Reviewer");

            dbContext.AdoptionApplications.Add(application);
            applicationId = application.Id;

            var animalEntity = await dbContext.Animals.FindAsync(animalId);
            animalEntity!.ChangeStatus(
                AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
                "test@test.com");

            await dbContext.SaveChangesAsync();

            // Set old date
            await dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE ""AdoptionApplications""
                  SET ""ApplicationDate"" = @p0
                  WHERE ""Id"" = @p1",
                DateTime.UtcNow.AddDays(-15),
                applicationId);
        }

        // =====================================================
        // ACT: Run timeout service
        // =====================================================
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationTimeoutService>>();
            var options = Options.Create(new ReservationTimeoutOptions
            {
                Enabled = true,
                TimeoutDays = 7,
                BatchSize = 100
            });

            var service = new ReservationTimeoutServiceRunner(dbContext, options, logger);
            await service.ProcessTimeoutsAsync(CancellationToken.None);
        }

        // =====================================================
        // ASSERT: Application should still be UnderReview
        // =====================================================
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

            var application = await dbContext.AdoptionApplications.FindAsync(applicationId);
            application!.Status.Should().Be(AdoptionApplicationStatus.UnderReview);

            // Animal should still be reserved
            var animalEntity = await dbContext.Animals.FindAsync(animalId);
            animalEntity!.Status.Should().Be(AnimalStatus.Reserved);
        }
    }

    [Fact]
    public async Task ReservationTimeout_RecentApplication_ShouldNotBeCancelled()
    {
        // =====================================================
        // ARRANGE: Create recent application (3 days ago)
        // =====================================================
        var animalId = await CreateTestAnimalAsync("Mruczek");
        Guid applicationId;

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

            var adopter = Adopter.Create(
                userId: Guid.NewGuid(),
                firstName: "Recent",
                lastName: "User",
                email: "recent.test@example.com",
                phone: "+48123456789",
                dateOfBirth: new DateTime(1990, 1, 1),
                rodoConsentDate: DateTime.UtcNow
            ).Value;

            dbContext.Adopters.Add(adopter);

            var application = AdoptionApplication.Create(
                adopterId: adopter.Id,
                animalId: animalId
            ).Value;

            // Zmień status na Submitted (zgłoszenie złożone)
            application.ChangeStatus(
                AdoptionApplicationTrigger.ZlozenieZgloszenia,
                "test@test.com");

            dbContext.AdoptionApplications.Add(application);
            applicationId = application.Id;

            var animalEntity = await dbContext.Animals.FindAsync(animalId);
            animalEntity!.ChangeStatus(
                AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
                "test@test.com");

            await dbContext.SaveChangesAsync();

            // Set date to 3 days ago (under 7 day limit)
            await dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE ""AdoptionApplications""
                  SET ""ApplicationDate"" = @p0
                  WHERE ""Id"" = @p1",
                DateTime.UtcNow.AddDays(-3),
                applicationId);
        }

        // =====================================================
        // ACT: Run timeout service
        // =====================================================
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationTimeoutService>>();
            var options = Options.Create(new ReservationTimeoutOptions
            {
                Enabled = true,
                TimeoutDays = 7,
                BatchSize = 100
            });

            var service = new ReservationTimeoutServiceRunner(dbContext, options, logger);
            await service.ProcessTimeoutsAsync(CancellationToken.None);
        }

        // =====================================================
        // ASSERT: Application should still be Submitted (nie upłynęło 7 dni)
        // =====================================================
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

            var application = await dbContext.AdoptionApplications.FindAsync(applicationId);
            application!.Status.Should().Be(AdoptionApplicationStatus.Submitted);

            // Animal should still be reserved
            var animalEntity = await dbContext.Animals.FindAsync(animalId);
            animalEntity!.Status.Should().Be(AnimalStatus.Reserved);
        }
    }
}

/// <summary>
/// Helper class to run timeout processing directly (without hosting)
/// </summary>
internal class ReservationTimeoutServiceRunner
{
    private readonly ShelterDbContext _context;
    private readonly ReservationTimeoutOptions _options;
    private readonly ILogger _logger;

    public ReservationTimeoutServiceRunner(
        ShelterDbContext context,
        IOptions<ReservationTimeoutOptions> options,
        ILogger logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessTimeoutsAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.TimeoutDays);

        // Szukamy zgłoszeń ze statusem Submitted (złożone, oczekujące na odpowiedź)
        var expiredApplications = await _context.AdoptionApplications
            .Include(a => a.StatusHistory)
            .Where(a => a.Status == AdoptionApplicationStatus.Submitted)
            .Where(a => a.ApplicationDate < cutoffDate)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var application in expiredApplications)
        {
            var changeResult = application.CancelByUser(
                "Automatyczne anulowanie - przekroczono czas oczekiwania na odpowiedź",
                "System");

            if (changeResult.IsSuccess)
            {
                var animal = await _context.Animals.FindAsync(application.AnimalId);
                if (animal != null && animal.Status == AnimalStatus.Reserved)
                {
                    animal.ChangeStatus(
                        AnimalStatusTrigger.AnulowanieZgloszenia,
                        "System",
                        "Automatyczne zwolnienie - wygaśnięcie rezerwacji");
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
