using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Commands;

// ============================================
// Command
// ============================================
public record AddMedicalRecordCommand(
    Guid AnimalId,
    string Type,
    string Title,
    string Description,
    DateTime? RecordDate,
    string? Diagnosis,
    string? Treatment,
    string? Medications,
    DateTime? NextVisitDate,
    string VeterinarianName,
    string? Notes,
    decimal? Cost,
    // Dane osoby wprowadzającej wpis (wymagane przez WF-06)
    string EnteredBy,
    Guid? EnteredByUserId = null
) : ICommand<Result<MedicalRecordDto>>;

// ============================================
// Handler
// ============================================
public class AddMedicalRecordHandler : ICommandHandler<AddMedicalRecordCommand, Result<MedicalRecordDto>>
{
    private readonly ShelterDbContext _context;

    public AddMedicalRecordHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MedicalRecordDto>> Handle(
        AddMedicalRecordCommand request,
        CancellationToken cancellationToken)
    {
        // Parse type enum
        if (!Enum.TryParse<MedicalRecordType>(request.Type, true, out var type))
        {
            return Result.Failure<MedicalRecordDto>(
                Error.Validation($"Nieprawidłowy typ rekordu medycznego: {request.Type}"));
        }

        // Get animal with medical records
        var animal = await _context.Animals
            .Include(a => a.MedicalRecords)
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<MedicalRecordDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        // Add medical record through domain method
        var result = animal.AddMedicalRecord(
            type: type,
            title: request.Title,
            description: request.Description,
            veterinarianName: request.VeterinarianName,
            enteredBy: request.EnteredBy,
            enteredByUserId: request.EnteredByUserId,
            recordDate: request.RecordDate,
            diagnosis: request.Diagnosis,
            treatment: request.Treatment,
            medications: request.Medications,
            nextVisitDate: request.NextVisitDate,
            notes: request.Notes,
            cost: request.Cost
        );

        if (result.IsFailure)
        {
            return Result.Failure<MedicalRecordDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.ToDto());
    }
}

// ============================================
// Validator
// ============================================
public class AddMedicalRecordValidator : AbstractValidator<AddMedicalRecordCommand>
{
    private static readonly string[] ValidTypes = Enum.GetNames<MedicalRecordType>();

    public AddMedicalRecordValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Typ rekordu jest wymagany")
            .Must(t => ValidTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Typ musi być jednym z: {string.Join(", ", ValidTypes)}");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tytuł jest wymagany")
            .MaximumLength(200).WithMessage("Tytuł nie może przekraczać 200 znaków");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Opis jest wymagany")
            .MaximumLength(4000).WithMessage("Opis nie może przekraczać 4000 znaków");

        RuleFor(x => x.RecordDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Data rekordu nie może być w przyszłości")
            .When(x => x.RecordDate.HasValue);

        RuleFor(x => x.Diagnosis)
            .MaximumLength(2000).WithMessage("Diagnoza nie może przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Diagnosis));

        RuleFor(x => x.Treatment)
            .MaximumLength(2000).WithMessage("Leczenie nie może przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Treatment));

        RuleFor(x => x.Medications)
            .MaximumLength(2000).WithMessage("Leki nie mogą przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Medications));

        RuleFor(x => x.NextVisitDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Data następnej wizyty musi być w przyszłości")
            .When(x => x.NextVisitDate.HasValue);

        RuleFor(x => x.VeterinarianName)
            .NotEmpty().WithMessage("Imię i nazwisko weterynarza jest wymagane")
            .MaximumLength(200).WithMessage("Imię i nazwisko weterynarza nie może przekraczać 200 znaków");

        RuleFor(x => x.EnteredBy)
            .NotEmpty().WithMessage("Dane osoby wprowadzającej wpis są wymagane")
            .MaximumLength(200).WithMessage("Dane osoby wprowadzającej nie mogą przekraczać 200 znaków");

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notatki nie mogą przekraczać 4000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Koszt musi być liczbą nieujemną")
            .When(x => x.Cost.HasValue);

        // Custom rules for specific record types
        When(x => IsVaccinationType(x.Type), () =>
        {
            RuleFor(x => x.NextVisitDate)
                .NotEmpty().WithMessage("Data następnego szczepienia jest wymagana dla szczepień");
        });

        When(x => IsTreatmentType(x.Type), () =>
        {
            RuleFor(x => x.Diagnosis)
                .NotEmpty().WithMessage("Diagnoza jest wymagana dla leczenia");

            RuleFor(x => x.Treatment)
                .NotEmpty().WithMessage("Plan leczenia jest wymagany");
        });
    }

    private static bool IsVaccinationType(string type) =>
        type.Equals(nameof(MedicalRecordType.Vaccination), StringComparison.OrdinalIgnoreCase);

    private static bool IsTreatmentType(string type) =>
        type.Equals(nameof(MedicalRecordType.Treatment), StringComparison.OrdinalIgnoreCase);
}
