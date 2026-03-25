using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Commands;

// ============================================
// Command
// ============================================
/// <summary>
/// Rezerwuje termin wizyty adopcyjnej
/// </summary>
public record ScheduleVisitCommand(
    Guid ApplicationId,
    DateTime VisitDate,
    string ScheduledByName,
    string? Notes
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler
// ============================================
public class ScheduleVisitHandler
    : ICommandHandler<ScheduleVisitCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ScheduleVisitHandler> _logger;

    // Adres schroniska - w prawdziwej aplikacji byłby w konfiguracji
    private const string ShelterAddress = "ul. Schroniskowa 1, 00-001 Warszawa";

    public ScheduleVisitHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<ScheduleVisitHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        ScheduleVisitCommand request,
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

        var result = application.ScheduleVisit(request.VisitDate, request.ScheduledByName);
        if (result.IsFailure)
        {
            return Result.Failure<AdoptionApplicationDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Pobierz dane do emaila
        var adopter = await _context.Adopters.FindAsync(new object[] { application.AdopterId }, cancellationToken);
        var animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        // Wyślij email z potwierdzeniem wizyty
        if (adopter is not null && animal is not null)
        {
            try
            {
                await _emailService.SendVisitScheduledAsync(
                    adopter.Email,
                    adopter.FullName,
                    animal.Name,
                    request.VisitDate,
                    ShelterAddress,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send visit scheduled email for application {ApplicationId}", application.Id);
            }
        }

        _logger.LogInformation(
            "Visit scheduled for application {ApplicationId} at {VisitDate} by {ScheduledBy}",
            application.Id,
            request.VisitDate,
            request.ScheduledByName);

        return Result.Success(application.ToDto(adopter, animal));
    }
}

// ============================================
// Validator
// ============================================
public class ScheduleVisitValidator : AbstractValidator<ScheduleVisitCommand>
{
    public ScheduleVisitValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.VisitDate)
            .NotEmpty().WithMessage("Data wizyty jest wymagana")
            .GreaterThan(DateTime.UtcNow.AddHours(24))
                .WithMessage("Wizyta musi być zaplanowana z co najmniej 24-godzinnym wyprzedzeniem")
            .LessThan(DateTime.UtcNow.AddMonths(3))
                .WithMessage("Wizyta nie może być zaplanowana dalej niż 3 miesiące w przyszłości");

        RuleFor(x => x.ScheduledByName)
            .NotEmpty().WithMessage("Nazwa osoby rezerwującej jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa nie może przekraczać 200 znaków");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notatki nie mogą przekraczać 500 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
