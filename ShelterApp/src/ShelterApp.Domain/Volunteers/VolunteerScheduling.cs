using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers.Events;

namespace ShelterApp.Domain.Volunteers;

#region Enums

/// <summary>
/// Volunteer slot assignment status
/// </summary>
public enum AssignmentStatus
{
    /// <summary>Awaiting confirmation</summary>
    Pending = 0,

    /// <summary>Confirmed</summary>
    Confirmed = 1,

    /// <summary>Cancelled</summary>
    Cancelled = 2
}

#endregion

#region ScheduleSlot

/// <summary>
/// Time slot in the volunteer schedule
/// </summary>
public class ScheduleSlot : Entity<Guid>
{
    private readonly List<VolunteerAssignment> _assignments = new();

    /// <summary>
    /// Slot date
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
    /// Maximum number of volunteers
    /// </summary>
    public int MaxVolunteers { get; private set; }

    /// <summary>
    /// Slot description (e.g. "Morning walks", "Feeding")
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// ID of the user who created the slot
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Whether the slot is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Volunteer assignments
    /// </summary>
    public IReadOnlyCollection<VolunteerAssignment> Assignments => _assignments.AsReadOnly();

    /// <summary>
    /// Current number of confirmed volunteers
    /// </summary>
    public int CurrentVolunteers => _assignments.Count(a => a.Status == AssignmentStatus.Confirmed);

    /// <summary>
    /// Whether there are available spots
    /// </summary>
    public bool HasAvailableSpots => CurrentVolunteers < MaxVolunteers;

    /// <summary>
    /// Slot duration in hours
    /// </summary>
    public decimal DurationHours => (decimal)(EndTime - StartTime).TotalHours;

    private ScheduleSlot() { }

    /// <summary>
    /// Creates a new time slot
    /// </summary>
    public static Result<ScheduleSlot> Create(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int maxVolunteers,
        string description,
        Guid createdByUserId)
    {
        if (endTime <= startTime)
        {
            return Result.Failure<ScheduleSlot>(
                Error.Validation("Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia"));
        }

        if (maxVolunteers < 1 || maxVolunteers > 20)
        {
            return Result.Failure<ScheduleSlot>(
                Error.Validation("Maksymalna liczba wolontariuszy musi być między 1 a 20"));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure<ScheduleSlot>(
                Error.Validation("Opis slotu jest wymagany"));
        }

        var slot = new ScheduleSlot
        {
            Id = Guid.NewGuid(),
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            MaxVolunteers = maxVolunteers,
            Description = description.Trim(),
            CreatedByUserId = createdByUserId,
            IsActive = true
        };

        return Result.Success(slot);
    }

    /// <summary>
    /// Assigns a volunteer to the slot
    /// </summary>
    public Result<VolunteerAssignment> AssignVolunteer(Guid volunteerId, Guid assignedByUserId)
    {
        if (!IsActive)
        {
            return Result.Failure<VolunteerAssignment>(
                Error.Validation("Slot jest nieaktywny"));
        }

        if (!HasAvailableSpots)
        {
            return Result.Failure<VolunteerAssignment>(
                Error.Validation("Brak wolnych miejsc w tym slocie"));
        }

        // Check if the volunteer is already assigned
        var existingAssignment = _assignments.FirstOrDefault(a =>
            a.VolunteerId == volunteerId && a.Status != AssignmentStatus.Cancelled);

        if (existingAssignment is not null)
        {
            return Result.Failure<VolunteerAssignment>(
                Error.Validation("Wolontariusz jest już przypisany do tego slotu"));
        }

        var assignment = VolunteerAssignment.Create(Id, volunteerId, assignedByUserId);
        _assignments.Add(assignment);
        SetUpdatedAt();

        return Result.Success(assignment);
    }

