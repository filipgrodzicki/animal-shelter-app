using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Commands;

// ============================================
// Command - Check In (WB-18)
// ============================================
/// <summary>
/// Records volunteer check-in
/// </summary>
public record CheckInCommand(
    Guid VolunteerId,
    Guid? ScheduleSlotId,
    string? Notes
) : ICommand<Result<CheckInResultDto>>;

// ============================================
// Handler - Check In
// ============================================
public class CheckInHandler
    : ICommandHandler<CheckInCommand, Result<CheckInResultDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CheckInHandler> _logger;

    public CheckInHandler(
        ShelterDbContext context,
        ILogger<CheckInHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CheckInResultDto>> Handle(
        CheckInCommand request,
        CancellationToken cancellationToken)
    {
        // Check if volunteer exists
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<CheckInResultDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        if (volunteer.Status != VolunteerStatus.Active)
        {
            return Result.Failure<CheckInResultDto>(
                Error.Validation($"Wolontariusz musi być aktywny. Aktualny status: {volunteer.Status}"));
        }

        // Check if the volunteer is already checked in
        var existingCheckIn = await _context.Attendances
            .AnyAsync(a =>
                a.VolunteerId == request.VolunteerId &&
                a.CheckOutTime == null,
                cancellationToken);

        if (existingCheckIn)
        {
            return Result.Failure<CheckInResultDto>(
                Error.Validation("Wolontariusz jest już zameldowany. Najpierw wymelduj się."));
        }

        // If a slot was provided, check if it exists
        if (request.ScheduleSlotId.HasValue)
        {
            var slot = await _context.ScheduleSlots
                .FirstOrDefaultAsync(s => s.Id == request.ScheduleSlotId.Value, cancellationToken);

            if (slot is null)
            {
                return Result.Failure<CheckInResultDto>(
                    Error.NotFound("ScheduleSlot", request.ScheduleSlotId.Value));
            }

            if (!slot.IsActive)
            {
                return Result.Failure<CheckInResultDto>(
                    Error.Validation("Slot jest nieaktywny"));
            }
        }

        // Create attendance record
        var attendanceResult = Attendance.CheckIn(
            request.VolunteerId,
            request.ScheduleSlotId,
            request.Notes);

        if (attendanceResult.IsFailure)
        {
            return Result.Failure<CheckInResultDto>(attendanceResult.Error);
        }

        var attendance = attendanceResult.Value;
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Volunteer {VolunteerId} checked in at {CheckInTime}",
            request.VolunteerId, attendance.CheckInTime);

        return Result.Success(new CheckInResultDto(
            AttendanceId: attendance.Id,
            VolunteerId: attendance.VolunteerId,
            CheckInTime: attendance.CheckInTime,
            Message: $"Zameldowano pomyślnie o {attendance.CheckInTime:HH:mm}"
        ));
    }
}

// ============================================
// Validator - Check In
// ============================================
public class CheckInValidator : AbstractValidator<CheckInCommand>
{
    public CheckInValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notatki nie mogą przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

// ============================================
// Command - Check Out (WB-18)
// ============================================
/// <summary>
/// Records volunteer check-out
/// </summary>
public record CheckOutCommand(
    Guid VolunteerId,
    string? WorkDescription
) : ICommand<Result<CheckOutResultDto>>;

// ============================================
// Handler - Check Out
// ============================================
public class CheckOutHandler
    : ICommandHandler<CheckOutCommand, Result<CheckOutResultDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CheckOutHandler> _logger;

    public CheckOutHandler(
        ShelterDbContext context,
        ILogger<CheckOutHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CheckOutResultDto>> Handle(
        CheckOutCommand request,
        CancellationToken cancellationToken)
    {
        // Find active check-in
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a =>
                a.VolunteerId == request.VolunteerId &&
                a.CheckOutTime == null,
                cancellationToken);

        if (attendance is null)
        {
            return Result.Failure<CheckOutResultDto>(
                Error.Validation("Wolontariusz nie jest zameldowany"));
        }

        // Check out
        var result = attendance.CheckOut(request.WorkDescription);

        if (result.IsFailure)
        {
            return Result.Failure<CheckOutResultDto>(result.Error);
        }

        // Add hours to volunteer
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is not null && attendance.HoursWorked.HasValue)
        {
            var addHoursResult = volunteer.AddWorkHours(attendance.HoursWorked.Value, "System");
            if (addHoursResult.IsFailure)
            {
                _logger.LogWarning(
                    "Could not add hours for volunteer {VolunteerId}: {Error}",
                    request.VolunteerId, addHoursResult.Error.Message);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Volunteer {VolunteerId} checked out at {CheckOutTime}. Hours: {Hours}",
            request.VolunteerId, attendance.CheckOutTime, attendance.HoursWorked);

        return Result.Success(new CheckOutResultDto(
            AttendanceId: attendance.Id,
            VolunteerId: attendance.VolunteerId,
            CheckInTime: attendance.CheckInTime,
            CheckOutTime: attendance.CheckOutTime!.Value,
            HoursWorked: attendance.HoursWorked!.Value,
            Message: $"Wymeldowano pomyślnie. Przepracowano {attendance.HoursWorked:F2} godzin."
        ));
    }
}

