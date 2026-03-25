using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Notifications;

public class AdminNotification : Entity<Guid>
{
    public NotificationType Type { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public Guid? ReadByUserId { get; private set; }
    public bool IsDismissed { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private AdminNotification() { }

    public static AdminNotification Create(
        NotificationType type,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal,
        string? link = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        DateTime? expiresAt = null)
    {
        return new AdminNotification
        {
            Id = Guid.NewGuid(),
            Type = type,
            Priority = priority,
            Title = title,
            Message = message,
            Link = link,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            IsRead = false,
            IsDismissed = false,
            ExpiresAt = expiresAt
        };
    }

    public void MarkAsRead(Guid userId)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            ReadByUserId = userId;
            SetUpdatedAt();
        }
    }

    public void Dismiss()
    {
        IsDismissed = true;
        SetUpdatedAt();
    }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}

public enum NotificationType
{
    NewAdoptionApplication,     // New adoption application
    ApplicationNeedsReview,     // Application needs review
    ApplicationEscalation,      // Escalation - no response in 48h
    VisitScheduled,             // Scheduled visit
    VisitReminder,              // Visit reminder
    NewVolunteerApplication,    // New volunteer application
    AnimalHealthAlert,          // Animal health alert
    SystemAlert,                // System alert
    GeneralInfo                 // General information
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}
