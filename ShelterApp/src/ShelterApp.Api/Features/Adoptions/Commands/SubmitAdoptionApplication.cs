using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Notifications;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Commands;

// ============================================
// Command
// ============================================
/// <summary>
/// Składa zgłoszenie adopcyjne (WB-07, WB-09, WB-10)
/// </summary>
public record SubmitAdoptionApplicationCommand(
    Guid AnimalId,
    // Dane adoptującego (wymagane jeśli użytkownik niezarejestrowany)
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? PostalCode,
    DateTime? DateOfBirth,
    // Zgody
    bool RodoConsent,
    // Dodatkowe informacje
    string? Motivation,
    string? LivingConditions,
    string? Experience,
    string? OtherPetsInfo,
    // Dane strukturalne do algorytmu dopasowania
    string? HousingType,
    bool? HasChildren,
    bool? HasOtherAnimals,
    string? ExperienceLevelApplicant,
    string? AvailableCareTime,
    // Opcjonalnie: ID już zarejestrowanego adoptującego
    Guid? ExistingAdopterId
) : ICommand<Result<SubmitApplicationResultDto>>;

// ============================================
// Handler
// ============================================
public class SubmitAdoptionApplicationHandler
    : ICommandHandler<SubmitAdoptionApplicationCommand, Result<SubmitApplicationResultDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubmitAdoptionApplicationHandler> _logger;

    public SubmitAdoptionApplicationHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<SubmitAdoptionApplicationHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<SubmitApplicationResultDto>> Handle(
        SubmitAdoptionApplicationCommand request,
        CancellationToken cancellationToken)
    {
        // Transaction temporarily disabled for testing
        // await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Sprawdź czy zwierzę istnieje i ma status Available
            var animal = await _context.Animals
                .Include(a => a.Photos)
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

            if (animal is null)
            {
                // // await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.NotFound("Animal", request.AnimalId));
            }

            if (animal.Status != AnimalStatus.Available)
            {
                // // await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.Validation($"Zwierzę nie jest dostępne do adopcji. Aktualny status: {animal.Status}"));
            }

            // 2. Pobierz lub utwórz Adopter
            Adopter adopter;

            if (request.ExistingAdopterId.HasValue)
            {
                // Użyj istniejącego adoptującego
                var existingAdopter = await _context.Adopters
                    .FirstOrDefaultAsync(a => a.Id == request.ExistingAdopterId.Value, cancellationToken);

                if (existingAdopter is null)
                {
                    // await _context.RollbackTransactionAsync(cancellationToken);
                    return Result.Failure<SubmitApplicationResultDto>(
                        Error.NotFound("Adopter", request.ExistingAdopterId.Value));
                }

                adopter = existingAdopter;
            }
            else
            {
                // Sprawdź czy adoptujący z tym emailem już istnieje
                var existingByEmail = await _context.Adopters
                    .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);

                if (existingByEmail is not null)
                {
                    adopter = existingByEmail;
                    // Aktualizuj dane kontaktowe jeśli podano
                    adopter.UpdateContactInfo(
                        phone: request.Phone,
                        address: request.Address,
                        city: request.City,
                        postalCode: request.PostalCode);
                }
                else
                {
                    // Utwórz nowego adoptującego
                    if (!request.DateOfBirth.HasValue)
                    {
                        // await _context.RollbackTransactionAsync(cancellationToken);
                        return Result.Failure<SubmitApplicationResultDto>(
                            Error.Validation("Data urodzenia jest wymagana dla nowego adoptującego"));
                    }

                    var createAdopterResult = Adopter.Create(
                        userId: Guid.NewGuid(), // W prawdziwej aplikacji byłby to ID z systemu Identity
                        firstName: request.FirstName!,
                        lastName: request.LastName!,
                        email: request.Email!,
                        phone: request.Phone!,
                        dateOfBirth: request.DateOfBirth.Value,
                        address: request.Address,
                        city: request.City,
                        postalCode: request.PostalCode,
                        rodoConsentDate: request.RodoConsent ? DateTime.UtcNow : null);

                    if (createAdopterResult.IsFailure)
                    {
                        // await _context.RollbackTransactionAsync(cancellationToken);
                        return Result.Failure<SubmitApplicationResultDto>(createAdopterResult.Error);
                    }

                    adopter = createAdopterResult.Value;
                    _context.Adopters.Add(adopter);
                }
            }

            // 3. Sprawdź czy nie ma aktywnego zgłoszenia na to zwierzę (WB-09)
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
                // await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.Validation("Masz już aktywne zgłoszenie adopcyjne na to zwierzę"));
            }

            // 4. Utwórz zgłoszenie adopcyjne
            var createApplicationResult = AdoptionApplication.Create(
                adopterId: adopter.Id,
                animalId: request.AnimalId,
                adoptionMotivation: request.Motivation,
                petExperience: request.Experience,
                livingConditions: request.LivingConditions,
                otherPetsInfo: request.OtherPetsInfo,
                housingType: request.HousingType,
                hasChildren: request.HasChildren,
                hasOtherAnimals: request.HasOtherAnimals,
                experienceLevelApplicant: request.ExperienceLevelApplicant,
                availableCareTime: request.AvailableCareTime);

            if (createApplicationResult.IsFailure)
            {
                // await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(createApplicationResult.Error);
            }

            var application = createApplicationResult.Value;
            _context.AdoptionApplications.Add(application);

            // 4a. Zmień status zgłoszenia na Submitted
            var submitResult = application.ChangeStatus(
                AdoptionApplicationTrigger.ZlozenieZgloszenia,
                adopter.Email);

            if (submitResult.IsFailure)
            {
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.Validation($"Nie można złożyć zgłoszenia: {submitResult.Error.Message}"));
            }

            // 5. Zmień status adoptującego na Applying
            if (adopter.Status == AdopterStatus.Registered)
            {
                var changeAdopterStatusResult = adopter.ChangeStatus(
                    AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
                    adopter.Email);

                if (changeAdopterStatusResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Could not change adopter status: {Error}",
                        changeAdopterStatusResult.Error.Message);
                }
            }

            // 6. Zmień status zwierzęcia na Reserved
            var changeAnimalStatusResult = animal.ChangeStatus(
                AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
                adopter.Email);

            if (changeAnimalStatusResult.IsFailure)
            {
                // await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<SubmitApplicationResultDto>(
                    Error.Validation($"Nie można zarezerwować zwierzęcia: {changeAnimalStatusResult.Error.Message}"));
            }

            // 7. Utwórz powiadomienie dla pracowników
            var notification = AdminNotification.Create(
                type: NotificationType.NewAdoptionApplication,
                title: "Nowe zgłoszenie adopcyjne",
                message: $"{adopter.FullName} złożył(a) wniosek o adopcję zwierzęcia {animal.Name} ({animal.RegistrationNumber})",
                priority: NotificationPriority.High,
                link: $"/admin/adoptions/{application.Id}",
                relatedEntityId: application.Id,
                relatedEntityType: "AdoptionApplication");
            _context.AdminNotifications.Add(notification);

            // 8. Zapisz wszystkie zmiany
            await _context.SaveChangesAsync(cancellationToken);
            // await _context.CommitTransactionAsync(cancellationToken);

            // 9. Wyślij email potwierdzający (poza transakcją)
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
                    "Failed to send confirmation email for application {ApplicationId}",
                    application.Id);
                // Nie przerywamy - email jest nice-to-have, zgłoszenie zostało zapisane
            }

            _logger.LogInformation(
                "Adoption application {ApplicationId} submitted for animal {AnimalId} by adopter {AdopterId}",
                application.Id,
                animal.Id,
                adopter.Id);

            return Result.Success(new SubmitApplicationResultDto(
                ApplicationId: application.Id,
                AdopterId: adopter.Id,
                AnimalId: animal.Id,
                ApplicationStatus: application.Status.ToString(),
                AnimalStatus: animal.Status.ToString(),
                Message: "Zgłoszenie adopcyjne zostało pomyślnie złożone. Wkrótce skontaktujemy się z Tobą."
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting adoption application for animal {AnimalId}", request.AnimalId);

            if (_context.HasActiveTransaction)
            {
                // await _context.RollbackTransactionAsync(cancellationToken);
            }

            throw;
        }
    }
}

