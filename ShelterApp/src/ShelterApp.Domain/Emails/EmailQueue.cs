using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Emails;

/// <summary>
/// Represents an email in the queue waiting to be sent
/// </summary>
public class EmailQueue : Entity<Guid>
{
    public string RecipientEmail { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string? TextBody { get; private set; }
    public EmailType EmailType { get; private set; }
    public EmailStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public string? Metadata { get; private set; }

    private EmailQueue() { }

    public static EmailQueue Create(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        EmailType emailType,
        string? textBody = null,
        DateTime? scheduledAt = null,
        string? metadata = null)
    {
        return new EmailQueue
        {
            Id = Guid.NewGuid(),
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            EmailType = emailType,
            Status = EmailStatus.Pending,
            RetryCount = 0,
            ScheduledAt = scheduledAt,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsSent()
    {
        Status = EmailStatus.Sent;
        SentAt = DateTime.UtcNow;
        LastAttemptAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkAsFailed(string error)
    {
        RetryCount++;
        LastAttemptAt = DateTime.UtcNow;
        LastError = error;

        if (RetryCount >= 5)
        {
            Status = EmailStatus.Failed;
        }
        else
        {
            Status = EmailStatus.Pending;
        }
    }

    public void MarkAsProcessing()
    {
        Status = EmailStatus.Processing;
        LastAttemptAt = DateTime.UtcNow;
    }

    public bool ShouldProcess()
    {
        if (Status != EmailStatus.Pending)
            return false;

        if (ScheduledAt.HasValue && ScheduledAt.Value > DateTime.UtcNow)
            return false;

        return true;
    }

    public TimeSpan GetNextRetryDelay()
    {
        // Exponential backoff: 1min, 5min, 15min, 30min, 1hour
        return RetryCount switch
        {
            0 => TimeSpan.Zero,
            1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            3 => TimeSpan.FromMinutes(15),
            4 => TimeSpan.FromMinutes(30),
            _ => TimeSpan.FromHours(1)
        };
    }
}

public enum EmailType
{
    AdoptionApplicationConfirmation,
    ApplicationAccepted,
    ApplicationRejected,
    VisitScheduled,
    VisitReminder,
    VisitApproved,
    VisitRejected,
    AdoptionCompleted,
    ApplicationCancelled,
    VolunteerApplicationConfirmation,
    VolunteerApproval,
    VolunteerActivation,
    PasswordReset,
    WelcomeEmail
}

public enum EmailStatus
{
    Pending,
    Processing,
    Sent,
    Failed
}
