namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Email service configuration settings
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Email provider: "SendGrid" or "Smtp"
    /// </summary>
    public string Provider { get; set; } = "Smtp";

    /// <summary>
    /// Sender email address
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Sender display name
    /// </summary>
    public string FromName { get; set; } = "Schronisko dla Zwierząt";

    /// <summary>
    /// SendGrid API key (when using SendGrid provider)
    /// </summary>
    public string? SendGridApiKey { get; set; }

    /// <summary>
    /// SMTP settings (when using SMTP provider)
    /// </summary>
    public SmtpSettings Smtp { get; set; } = new();

    /// <summary>
    /// Queue processing settings
    /// </summary>
    public QueueSettings Queue { get; set; } = new();

    /// <summary>
    /// Enable/disable actual email sending (for development)
    /// </summary>
    public bool EnableSending { get; set; } = true;
}

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class QueueSettings
{
    /// <summary>
    /// How often to check for pending emails (in seconds)
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum emails to process in one batch
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Maximum retry attempts before marking as failed
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// How often to check for visit reminders (in minutes)
    /// </summary>
    public int ReminderCheckIntervalMinutes { get; set; } = 15;
}
