using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Appointments.Shared;
using ShelterApp.Domain.Appointments;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Appointments.Commands;

// ============================================
// Command - Create Visit Slot
// ============================================
/// <summary>
/// Tworzy nowy slot czasowy na wizytę
/// </summary>
public record CreateVisitSlotCommand(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    string? Notes
) : ICommand<Result<VisitSlotDto>>;

// ============================================
// Handler - Create Visit Slot
// ============================================
public class CreateVisitSlotHandler
    : ICommandHandler<CreateVisitSlotCommand, Result<VisitSlotDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CreateVisitSlotHandler> _logger;

    public CreateVisitSlotHandler(
        ShelterDbContext context,
        ILogger<CreateVisitSlotHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<VisitSlotDto>> Handle(
        CreateVisitSlotCommand request,
        CancellationToken cancellationToken)
    {
        // Sprawdź czy nie ma nakładających się slotów
        var overlapping = await _context.VisitSlots
            .AnyAsync(s =>
                s.Date == request.Date &&
                s.IsActive &&
                ((request.StartTime >= s.StartTime && request.StartTime < s.EndTime) ||
                 (request.EndTime > s.StartTime && request.EndTime <= s.EndTime) ||
                 (request.StartTime <= s.StartTime && request.EndTime >= s.EndTime)),
                cancellationToken);

        if (overlapping)
        {
            return Result.Failure<VisitSlotDto>(
                Error.Validation("W tym czasie istnieje już inny slot"));
        }

        var slotResult = VisitSlot.Create(
            request.Date,
            request.StartTime,
            request.EndTime,
            request.MaxCapacity,
            request.Notes);

        if (slotResult.IsFailure)
        {
            return Result.Failure<VisitSlotDto>(slotResult.Error);
        }

        var slot = slotResult.Value;
        _context.VisitSlots.Add(slot);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Visit slot created: {SlotId} on {Date} at {StartTime}-{EndTime}",
            slot.Id, slot.Date, slot.StartTime, slot.EndTime);

        return Result.Success(slot.ToDto());
    }
}

// ============================================
// Validator - Create Visit Slot
// ============================================
public class CreateVisitSlotValidator : AbstractValidator<CreateVisitSlotCommand>
{
    public CreateVisitSlotValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Data jest wymagana")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Data nie może być w przeszłości");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Godzina rozpoczęcia jest wymagana");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("Godzina zakończenia jest wymagana")
            .GreaterThan(x => x.StartTime)
                .WithMessage("Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia");

        RuleFor(x => x.MaxCapacity)
            .InclusiveBetween(1, 10)
                .WithMessage("Maksymalna pojemność musi być między 1 a 10");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notatki nie mogą przekraczać 500 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

// ============================================
// Command - Create Multiple Slots (Bulk)
// ============================================
/// <summary>
/// Tworzy wiele slotów czasowych naraz (np. na cały tydzień)
/// </summary>
public record CreateVisitSlotsBulkCommand(
    DateOnly FromDate,
    DateOnly ToDate,
    IEnumerable<SlotTemplate> Templates
) : ICommand<Result<List<VisitSlotDto>>>;

public record SlotTemplate(
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    DayOfWeek[]? DaysOfWeek // null = wszystkie dni
);

