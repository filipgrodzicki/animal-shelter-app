using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Appointments;

/// <summary>
/// Time slot for an adoption visit
/// </summary>
public class VisitSlot : Entity<Guid>
{
    private readonly List<VisitBooking> _bookings = new();

    /// <summary>
    /// Visit date
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Start time
    /// </summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>
    /// End time
    /// </summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>
    /// Maximum number of concurrent visits
    /// </summary>
    public int MaxCapacity { get; private set; }

    /// <summary>
    /// Current number of bookings
    /// </summary>
    public int CurrentBookings => _bookings.Count(b => b.Status == BookingStatus.Confirmed);

    /// <summary>
    /// Whether the slot is available
    /// </summary>
    public bool IsAvailable => CurrentBookings < MaxCapacity && !IsPast && IsActive;

    /// <summary>
    /// Whether the slot is in the past
    /// </summary>
    public bool IsPast => Date < DateOnly.FromDateTime(DateTime.UtcNow) ||
                          (Date == DateOnly.FromDateTime(DateTime.UtcNow) &&
                           StartTime <= TimeOnly.FromDateTime(DateTime.UtcNow));

    /// <summary>
    /// Whether the slot is active (not cancelled)
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Slot notes (e.g. "Cats only")
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Bookings in this slot
    /// </summary>
    public IReadOnlyCollection<VisitBooking> Bookings => _bookings.AsReadOnly();

    private VisitSlot() { }

    /// <summary>
    /// Creates a new time slot
    /// </summary>
    public static Result<VisitSlot> Create(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int maxCapacity,
        string? notes = null)
    {
        if (date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Result.Failure<VisitSlot>(
                Error.Validation("Data slotu nie może być w przeszłości"));
        }

        if (endTime <= startTime)
        {
            return Result.Failure<VisitSlot>(
                Error.Validation("Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia"));
        }

        if (maxCapacity < 1 || maxCapacity > 10)
        {
            return Result.Failure<VisitSlot>(
                Error.Validation("Maksymalna pojemność musi być między 1 a 10"));
        }

        var slot = new VisitSlot
        {
            Id = Guid.NewGuid(),
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            MaxCapacity = maxCapacity,
            Notes = notes,
            IsActive = true
        };

        return Result.Success(slot);
    }

    /// <summary>
    /// Books the slot for an adoption application
    /// </summary>
    public Result<VisitBooking> Book(Guid applicationId, Guid adopterId, string adopterName, string animalName)
    {
        if (!IsAvailable)
        {
            if (IsPast)
            {
                return Result.Failure<VisitBooking>(
                    Error.Validation("Nie można zarezerwować slotu z przeszłości"));
            }

            if (!IsActive)
            {
                return Result.Failure<VisitBooking>(
                    Error.Validation("Ten slot został anulowany"));
            }

            return Result.Failure<VisitBooking>(
                Error.Validation("Brak wolnych miejsc w tym slocie"));
        }

        // Check if this application already has a booking in this slot
        if (_bookings.Any(b => b.ApplicationId == applicationId && b.Status == BookingStatus.Confirmed))
        {
            return Result.Failure<VisitBooking>(
                Error.Validation("To zgłoszenie ma już rezerwację w tym slocie"));
        }

        var booking = VisitBooking.Create(Id, applicationId, adopterId, adopterName, animalName);
        _bookings.Add(booking);

        SetUpdatedAt();

        return Result.Success(booking);
    }

