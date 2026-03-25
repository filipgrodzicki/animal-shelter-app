using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Commands;

// ============================================
// Command - Approve Application (WB-16)
// ============================================
/// <summary>
/// Akceptuje zgłoszenie kandydata i rozpoczyna szkolenie
/// </summary>
public record ApproveVolunteerApplicationCommand(
    Guid VolunteerId,
    Guid ApprovedByUserId,
    string ApprovedByName,
    DateTime? TrainingStartDate,
    string? Notes
) : ICommand<Result<VolunteerDto>>;

// ============================================
// Handler - Approve Application
// ============================================
public class ApproveVolunteerApplicationHandler
    : ICommandHandler<ApproveVolunteerApplicationCommand, Result<VolunteerDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ApproveVolunteerApplicationHandler> _logger;

    public ApproveVolunteerApplicationHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<ApproveVolunteerApplicationHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<VolunteerDto>> Handle(
        ApproveVolunteerApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        // Akceptuj i rozpocznij szkolenie
        var result = volunteer.AcceptAndStartTraining(
            request.ApprovedByName,
            request.TrainingStartDate);

        if (result.IsFailure)
        {
            return Result.Failure<VolunteerDto>(result.Error);
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            volunteer.UpdateNotes(
                string.IsNullOrWhiteSpace(volunteer.Notes)
                    ? request.Notes
                    : $"{volunteer.Notes}\n\n[Akceptacja] {request.Notes}");
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Wyślij email
        try
        {
            await _emailService.SendVolunteerApprovalNotificationAsync(
                recipientEmail: volunteer.Email,
                recipientName: volunteer.FullName,
                trainingStartDate: volunteer.TrainingStartDate!.Value,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send approval notification for volunteer {VolunteerId}",
                volunteer.Id);
        }

        _logger.LogInformation(
            "Volunteer application approved: {VolunteerId} by {ApprovedBy}",
            volunteer.Id, request.ApprovedByName);

        return Result.Success(volunteer.ToDto());
    }
}

// ============================================
// Validator - Approve Application
// ============================================
public class ApproveVolunteerApplicationValidator : AbstractValidator<ApproveVolunteerApplicationCommand>
{
    public ApproveVolunteerApplicationValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.ApprovedByUserId)
            .NotEmpty().WithMessage("ID użytkownika zatwierdzającego jest wymagane");

        RuleFor(x => x.ApprovedByName)
            .NotEmpty().WithMessage("Imię osoby zatwierdzającej jest wymagane")
            .MaximumLength(200).WithMessage("Nazwa nie może przekraczać 200 znaków");

        RuleFor(x => x.TrainingStartDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Data rozpoczęcia szkolenia nie może być w przeszłości")
            .When(x => x.TrainingStartDate.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notatki nie mogą przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

// ============================================
// Command - Reject Application
// ============================================
/// <summary>
/// Odrzuca zgłoszenie kandydata
/// </summary>
public record RejectVolunteerApplicationCommand(
    Guid VolunteerId,
    string RejectedByName,
    string Reason
) : ICommand<Result<VolunteerDto>>;

// ============================================
// Handler - Reject Application
// ============================================
public class RejectVolunteerApplicationHandler
    : ICommandHandler<RejectVolunteerApplicationCommand, Result<VolunteerDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RejectVolunteerApplicationHandler> _logger;

    public RejectVolunteerApplicationHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<RejectVolunteerApplicationHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<VolunteerDto>> Handle(
        RejectVolunteerApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        var result = volunteer.RejectApplication(request.RejectedByName, request.Reason);

        if (result.IsFailure)
        {
            return Result.Failure<VolunteerDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Volunteer application rejected: {VolunteerId} by {RejectedBy}. Reason: {Reason}",
            volunteer.Id, request.RejectedByName, request.Reason);

        return Result.Success(volunteer.ToDto());
    }
}

// ============================================
// Validator - Reject Application
// ============================================
public class RejectVolunteerApplicationValidator : AbstractValidator<RejectVolunteerApplicationCommand>
{
    public RejectVolunteerApplicationValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.RejectedByName)
            .NotEmpty().WithMessage("Imię osoby odrzucającej jest wymagane")
            .MaximumLength(200).WithMessage("Nazwa nie może przekraczać 200 znaków");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód odrzucenia jest wymagany")
            .MaximumLength(1000).WithMessage("Powód nie może przekraczać 1000 znaków");
    }
}

// ============================================
// Command - Complete Training
// ============================================
/// <summary>
/// Kończy szkolenie wolontariusza i aktywuje go
/// </summary>
public record CompleteTrainingCommand(
    Guid VolunteerId,
    string CompletedByName,
    string ContractNumber,
    DateTime? TrainingEndDate
) : ICommand<Result<VolunteerDto>>;

