using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Notifications;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Escalation service - generates alerts when an adoption application
/// remains unprocessed for 48 hours (WF-31)
/// </summary>
public class EscalationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EscalationService> _logger;
    private readonly EscalationSettings _settings;

    public EscalationService(
        IServiceProvider serviceProvider,
        ILogger<EscalationService> logger,
        IOptions<EscalationSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Escalation Service is disabled");
            return;
        }

        _logger.LogInformation("Escalation Service started. Checking every {IntervalMinutes} minutes for applications unprocessed for {HoursThreshold} hours",
            _settings.CheckIntervalMinutes, _settings.EscalationHoursThreshold);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForEscalationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in escalation check");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.CheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckForEscalationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

        var threshold = DateTime.UtcNow.AddHours(-_settings.EscalationHoursThreshold);

        // Find applications that:
        // 1. Have status New (new, unprocessed)
        // 2. Were submitted earlier than the threshold (48h)
        // 3. Do not yet have an escalation alert
        var unprocessedApplications = await (
            from app in context.AdoptionApplications
            join adopter in context.Adopters on app.AdopterId equals adopter.Id
            join animal in context.Animals on app.AnimalId equals animal.Id
            where app.Status == AdoptionApplicationStatus.New
            where app.CreatedAt < threshold
            select new
            {
                Application = app,
                AdopterFullName = adopter.FirstName + " " + adopter.LastName,
                AnimalName = animal.Name
            }
        ).ToListAsync(cancellationToken);

        foreach (var item in unprocessedApplications)
        {
            var application = item.Application;

            // Check if an escalation alert already exists for this application
            var existingAlert = await context.AdminNotifications
                .AnyAsync(n =>
                    n.Type == NotificationType.ApplicationEscalation &&
                    n.RelatedEntityId == application.Id &&
                    !n.IsDismissed,
                    cancellationToken);

            if (existingAlert)
                continue;

            // Calculate how many hours have passed
            var hoursWaiting = (int)(DateTime.UtcNow - application.CreatedAt).TotalHours;

            // Create a new escalation alert
            var notification = AdminNotification.Create(
                type: NotificationType.ApplicationEscalation,
                title: $"Zgłoszenie oczekuje {hoursWaiting}h na rozpatrzenie",
                message: $"Zgłoszenie adopcyjne od {item.AdopterFullName} " +
                         $"dla {item.AnimalName} " +
                         $"nie zostało rozpatrzone od {hoursWaiting} godzin. Wymaga pilnej reakcji!",
                priority: hoursWaiting >= 72 ? NotificationPriority.Urgent : NotificationPriority.High,
                link: $"/admin/adoptions/{application.Id}",
                relatedEntityId: application.Id,
                relatedEntityType: "AdoptionApplication",
                expiresAt: DateTime.UtcNow.AddDays(7) // Alert expires after 7 days
            );

            context.AdminNotifications.Add(notification);

            _logger.LogWarning(
                "Created escalation alert for application {ApplicationId} - waiting {Hours}h",
                application.Id, hoursWaiting);
        }

        var alertsCreated = context.ChangeTracker.Entries<AdminNotification>()
            .Count(e => e.State == EntityState.Added);

        if (alertsCreated > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created {Count} escalation alerts", alertsCreated);
        }

        // Additionally: create notifications about new applications (WF-30)
        await CreateNewApplicationNotificationsAsync(context, cancellationToken);
    }

    private async Task CreateNewApplicationNotificationsAsync(ShelterDbContext context, CancellationToken cancellationToken)
    {
        // Find new applications from the last 5 minutes that don't have a notification yet
        var recentThreshold = DateTime.UtcNow.AddMinutes(-5);

        var recentApplications = await (
            from app in context.AdoptionApplications
            join adopter in context.Adopters on app.AdopterId equals adopter.Id
            join animal in context.Animals on app.AnimalId equals animal.Id
            where app.CreatedAt > recentThreshold
            select new
            {
                Application = app,
                AdopterFullName = adopter.FirstName + " " + adopter.LastName,
                AnimalName = animal.Name
            }
        ).ToListAsync(cancellationToken);

        foreach (var item in recentApplications)
        {
            var existingNotification = await context.AdminNotifications
                .AnyAsync(n =>
                    n.Type == NotificationType.NewAdoptionApplication &&
                    n.RelatedEntityId == item.Application.Id,
                    cancellationToken);

            if (existingNotification)
                continue;

            var notification = AdminNotification.Create(
                type: NotificationType.NewAdoptionApplication,
                title: "Nowe zgłoszenie adopcyjne",
                message: $"{item.AdopterFullName} złożył(a) zgłoszenie adopcyjne dla {item.AnimalName}",
                priority: NotificationPriority.Normal,
                link: $"/admin/adoptions/{item.Application.Id}",
                relatedEntityId: item.Application.Id,
                relatedEntityType: "AdoptionApplication"
            );

            context.AdminNotifications.Add(notification);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

public class EscalationSettings
{
    public const string SectionName = "Escalation";

    public bool Enabled { get; set; } = true;
    public int EscalationHoursThreshold { get; set; } = 48; // WF-31: 48 hours
    public int CheckIntervalMinutes { get; set; } = 30; // Check every 30 minutes
}
