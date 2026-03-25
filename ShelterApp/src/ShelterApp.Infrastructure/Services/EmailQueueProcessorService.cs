using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Emails;

namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Background service that processes the email queue
/// </summary>
public class EmailQueueProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailQueueProcessorService> _logger;
    private readonly EmailSettings _settings;

    public EmailQueueProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailQueueProcessorService> logger,
        IOptions<EmailSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email queue processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_settings.Queue.ProcessingIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("Email queue processor stopped");
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();

        var pendingEmails = await repository.GetPendingEmailsAsync(
            _settings.Queue.BatchSize,
            cancellationToken);

        if (pendingEmails.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Processing {Count} pending emails",
            pendingEmails.Count);

        foreach (var email in pendingEmails)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Check if we should wait due to retry backoff
            if (email.RetryCount > 0)
            {
                var nextRetryTime = email.LastAttemptAt?.Add(email.GetNextRetryDelay());
                if (nextRetryTime > DateTime.UtcNow)
                {
                    continue;
                }
            }

            await ProcessEmailAsync(email, repository, sender, cancellationToken);
        }
    }

    private async Task ProcessEmailAsync(
        EmailQueue email,
        IEmailQueueRepository repository,
        IEmailSenderService sender,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Processing email: Id={Id}, Type={Type}, To={To}, Attempt={Attempt}",
                email.Id, email.EmailType, email.RecipientEmail, email.RetryCount + 1);

            email.MarkAsProcessing();
            await repository.UpdateAsync(email, cancellationToken);

            var success = await sender.SendEmailAsync(email, cancellationToken);

            if (success)
            {
                email.MarkAsSent();
                _logger.LogInformation(
                    "Email sent successfully: Id={Id}, Type={Type}, To={To}",
                    email.Id, email.EmailType, email.RecipientEmail);
            }
            else
            {
                email.MarkAsFailed("Send operation returned false");
                _logger.LogWarning(
                    "Email send failed: Id={Id}, Type={Type}, To={To}, Attempt={Attempt}",
                    email.Id, email.EmailType, email.RecipientEmail, email.RetryCount);
            }
        }
        catch (Exception ex)
        {
            email.MarkAsFailed(ex.Message);
            _logger.LogError(ex,
                "Email send error: Id={Id}, Type={Type}, To={To}, Attempt={Attempt}, Error={Error}",
                email.Id, email.EmailType, email.RecipientEmail, email.RetryCount, ex.Message);
        }
        finally
        {
            await repository.UpdateAsync(email, cancellationToken);

            if (email.Status == EmailStatus.Failed)
            {
                _logger.LogError(
                    "Email permanently failed after {MaxRetries} attempts: Id={Id}, Type={Type}, To={To}",
                    _settings.Queue.MaxRetries, email.Id, email.EmailType, email.RecipientEmail);
            }
        }
    }
}
