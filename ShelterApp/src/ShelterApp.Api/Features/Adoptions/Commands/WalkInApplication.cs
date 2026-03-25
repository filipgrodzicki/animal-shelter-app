using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Commands;

// ============================================
// Command
// ============================================
/// <summary>
/// Składa zgłoszenie adopcyjne dla klienta stacjonarnego (wprowadzane przez pracownika)
/// </summary>
public record WalkInApplicationCommand(
    Guid AnimalId,
    // Dane adoptującego
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateTime DateOfBirth,
    string? Address,
    string? City,
    string? PostalCode,
    string? DocumentNumber,
    // Zgody
    bool RodoConsent,
    // Dodatkowe informacje
    string? Motivation,
    string? LivingConditions,
    string? Experience,
    string? OtherPetsInfo,
    // Dane pracownika
    Guid StaffUserId,
    string StaffName,
    // Opcje
    bool SkipEmailConfirmation = false
) : ICommand<Result<SubmitApplicationResultDto>>;

// ============================================
// Handler
// ============================================
public class WalkInApplicationHandler
    : ICommandHandler<WalkInApplicationCommand, Result<SubmitApplicationResultDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<WalkInApplicationHandler> _logger;

    public WalkInApplicationHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<WalkInApplicationHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<SubmitApplicationResultDto>> Handle(
        WalkInApplicationCommand request,
        CancellationToken cancellationToken)
    {
        await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Sprawdź czy zwierzę istnieje i ma status Available
            var animal = await _context.Animals
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

            if (animal is null)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.NotFound("Animal", request.AnimalId));
            }

            if (animal.Status != AnimalStatus.Available)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.Validation($"Zwierzę nie jest dostępne do adopcji. Aktualny status: {animal.Status}"));
            }

            // 2. Znajdź lub utwórz Adopter
            var existingAdopter = await _context.Adopters
                .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);

            Adopter adopter;

            if (existingAdopter is not null)
            {
                adopter = existingAdopter;
                // Aktualizuj dane kontaktowe
                adopter.UpdateContactInfo(
                    phone: request.Phone,
                    address: request.Address,
                    city: request.City,
                    postalCode: request.PostalCode);
            }
            else
            {
                // Utwórz nowego adoptującego
                var createAdopterResult = Adopter.Create(
                    userId: Guid.NewGuid(),
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    email: request.Email,
                    phone: request.Phone,
                    dateOfBirth: request.DateOfBirth,
                    address: request.Address,
                    city: request.City,
                    postalCode: request.PostalCode,
                    rodoConsentDate: request.RodoConsent ? DateTime.UtcNow : null);

                if (createAdopterResult.IsFailure)
                {
                    await _context.RollbackTransactionAsync(cancellationToken);
                    return Result.Failure<SubmitApplicationResultDto>(createAdopterResult.Error);
                }

                adopter = createAdopterResult.Value;
                _context.Adopters.Add(adopter);
            }

            // 3. Sprawdź czy nie ma aktywnego zgłoszenia
            var hasActiveApplication = await _context.AdoptionApplications
                .AnyAsync(a =>
                    a.AdopterId == adopter.Id &&
                    a.AnimalId == request.AnimalId &&
                    a.Status != AdoptionApplicationStatus.Completed &&
                    a.Status != AdoptionApplicationStatus.Rejected &&
                    a.Status != AdoptionApplicationStatus.Cancelled,
                    cancellationToken);

            if (hasActiveApplication)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.Validation("Istnieje już aktywne zgłoszenie adopcyjne na to zwierzę dla tego adoptującego"));
            }

            // 4. Utwórz zgłoszenie
            var createApplicationResult = AdoptionApplication.Create(
                adopterId: adopter.Id,
                animalId: request.AnimalId,
                adoptionMotivation: request.Motivation,
                petExperience: request.Experience,
                livingConditions: request.LivingConditions,
                otherPetsInfo: request.OtherPetsInfo);

            if (createApplicationResult.IsFailure)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(createApplicationResult.Error);
            }

            var application = createApplicationResult.Value;
            _context.AdoptionApplications.Add(application);

            // 5. Zmień status adoptującego
            if (adopter.Status == AdopterStatus.Registered)
            {
                adopter.ChangeStatus(
                    AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
                    request.StaffName);
            }

            // 6. Zmień status zwierzęcia
            var changeAnimalStatusResult = animal.ChangeStatus(
                AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
                request.StaffName);

            if (changeAnimalStatusResult.IsFailure)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.Validation($"Nie można zarezerwować zwierzęcia: {changeAnimalStatusResult.Error.Message}"));
            }

            // 7. Od razu podejmij do rozpatrzenia (pracownik przyjmuje stacjonarnie)
            var takeForReviewResult = application.TakeForReview(request.StaffUserId, request.StaffName);
            if (takeForReviewResult.IsFailure)
            {
                _logger.LogWarning(
                    "Could not auto-take application for review: {Error}",
                    takeForReviewResult.Error.Message);
            }

            // 8. Zapisz zmiany
            await _context.SaveChangesAsync(cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);

            // 9. Wyślij email (opcjonalnie)
            if (!request.SkipEmailConfirmation)
            {
                try
                {
                    await _emailService.SendAdoptionApplicationConfirmationAsync(
                        recipientEmail: adopter.Email,
                        recipientName: adopter.FullName,
                        animalName: animal.Name,
                        applicationNumber: application.Id.ToString("N")[..8].ToUpper(),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send confirmation email for walk-in application {ApplicationId}",
                        application.Id);
                }
            }

            _logger.LogInformation(
                "Walk-in adoption application {ApplicationId} submitted by staff {StaffName} for animal {AnimalId}",
                application.Id,
                request.StaffName,
                animal.Id);

            return Result.Success(new SubmitApplicationResultDto(
                ApplicationId: application.Id,
                AdopterId: adopter.Id,
                AnimalId: animal.Id,
                ApplicationStatus: application.Status.ToString(),
                AnimalStatus: animal.Status.ToString(),
                Message: "Zgłoszenie adopcyjne zostało pomyślnie zarejestrowane przez pracownika."
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting walk-in adoption application for animal {AnimalId}", request.AnimalId);

            if (_context.HasActiveTransaction)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
            }

            throw;
        }
    }
}

// ============================================
// Validator
// ============================================
public class WalkInApplicationValidator : AbstractValidator<WalkInApplicationCommand>
{
    public WalkInApplicationValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

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
            .LessThan(DateTime.Today.AddYears(-18))
                .WithMessage("Adoptujący musi mieć ukończone 18 lat");

        RuleFor(x => x.RodoConsent)
            .Equal(true).WithMessage("Zgoda RODO jest wymagana");

        RuleFor(x => x.StaffUserId)
            .NotEmpty().WithMessage("ID pracownika jest wymagane");

        RuleFor(x => x.StaffName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");

        RuleFor(x => x.Address)
            .MaximumLength(300).WithMessage("Adres nie może przekraczać 300 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("Miasto nie może przekraczać 100 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.PostalCode)
            .Matches(@"^\d{2}-\d{3}$").WithMessage("Nieprawidłowy format kodu pocztowego (XX-XXX)")
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));

        RuleFor(x => x.DocumentNumber)
            .MaximumLength(50).WithMessage("Numer dokumentu nie może przekraczać 50 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.DocumentNumber));
    }
}
