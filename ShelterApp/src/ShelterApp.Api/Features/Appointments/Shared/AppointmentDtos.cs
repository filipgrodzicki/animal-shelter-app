namespace ShelterApp.Api.Features.Appointments.Shared;

/// <summary>
/// Visit time slot
/// </summary>
public record VisitSlotDto(
    Guid Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    int CurrentBookings,
    int RemainingCapacity,
    bool IsAvailable,
    string? Notes,
    IEnumerable<VisitBookingDto>? Bookings = null
);

/// <summary>
/// Visit booking
/// </summary>
public record VisitBookingDto(
    Guid Id,
    Guid SlotId,
    Guid ApplicationId,
    Guid AdopterId,
    string AdopterName,
    string AnimalName,
    string Status,
    DateTime BookedAt,
    string? CancelledBy,
    string? CancellationReason,
    DateTime? CancelledAt,
    string? AttendanceConfirmedBy,
    DateTime? AttendanceConfirmedAt
);

/// <summary>
/// Daily slot availability summary
/// </summary>
public record DailyAvailabilityDto(
    DateOnly Date,
    int TotalSlots,
    int AvailableSlots,
    int TotalCapacity,
    int RemainingCapacity,
    IEnumerable<VisitSlotDto> Slots
);

/// <summary>
/// Booking result
/// </summary>
public record BookingResultDto(
    Guid BookingId,
    Guid SlotId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string AdopterName,
    string AnimalName,
    string Message
);
