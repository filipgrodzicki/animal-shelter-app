using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Commands;

// ============================================
// Command - Create Schedule Slot (WB-17)
// ============================================
/// <summary>
/// Tworzy pojedynczy slot w harmonogramie
/// </summary>
public record CreateScheduleSlotCommand(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxVolunteers,
    string Description,
    Guid CreatedByUserId
) : ICommand<Result<ScheduleSlotDto>>;

// ============================================
// Handler - Create Schedule Slot
// ============================================
public class CreateScheduleSlotHandler
    : ICommandHandler<CreateScheduleSlotCommand, Result<ScheduleSlotDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CreateScheduleSlotHandler> _logger;

    public CreateScheduleSlotHandler(
        ShelterDbContext context,
        ILogger<CreateScheduleSlotHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ScheduleSlotDto>> Handle(
        CreateScheduleSlotCommand request,
        CancellationToken cancellationToken)
    {
        // Sprawdź czy nie ma nakładających się slotów
        var overlapping = await _context.ScheduleSlots
            .AnyAsync(s =>
                s.Date == request.Date &&
                s.IsActive &&
                ((request.StartTime >= s.StartTime && request.StartTime < s.EndTime) ||
                 (request.EndTime > s.StartTime && request.EndTime <= s.EndTime) ||
                 (request.StartTime <= s.StartTime && request.EndTime >= s.EndTime)),
                cancellationToken);

        if (overlapping)
        {
            return Result.Failure<ScheduleSlotDto>(
                Error.Validation("W tym czasie istnieje już inny slot"));
        }

        var slotResult = ScheduleSlot.Create(
            request.Date,
            request.StartTime,
            request.EndTime,
            request.MaxVolunteers,
            request.Description,
            request.CreatedByUserId);

        if (slotResult.IsFailure)
        {
            return Result.Failure<ScheduleSlotDto>(slotResult.Error);
        }

        var slot = slotResult.Value;
        _context.ScheduleSlots.Add(slot);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Schedule slot created: {SlotId} on {Date} at {StartTime}-{EndTime}",
            slot.Id, slot.Date, slot.StartTime, slot.EndTime);

        return Result.Success(slot.ToDto());
    }
}

// ============================================
// Validator - Create Schedule Slot
// ============================================
public class CreateScheduleSlotValidator : AbstractValidator<CreateScheduleSlotCommand>
{
    public CreateScheduleSlotValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Data jest wymagana");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Godzina rozpoczęcia jest wymagana");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("Godzina zakończenia jest wymagana")
            .GreaterThan(x => x.StartTime)
                .WithMessage("Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia");

        RuleFor(x => x.MaxVolunteers)
            .InclusiveBetween(1, 20)
                .WithMessage("Maksymalna liczba wolontariuszy musi być między 1 a 20");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Opis jest wymagany")
            .MaximumLength(500).WithMessage("Opis nie może przekraczać 500 znaków");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("ID użytkownika tworzącego jest wymagane");
    }
}

// ============================================
// Command - Create Schedule Slots Bulk (WB-17)
// ============================================
/// <summary>
/// Tworzy wiele slotów w harmonogramie (na okres)
/// </summary>
public record CreateScheduleSlotsBulkCommand(
    DateOnly FromDate,
    DateOnly ToDate,
    IEnumerable<ScheduleSlotTemplate> Templates,
    Guid CreatedByUserId
) : ICommand<Result<CreateScheduleSlotsResultDto>>;

public record ScheduleSlotTemplate(
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxVolunteers,
    string Description,
    DayOfWeek[]? DaysOfWeek // null = wszystkie dni
);

