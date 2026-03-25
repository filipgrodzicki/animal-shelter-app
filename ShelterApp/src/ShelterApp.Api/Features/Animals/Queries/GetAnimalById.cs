using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Queries;

// ============================================
// Query
// ============================================
public record GetAnimalByIdQuery(Guid Id) : IQuery<Result<AnimalDetailDto>>;

// ============================================
// Response DTO
// ============================================
public record AnimalDetailDto(
    Guid Id,
    string RegistrationNumber,
    string Species,
    string Breed,
    string Name,
    int? AgeInMonths,
    string Gender,
    string Size,
    string Color,
    string? ChipNumber,
    string? DistinguishingMarks,
    DateTime AdmissionDate,
    string AdmissionCircumstances,
    string Status,
    string? Description,
    string ExperienceLevel,
    string ChildrenCompatibility,
    string AnimalCompatibility,
    string SpaceRequirement,
    string CareTime,
    SurrenderedByDto? SurrenderedBy,
    AdoptionInfoDto? AdoptionInfo,
    IEnumerable<AnimalPhotoDto> Photos,
    IEnumerable<AnimalStatusChangeDto> StatusHistory,
    IEnumerable<MedicalRecordDto> MedicalRecords,
    IEnumerable<string> PermittedActions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record SurrenderedByDto(
    string FirstName,
    string LastName,
    string? Phone
);

public record AdoptionInfoDto(
    DateTime AdoptionDate,
    string? ReleaseCircumstances,
    AdopterDto Adopter
);

public record AdopterDto(
    string FirstName,
    string LastName,
    string? Phone,
    string? Address
);

// ============================================
// Handler
// ============================================
public class GetAnimalByIdHandler : IQueryHandler<GetAnimalByIdQuery, Result<AnimalDetailDto>>
{
    private readonly ShelterDbContext _context;

    public GetAnimalByIdHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AnimalDetailDto>> Handle(
        GetAnimalByIdQuery request,
        CancellationToken cancellationToken)
    {
        var animal = await _context.Animals
            .Include(a => a.Photos)
            .Include(a => a.StatusHistory)
            .Include(a => a.MedicalRecords)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<AnimalDetailDto>(
                Error.NotFound("Animal", request.Id));
        }

        // Get permitted actions
        var permittedActions = animal.GetPermittedTriggers()
            .Select(t => t.ToString())
            .ToList();

        // Build SurrenderedBy DTO if available
        SurrenderedByDto? surrenderedBy = null;
        if (!string.IsNullOrWhiteSpace(animal.SurrenderedByFirstName) &&
            !string.IsNullOrWhiteSpace(animal.SurrenderedByLastName))
        {
            surrenderedBy = new SurrenderedByDto(
                animal.SurrenderedByFirstName,
                animal.SurrenderedByLastName,
                animal.SurrenderedByPhone
            );
        }

        // Get adoption info for adopted animals
        AdoptionInfoDto? adoptionInfo = null;
        if (animal.Status == Domain.Animals.AnimalStatus.Adopted)
        {
            var completedApplication = await _context.AdoptionApplications
                .Include(a => a.StatusHistory)
                .Where(a => a.AnimalId == animal.Id &&
                           a.Status == Domain.Adoptions.AdoptionApplicationStatus.Completed)
                .FirstOrDefaultAsync(cancellationToken);

            if (completedApplication != null)
            {
                var adopter = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == completedApplication.AdopterId, cancellationToken);

                if (adopter != null)
                {
                    var adoptionDate = completedApplication.ContractSignedDate ??
                                       completedApplication.CompletionDate ??
                                       completedApplication.UpdatedAt ??
                                       DateTime.UtcNow;

                    adoptionInfo = new AdoptionInfoDto(
                        adoptionDate,
                        null, // ReleaseCircumstances - can be added later
                        new AdopterDto(
                            adopter.FirstName,
                            adopter.LastName,
                            adopter.PhoneNumber,
                            null // Address - not stored currently
                        )
                    );
                }
            }
        }

        var dto = new AnimalDetailDto(
            Id: animal.Id,
            RegistrationNumber: animal.RegistrationNumber,
            Species: animal.Species.ToString(),
            Breed: animal.Breed,
            Name: animal.Name,
            AgeInMonths: animal.AgeInMonths,
            Gender: animal.Gender.ToString(),
            Size: animal.Size.ToString(),
            Color: animal.Color,
            ChipNumber: animal.ChipNumber,
            DistinguishingMarks: animal.DistinguishingMarks,
            AdmissionDate: animal.AdmissionDate,
            AdmissionCircumstances: animal.AdmissionCircumstances,
            Status: animal.Status.ToString(),
            Description: animal.Description,
            ExperienceLevel: animal.ExperienceLevel.ToString(),
            ChildrenCompatibility: animal.ChildrenCompatibility.ToString(),
            AnimalCompatibility: animal.AnimalCompatibility.ToString(),
            SpaceRequirement: animal.SpaceRequirement.ToString(),
            CareTime: animal.CareTime.ToString(),
            SurrenderedBy: surrenderedBy,
            AdoptionInfo: adoptionInfo,
            Photos: animal.Photos
                .OrderBy(p => p.DisplayOrder)
                .Select(p => p.ToDto()),
            StatusHistory: animal.StatusHistory
                .OrderByDescending(s => s.ChangedAt)
                .Select(s => s.ToDto()),
            MedicalRecords: animal.MedicalRecords
                .OrderByDescending(m => m.RecordDate)
                .Select(m => m.ToDto()),
            PermittedActions: permittedActions,
            CreatedAt: animal.CreatedAt,
            UpdatedAt: animal.UpdatedAt
        );

        return Result.Success(dto);
    }
}

// ============================================
// Validator
// ============================================
public class GetAnimalByIdValidator : AbstractValidator<GetAnimalByIdQuery>
{
    public GetAnimalByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");
    }
}
