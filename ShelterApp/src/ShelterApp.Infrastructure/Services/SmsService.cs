using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Common;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ShelterApp.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly SmsSettings _settings;
    private readonly ILogger<SmsService> _logger;
    private readonly bool _isInitialized;

    public SmsService(IOptions<SmsSettings> settings, ILogger<SmsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (_settings.Enabled && !string.IsNullOrEmpty(_settings.TwilioAccountSid))
        {
            try
            {
                TwilioClient.Init(_settings.TwilioAccountSid, _settings.TwilioAuthToken);
                _isInitialized = true;
                _logger.LogInformation("SMS Service initialized with Twilio");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Twilio client");
                _isInitialized = false;
            }
        }
        else
        {
            _logger.LogWarning("SMS Service is disabled or not configured");
            _isInitialized = false;
        }
    }

    public async Task SendVisitReminderAsync(
        string phoneNumber,
        string recipientName,
        string animalName,
        DateTime visitDate,
        string shelterAddress,
        CancellationToken cancellationToken = default)
    {
        var message = $"Przypomnienie: {recipientName}, jutro ({visitDate:dd.MM.yyyy} o {visitDate:HH:mm}) " +
                      $"masz wizytę adopcyjną dla {animalName}. " +
                      $"Adres: {shelterAddress}. Do zobaczenia!";

        await SendSmsAsync(phoneNumber, message, cancellationToken);
    }

    public async Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SMS disabled. Would send to {PhoneNumber}: {Message}",
                MaskPhoneNumber(phoneNumber), message);
            return;
        }

        if (!_isInitialized)
        {
            _logger.LogWarning("SMS service not initialized. Cannot send SMS to {PhoneNumber}",
                MaskPhoneNumber(phoneNumber));
            return;
        }

        try
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            var result = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_settings.TwilioPhoneNumber),
                to: new PhoneNumber(normalizedPhone)
            );

            _logger.LogInformation("SMS sent successfully to {PhoneNumber}, SID: {Sid}",
                MaskPhoneNumber(phoneNumber), result.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}",
                MaskPhoneNumber(phoneNumber));
            throw;
        }
    }

    public async Task SendApplicationStatusChangeAsync(
        string phoneNumber,
        string recipientName,
        string animalName,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        var statusMessage = newStatus switch
        {
            "Accepted" => "zostało zaakceptowane",
            "Rejected" => "zostało odrzucone",
            "VisitScheduled" => "ma zaplanowaną wizytę",
            "VisitCompleted" => "przeszło wizytę pozytywnie",
            "Completed" => "zostało zakończone - gratulacje!",
            _ => $"zmieniło status na: {newStatus}"
        };

        var message = $"{recipientName}, Twoje zgłoszenie adopcyjne dla {animalName} {statusMessage}. " +
                      "Sprawdź szczegóły w panelu użytkownika.";

        await SendSmsAsync(phoneNumber, message, cancellationToken);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // Remove spaces and dashes
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "");

        // Add Polish country code if missing
        if (cleaned.StartsWith("0"))
            cleaned = "+48" + cleaned.Substring(1);
        else if (!cleaned.StartsWith("+"))
            cleaned = "+48" + cleaned;

        return cleaned;
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "***";

        return phoneNumber.Substring(0, 3) + "***" + phoneNumber.Substring(phoneNumber.Length - 2);
    }
}
