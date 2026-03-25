using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;

namespace ShelterApp.Api.Features.Animals.Commands;

// ============================================
// Command
// ============================================
public record RegisterAnimalCommand(
    string Species,
    string Breed,
    string Name,
    int? AgeInMonths,
    string Gender,
    string Size,
    string Color,
    string? ChipNumber,
    DateTime? AdmissionDate,
    string AdmissionCircumstances,
    string? Description,
    string ExperienceLevel,
    string ChildrenCompatibility,
    string AnimalCompatibility,
    string SpaceRequirement,
    string CareTime,
    Guid RegisteredByUserId,
    // Znaki szczególne (wymagane przez rozporządzenie)
    string? DistinguishingMarks = null,
    // Opcjonalne dane osoby oddającej zwierzę
    string? SurrenderedByFirstName = null,
    string? SurrenderedByLastName = null,
    string? SurrenderedByPhone = null
) : ICommand<Result<AnimalDto>>;

// ============================================
// Handler
// ============================================
public class RegisterAnimalHandler : ICommandHandler<RegisterAnimalCommand, Result<AnimalDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IRegistrationNumberGenerator _registrationNumberGenerator;

    public RegisterAnimalHandler(
        ShelterDbContext context,
        IRegistrationNumberGenerator registrationNumberGenerator)
    {
        _context = context;
        _registrationNumberGenerator = registrationNumberGenerator;
    }

    public async Task<Result<AnimalDto>> Handle(
        RegisterAnimalCommand request,
        CancellationToken cancellationToken)
    {
        // Parse enums
        if (!Enum.TryParse<Species>(request.Species, true, out var species))
            return Result.Failure<AnimalDto>(Error.Validation($"Nieprawidłowy gatunek: {request.Species}"));

        if (!Enum.TryParse<Gender>(request.Gender, true, out var gender))
            return Result.Failure<AnimalDto>(Error.Validation($"Nieprawidłowa płeć: {request.Gender}"));

        if (!Enum.TryParse<Size>(request.Size, true, out var size))
            return Result.Failure<AnimalDto>(Error.Validation($"Nieprawidłowy rozmiar: {request.Size}"));

        if (!Enum.TryParse<ExperienceLevel>(request.ExperienceLevel, true, out var experienceLevel))
            return Result.Failure<AnimalDto>(Error.Validation($"Nieprawidłowy poziom aktywności: {request.ExperienceLevel}"));

        if (!Enum.TryParse<ChildrenCompatibility>(request.ChildrenCompatibility, true, out var childrenCompatibility))
            return Result.Failure<AnimalDto>(Error.Validation($"Nieprawidłowa zgodność z dziećmi: {request.ChildrenCompatibility}"));

        if (!Enum.TryParse<AnimalCompatibility>(request.AnimalCompatibility, true, out var animalCompatibility))
            return Result.Failure<AnimalDto>(Error.Validation($"Nieprawidłowa zgodność z innymi zwierzętami: {request.AnimalCompatibility}"));

        if (!Enum.TryParse<SpaceRequirement>(request.SpaceRequirement, true, out var spaceRequirement))
            return Result.Failure<AnimalDto>(Error.Validation($"Nieprawidłowe wymagania przestrzeni: {request.SpaceRequirement}"));

        if (!Enum.TryParse<CareTime>(request.CareTime, true, out var careTime))
            return Result.Failure<AnimalDto>(Error.Validation($"Nieprawidłowy czas opieki: {request.CareTime}"));

        // Check for duplicate chip number
        if (!string.IsNullOrWhiteSpace(request.ChipNumber))
        {
            var chipExists = await _context.Animals
                .AnyAsync(a => a.ChipNumber == request.ChipNumber, cancellationToken);

            if (chipExists)
                return Result.Failure<AnimalDto>(Error.Conflict($"Zwierzę z numerem chip {request.ChipNumber} już istnieje"));
        }

        // Generate registration number
        var registrationNumber = await _registrationNumberGenerator.GenerateAsync(species, cancellationToken);

        // Create animal
        var animal = Animal.Create(
            registrationNumber: registrationNumber,
            species: species,
            breed: request.Breed,
            name: request.Name,
            gender: gender,
            size: size,
            color: request.Color,
            admissionCircumstances: request.AdmissionCircumstances,
            ageInMonths: request.AgeInMonths,
            chipNumber: request.ChipNumber,
            admissionDate: request.AdmissionDate,
            description: request.Description,
            experienceLevel: experienceLevel,
            childrenCompatibility: childrenCompatibility,
            animalCompatibility: animalCompatibility,
            spaceRequirement: spaceRequirement,
            careTime: careTime,
            surrenderedByFirstName: request.SurrenderedByFirstName,
            surrenderedByLastName: request.SurrenderedByLastName,
            surrenderedByPhone: request.SurrenderedByPhone,
            distinguishingMarks: request.DistinguishingMarks
        );

        _context.Animals.Add(animal);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(animal.ToDto());
    }
}

