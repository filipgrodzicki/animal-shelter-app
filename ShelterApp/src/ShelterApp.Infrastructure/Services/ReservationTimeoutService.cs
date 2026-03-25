using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Background service for automatic cancellation of expired reservations (WB-14, WS-13)
/// </summary>
public class ReservationTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationTimeoutService> _logger;
    private readonly ReservationTimeoutOptions _options;

    private const string SystemUser = "System";
    private const string CancellationReason = "Automatyczne anulowanie z powodu przekroczenia czasu oczekiwania (7 dni bez postępu)";

    public ReservationTimeoutService(
        IServiceProvider serviceProvider,
        ILogger<ReservationTimeoutService> logger,
        IOptions<ReservationTimeoutOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ReservationTimeoutService is disabled");
            return;
        }

        _logger.LogInformation(
            "ReservationTimeoutService starting. Check interval: {IntervalHours}h, Timeout: {TimeoutDays} days",
            _options.CheckIntervalHours,
            _options.TimeoutDays);

        // Wait briefly for the application to start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired reservations");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(_options.CheckIntervalHours), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("ReservationTimeoutService stopped");
    }

    /// <summary>
    /// Processes expired reservations
    /// </summary>
    private async Task ProcessExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting expired reservations check");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_options.TimeoutDays);

        // Find applications with status New/UnderReview/Accepted older than 7 days
        var expiredApplications = await GetExpiredApplicationsAsync(context, cutoffDate, cancellationToken);

        if (expiredApplications.Count == 0)
        {
            _logger.LogDebug("No expired reservations found");
            return;
        }

        _logger.LogInformation("Found {Count} expired reservations to process", expiredApplications.Count);

        var processedCount = 0;
        var errorCount = 0;

        foreach (var application in expiredApplications)
        {
            try
            {
                await ProcessSingleApplicationAsync(
                    context,
                    emailService,
                    application,
                    cancellationToken);

                processedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex,
                    "Error cancelling expired application {ApplicationId}",
                    application.Application.Id);
            }
        }

        _logger.LogInformation(
            "Expired reservations processing completed. Processed: {Processed}, Errors: {Errors}",
            processedCount,
            errorCount);
    }

    /// <summary>
    /// Retrieves expired applications
    /// </summary>
    private async Task<List<ExpiredApplicationInfo>> GetExpiredApplicationsAsync(
        ShelterDbContext context,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        // Statuses eligible for automatic cancellation
        var eligibleStatuses = new[]
        {
            AdoptionApplicationStatus.New,
            AdoptionApplicationStatus.UnderReview,
            AdoptionApplicationStatus.Accepted
        };

        // Retrieve applications with last status change older than cutoffDate
        var applications = await context.AdoptionApplications
            .Include(a => a.StatusHistory)
            .Where(a => eligibleStatuses.Contains(a.Status))
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        var expiredApplications = new List<ExpiredApplicationInfo>();

        foreach (var application in applications)
        {
            // Check the last activity date
            var lastActivityDate = GetLastActivityDate(application);

            if (lastActivityDate < cutoffDate)
            {
                // Retrieve adopter and animal data
                var adopter = await context.Adopters
                    .FirstOrDefaultAsync(a => a.Id == application.AdopterId, cancellationToken);

                var animal = await context.Animals
                    .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

                if (adopter is not null && animal is not null)
                {
                    expiredApplications.Add(new ExpiredApplicationInfo
                    {
                        Application = application,
                        Adopter = adopter,
                        Animal = animal,
                        LastActivityDate = lastActivityDate
                    });
                }
            }
        }

        return expiredApplications;
    }

    /// <summary>
    /// Gets the last activity date for an application
    /// </summary>
    private static DateTime GetLastActivityDate(AdoptionApplication application)
    {
        // Date of the last status change or the application submission date
        var lastStatusChange = application.StatusHistory
            .OrderByDescending(sh => sh.ChangedAt)
            .FirstOrDefault();

        return lastStatusChange?.ChangedAt ?? application.ApplicationDate;
    }

    /// <summary>
    /// Processes a single expired application
    /// </summary>
    private async Task ProcessSingleApplicationAsync(
        ShelterDbContext context,
        IEmailService emailService,
        ExpiredApplicationInfo info,
        CancellationToken cancellationToken)
    {
        var application = info.Application;
        var adopter = info.Adopter;
        var animal = info.Animal;

        _logger.LogInformation(
            "Cancelling expired application {ApplicationId}. Status: {Status}, Last activity: {LastActivity}",
            application.Id,
            application.Status,
            info.LastActivityDate);

        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            // Cancel the application
            var cancelResult = application.CancelByUser(CancellationReason, SystemUser);

            if (cancelResult.IsFailure)
            {
                _logger.LogWarning(
                    "Could not cancel application {ApplicationId}: {Error}",
                    application.Id,
                    cancelResult.Error.Message);

                await context.RollbackTransactionAsync(cancellationToken);
                return;
            }

            // Restore the animal's status to Available
            if (animal.CanChangeStatus(AnimalStatusTrigger.AnulowanieZgloszenia))
            {
                animal.ChangeStatus(
                    AnimalStatusTrigger.AnulowanieZgloszenia,
                    SystemUser,
                    CancellationReason);
            }

            // Restore the adopter's status (if possible)
            if (adopter.CanChangeStatus(AdopterStatusTrigger.AnulowanieZgloszenia))
            {
                adopter.ChangeStatus(
                    AdopterStatusTrigger.AnulowanieZgloszenia,
                    SystemUser,
                    CancellationReason);
            }

            await context.SaveChangesAsync(cancellationToken);
            await context.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Application {ApplicationId} cancelled successfully. Animal {AnimalId} status restored to Available",
                application.Id,
                animal.Id);

            // Send cancellation notification email (outside the transaction)
            try
            {
                await emailService.SendApplicationCancelledAsync(
                    adopter.Email,
                    adopter.FullName,
                    animal.Name,
                    CancellationReason,
                    cancellationToken);

                _logger.LogDebug(
                    "Cancellation email sent to {Email} for application {ApplicationId}",
                    adopter.Email,
                    application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send cancellation email for application {ApplicationId}",
                    application.Id);
                // Don't rethrow - email is nice-to-have
            }
        }
        catch (Exception)
        {
            if (context.HasActiveTransaction)
            {
                await context.RollbackTransactionAsync(cancellationToken);
            }
            throw;
        }
    }

    /// <summary>
    /// Information about an expired application
    /// </summary>
    private class ExpiredApplicationInfo
    {
        public required AdoptionApplication Application { get; init; }
        public required Adopter Adopter { get; init; }
        public required Animal Animal { get; init; }
        public DateTime LastActivityDate { get; init; }
    }
}
