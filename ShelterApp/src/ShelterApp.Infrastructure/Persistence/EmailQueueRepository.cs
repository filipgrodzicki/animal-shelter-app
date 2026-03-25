using Microsoft.EntityFrameworkCore;
using ShelterApp.Domain.Emails;

namespace ShelterApp.Infrastructure.Persistence;

public class EmailQueueRepository : IEmailQueueRepository
{
    private readonly ShelterDbContext _context;

    public EmailQueueRepository(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<EmailQueue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmailQueue
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailQueue>> GetPendingEmailsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.EmailQueue
            .Where(e => e.Status == EmailStatus.Pending)
            .Where(e => e.ScheduledAt == null || e.ScheduledAt <= DateTime.UtcNow)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailQueue>> GetScheduledRemindersAsync(DateTime until, CancellationToken cancellationToken = default)
    {
        return await _context.EmailQueue
            .Where(e => e.Status == EmailStatus.Pending)
            .Where(e => e.EmailType == EmailType.VisitReminder)
            .Where(e => e.ScheduledAt != null && e.ScheduledAt <= until)
            .OrderBy(e => e.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmailQueue email, CancellationToken cancellationToken = default)
    {
        await _context.EmailQueue.AddAsync(email, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EmailQueue email, CancellationToken cancellationToken = default)
    {
        _context.EmailQueue.Update(email);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmailQueue
            .CountAsync(e => e.Status == EmailStatus.Failed, cancellationToken);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmailQueue
            .CountAsync(e => e.Status == EmailStatus.Pending, cancellationToken);
    }
}
