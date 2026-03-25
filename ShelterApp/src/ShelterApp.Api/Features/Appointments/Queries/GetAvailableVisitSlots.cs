using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Appointments.Shared;
using ShelterApp.Domain.Appointments;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Appointments.Queries;

// ============================================
// Query
// ============================================
/// <summary>
/// Pobiera dostępne sloty wizyt w podanym zakresie dat (WS-15)
/// </summary>
public record GetAvailableVisitSlotsQuery(
    DateTime FromDate,
    DateTime ToDate,
    bool IncludeFullSlots = false
) : IQuery<Result<List<DailyAvailabilityDto>>>;

// ============================================
// Handler
// ============================================
public class GetAvailableVisitSlotsHandler
    : IQueryHandler<GetAvailableVisitSlotsQuery, Result<List<DailyAvailabilityDto>>>
{
    private readonly ShelterDbContext _context;

    public GetAvailableVisitSlotsHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<DailyAvailabilityDto>>> Handle(
        GetAvailableVisitSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = DateOnly.FromDateTime(request.FromDate);
        var toDate = DateOnly.FromDateTime(request.ToDate);

        // Pobierz wszystkie aktywne sloty w zakresie dat
        var slots = await _context.VisitSlots
            .Include(s => s.Bookings)
            .Where(s => s.Date >= fromDate && s.Date <= toDate && s.IsActive)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Grupuj po dacie
        var dailyAvailability = slots
            .GroupBy(s => s.Date)
            .Select(g => new DailyAvailabilityDto(
                Date: g.Key,
                TotalSlots: g.Count(),
                AvailableSlots: g.Count(s => s.IsAvailable),
                TotalCapacity: g.Sum(s => s.MaxCapacity),
                RemainingCapacity: g.Sum(s => s.RemainingCapacity),
                Slots: request.IncludeFullSlots
                    ? g.Select(s => s.ToDto())
                    : g.Where(s => s.IsAvailable).Select(s => s.ToDto())
            ))
            .OrderBy(d => d.Date)
            .ToList();

        return Result.Success(dailyAvailability);
    }
}

// ============================================
// Validator
// ============================================
public class GetAvailableVisitSlotsValidator : AbstractValidator<GetAvailableVisitSlotsQuery>
{
    public GetAvailableVisitSlotsValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("Data początkowa jest wymagana")
            .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Data początkowa nie może być w przeszłości");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("Data końcowa jest wymagana")
            .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("Data końcowa musi być równa lub późniejsza niż data początkowa");

        RuleFor(x => x)
            .Must(x => (x.ToDate - x.FromDate).TotalDays <= 90)
            .WithMessage("Maksymalny zakres dat to 90 dni");
    }
}