// ============================================
// Handler - Create Schedule Slots Bulk
// ============================================
public class CreateScheduleSlotsBulkHandler
    : ICommandHandler<CreateScheduleSlotsBulkCommand, Result<CreateScheduleSlotsResultDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CreateScheduleSlotsBulkHandler> _logger;

    public CreateScheduleSlotsBulkHandler(
        ShelterDbContext context,
        ILogger<CreateScheduleSlotsBulkHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CreateScheduleSlotsResultDto>> Handle(
        CreateScheduleSlotsBulkCommand request,
        CancellationToken cancellationToken)
    {
        var createdSlots = new List<ScheduleSlot>();
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
                var overlapping = await _context.ScheduleSlots
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

                var slotResult = ScheduleSlot.Create(
                    currentDate,
                    template.StartTime,
                    template.EndTime,
                    template.MaxVolunteers,
                    template.Description,
                    request.CreatedByUserId);

                if (slotResult.IsSuccess)
                {
                    createdSlots.Add(slotResult.Value);
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        if (createdSlots.Count > 0)
        {
            _context.ScheduleSlots.AddRange(createdSlots);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Created {Count} schedule slots from {FromDate} to {ToDate}",
            createdSlots.Count, request.FromDate, request.ToDate);

        var result = new CreateScheduleSlotsResultDto(
            CreatedCount: createdSlots.Count,
            Slots: createdSlots.Select(s => s.ToDto())
        );

        return Result.Success(result);
    }
}

// ============================================
// Validator - Create Schedule Slots Bulk
// ============================================
public class CreateScheduleSlotsBulkValidator : AbstractValidator<CreateScheduleSlotsBulkCommand>
{
    public CreateScheduleSlotsBulkValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("Data początkowa jest wymagana");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("Data końcowa jest wymagana")
            .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("Data końcowa musi być równa lub późniejsza niż data początkowa");

        RuleFor(x => x.Templates)
            .NotEmpty().WithMessage("Wymagany jest co najmniej jeden szablon slotu");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("ID użytkownika tworzącego jest wymagane");

        RuleForEach(x => x.Templates).ChildRules(template =>
        {
            template.RuleFor(t => t.EndTime)
                .GreaterThan(t => t.StartTime)
                .WithMessage("Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia");

            template.RuleFor(t => t.MaxVolunteers)
                .InclusiveBetween(1, 20)
                .WithMessage("Maksymalna liczba wolontariuszy musi być między 1 a 20");

            template.RuleFor(t => t.Description)
                .NotEmpty().WithMessage("Opis jest wymagany")
                .MaximumLength(500).WithMessage("Opis nie może przekraczać 500 znaków");
        });
    }
}

// ============================================
// Command - Assign Volunteer To Slot (WB-17)
// ============================================
/// <summary>
/// Przypisuje wolontariusza do slotu w harmonogramie
/// </summary>
public record AssignVolunteerToSlotCommand(
    Guid ScheduleSlotId,
    Guid VolunteerId,
    Guid AssignedByUserId
) : ICommand<Result<VolunteerAssignmentDto>>;

// ============================================
// Handler - Assign Volunteer To Slot
// ============================================
public class AssignVolunteerToSlotHandler
    : ICommandHandler<AssignVolunteerToSlotCommand, Result<VolunteerAssignmentDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<AssignVolunteerToSlotHandler> _logger;

    public AssignVolunteerToSlotHandler(
        ShelterDbContext context,
        ILogger<AssignVolunteerToSlotHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<VolunteerAssignmentDto>> Handle(
        AssignVolunteerToSlotCommand request,
        CancellationToken cancellationToken)
    {
        // Pobierz slot z przypisaniami
        var slot = await _context.ScheduleSlots
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleSlotId, cancellationToken);

        if (slot is null)
        {
            return Result.Failure<VolunteerAssignmentDto>(
                Error.NotFound("ScheduleSlot", request.ScheduleSlotId));
        }

        // Sprawdź czy wolontariusz istnieje i jest aktywny
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerAssignmentDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        if (volunteer.Status != VolunteerStatus.Active)
        {
            return Result.Failure<VolunteerAssignmentDto>(
                Error.Validation($"Wolontariusz musi być aktywny. Aktualny status: {volunteer.Status}"));
        }

        // Sprawdź dostępność wolontariusza
        if (!volunteer.Availability.Contains(slot.Date.DayOfWeek))
        {
            return Result.Failure<VolunteerAssignmentDto>(
                Error.Validation($"Wolontariusz nie jest dostępny w {slot.Date.DayOfWeek}"));
        }

        // Przypisz wolontariusza
        var assignResult = slot.AssignVolunteer(request.VolunteerId, request.AssignedByUserId);

        if (assignResult.IsFailure)
        {
            return Result.Failure<VolunteerAssignmentDto>(assignResult.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var assignment = assignResult.Value;
        _logger.LogInformation(
            "Volunteer {VolunteerId} assigned to slot {SlotId}",
            request.VolunteerId, request.ScheduleSlotId);

        return Result.Success(assignment.ToDto(volunteer.FullName));
    }
}

// ============================================
// Validator - Assign Volunteer To Slot
// ============================================
public class AssignVolunteerToSlotValidator : AbstractValidator<AssignVolunteerToSlotCommand>
{
    public AssignVolunteerToSlotValidator()
    {
        RuleFor(x => x.ScheduleSlotId)
            .NotEmpty().WithMessage("ID slotu jest wymagane");

        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.AssignedByUserId)
            .NotEmpty().WithMessage("ID użytkownika przypisującego jest wymagane");
    }
}