    /// <summary>
    /// Cancels a booking
    /// </summary>
    public Result CancelBooking(Guid bookingId, string cancelledBy, string reason)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return Result.Failure(Error.NotFound("VisitBooking", bookingId));
        }

        var result = booking.Cancel(cancelledBy, reason);
        if (result.IsFailure)
        {
            return result;
        }

        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Confirms booking attendance
    /// </summary>
    public Result ConfirmAttendance(Guid bookingId, string confirmedBy)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return Result.Failure(Error.NotFound("VisitBooking", bookingId));
        }

        var result = booking.ConfirmAttendance(confirmedBy);
        if (result.IsFailure)
        {
            return result;
        }

        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Marks as no-show
    /// </summary>
    public Result MarkNoShow(Guid bookingId, string markedBy)
    {
        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return Result.Failure(Error.NotFound("VisitBooking", bookingId));
        }

        var result = booking.MarkNoShow(markedBy);
        if (result.IsFailure)
        {
            return result;
        }

        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Updates the slot
    /// </summary>
    public Result Update(TimeOnly? startTime, TimeOnly? endTime, int? maxCapacity, string? notes)
    {
        if (startTime.HasValue && endTime.HasValue && endTime.Value <= startTime.Value)
        {
            return Result.Failure(
                Error.Validation("Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia"));
        }

        if (maxCapacity.HasValue)
        {
            if (maxCapacity.Value < 1 || maxCapacity.Value > 10)
            {
                return Result.Failure(
                    Error.Validation("Maksymalna pojemność musi być między 1 a 10"));
            }

            if (maxCapacity.Value < CurrentBookings)
            {
                return Result.Failure(
                    Error.Validation($"Nie można zmniejszyć pojemności poniżej aktualnej liczby rezerwacji ({CurrentBookings})"));
            }

            MaxCapacity = maxCapacity.Value;
        }

        if (startTime.HasValue) StartTime = startTime.Value;
        if (endTime.HasValue) EndTime = endTime.Value;
        if (notes is not null) Notes = notes;

        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Deactivates the slot
    /// </summary>
    public Result Deactivate(string deactivatedBy, string reason)
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Validation("Slot jest już nieaktywny"));
        }

        // Cancel all active bookings
        foreach (var booking in _bookings.Where(b => b.Status == BookingStatus.Confirmed))
        {
            booking.Cancel(deactivatedBy, $"Slot anulowany: {reason}");
        }

        IsActive = false;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Reactivates the slot
    /// </summary>
    public Result Reactivate()
    {
        if (IsActive)
        {
            return Result.Failure(Error.Validation("Slot jest już aktywny"));
        }

        if (IsPast)
        {
            return Result.Failure(Error.Validation("Nie można reaktywować slotu z przeszłości"));
        }

        IsActive = true;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Gets the slot duration
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Gets the remaining capacity
    /// </summary>
    public int RemainingCapacity => Math.Max(0, MaxCapacity - CurrentBookings);
}

/// <summary>
/// Visit booking
/// </summary>
public class VisitBooking : Entity<Guid>
{
    /// <summary>
    /// Slot ID
    /// </summary>
    public Guid SlotId { get; private set; }

    /// <summary>
    /// Adoption application ID
    /// </summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>
    /// Adopter ID
    /// </summary>
    public Guid AdopterId { get; private set; }

    /// <summary>
    /// Adopter name (denormalized for display)
    /// </summary>
    public string AdopterName { get; private set; } = string.Empty;

    /// <summary>
    /// Animal name (denormalized for display)
    /// </summary>
    public string AnimalName { get; private set; } = string.Empty;

    /// <summary>
    /// Booking status
    /// </summary>
    public BookingStatus Status { get; private set; }

    /// <summary>
    /// Booking creation date
    /// </summary>
    public DateTime BookedAt { get; private set; }

    /// <summary>
    /// Who cancelled (if cancelled)
    /// </summary>
    public string? CancelledBy { get; private set; }

    /// <summary>
    /// Cancellation reason
    /// </summary>
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Cancellation date
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// Who confirmed attendance
    /// </summary>
    public string? AttendanceConfirmedBy { get; private set; }

    /// <summary>
    /// Attendance confirmation date
    /// </summary>
    public DateTime? AttendanceConfirmedAt { get; private set; }

    private VisitBooking() { }

    internal static VisitBooking Create(
        Guid slotId,
        Guid applicationId,
        Guid adopterId,
        string adopterName,
        string animalName)
    {
        return new VisitBooking
        {
            Id = Guid.NewGuid(),
            SlotId = slotId,
            ApplicationId = applicationId,
            AdopterId = adopterId,
            AdopterName = adopterName,
            AnimalName = animalName,
            Status = BookingStatus.Confirmed,
            BookedAt = DateTime.UtcNow
        };
    }

    internal Result Cancel(string cancelledBy, string reason)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(Error.Validation($"Nie można anulować rezerwacji w statusie {Status}"));
        }

        Status = BookingStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }

    internal Result ConfirmAttendance(string confirmedBy)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(Error.Validation($"Nie można potwierdzić obecności dla rezerwacji w statusie {Status}"));
        }

        Status = BookingStatus.Attended;
        AttendanceConfirmedBy = confirmedBy;
        AttendanceConfirmedAt = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }

    internal Result MarkNoShow(string markedBy)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(Error.Validation($"Nie można oznaczyć nieobecności dla rezerwacji w statusie {Status}"));
        }

        Status = BookingStatus.NoShow;
        AttendanceConfirmedBy = markedBy;
        AttendanceConfirmedAt = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }
}

/// <summary>
/// Booking status
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Booking confirmed
    /// </summary>
    Confirmed,

    /// <summary>
    /// Booking cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Visit attended
    /// </summary>
    Attended,

    /// <summary>
    /// No-show
    /// </summary>
    NoShow
}
