namespace ShelterApp.Domain.Common;

public interface ISmsService
{
    /// <summary>
    /// Sends adoption visit reminder SMS (WF-29)
    /// </summary>
    Task SendVisitReminderAsync(
        string phoneNumber,
        string recipientName,
        string animalName,
        DateTime visitDate,
        string shelterAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a general SMS message
    /// </summary>
    Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends application status change SMS
    /// </summary>
    Task SendApplicationStatusChangeAsync(
        string phoneNumber,
        string recipientName,
        string animalName,
        string newStatus,
        CancellationToken cancellationToken = default);
}