// ============================================
// Validator - Check Out
// ============================================
public class CheckOutValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.WorkDescription)
            .MaximumLength(1000).WithMessage("Opis pracy nie może przekraczać 1000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.WorkDescription));
    }
}

// ============================================
// Command - Approve Attendance (WB-18)
// ============================================
/// <summary>
/// Approves an attendance record
/// </summary>
public record ApproveAttendanceCommand(
    Guid AttendanceId,
    Guid ApprovedByUserId
) : ICommand<Result<AttendanceDto>>;

// ============================================
// Handler - Approve Attendance
// ============================================
public class ApproveAttendanceHandler
    : ICommandHandler<ApproveAttendanceCommand, Result<AttendanceDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<ApproveAttendanceHandler> _logger;

    public ApproveAttendanceHandler(
        ShelterDbContext context,
        ILogger<ApproveAttendanceHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AttendanceDto>> Handle(
        ApproveAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.Id == request.AttendanceId, cancellationToken);

        if (attendance is null)
        {
            return Result.Failure<AttendanceDto>(
                Error.NotFound("Attendance", request.AttendanceId));
        }

        var result = attendance.Approve(request.ApprovedByUserId);

        if (result.IsFailure)
        {
            return Result.Failure<AttendanceDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == attendance.VolunteerId, cancellationToken);

        string? slotDescription = null;
        if (attendance.ScheduleSlotId.HasValue)
        {
            var slot = await _context.ScheduleSlots
                .FirstOrDefaultAsync(s => s.Id == attendance.ScheduleSlotId.Value, cancellationToken);
            slotDescription = slot?.Description;
        }

        _logger.LogInformation(
            "Attendance {AttendanceId} approved by user {ApprovedByUserId}",
            attendance.Id, request.ApprovedByUserId);

        return Result.Success(attendance.ToDto(volunteer?.FullName, slotDescription));
    }
}

// ============================================
// Validator - Approve Attendance
// ============================================
public class ApproveAttendanceValidator : AbstractValidator<ApproveAttendanceCommand>
{
    public ApproveAttendanceValidator()
    {
        RuleFor(x => x.AttendanceId)
            .NotEmpty().WithMessage("ID obecności jest wymagane");

        RuleFor(x => x.ApprovedByUserId)
            .NotEmpty().WithMessage("ID użytkownika zatwierdzającego jest wymagane");
    }
}

// ============================================
// Command - Correct Attendance (WB-18)
// ============================================
/// <summary>
/// Corrects an attendance record
/// </summary>
public record CorrectAttendanceCommand(
    Guid AttendanceId,
    DateTime? CheckInTime,
    DateTime? CheckOutTime,
    string? CorrectionNotes
) : ICommand<Result<AttendanceDto>>;

// ============================================
// Handler - Correct Attendance
// ============================================
public class CorrectAttendanceHandler
    : ICommandHandler<CorrectAttendanceCommand, Result<AttendanceDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CorrectAttendanceHandler> _logger;

    public CorrectAttendanceHandler(
        ShelterDbContext context,
        ILogger<CorrectAttendanceHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AttendanceDto>> Handle(
        CorrectAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.Id == request.AttendanceId, cancellationToken);

        if (attendance is null)
        {
            return Result.Failure<AttendanceDto>(
                Error.NotFound("Attendance", request.AttendanceId));
        }

        var result = attendance.CorrectTimes(
            request.CheckInTime,
            request.CheckOutTime,
            request.CorrectionNotes);

        if (result.IsFailure)
        {
            return Result.Failure<AttendanceDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == attendance.VolunteerId, cancellationToken);

        string? slotDescription = null;
        if (attendance.ScheduleSlotId.HasValue)
        {
            var slot = await _context.ScheduleSlots
                .FirstOrDefaultAsync(s => s.Id == attendance.ScheduleSlotId.Value, cancellationToken);
            slotDescription = slot?.Description;
        }

        _logger.LogInformation(
            "Attendance {AttendanceId} corrected. Notes: {Notes}",
            attendance.Id, request.CorrectionNotes);

        return Result.Success(attendance.ToDto(volunteer?.FullName, slotDescription));
    }
}