// ============================================
// Validator
// ============================================
public class RegisterAnimalValidator : AbstractValidator<RegisterAnimalCommand>
{
    private static readonly string[] ValidSpecies = { "Dog", "Cat", "Other" };
    private static readonly string[] ValidGenders = { "Male", "Female", "Unknown" };
    private static readonly string[] ValidSizes = { "Small", "Medium", "Large" };
    private static readonly string[] ValidExperienceLevels = { "None", "Basic", "Advanced" };
    private static readonly string[] ValidChildrenCompatibility = { "Yes", "Partially", "No" };
    private static readonly string[] ValidAnimalCompatibility = { "Yes", "Partially", "No" };
    private static readonly string[] ValidSpaceRequirements = { "Apartment", "House", "HouseWithGarden" };
    private static readonly string[] ValidCareTime = { "LessThan1Hour", "OneToThreeHours", "MoreThan3Hours" };

    public RegisterAnimalValidator()
    {
        RuleFor(x => x.Species)
            .NotEmpty().WithMessage("Gatunek jest wymagany")
            .Must(s => ValidSpecies.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Gatunek musi być jednym z: {string.Join(", ", ValidSpecies)}");

        RuleFor(x => x.Breed)
            .NotEmpty().WithMessage("Rasa jest wymagana")
            .MaximumLength(100).WithMessage("Rasa nie może przekraczać 100 znaków");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Imię jest wymagane")
            .MaximumLength(100).WithMessage("Imię nie może przekraczać 100 znaków");

        RuleFor(x => x.AgeInMonths)
            .GreaterThanOrEqualTo(0).WithMessage("Wiek musi być liczbą nieujemną")
            .LessThanOrEqualTo(360).WithMessage("Wiek nie może przekraczać 360 miesięcy (30 lat)")
            .When(x => x.AgeInMonths.HasValue);

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Płeć jest wymagana")
            .Must(g => ValidGenders.Contains(g, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Płeć musi być jedną z: {string.Join(", ", ValidGenders)}");

        RuleFor(x => x.Size)
            .NotEmpty().WithMessage("Rozmiar jest wymagany")
            .Must(s => ValidSizes.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Rozmiar musi być jednym z: {string.Join(", ", ValidSizes)}");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Umaszczenie jest wymagane")
            .MaximumLength(100).WithMessage("Umaszczenie nie może przekraczać 100 znaków");

        RuleFor(x => x.ChipNumber)
            .MaximumLength(50).WithMessage("Numer chip nie może przekraczać 50 znaków")
            .Matches(@"^[A-Za-z0-9]*$").WithMessage("Numer chip może zawierać tylko litery i cyfry")
            .When(x => !string.IsNullOrWhiteSpace(x.ChipNumber));

        RuleFor(x => x.AdmissionDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Data przyjęcia nie może być z przyszłości")
            .When(x => x.AdmissionDate.HasValue);

        RuleFor(x => x.DistinguishingMarks)
            .MaximumLength(500).WithMessage("Znaki szczególne nie mogą przekraczać 500 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.DistinguishingMarks));

        RuleFor(x => x.AdmissionCircumstances)
            .NotEmpty().WithMessage("Okoliczności przyjęcia są wymagane")
            .MaximumLength(2000).WithMessage("Okoliczności przyjęcia nie mogą przekraczać 2000 znaków");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Opis nie może przekraczać 4000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.ExperienceLevel)
            .NotEmpty().WithMessage("Wymagane doświadczenie jest wymagany")
            .Must(a => ValidExperienceLevels.Contains(a, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Wymagane doświadczenie musi być jednym z: {string.Join(", ", ValidExperienceLevels)}");

        RuleFor(x => x.ChildrenCompatibility)
            .NotEmpty().WithMessage("Zgodność z dziećmi jest wymagana")
            .Must(c => ValidChildrenCompatibility.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Zgodność z dziećmi musi być jedną z: {string.Join(", ", ValidChildrenCompatibility)}");

        RuleFor(x => x.AnimalCompatibility)
            .NotEmpty().WithMessage("Zgodność z innymi zwierzętami jest wymagana")
            .Must(a => ValidAnimalCompatibility.Contains(a, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Zgodność z innymi zwierzętami musi być jedną z: {string.Join(", ", ValidAnimalCompatibility)}");

        RuleFor(x => x.SpaceRequirement)
            .NotEmpty().WithMessage("Wymagania przestrzeni są wymagane")
            .Must(s => ValidSpaceRequirements.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Wymagania przestrzeni muszą być jednym z: {string.Join(", ", ValidSpaceRequirements)}");

        RuleFor(x => x.CareTime)
            .NotEmpty().WithMessage("Czas opieki jest wymagany")
            .Must(c => ValidCareTime.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Czas opieki musi być jednym z: {string.Join(", ", ValidCareTime)}");

        RuleFor(x => x.RegisteredByUserId)
            .NotEmpty().WithMessage("ID użytkownika rejestrującego jest wymagane");
    }
}