// ============================================
// Handler - Complete Training
// ============================================
public class CompleteTrainingHandler
    : ICommandHandler<CompleteTrainingCommand, Result<VolunteerDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<CompleteTrainingHandler> _logger;

    public CompleteTrainingHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<CompleteTrainingHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<VolunteerDto>> Handle(
        CompleteTrainingCommand request,
        CancellationToken cancellationToken)
    {
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        // Sprawdź czy numer umowy nie jest już używany
        var contractExists = await _context.Volunteers
            .AnyAsync(v => v.ContractNumber == request.ContractNumber && v.Id != request.VolunteerId,
                cancellationToken);

        if (contractExists)
        {
            return Result.Failure<VolunteerDto>(
                Error.Validation("Ten numer umowy jest już przypisany do innego wolontariusza"));
        }

        var result = volunteer.CompleteTraining(
            request.CompletedByName,
            request.ContractNumber,
            request.TrainingEndDate);

        if (result.IsFailure)
        {
            return Result.Failure<VolunteerDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Wyślij email
        try
        {
            await _emailService.SendVolunteerActivationNotificationAsync(
                recipientEmail: volunteer.Email,
                recipientName: volunteer.FullName,
                contractNumber: volunteer.ContractNumber!,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send activation notification for volunteer {VolunteerId}",
                volunteer.Id);
        }

        _logger.LogInformation(
            "Volunteer training completed: {VolunteerId}, Contract: {ContractNumber}",
            volunteer.Id, volunteer.ContractNumber);

        return Result.Success(volunteer.ToDto());
    }
}

// ============================================
// Validator - Complete Training
// ============================================
public class CompleteTrainingValidator : AbstractValidator<CompleteTrainingCommand>
{
    public CompleteTrainingValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.CompletedByName)
            .NotEmpty().WithMessage("Imię osoby kończącej szkolenie jest wymagane")
            .MaximumLength(200).WithMessage("Nazwa nie może przekraczać 200 znaków");

        RuleFor(x => x.ContractNumber)
            .NotEmpty().WithMessage("Numer umowy jest wymagany")
            .MaximumLength(50).WithMessage("Numer umowy nie może przekraczać 50 znaków");

        RuleFor(x => x.TrainingEndDate)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Data zakończenia szkolenia nie może być w przyszłości")
            .When(x => x.TrainingEndDate.HasValue);
    }
}

// ============================================
// Command - Suspend Volunteer
// ============================================
/// <summary>
/// Zawiesza wolontariusza
/// </summary>
public record SuspendVolunteerCommand(
    Guid VolunteerId,
    string SuspendedByName,
    string Reason
) : ICommand<Result<VolunteerDto>>;

// ============================================
// Handler - Suspend Volunteer
// ============================================
public class SuspendVolunteerHandler
    : ICommandHandler<SuspendVolunteerCommand, Result<VolunteerDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<SuspendVolunteerHandler> _logger;

    public SuspendVolunteerHandler(
        ShelterDbContext context,
        ILogger<SuspendVolunteerHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<VolunteerDto>> Handle(
        SuspendVolunteerCommand request,
        CancellationToken cancellationToken)
    {
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        var result = volunteer.Suspend(request.SuspendedByName, request.Reason);

        if (result.IsFailure)
        {
            return Result.Failure<VolunteerDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Volunteer suspended: {VolunteerId} by {SuspendedBy}. Reason: {Reason}",
            volunteer.Id, request.SuspendedByName, request.Reason);

        return Result.Success(volunteer.ToDto());
    }
}

// ============================================
// Validator - Suspend Volunteer
// ============================================
public class SuspendVolunteerValidator : AbstractValidator<SuspendVolunteerCommand>
{
    public SuspendVolunteerValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.SuspendedByName)
            .NotEmpty().WithMessage("Imię osoby zawieszającej jest wymagane")
            .MaximumLength(200).WithMessage("Nazwa nie może przekraczać 200 znaków");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód zawieszenia jest wymagany")
            .MaximumLength(1000).WithMessage("Powód nie może przekraczać 1000 znaków");
    }
}

// ============================================
// Command - Resume Volunteer
// ============================================
/// <summary>
/// Wznawia aktywność wolontariusza po zawieszeniu
/// </summary>
public record ResumeVolunteerCommand(
    Guid VolunteerId,
    string ResumedByName,
    string? Notes
) : ICommand<Result<VolunteerDto>>;

// ============================================
// Handler - Resume Volunteer
// ============================================
public class ResumeVolunteerHandler
    : ICommandHandler<ResumeVolunteerCommand, Result<VolunteerDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<ResumeVolunteerHandler> _logger;

    public ResumeVolunteerHandler(
        ShelterDbContext context,
        ILogger<ResumeVolunteerHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<VolunteerDto>> Handle(
        ResumeVolunteerCommand request,
        CancellationToken cancellationToken)
    {
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        var result = volunteer.Resume(request.ResumedByName, request.Notes);

        if (result.IsFailure)
        {
            return Result.Failure<VolunteerDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Volunteer resumed: {VolunteerId} by {ResumedBy}",
            volunteer.Id, request.ResumedByName);

        return Result.Success(volunteer.ToDto());
    }
}

// ============================================
// Validator - Resume Volunteer
// ============================================
public class ResumeVolunteerValidator : AbstractValidator<ResumeVolunteerCommand>
{
    public ResumeVolunteerValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.ResumedByName)
            .NotEmpty().WithMessage("Imię osoby wznawiającej jest wymagane")
            .MaximumLength(200).WithMessage("Nazwa nie może przekraczać 200 znaków");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notatki nie mogą przekraczać 1000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
