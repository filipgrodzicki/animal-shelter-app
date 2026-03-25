namespace ShelterApp.Domain.Common;

/// <summary>
/// Email sending service interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends adoption application confirmation email
    /// </summary>
    Task SendAdoptionApplicationConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string applicationNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends application accepted email
    /// </summary>
    Task SendApplicationAcceptedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends application rejected email
    /// </summary>
    Task SendApplicationRejectedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends scheduled visit confirmation email
    /// </summary>
    Task SendVisitScheduledAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        DateTime visitDate,
        string shelterAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends visit approved email
    /// </summary>
    Task SendVisitApprovedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends visit rejected email
    /// </summary>
    Task SendVisitRejectedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends adoption completed email with contract
    /// </summary>
    Task SendAdoptionCompletedAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string contractNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends application cancelled email
    /// </summary>
    Task SendApplicationCancelledAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends visit reminder email
    /// </summary>
    Task SendVisitReminderAsync(
        string recipientEmail,
        string recipientName,
        string animalName,
        DateTime visitDate,
        CancellationToken cancellationToken = default);

    #region Volunteer Emails

    /// <summary>
    /// Sends volunteer application confirmation email
    /// </summary>
    Task SendVolunteerApplicationConfirmationAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends volunteer application approval email
    /// </summary>
    Task SendVolunteerApprovalNotificationAsync(
        string recipientEmail,
        string recipientName,
        DateTime trainingStartDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends volunteer activation email
    /// </summary>
    Task SendVolunteerActivationNotificationAsync(
        string recipientEmail,
        string recipientName,
        string contractNumber,
        CancellationToken cancellationToken = default);

    #endregion

    #region Auth Emails

    /// <summary>
    /// Sends password reset link email
    /// </summary>
    Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends welcome email after registration
    /// </summary>
    Task SendWelcomeEmailAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default);

    #endregion
}
