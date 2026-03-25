using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Common;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Notifications;
using ShelterApp.Infrastructure.Persistence;
using System.Security.Claims;

namespace ShelterApp.Api.Features.Notifications;

/// <summary>
/// Panel powiadomień dla administratora (WF-30)
/// </summary>
[Route("api/notifications")]
[Produces("application/json")]
[ApiController]
[Authorize(Roles = "Admin,Staff")]
public class NotificationsController : ApiController
{
    private readonly ShelterDbContext _context;

    public NotificationsController(ShelterDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Pobiera listę powiadomień dla panelu admina
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationsResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] string? type = null,
        [FromQuery] string? priority = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AdminNotifications
            .Where(n => !n.IsDismissed)
            .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt > DateTime.UtcNow);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<NotificationType>(type, out var notifType))
            query = query.Where(n => n.Type == notifType);

        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<NotificationPriority>(priority, out var notifPriority))
            query = query.Where(n => n.Priority == notifPriority);

        var totalCount = await query.CountAsync(cancellationToken);
        var unreadCount = await _context.AdminNotifications
            .CountAsync(n => !n.IsRead && !n.IsDismissed, cancellationToken);

        var notifications = await query
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type.ToString(),
                n.Priority.ToString(),
                n.Title,
                n.Message,
                n.Link,
                n.RelatedEntityId,
                n.RelatedEntityType,
                n.IsRead,
                n.ReadAt,
                n.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(new NotificationsResultDto(
            notifications,
            unreadCount,
            totalCount,
            page,
            pageSize
        ));
    }

    /// <summary>
    /// Pobiera liczbę nieprzeczytanych powiadomień (dla badge)
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _context.AdminNotifications
            .CountAsync(n => !n.IsRead && !n.IsDismissed &&
                (!n.ExpiresAt.HasValue || n.ExpiresAt > DateTime.UtcNow),
                cancellationToken);

        var urgentCount = await _context.AdminNotifications
            .CountAsync(n => !n.IsRead && !n.IsDismissed &&
                n.Priority == NotificationPriority.Urgent &&
                (!n.ExpiresAt.HasValue || n.ExpiresAt > DateTime.UtcNow),
                cancellationToken);

        return Ok(new UnreadCountDto(count, urgentCount));
    }

    /// <summary>
    /// Oznacza powiadomienie jako przeczytane
    /// </summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _context.AdminNotifications.FindAsync(new object[] { id }, cancellationToken);

        if (notification is null)
            return NotFound();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        notification.MarkAsRead(userId);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Oznacza wszystkie powiadomienia jako przeczytane
    /// </summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

        var unreadNotifications = await _context.AdminNotifications
            .Where(n => !n.IsRead && !n.IsDismissed)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead(userId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { MarkedCount = unreadNotifications.Count });
    }

    /// <summary>
    /// Odrzuca (ukrywa) powiadomienie
    /// </summary>
    [HttpPost("{id:guid}/dismiss")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissNotification(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _context.AdminNotifications.FindAsync(new object[] { id }, cancellationToken);

        if (notification is null)
            return NotFound();

        notification.Dismiss();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok();
    }
}

// ============================================
// DTOs
// ============================================

public record NotificationDto(
    Guid Id,
    string Type,
    string Priority,
    string Title,
    string Message,
    string? Link,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt
);

public record NotificationsResultDto(
    List<NotificationDto> Items,
    int UnreadCount,
    int TotalCount,
    int Page,
    int PageSize
);

public record UnreadCountDto(
    int Total,
    int Urgent
);