    /// <summary>
    /// Updates the slot
    /// </summary>
    public Result Update(
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        int? maxVolunteers = null,
        string? description = null)
    {
        var newStartTime = startTime ?? StartTime;
        var newEndTime = endTime ?? EndTime;

        if (newEndTime <= newStartTime)
        {
            return Result.Failure(
                Error.Validation("Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia"));
        }

        if (maxVolunteers.HasValue)
        {
            if (maxVolunteers.Value < 1 || maxVolunteers.Value > 20)
            {
                return Result.Failure(
                    Error.Validation("Maksymalna liczba wolontariuszy musi być między 1 a 20"));
            }

            // Cannot reduce below the current number of confirmed volunteers
            if (maxVolunteers.Value < CurrentVolunteers)
            {
                return Result.Failure(
                    Error.Validation($"Nie można ustawić maksimum mniejszego niż aktualna liczba wolontariuszy ({CurrentVolunteers})"));
            }

            MaxVolunteers = maxVolunteers.Value;
        }

        StartTime = newStartTime;
        EndTime = newEndTime;

        if (!string.IsNullOrWhiteSpace(description))
        {
            Description = description.Trim();
        }

        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Deactivates the slot
    /// </summary>
    public Result Deactivate(string reason)
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Validation("Slot jest już nieaktywny"));
        }

        // Cancel all pending assignments
        foreach (var assignment in _assignments.Where(a => a.Status == AssignmentStatus.Pending))
        {
            assignment.Cancel("Slot został dezaktywowany: " + reason);
        }

        IsActive = false;
        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Reactivates the slot
    /// </summary>
    public Result Activate()
    {
        if (IsActive)
        {
            return Result.Failure(Error.Validation("Slot jest już aktywny"));
        }

        IsActive = true;
        SetUpdatedAt();
        return Result.Success();
    }
}

#endregion

#region VolunteerAssignment

/// <summary>
/// Volunteer assignment to a time slot
/// </summary>
public class VolunteerAssignment : Entity<Guid>
{
    /// <summary>
    /// Time slot ID
    /// </summary>
    public Guid ScheduleSlotId { get; private set; }

    /// <summary>
    /// Volunteer ID
    /// </summary>
    public Guid VolunteerId { get; private set; }

    /// <summary>
    /// ID of the user who assigned the volunteer
    /// </summary>
    public Guid AssignedByUserId { get; private set; }

    /// <summary>
    /// Assignment status
    /// </summary>
    public AssignmentStatus Status { get; private set; }

    /// <summary>
    /// Assignment date
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// Confirmation date
    /// </summary>
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>
    /// Cancellation date
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// Cancellation reason
    /// </summary>
    public string? CancellationReason { get; private set; }

    private VolunteerAssignment() { }

