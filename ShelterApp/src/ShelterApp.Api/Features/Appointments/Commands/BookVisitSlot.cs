using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Appointments.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Appointments.Commands;

// ============================================
// Command
// ============================================
/// <summary>
/// Books a visit slot for an adoption appointment
/// </summary>
public record BookVisitSlotCommand(
    Guid SlotId,
    Guid ApplicationId
) : ICommand<Result<BookingResultDto>>;

// ============================================
// Handler
// ============================================
public class BookVisitSlotHandler
    : ICommandHandler<BookVisitSlotCommand, Result<BookingResultDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<BookVisitSlotHandler> _logger;

    public BookVisitSlotHandler(
        ShelterDbContext context,
        ILogger<BookVisitSlotHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<BookingResultDto>> Handle(
        BookVisitSlotCommand request,
        CancellationToken cancellationToken)
    {
        // Fetch the slot
        var slot = await _context.VisitSlots
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == request.SlotId, cancellationToken);

        if (slot is null)
        {
            return Result.Failure<BookingResultDto>(
                Error.NotFound("VisitSlot", request.SlotId));
        }

        // Fetch adoption application with status history
        var application = await _context.AdoptionApplications
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application is null)
        {
            return Result.Failure<BookingResultDto>(
                Error.NotFound("AdoptionApplication", request.ApplicationId));
        }

        // Check application status - must be Accepted
        if (application.Status != AdoptionApplicationStatus.Accepted)
        {
            return Result.Failure<BookingResultDto>(
                Error.Validation($"Rezerwacja wizyty możliwa tylko dla zaakceptowanych zgłoszeń. Aktualny status: {application.Status}"));
        }

        // Check if the application already has a booking
        var existingBooking = await _context.VisitBookings
            .AnyAsync(b =>
                b.ApplicationId == request.ApplicationId &&
                b.Status == Domain.Appointments.BookingStatus.Confirmed,
                cancellationToken);

        if (existingBooking)
        {
            return Result.Failure<BookingResultDto>(
                Error.Validation("To zgłoszenie ma już aktywną rezerwację wizyty"));
        }

        // Fetch adopter and animal data
        var adopter = await _context.Adopters
            .FirstOrDefaultAsync(a => a.Id == application.AdopterId, cancellationToken);

        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        if (adopter is null || animal is null)
        {
            return Result.Failure<BookingResultDto>(
                Error.Validation("Nie znaleziono danych adoptującego lub zwierzęcia"));
        }

        // Book the slot
        var bookingResult = slot.Book(
            request.ApplicationId,
            adopter.Id,
            adopter.FullName,
            animal.Name);

        if (bookingResult.IsFailure)
        {
            return Result.Failure<BookingResultDto>(bookingResult.Error);
        }

        var booking = bookingResult.Value;

        // Explicitly add the booking to DbContext
        _context.VisitBookings.Add(booking);

        // Change application status to VisitScheduled
        var visitDateTime = DateTime.SpecifyKind(slot.Date.ToDateTime(slot.StartTime), DateTimeKind.Utc);
        var scheduleResult = application.ScheduleVisit(visitDateTime, adopter.FullName);

        if (scheduleResult.IsFailure)
        {
            return Result.Failure<BookingResultDto>(scheduleResult.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Visit slot {SlotId} booked for application {ApplicationId}. Booking: {BookingId}",
            slot.Id,
            request.ApplicationId,
            booking.Id);

        return Result.Success(new BookingResultDto(
            BookingId: booking.Id,
            SlotId: slot.Id,
            Date: slot.Date,
            StartTime: slot.StartTime,
            EndTime: slot.EndTime,
            AdopterName: adopter.FullName,
            AnimalName: animal.Name,
            Message: $"Wizyta zarezerwowana na {slot.Date:dd.MM.yyyy} godz. {slot.StartTime:HH:mm}-{slot.EndTime:HH:mm}"
        ));
    }
}

// ============================================
// Validator
// ============================================
public class BookVisitSlotValidator : AbstractValidator<BookVisitSlotCommand>
{
    public BookVisitSlotValidator()
    {
        RuleFor(x => x.SlotId)
            .NotEmpty().WithMessage("ID slotu jest wymagane");

        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia adopcyjnego jest wymagane");
    }
}