// ============================================
// Command - Confirm Assignment
// ============================================
/// <summary>
/// Potwierdza przypisanie wolontariusza do slotu
/// </summary>
public record ConfirmAssignmentCommand(
    Guid AssignmentId
) : ICommand<Result<VolunteerAssignmentDto>>;

// ============================================
// Handler - Confirm Assignment
// ============================================
public class ConfirmAssignmentHandler
    : ICommandHandler<ConfirmAssignmentCommand, Result<VolunteerAssignmentDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<ConfirmAssignmentHandler> _logger;

    public ConfirmAssignmentHandler(
        ShelterDbContext context,
        ILogger<ConfirmAssignmentHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<VolunteerAssignmentDto>> Handle(
        ConfirmAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _context.VolunteerAssignments
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<VolunteerAssignmentDto>(
                Error.NotFound("VolunteerAssignment", request.AssignmentId));
        }

        var result = assignment.Confirm();

        if (result.IsFailure)
        {
            return Result.Failure<VolunteerAssignmentDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == assignment.VolunteerId, cancellationToken);

        _logger.LogInformation(
            "Assignment {AssignmentId} confirmed for volunteer {VolunteerId}",
            assignment.Id, assignment.VolunteerId);

        return Result.Success(assignment.ToDto(volunteer?.FullName));
    }
}

// ============================================
// Validator - Confirm Assignment
// ============================================
public class ConfirmAssignmentValidator : AbstractValidator<ConfirmAssignmentCommand>
{
    public ConfirmAssignmentValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty().WithMessage("ID przypisania jest wymagane");
    }
}

// ============================================
// Command - Cancel Assignment
// ============================================
/// <summary>
/// Anuluje przypisanie wolontariusza do slotu
/// </summary>
public record CancelAssignmentCommand(
    Guid AssignmentId,
    string Reason
) : ICommand<Result>;

// ============================================
// Handler - Cancel Assignment
// ============================================
public class CancelAssignmentHandler
    : ICommandHandler<CancelAssignmentCommand, Result>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<CancelAssignmentHandler> _logger;

    public CancelAssignmentHandler(
        ShelterDbContext context,
        ILogger<CancelAssignmentHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CancelAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _context.VolunteerAssignments
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure(Error.NotFound("VolunteerAssignment", request.AssignmentId));
        }

        var result = assignment.Cancel(request.Reason);

        if (result.IsFailure)
        {
            return result;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Assignment {AssignmentId} cancelled. Reason: {Reason}",
            assignment.Id, request.Reason);

        return Result.Success();
    }
}

// ============================================
// Validator - Cancel Assignment
// ============================================
public class CancelAssignmentValidator : AbstractValidator<CancelAssignmentCommand>
{
    public CancelAssignmentValidator()
    {
        RuleFor(x => x.AssignmentId)
            .NotEmpty().WithMessage("ID przypisania jest wymagane");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód anulowania jest wymagany")
            .MaximumLength(500).WithMessage("Powód nie może przekraczać 500 znaków");
    }
}