// ============================================
// Validator
// ============================================
public class SubmitAdoptionApplicationValidator : AbstractValidator<SubmitAdoptionApplicationCommand>
{
    public SubmitAdoptionApplicationValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.RodoConsent)
            .Equal(true).WithMessage("Zgoda na przetwarzanie danych osobowych (RODO) jest wymagana");

        // Walidacja dla nowego adoptującego (gdy nie podano ExistingAdopterId)
        When(x => !x.ExistingAdopterId.HasValue, () =>
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
                .LessThan(DateTime.Today.AddYears(-18))
                    .WithMessage("Musisz mieć ukończone 18 lat aby adoptować zwierzę");
        });

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

        RuleFor(x => x.LivingConditions)
            .MaximumLength(2000).WithMessage("Opis warunków mieszkaniowych nie może przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.LivingConditions));

        RuleFor(x => x.Experience)
            .MaximumLength(2000).WithMessage("Opis doświadczenia nie może przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Experience));

        RuleFor(x => x.OtherPetsInfo)
            .MaximumLength(1000).WithMessage("Informacje o innych zwierzętach nie mogą przekraczać 1000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.OtherPetsInfo));

        // Walidacja pól strukturalnych do dopasowania
        RuleFor(x => x.HousingType)
            .Must(v => v == null || new[] { "apartment", "house", "houseWithGarden" }.Contains(v))
            .WithMessage("Nieprawidłowy typ mieszkania")
            .When(x => !string.IsNullOrWhiteSpace(x.HousingType));

        RuleFor(x => x.ExperienceLevelApplicant)
            .Must(v => v == null || new[] { "none", "basic", "intermediate", "advanced" }.Contains(v))
            .WithMessage("Nieprawidłowy poziom doświadczenia")
            .When(x => !string.IsNullOrWhiteSpace(x.ExperienceLevelApplicant));

        RuleFor(x => x.AvailableCareTime)
            .Must(v => v == null || new[] { "lessThan1Hour", "oneToThreeHours", "moreThan3Hours" }.Contains(v))
            .WithMessage("Nieprawidłowy czas opieki")
            .When(x => !string.IsNullOrWhiteSpace(x.AvailableCareTime));
    }
}
