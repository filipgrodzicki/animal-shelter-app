namespace ShelterApp.Infrastructure.Services;

public class SmsSettings
{
    public const string SectionName = "Sms";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Twilio"; // Twilio, SMSAPI, etc.

    // Twilio settings
    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;
    public string TwilioPhoneNumber { get; set; } = string.Empty;

    // General settings
    public string SenderName { get; set; } = "Schronisko";
    public int VisitReminderHoursBefore { get; set; } = 24;
}
