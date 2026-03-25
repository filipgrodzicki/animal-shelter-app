using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using ShelterApp.Domain.Emails;

namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Service responsible for actual email delivery via SendGrid or SMTP
/// </summary>
public interface IEmailSenderService
{
    Task<bool> SendEmailAsync(EmailQueue email, CancellationToken cancellationToken = default);
}

public class EmailSenderService : IEmailSenderService
{
    private readonly ILogger<EmailSenderService> _logger;
    private readonly EmailSettings _settings;

    public EmailSenderService(
        ILogger<EmailSenderService> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<bool> SendEmailAsync(EmailQueue email, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableSending)
        {
            _logger.LogInformation(
                "Email sending disabled. Would send: To={To}, Subject={Subject}",
                email.RecipientEmail, email.Subject);
            return true;
        }

        return _settings.Provider.ToLowerInvariant() switch
        {
            "sendgrid" => await SendViaSendGridAsync(email, cancellationToken),
            "smtp" => await SendViaSmtpAsync(email, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown email provider: {_settings.Provider}")
        };
    }

    private async Task<bool> SendViaSendGridAsync(EmailQueue email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_settings.SendGridApiKey))
        {
            throw new InvalidOperationException("SendGrid API key is not configured");
        }

        try
        {
            var client = new SendGridClient(_settings.SendGridApiKey);

            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var to = new EmailAddress(email.RecipientEmail, email.RecipientName);

            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                email.Subject,
                email.TextBody,
                email.HtmlBody);

            var response = await client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email sent via SendGrid: To={To}, Subject={Subject}",
                    email.RecipientEmail, email.Subject);
                return true;
            }

            var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "SendGrid returned non-success status: {StatusCode}, Body={Body}",
                response.StatusCode, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email via SendGrid: To={To}, Subject={Subject}",
                email.RecipientEmail, email.Subject);
            throw;
        }
    }

    private async Task<bool> SendViaSmtpAsync(EmailQueue email, CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(email.RecipientName, email.RecipientEmail));
            message.Subject = email.Subject;

            var builder = new BodyBuilder
            {
                HtmlBody = email.HtmlBody
            };

            if (!string.IsNullOrEmpty(email.TextBody))
            {
                builder.TextBody = email.TextBody;
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            var secureSocketOptions = _settings.Smtp.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                _settings.Smtp.Host,
                _settings.Smtp.Port,
                secureSocketOptions,
                cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Smtp.Username))
            {
                await client.AuthenticateAsync(
                    _settings.Smtp.Username,
                    _settings.Smtp.Password,
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent via SMTP: To={To}, Subject={Subject}",
                email.RecipientEmail, email.Subject);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email via SMTP: To={To}, Subject={Subject}",
                email.RecipientEmail, email.Subject);
            throw;
        }
    }
}