    internal static VolunteerAssignment Create(
        Guid scheduleSlotId,
        Guid volunteerId,
        Guid assignedByUserId)
    {
        return new VolunteerAssignment
        {
            Id = Guid.NewGuid(),
            ScheduleSlotId = scheduleSlotId,
            VolunteerId = volunteerId,
            AssignedByUserId = assignedByUserId,
            Status = AssignmentStatus.Pending,
            AssignedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Confirms the assignment
    /// </summary>
    public Result Confirm()
    {
        if (Status != AssignmentStatus.Pending)
        {
            return Result.Failure(
                Error.Validation($"Nie można potwierdzić przypisania w statusie '{Status}'"));
        }

        Status = AssignmentStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Cancels the assignment
    /// </summary>
    public Result Cancel(string reason)
    {
        if (Status == AssignmentStatus.Cancelled)
        {
            return Result.Failure(Error.Validation("Przypisanie jest już anulowane"));
        }

        Status = AssignmentStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        SetUpdatedAt();

        return Result.Success();
    }
}

#endregion

#region Attendance

/// <summary>
/// Volunteer attendance record (WB-18)
/// </summary>
public class Attendance : Entity<Guid>
{
    /// <summary>
    /// Volunteer ID
    /// </summary>
    public Guid VolunteerId { get; private set; }

    /// <summary>
    /// Time slot ID (optional - work may be outside schedule)
    /// </summary>
    public Guid? ScheduleSlotId { get; private set; }

    /// <summary>
    /// Check-in time
    /// </summary>
    public DateTime CheckInTime { get; private set; }

    /// <summary>
    /// Check-out time
    /// </summary>
    public DateTime? CheckOutTime { get; private set; }

    /// <summary>
    /// Hours worked (calculated)
    /// </summary>
    public decimal? HoursWorked => CheckOutTime.HasValue
        ? Math.Round((decimal)(CheckOutTime.Value - CheckInTime).TotalHours, 2)
        : null;

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Work description
    /// </summary>
    public string? WorkDescription { get; private set; }

    /// <summary>
    /// ID of the user who approved
    /// </summary>
    public Guid? ApprovedByUserId { get; private set; }

    /// <summary>
    /// Approval date
    /// </summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>
    /// Whether the attendance has been approved
    /// </summary>
    public bool IsApproved => ApprovedAt.HasValue;

    /// <summary>
    /// Whether the volunteer is still checked in
    /// </summary>
    public bool IsCheckedIn => !CheckOutTime.HasValue;

    private Attendance() { }

    /// <summary>
    /// Creates a new attendance record (check-in)
    /// </summary>
    public static Result<Attendance> CheckIn(
        Guid volunteerId,
        Guid? scheduleSlotId = null,
        string? notes = null,
        DateTime? checkInTime = null)
    {
        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            VolunteerId = volunteerId,
            ScheduleSlotId = scheduleSlotId,
            CheckInTime = checkInTime ?? DateTime.UtcNow,
            Notes = notes
        };

        return Result.Success(attendance);
    }

    /// <summary>
    /// Checks out the volunteer
    /// </summary>
    public Result CheckOut(string? workDescription = null, DateTime? checkOutTime = null)
    {
        if (CheckOutTime.HasValue)
        {
            return Result.Failure(Error.Validation("Wolontariusz jest już wymeldowany"));
        }

        var outTime = checkOutTime ?? DateTime.UtcNow;

        if (outTime < CheckInTime)
        {
            return Result.Failure(
                Error.Validation("Czas wymeldowania nie może być wcześniejszy niż czas zameldowania"));
        }

        CheckOutTime = outTime;
        WorkDescription = workDescription;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Approves the attendance record
    /// </summary>
    public Result Approve(Guid approvedByUserId)
    {
        if (!CheckOutTime.HasValue)
        {
            return Result.Failure(
                Error.Validation("Nie można zatwierdzić obecności bez wymeldowania"));
        }

        if (IsApproved)
        {
            return Result.Failure(Error.Validation("Obecność jest już zatwierdzona"));
        }

        ApprovedByUserId = approvedByUserId;
        ApprovedAt = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Corrects check-in/check-out times
    /// </summary>
    public Result CorrectTimes(
        DateTime? checkInTime = null,
        DateTime? checkOutTime = null,
        string? correctionNotes = null)
    {
        if (IsApproved)
        {
            return Result.Failure(
                Error.Validation("Nie można korygować zatwierdzonej obecności"));
        }

        var newCheckIn = checkInTime ?? CheckInTime;
        var newCheckOut = checkOutTime ?? CheckOutTime;

        if (newCheckOut.HasValue && newCheckOut < newCheckIn)
        {
            return Result.Failure(
                Error.Validation("Czas wymeldowania nie może być wcześniejszy niż czas zameldowania"));
        }

        CheckInTime = newCheckIn;
        CheckOutTime = newCheckOut;

        if (!string.IsNullOrWhiteSpace(correctionNotes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? $"[Korekta] {correctionNotes}"
                : $"{Notes}\n[Korekta] {correctionNotes}";
        }

        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Updates notes
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        SetUpdatedAt();
    }

    /// <summary>
    /// Updates the work description
    /// </summary>
    public void UpdateWorkDescription(string? workDescription)
    {
        WorkDescription = workDescription;
        SetUpdatedAt();
    }
}

#endregion
