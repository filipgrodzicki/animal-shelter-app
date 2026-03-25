namespace ShelterApp.Api.Features.Animals.Shared;

public record AnimalDto(
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
    DateTime AdmissionDate,
    string AdmissionCircumstances,
    string Status,
    string? Description,
    string ExperienceLevel,
    string ChildrenCompatibility,
    string AnimalCompatibility,
    string SpaceRequirement,
    string CareTime,
    string? MainPhotoUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record AnimalListItemDto(
    Guid Id,
    string RegistrationNumber,
    string Species,
    string Breed,
    string Name,
    int? AgeInMonths,
    string Gender,
    string Size,
    string Status,
    string? MainPhotoUrl,
    DateTime AdmissionDate
);

public record AnimalStatusChangeDto(
    Guid Id,
    string PreviousStatus,
    string NewStatus,
    string Trigger,
    string? Reason,
    string ChangedBy,
    DateTime ChangedAt
);

public record MedicalRecordDto(
    Guid Id,
    string Type,
    string Title,
    string Description,
    DateTime RecordDate,
    string? Diagnosis,
    string? Treatment,
    string? Medications,
    DateTime? NextVisitDate,
    string VeterinarianName,
    string? Notes,
    decimal? Cost,
    // Dane osoby wprowadzającej (WF-06)
    string EnteredBy,
    Guid? EnteredByUserId,
    // Załączniki
    List<MedicalRecordAttachmentDto> Attachments,
    DateTime CreatedAt
);

public record MedicalRecordAttachmentDto(
    Guid Id,
    string FileName,
    string Url,
    string? ContentType,
    long FileSize,
    string? Description,
    DateTime CreatedAt
);

public record AnimalPhotoDto(
    Guid Id,
    string FileName,
    string Url,
    string? ThumbnailUrl,
    bool IsMain,
    string? Description,
    int DisplayOrder
);
