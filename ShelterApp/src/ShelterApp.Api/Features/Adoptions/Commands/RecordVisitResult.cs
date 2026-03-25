using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Commands;

// ============================================
// Command - Record Visit Attendance
// ============================================
/// <summary>
/// Rejestruje stawienie się adoptującego na wizytę
/// </summary>
public record RecordVisitAttendanceCommand(
    Guid ApplicationId,
    Guid ConductedByUserId,
    string ConductedByName
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Record Visit Attendance
// ============================================
public class RecordVisitAttendanceHandler
    : ICommandHandler<RecordVisitAttendanceCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<RecordVisitAttendanceHandler> _logger;

    public RecordVisitAttendanceHandler(
        ShelterDbContext context,
        ILogger<RecordVisitAttendanceHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        RecordVisitAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _context.AdoptionApplications
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application is null)
        {
            return Result.Failure<AdoptionApplicationDto>(
                Error.NotFound("AdoptionApplication", request.ApplicationId));
        }

        var result = application.RecordVisitAttendance(
            request.ConductedByUserId,
            request.ConductedByName);

        if (result.IsFailure)
        {
            return Result.Failure<AdoptionApplicationDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Visit attendance recorded for application {ApplicationId} by {ConductedBy}",
            application.Id,
            request.ConductedByName);

        var adopter = await _context.Adopters.FindAsync(new object[] { application.AdopterId }, cancellationToken);
        var animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        return Result.Success(application.ToDto(adopter, animal));
    }
}

// ============================================
// Validator - Record Visit Attendance
// ============================================
public class RecordVisitAttendanceValidator : AbstractValidator<RecordVisitAttendanceCommand>
{
    public RecordVisitAttendanceValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.ConductedByUserId)
            .NotEmpty().WithMessage("ID pracownika przeprowadzającego wizytę jest wymagane");

        RuleFor(x => x.ConductedByName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");
    }
}

// ============================================
// Command - Record Visit Result
// ============================================
/// <summary>
/// Zapisuje wynik wizyty adopcyjnej
/// </summary>
public record RecordVisitResultCommand(
    Guid ApplicationId,
    bool IsPositive,
    int Assessment,
    string Notes,
    string RecordedByName
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Record Visit Result
// ============================================
public class RecordVisitResultHandler
    : ICommandHandler<RecordVisitResultCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RecordVisitResultHandler> _logger;

    public RecordVisitResultHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<RecordVisitResultHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        RecordVisitResultCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _context.AdoptionApplications
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application is null)
        {
            return Result.Failure<AdoptionApplicationDto>(
                Error.NotFound("AdoptionApplication", request.ApplicationId));
        }

        var result = application.RecordVisitResult(
            request.IsPositive,
            request.Assessment,
            request.Notes,
            request.RecordedByName);

        if (result.IsFailure)
        {
            return Result.Failure<AdoptionApplicationDto>(result.Error);
        }

        // Pobierz zwierzę i adoptującego
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        var adopter = await _context.Adopters
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == application.AdopterId, cancellationToken);

        if (request.IsPositive)
        {
            // Pozytywna wizyta - zmień status zwierzęcia na InAdoptionProcess
            if (animal is not null &&
                animal.CanChangeStatus(Domain.Animals.AnimalStatusTrigger.ZatwierdznieZgloszenia))
            {
                animal.ChangeStatus(
                    Domain.Animals.AnimalStatusTrigger.ZatwierdznieZgloszenia,
                    request.RecordedByName);
            }

            // Pozytywna wizyta - zmień status adoptującego na Adopter
            if (adopter is not null && adopter.CanChangeStatus(AdopterStatusTrigger.PozytywnaWeryfikacja))
            {
                adopter.ChangeStatus(
                    AdopterStatusTrigger.PozytywnaWeryfikacja,
                    request.RecordedByName);
            }
        }
        else
        {
            // Negatywna ocena - przywróć status zwierzęcia i adoptującego
            if (animal is not null &&
                animal.CanChangeStatus(Domain.Animals.AnimalStatusTrigger.NegatywnaOcena))
            {
                animal.ChangeStatus(
                    Domain.Animals.AnimalStatusTrigger.NegatywnaOcena,
                    request.RecordedByName,
                    request.Notes);
            }

            if (adopter is not null && adopter.CanChangeStatus(AdopterStatusTrigger.NegatywnaWeryfikacja))
            {
                adopter.ChangeStatus(
                    AdopterStatusTrigger.NegatywnaWeryfikacja,
                    request.RecordedByName,
                    request.Notes);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Pobierz świeże dane dla DTO
        animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        // Wyślij email z wynikiem wizyty
        if (adopter is not null && animal is not null)
        {
            try
            {
                if (request.IsPositive)
                {
                    await _emailService.SendVisitApprovedAsync(
                        adopter.Email,
                        adopter.FullName,
                        animal.Name,
                        cancellationToken);
                }
                else
                {
                    await _emailService.SendVisitRejectedAsync(
                        adopter.Email,
                        adopter.FullName,
                        animal.Name,
                        request.Notes,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send visit result email for application {ApplicationId}", application.Id);
            }
        }

        _logger.LogInformation(
            "Visit result recorded for application {ApplicationId}. Positive: {IsPositive}, Assessment: {Assessment}",
            application.Id,
            request.IsPositive,
            request.Assessment);

        return Result.Success(application.ToDto(adopter, animal));
    }
}

// ============================================
// Validator - Record Visit Result
// ============================================
public class RecordVisitResultValidator : AbstractValidator<RecordVisitResultCommand>
{
    public RecordVisitResultValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.Assessment)
            .InclusiveBetween(1, 5).WithMessage("Ocena musi być w skali od 1 do 5");

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("Notatki z wizyty są wymagane")
            .MaximumLength(2000).WithMessage("Notatki nie mogą przekraczać 2000 znaków");

        RuleFor(x => x.RecordedByName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");
    }
}

// ============================================
// Command - Record No-Show
// ============================================
/// <summary>
/// Rejestruje niestawienie się na wizytę
/// </summary>
public record RecordNoShowCommand(
    Guid ApplicationId,
    string RecordedByName,
    string? Notes
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Record No-Show
// ============================================
public class RecordNoShowHandler
    : ICommandHandler<RecordNoShowCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RecordNoShowHandler> _logger;

    public RecordNoShowHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<RecordNoShowHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        RecordNoShowCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _context.AdoptionApplications
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application is null)
        {
            return Result.Failure<AdoptionApplicationDto>(
                Error.NotFound("AdoptionApplication", request.ApplicationId));
        }

        var reason = request.Notes ?? "Niestawienie się na umówioną wizytę";
        var result = application.CancelByUser(reason, request.RecordedByName);

        if (result.IsFailure)
        {
            return Result.Failure<AdoptionApplicationDto>(result.Error);
        }

        // Przywróć status zwierzęcia do Available
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        if (animal is not null && animal.CanChangeStatus(Domain.Animals.AnimalStatusTrigger.AnulowanieZgloszenia))
        {
            animal.ChangeStatus(
                Domain.Animals.AnimalStatusTrigger.AnulowanieZgloszenia,
                request.RecordedByName,
                reason);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "No-show recorded for application {ApplicationId}",
            application.Id);

        var adopter = await _context.Adopters.FindAsync(new object[] { application.AdopterId }, cancellationToken);
        animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        return Result.Success(application.ToDto(adopter, animal));
    }
}

// ============================================
// Validator - Record No-Show
// ============================================
public class RecordNoShowValidator : AbstractValidator<RecordNoShowCommand>
{
    public RecordNoShowValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.RecordedByName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notatki nie mogą przekraczać 500 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