// ============================================
// Handler - Create Multiple Slots
// ============================================
public class CreateVisitSlotsBulkHandler
    : ICommandHandler<CreateVisitSlotsBulkCommand, Result<List<VisitSlotDto>>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CreateVisitSlotsBulkHandler> _logger;

    public CreateVisitSlotsBulkHandler(
        ShelterDbContext context,
        ILogger<CreateVisitSlotsBulkHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<VisitSlotDto>>> Handle(
        CreateVisitSlotsBulkCommand request,
        CancellationToken cancellationToken)
    {
        var createdSlots = new List<VisitSlot>();
        var currentDate = request.FromDate;

        while (currentDate <= request.ToDate)
        {
            foreach (var template in request.Templates)
            {
                // Sprawdź czy ten dzień tygodnia jest w szablonie
                if (template.DaysOfWeek is not null &&
                    !template.DaysOfWeek.Contains(currentDate.DayOfWeek))
                {
                    continue;
                }

                // Sprawdź czy nie ma nakładających się slotów
                var overlapping = await _context.VisitSlots
                    .AnyAsync(s =>
                        s.Date == currentDate &&
                        s.IsActive &&
                        ((template.StartTime >= s.StartTime && template.StartTime < s.EndTime) ||
                         (template.EndTime > s.StartTime && template.EndTime <= s.EndTime)),
                        cancellationToken);

                if (overlapping)
                {
                    continue; // Pomiń nakładające się sloty
                }

                var slotResult = VisitSlot.Create(
                    currentDate,
                    template.StartTime,
                    template.EndTime,
                    template.MaxCapacity);

                if (slotResult.IsSuccess)
                {
                    createdSlots.Add(slotResult.Value);
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        if (createdSlots.Count > 0)
        {
            _context.VisitSlots.AddRange(createdSlots);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Created {Count} visit slots from {FromDate} to {ToDate}",
            createdSlots.Count, request.FromDate, request.ToDate);

        return Result.Success(createdSlots.Select(s => s.ToDto()).ToList());
    }
}

// ============================================
// Validator - Create Multiple Slots
// ============================================
public class CreateVisitSlotsBulkValidator : AbstractValidator<CreateVisitSlotsBulkCommand>
{
    public CreateVisitSlotsBulkValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("Data początkowa jest wymagana")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Data początkowa nie może być w przeszłości");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("Data końcowa jest wymagana")
            .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("Data końcowa musi być równa lub późniejsza niż data początkowa");

        RuleFor(x => x.Templates)
            .NotEmpty().WithMessage("Wymagany jest co najmniej jeden szablon slotu");

        RuleForEach(x => x.Templates).ChildRules(template =>
        {
            template.RuleFor(t => t.EndTime)
                .GreaterThan(t => t.StartTime)
                .WithMessage("Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia");

            template.RuleFor(t => t.MaxCapacity)
                .InclusiveBetween(1, 10)
                .WithMessage("Maksymalna pojemność musi być między 1 a 10");
        });
    }
}

// ============================================
// Command - Cancel Booking
// ============================================
/// <summary>
/// Anuluje rezerwację wizyty
/// </summary>
public record CancelBookingCommand(
    Guid BookingId,
    string CancelledBy,
    string Reason
) : ICommand<Result>;

// ============================================
// Handler - Cancel Booking
// ============================================
public class CancelBookingHandler
    : ICommandHandler<CancelBookingCommand, Result>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CancelBookingHandler> _logger;

    public CancelBookingHandler(
        ShelterDbContext context,
        ILogger<CancelBookingHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CancelBookingCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await _context.VisitBookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(Error.NotFound("VisitBooking", request.BookingId));
        }

        var slot = await _context.VisitSlots
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == booking.SlotId, cancellationToken);

        if (slot is null)
        {
            return Result.Failure(Error.NotFound("VisitSlot", booking.SlotId));
        }

        var result = slot.CancelBooking(request.BookingId, request.CancelledBy, request.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingId} cancelled by {CancelledBy}. Reason: {Reason}",
            request.BookingId, request.CancelledBy, request.Reason);

        return Result.Success();
    }
}

// ============================================
// Validator - Cancel Booking
// ============================================
public class CancelBookingValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("ID rezerwacji jest wymagane");

        RuleFor(x => x.CancelledBy)
            .NotEmpty().WithMessage("Nazwa osoby anulującej jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa nie może przekraczać 200 znaków");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód anulowania jest wymagany")
            .MaximumLength(500).WithMessage("Powód nie może przekraczać 500 znaków");
    }
}

// ============================================
// Command - Deactivate Slot
// ============================================
/// <summary>
/// Dezaktywuje slot (anuluje wszystkie rezerwacje)
/// </summary>
public record DeactivateSlotCommand(
    Guid SlotId,
    string DeactivatedBy,
    string Reason
) : ICommand<Result>;

// ============================================
// Handler - Deactivate Slot
// ============================================
public class DeactivateSlotHandler
    : ICommandHandler<DeactivateSlotCommand, Result>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<DeactivateSlotHandler> _logger;

    public DeactivateSlotHandler(
        ShelterDbContext context,
        ILogger<DeactivateSlotHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeactivateSlotCommand request,
        CancellationToken cancellationToken)
    {
        var slot = await _context.VisitSlots
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == request.SlotId, cancellationToken);

        if (slot is null)
        {
            return Result.Failure(Error.NotFound("VisitSlot", request.SlotId));
        }

        var result = slot.Deactivate(request.DeactivatedBy, request.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Slot {SlotId} deactivated by {DeactivatedBy}. Reason: {Reason}",
            request.SlotId, request.DeactivatedBy, request.Reason);

        return Result.Success();
    }
}

// ============================================
// Validator - Deactivate Slot
// ============================================
public class DeactivateSlotValidator : AbstractValidator<DeactivateSlotCommand>
{
    public DeactivateSlotValidator()
    {
        RuleFor(x => x.SlotId)
            .NotEmpty().WithMessage("ID slotu jest wymagane");

        RuleFor(x => x.DeactivatedBy)
            .NotEmpty().WithMessage("Nazwa osoby dezaktywującej jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa nie może przekraczać 200 znaków");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód dezaktywacji jest wymagany")
            .MaximumLength(500).WithMessage("Powód nie może przekraczać 500 znaków");
    }
}
