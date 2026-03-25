using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Entities;

namespace ShelterApp.Api.Features.Animals.Shared;

public static class AnimalMappingExtensions
{
    public static AnimalDto ToDto(this Animal animal)
    {
        var mainPhoto = animal.Photos.FirstOrDefault(p => p.IsMain);

        return new AnimalDto(
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
            AdmissionDate: animal.AdmissionDate,
            AdmissionCircumstances: animal.AdmissionCircumstances,
            Status: animal.Status.ToString(),
            Description: animal.Description,
            ExperienceLevel: animal.ExperienceLevel.ToString(),
            ChildrenCompatibility: animal.ChildrenCompatibility.ToString(),
            AnimalCompatibility: animal.AnimalCompatibility.ToString(),
            SpaceRequirement: animal.SpaceRequirement.ToString(),
            CareTime: animal.CareTime.ToString(),
            MainPhotoUrl: mainPhoto?.FilePath,
            CreatedAt: animal.CreatedAt,
            UpdatedAt: animal.UpdatedAt
        );
    }

    public static AnimalListItemDto ToListItemDto(this Animal animal)
    {
        var mainPhoto = animal.Photos.FirstOrDefault(p => p.IsMain);

        return new AnimalListItemDto(
            Id: animal.Id,
            RegistrationNumber: animal.RegistrationNumber,
            Species: animal.Species.ToString(),
            Breed: animal.Breed,
            Name: animal.Name,
            AgeInMonths: animal.AgeInMonths,
            Gender: animal.Gender.ToString(),
            Size: animal.Size.ToString(),
            Status: animal.Status.ToString(),
            MainPhotoUrl: mainPhoto?.FilePath,
            AdmissionDate: animal.AdmissionDate
        );
    }

    public static AnimalStatusChangeDto ToDto(this AnimalStatusChange statusChange)
    {
        return new AnimalStatusChangeDto(
            Id: statusChange.Id,
            PreviousStatus: statusChange.PreviousStatus.ToString(),
            NewStatus: statusChange.NewStatus.ToString(),
            Trigger: statusChange.Trigger.ToString(),
            Reason: statusChange.Reason,
            ChangedBy: statusChange.ChangedBy,
            ChangedAt: statusChange.ChangedAt
        );
    }

    public static MedicalRecordDto ToDto(this MedicalRecord record)
    {
        return new MedicalRecordDto(
            Id: record.Id,
            Type: record.Type.ToString(),
            Title: record.Title,
            Description: record.Description,
            RecordDate: record.RecordDate,
            Diagnosis: record.Diagnosis,
            Treatment: record.Treatment,
            Medications: record.Medications,
            NextVisitDate: record.NextVisitDate,
            VeterinarianName: record.VeterinarianName,
            Notes: record.Notes,
            Cost: record.Cost,
            EnteredBy: record.EnteredBy,
            EnteredByUserId: record.EnteredByUserId,
            Attachments: record.Attachments.Select(a => a.ToDto()).ToList(),
            CreatedAt: record.CreatedAt
        );
    }

    public static MedicalRecordAttachmentDto ToDto(this MedicalRecordAttachment attachment)
    {
        return new MedicalRecordAttachmentDto(
            Id: attachment.Id,
            FileName: attachment.FileName,
            Url: attachment.FilePath,
            ContentType: attachment.ContentType,
            FileSize: attachment.FileSize,
            Description: attachment.Description,
            CreatedAt: attachment.CreatedAt
        );
    }

    public static AnimalPhotoDto ToDto(this AnimalPhoto photo)
    {
        return new AnimalPhotoDto(
            Id: photo.Id,
            FileName: photo.FileName,
            Url: photo.FilePath,
            ThumbnailUrl: photo.FilePath,
            IsMain: photo.IsMain,
            Description: photo.Description,
            DisplayOrder: photo.DisplayOrder
        );
    }
}
