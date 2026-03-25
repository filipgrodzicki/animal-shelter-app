using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Commands;

// ============================================
// Command (WB-15)
// ============================================
/// <summary>
/// Rejestruje nowego wolontariusza (kandydata)
/// </summary>
public record RegisterVolunteerCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateTime DateOfBirth,
    string? Address,
    string? City,
    string? PostalCode,
    string EmergencyContactName,
    string EmergencyContactPhone,
    List<string>? Skills,
    List<DayOfWeek>? Availability,
    string? Motivation
) : ICommand<Result<VolunteerDto>>;

// ============================================
// Handler
// ============================================
public class RegisterVolunteerHandler
    : ICommandHandler<RegisterVolunteerCommand, Result<VolunteerDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterVolunteerHandler> _logger;

    public RegisterVolunteerHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<RegisterVolunteerHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<VolunteerDto>> Handle(
        RegisterVolunteerCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Sprawdź czy email nie jest już zarejestrowany
        var existingVolunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Email == request.Email, cancellationToken);

        if (existingVolunteer is not null)
        {
            // Jeśli jest nieaktywny, pozwól na ponowne zgłoszenie
            if (existingVolunteer.Status == VolunteerStatus.Inactive)
            {
                var reapplyResult = existingVolunteer.Reapply(request.Email);
                if (reapplyResult.IsFailure)
                {
                    return Result.Failure<VolunteerDto>(reapplyResult.Error);
                }

                // Aktualizuj dane
                existingVolunteer.UpdateContactInfo(
                    phone: request.Phone,
                    address: request.Address,
                    city: request.City,
                    postalCode: request.PostalCode,
                    emergencyContactName: request.EmergencyContactName,
                    emergencyContactPhone: request.EmergencyContactPhone);

                if (request.Skills is not null)
                {
                    existingVolunteer.UpdateSkills(request.Skills);
                }

                if (request.Availability is not null)
                {
                    existingVolunteer.UpdateAvailability(request.Availability);
                }

                if (!string.IsNullOrWhiteSpace(request.Motivation))
                {
                    existingVolunteer.UpdateNotes(request.Motivation);
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Volunteer {VolunteerId} reapplied: {Email}",
                    existingVolunteer.Id, existingVolunteer.Email);

                return Result.Success(existingVolunteer.ToDto());
            }

            return Result.Failure<VolunteerDto>(
                Error.Validation("Ten adres email jest już zarejestrowany w systemie wolontariuszy"));
        }

        // 2. Utwórz nowego wolontariusza
        var createResult = Volunteer.Create(
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            phone: request.Phone,
            dateOfBirth: request.DateOfBirth,
            address: request.Address,
            city: request.City,
            postalCode: request.PostalCode,
            emergencyContactName: request.EmergencyContactName,
            emergencyContactPhone: request.EmergencyContactPhone,
            skills: request.Skills,
            availability: request.Availability,
            notes: request.Motivation);

        if (createResult.IsFailure)
        {
            return Result.Failure<VolunteerDto>(createResult.Error);
        }

        var volunteer = createResult.Value;
        _context.Volunteers.Add(volunteer);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. Wyślij email potwierdzający
        try
        {
            await _emailService.SendVolunteerApplicationConfirmationAsync(
                recipientEmail: volunteer.Email,
                recipientName: volunteer.FullName,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send confirmation email for volunteer application {VolunteerId}",
                volunteer.Id);
        }

        _logger.LogInformation(
            "New volunteer registered: {VolunteerId}, {Email}",
            volunteer.Id, volunteer.Email);

        return Result.Success(volunteer.ToDto());
    }
}

// ============================================
// Validator
// ============================================
public class RegisterVolunteerValidator : AbstractValidator<RegisterVolunteerCommand>
{
    public RegisterVolunteerValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Imię jest wymagane")
            .MaximumLength(100).WithMessage("Imię nie może przekraczać 100 znaków");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Nazwisko jest wymagane")
            .MaximumLength(100).WithMessage("Nazwisko nie może przekraczać 100 znaków");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany")
            .EmailAddress().WithMessage("Nieprawidłowy format adresu email")
            .MaximumLength(200).WithMessage("Email nie może przekraczać 200 znaków");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Numer telefonu jest wymagany")
            .Matches(@"^[\d\s\+\-\(\)]{9,20}$").WithMessage("Nieprawidłowy format numeru telefonu");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Data urodzenia jest wymagana")
            .LessThan(DateTime.Today.AddYears(-16))
                .WithMessage("Wolontariusz musi mieć ukończone 16 lat");

        RuleFor(x => x.EmergencyContactName)
            .NotEmpty().WithMessage("Imię i nazwisko osoby kontaktowej jest wymagane")
            .MaximumLength(200).WithMessage("Dane kontaktowe nie mogą przekraczać 200 znaków");

        RuleFor(x => x.EmergencyContactPhone)
            .NotEmpty().WithMessage("Telefon osoby kontaktowej jest wymagany")
            .Matches(@"^[\d\s\+\-\(\)]{9,20}$").WithMessage("Nieprawidłowy format numeru telefonu");

        RuleFor(x => x.Address)
            .MaximumLength(300).WithMessage("Adres nie może przekraczać 300 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("Miasto nie może przekraczać 100 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.PostalCode)
            .Matches(@"^\d{2}-\d{3}$").WithMessage("Nieprawidłowy format kodu pocztowego (XX-XXX)")
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));

        RuleFor(x => x.Motivation)
            .MaximumLength(2000).WithMessage("Motywacja nie może przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Motivation));
    }
}