// ============================================
// Validator - Correct Attendance
// ============================================
public class CorrectAttendanceValidator : AbstractValidator<CorrectAttendanceCommand>
{
    public CorrectAttendanceValidator()
    {
        RuleFor(x => x.AttendanceId)
            .NotEmpty().WithMessage("ID obecności jest wymagane");

        RuleFor(x => x.CorrectionNotes)
            .MaximumLength(500).WithMessage("Notatki korekty nie mogą przekraczać 500 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.CorrectionNotes));

        RuleFor(x => x)
            .Must(x => x.CheckInTime.HasValue || x.CheckOutTime.HasValue)
            .WithMessage("Wymagany jest przynajmniej jeden czas do korekty");
    }
}

// ============================================
// Command - Manual Attendance Entry (WB-18)
// ============================================
/// <summary>
/// Manual attendance entry (for work outside the schedule)
/// </summary>
public record ManualAttendanceEntryCommand(
    Guid VolunteerId,
    DateTime CheckInTime,
    DateTime CheckOutTime,
    string WorkDescription,
    string? Notes
) : ICommand<Result<AttendanceDto>>;

// ============================================
// Handler - Manual Attendance Entry
// ============================================
public class ManualAttendanceEntryHandler
    : ICommandHandler<ManualAttendanceEntryCommand, Result<AttendanceDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<ManualAttendanceEntryHandler> _logger;

    public ManualAttendanceEntryHandler(
        ShelterDbContext context,
        ILogger<ManualAttendanceEntryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AttendanceDto>> Handle(
        ManualAttendanceEntryCommand request,
        CancellationToken cancellationToken)
    {
        // Check if volunteer exists
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<AttendanceDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        // Create attendance record
        var attendanceResult = Attendance.CheckIn(
            request.VolunteerId,
            null,
            request.Notes,
            request.CheckInTime);

        if (attendanceResult.IsFailure)
        {
            return Result.Failure<AttendanceDto>(attendanceResult.Error);
        }

        var attendance = attendanceResult.Value;

        // Immediately check out
        var checkOutResult = attendance.CheckOut(request.WorkDescription, request.CheckOutTime);
        if (checkOutResult.IsFailure)
        {
            return Result.Failure<AttendanceDto>(checkOutResult.Error);
        }

        _context.Attendances.Add(attendance);

        // Add hours to volunteer
        if (volunteer.Status == VolunteerStatus.Active && attendance.HoursWorked.HasValue)
        {
            var addHoursResult = volunteer.AddWorkHours(attendance.HoursWorked.Value, "Manual Entry");
            if (addHoursResult.IsFailure)
            {
                _logger.LogWarning(
                    "Could not add hours for volunteer {VolunteerId}: {Error}",
                    request.VolunteerId, addHoursResult.Error.Message);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Manual attendance entry created for volunteer {VolunteerId}. Hours: {Hours}",
            request.VolunteerId, attendance.HoursWorked);

        return Result.Success(attendance.ToDto(volunteer.FullName));
    }
}

// ============================================
// Validator - Manual Attendance Entry
// ============================================
public class ManualAttendanceEntryValidator : AbstractValidator<ManualAttendanceEntryCommand>
{
    public ManualAttendanceEntryValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.CheckInTime)
            .NotEmpty().WithMessage("Czas zameldowania jest wymagany")
            .LessThan(x => x.CheckOutTime)
                .WithMessage("Czas zameldowania musi być wcześniejszy niż czas wymeldowania");

        RuleFor(x => x.CheckOutTime)
            .NotEmpty().WithMessage("Czas wymeldowania jest wymagany")
            .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Czas wymeldowania nie może być w przyszłości");

        RuleFor(x => x.WorkDescription)
            .NotEmpty().WithMessage("Opis pracy jest wymagany dla ręcznego wpisu")
            .MaximumLength(1000).WithMessage("Opis pracy nie może przekraczać 1000 znaków");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notatki nie mogą przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
