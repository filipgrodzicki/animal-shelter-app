using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Emails;

namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Email service implementation that queues emails for background processing
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailQueueRepository _emailQueueRepository;
    private readonly IEmailTemplateService _templateService;
    private readonly EmailSettings _emailSettings;
    private readonly ShelterOptions _shelterOptions;

    public EmailService(
        ILogger<EmailService> logger,
        IEmailQueueRepository emailQueueRepository,
        IEmailTemplateService templateService,
        IOptions<EmailSettings> emailSettings,
        IOptions<ShelterOptions> shelterOptions)
    {
        _logger = logger;
        _emailQueueRepository = emailQueueRepository;
        _templateService = templateService;
        _emailSettings = emailSettings.Value;
        _shelterOptions = shelterOptions.Value;
    }

    public async Task SendAdoptionApplicationConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string applicationNumber,
        CancellationToken cancellationToken = default)
    {
        var model = new AdoptionApplicationConfirmationModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            ApplicationNumber = applicationNumber,
            ApplicationDate = DateTime.UtcNow
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.AdoptionApplicationConfirmation,
            model,
            cancellationToken);
    }

    public async Task SendApplicationAcceptedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        CancellationToken cancellationToken = default)
    {
        var model = new ApplicationAcceptedModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            NextStepsUrl = $"{_shelterOptions.Website}/profile/adoptions"
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.ApplicationAccepted,
            model,
            cancellationToken);
    }

    public async Task SendApplicationRejectedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var model = new ApplicationRejectedModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            Reason = reason
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.ApplicationRejected,
            model,
            cancellationToken);
    }

    public async Task SendVisitScheduledAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        DateTime visitDate,
        string shelterAddress,
        CancellationToken cancellationToken = default)
    {
        var model = new VisitScheduledModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            VisitDate = visitDate,
            VisitAddress = shelterAddress
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.VisitScheduled,
            model,
            cancellationToken);

        // Also schedule a reminder for 24 hours before the visit
        await ScheduleVisitReminderAsync(
            recipientEmail,
            recipientName,
            animalName,
            visitDate,
            shelterAddress,
            cancellationToken);
    }

    public async Task SendVisitApprovedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        CancellationToken cancellationToken = default)
    {
        var model = new VisitResultModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            IsApproved = true,
            NextSteps = "Wkrótce skontaktujemy się w sprawie podpisania umowy adopcyjnej."
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.VisitApproved,
            model,
            cancellationToken);
    }

    public async Task SendVisitRejectedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var model = new VisitResultModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            IsApproved = false,
            Reason = reason
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.VisitRejected,
            model,
            cancellationToken);
    }

    public async Task SendAdoptionCompletedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string contractNumber,
        CancellationToken cancellationToken = default)
    {
        var model = new AdoptionCompletedModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            ContractNumber = contractNumber,
            AdoptionDate = DateTime.UtcNow
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.AdoptionCompleted,
            model,
            cancellationToken);
    }

    public async Task SendApplicationCancelledAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var model = new ApplicationCancelledModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            Reason = reason,
            CancelledByUser = false
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.ApplicationCancelled,
            model,
            cancellationToken);
    }

    public async Task SendVisitReminderAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        DateTime visitDate,
        CancellationToken cancellationToken = default)
    {
        var model = new VisitReminderModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            VisitDate = visitDate,
            VisitAddress = _shelterOptions.Address,
            HoursUntilVisit = (int)(visitDate - DateTime.UtcNow).TotalHours
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.VisitReminder,
            model,
            cancellationToken);
    }

    #region Volunteer Emails

    public async Task SendVolunteerApplicationConfirmationAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        var model = new VolunteerApplicationConfirmationModel
        {
            RecipientName = recipientName,
            ApplicationDate = DateTime.UtcNow
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.VolunteerApplicationConfirmation,
            model,
            cancellationToken);
    }

    public async Task SendVolunteerApprovalNotificationAsync(
        string recipientEmail,
        string recipientName,
        DateTime trainingStartDate,
        CancellationToken cancellationToken = default)
    {
        var model = new VolunteerApprovalModel
        {
            RecipientName = recipientName,
            TrainingStartDate = trainingStartDate,
            TrainingLocation = _shelterOptions.Address
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.VolunteerApproval,
            model,
            cancellationToken);
    }

    public async Task SendVolunteerActivationNotificationAsync(
        string recipientEmail,
        string recipientName,
        string contractNumber,
        CancellationToken cancellationToken = default)
    {
        var model = new VolunteerActivationModel
        {
            RecipientName = recipientName,
            ContractNumber = contractNumber,
            VolunteerPortalUrl = $"{_shelterOptions.Website}/volunteer/portal"
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.VolunteerActivation,
            model,
            cancellationToken);
    }

    #endregion

    #region Auth Emails

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        var model = new PasswordResetModel
        {
            RecipientName = recipientName,
            ResetUrl = resetUrl,
            ExpirationMinutes = 60
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.PasswordReset,
            model,
            cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        var model = new WelcomeEmailModel
        {
            RecipientName = recipientName,
            LoginUrl = $"{_shelterOptions.Website}/login"
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.WelcomeEmail,
            model,
            cancellationToken);
    }

    #endregion

    #region Private Methods

    private async Task QueueEmailAsync<T>(
        string recipientEmail,
        string recipientName,
        EmailType emailType,
        T model,
        CancellationToken cancellationToken,
        DateTime? scheduledAt = null) where T : EmailTemplateModel
    {
        try
        {
            var (subject, htmlBody, textBody) = await _templateService.RenderTemplateAsync(
                emailType,
                model,
                cancellationToken);

            var email = EmailQueue.Create(
                recipientEmail,
                recipientName,
                subject,
                htmlBody,
                emailType,
                textBody,
                scheduledAt);

            await _emailQueueRepository.AddAsync(email, cancellationToken);

            _logger.LogInformation(
                "Email queued: Type={EmailType}, To={Email}, ScheduledAt={ScheduledAt}",
                emailType, recipientEmail, scheduledAt?.ToString() ?? "immediate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to queue email: Type={EmailType}, To={Email}",
                emailType, recipientEmail);
            throw;
        }
    }

    private async Task ScheduleVisitReminderAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        DateTime visitDate,
        string shelterAddress,
        CancellationToken cancellationToken)
    {
        // Schedule reminder for 24 hours before visit
        var reminderTime = visitDate.AddHours(-24);

        // Only schedule if the reminder time is in the future
        if (reminderTime <= DateTime.UtcNow)
            return;

        var model = new VisitReminderModel
        {
            RecipientName = recipientName,
            AnimalName = animalName,
            VisitDate = visitDate,
            VisitAddress = shelterAddress,
            HoursUntilVisit = 24
        };

        await QueueEmailAsync(
            recipientEmail,
            recipientName,
            EmailType.VisitReminder,
            model,
            cancellationToken,
            reminderTime);
    }

    #endregion
}
