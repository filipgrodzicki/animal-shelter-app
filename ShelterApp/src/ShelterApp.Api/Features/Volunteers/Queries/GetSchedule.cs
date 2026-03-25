using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Queries;

// ============================================
// Query - Get Schedule (WB-17)
// ============================================
public record GetScheduleQuery(
    DateTime FromDate,
    DateTime ToDate,
    bool? ActiveOnly = true,
    bool IncludeAssignments = true,
    Guid? VolunteerId = null
) : IQuery<Result<List<ScheduleSlotDto>>>;

// ============================================
// Handler
// ============================================
public class GetScheduleHandler : IQueryHandler<GetScheduleQuery, Result<List<ScheduleSlotDto>>>
{
    private readonly ShelterDbContext _context;

    public GetScheduleHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ScheduleSlotDto>>> Handle(
        GetScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = DateOnly.FromDateTime(request.FromDate);
        var toDate = DateOnly.FromDateTime(request.ToDate);

        var query = _context.ScheduleSlots
            .AsNoTracking()
            .Where(s => s.Date >= fromDate && s.Date <= toDate);

        // Active only filter
        if (request.ActiveOnly == true)
        {
            query = query.Where(s => s.IsActive);
        }

        // Include assignments if requested
        if (request.IncludeAssignments)
        {
            query = query.Include(s => s.Assignments);
        }

        // Filter by volunteer if specified
        if (request.VolunteerId.HasValue)
        {
            query = query.Where(s =>
                s.Assignments.Any(a => a.VolunteerId == request.VolunteerId.Value));
        }

        var slots = await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        // Get volunteer names for assignments
        var volunteerIds = slots
            .SelectMany(s => s.Assignments)
            .Select(a => a.VolunteerId)
            .Distinct()
            .ToList();

        var volunteerNames = await _context.Volunteers
            .Where(v => volunteerIds.Contains(v.Id))
            .AsNoTracking()
            .ToDictionaryAsync(v => v.Id, v => v.FullName, cancellationToken);

        var result = slots.Select(slot =>
        {
            var assignments = request.IncludeAssignments
                ? slot.Assignments
                    .Select(a => new VolunteerAssignmentDto(
                        Id: a.Id,
                        ScheduleSlotId: a.ScheduleSlotId,
                        VolunteerId: a.VolunteerId,
                        VolunteerName: volunteerNames.GetValueOrDefault(a.VolunteerId, ""),
                        Status: a.Status.ToString(),
                        AssignedAt: a.AssignedAt,
                        ConfirmedAt: a.ConfirmedAt,
                        CancelledAt: a.CancelledAt,
                        CancellationReason: a.CancellationReason
                    ))
                    .ToList()
                : null;

            return new ScheduleSlotDto(
                Id: slot.Id,
                Date: slot.Date,
                StartTime: slot.StartTime,
                EndTime: slot.EndTime,
                MaxVolunteers: slot.MaxVolunteers,
                CurrentVolunteers: slot.CurrentVolunteers,
                HasAvailableSpots: slot.HasAvailableSpots,
                Description: slot.Description,
                IsActive: slot.IsActive,
                Assignments: assignments,
                CreatedAt: slot.CreatedAt
            );
        }).ToList();

        return Result.Success(result);
    }
}

// ============================================
// Validator
// ============================================
public class GetScheduleValidator : AbstractValidator<GetScheduleQuery>
{
    public GetScheduleValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("Data początkowa jest wymagana");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("Data końcowa jest wymagana")
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("Data końcowa musi być większa lub równa dacie początkowej");

        RuleFor(x => x)
            .Must(x => (x.ToDate - x.FromDate).TotalDays <= 90)
            .WithMessage("Zakres dat nie może przekraczać 90 dni");
    }
}

// ============================================
// Query - Get My Schedule (for volunteer)
// ============================================
public record GetMyScheduleQuery(
    Guid VolunteerId,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IQuery<Result<List<VolunteerScheduleItemDto>>>;

// ============================================
// DTO for volunteer's schedule view
// ============================================
public record VolunteerScheduleItemDto(
    Guid SlotId,
    Guid AssignmentId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Description,
    string AssignmentStatus,
    DateTime? ConfirmedAt,
    bool HasAttendance,
    decimal? HoursWorked
);

// ============================================
// Handler - Get My Schedule
// ============================================
public class GetMyScheduleHandler : IQueryHandler<GetMyScheduleQuery, Result<List<VolunteerScheduleItemDto>>>
{
    private readonly ShelterDbContext _context;

    public GetMyScheduleHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<VolunteerScheduleItemDto>>> Handle(
        GetMyScheduleQuery request,
        CancellationToken cancellationToken)
    {
        // Verify volunteer exists
        var volunteerExists = await _context.Volunteers
            .AnyAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (!volunteerExists)
        {
            return Result.Failure<List<VolunteerScheduleItemDto>>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        var fromDate = request.FromDate.HasValue
            ? DateOnly.FromDateTime(request.FromDate.Value)
            : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

        var toDate = request.ToDate.HasValue
            ? DateOnly.FromDateTime(request.ToDate.Value)
            : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        // Get assignments for this volunteer
        var assignments = await _context.VolunteerAssignments
            .Where(a => a.VolunteerId == request.VolunteerId)
            .Where(a => a.Status != AssignmentStatus.Cancelled)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var slotIds = assignments.Select(a => a.ScheduleSlotId).Distinct().ToList();

        // Get schedule slots for these assignments
        var slots = await _context.ScheduleSlots
            .Where(s => slotIds.Contains(s.Id))
            .Where(s => s.Date >= fromDate && s.Date <= toDate)
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        // Filter assignments to only those with slots in the date range
        var filteredAssignments = assignments
            .Where(a => slots.ContainsKey(a.ScheduleSlotId))
            .ToList();

        // Get attendances for these slots
        var attendances = await _context.Attendances
            .Where(a => a.VolunteerId == request.VolunteerId)
            .Where(a => a.ScheduleSlotId.HasValue && slotIds.Contains(a.ScheduleSlotId.Value))
            .AsNoTracking()
            .ToDictionaryAsync(a => a.ScheduleSlotId!.Value, cancellationToken);

        var result = filteredAssignments
            .Select(a =>
            {
                var slot = slots[a.ScheduleSlotId];
                var hasAttendance = attendances.TryGetValue(a.ScheduleSlotId, out var attendance);
                return new VolunteerScheduleItemDto(
                    SlotId: a.ScheduleSlotId,
                    AssignmentId: a.Id,
                    Date: slot.Date,
                    StartTime: slot.StartTime,
                    EndTime: slot.EndTime,
                    Description: slot.Description,
                    AssignmentStatus: a.Status.ToString(),
                    ConfirmedAt: a.ConfirmedAt,
                    HasAttendance: hasAttendance,
                    HoursWorked: hasAttendance ? attendance!.HoursWorked : null
                );
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartTime)
            .ToList();

        return Result.Success(result);
    }
}

// ============================================
// Validator - Get My Schedule
// ============================================
public class GetMyScheduleValidator : AbstractValidator<GetMyScheduleQuery>
{
    public GetMyScheduleValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("Data końcowa musi być większa lub równa dacie początkowej");

            RuleFor(x => x)
                .Must(x => (x.ToDate!.Value - x.FromDate!.Value).TotalDays <= 365)
                .WithMessage("Zakres dat nie może przekraczać 365 dni");
        });
    }
}
