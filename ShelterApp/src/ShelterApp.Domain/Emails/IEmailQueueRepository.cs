namespace ShelterApp.Domain.Emails;

/// <summary>
/// Repository for email queue operations
/// </summary>
public interface IEmailQueueRepository
{
    Task<EmailQueue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailQueue>> GetPendingEmailsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailQueue>> GetScheduledRemindersAsync(DateTime until, CancellationToken cancellationToken = default);
    Task AddAsync(EmailQueue email, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailQueue email, CancellationToken cancellationToken = default);
    Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}
