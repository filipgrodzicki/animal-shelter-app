using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;

namespace ShelterApp.Api.Features.Adoptions.Shared;

public static class AdoptionMappingExtensions
{
    public static AdopterDto ToDto(this Adopter adopter)
    {
        return new AdopterDto(
            Id: adopter.Id,
            UserId: adopter.UserId,
            FirstName: adopter.FirstName,
            LastName: adopter.LastName,
            FullName: adopter.FullName,
            Email: adopter.Email,
            Phone: adopter.Phone,
            Address: adopter.Address,
            City: adopter.City,
            PostalCode: adopter.PostalCode,
            DateOfBirth: adopter.DateOfBirth,
            Age: adopter.Age,
            Status: adopter.Status.ToString(),
            RodoConsentDate: adopter.RodoConsentDate,
            CreatedAt: adopter.CreatedAt,
            UpdatedAt: adopter.UpdatedAt
        );
    }

    public static AdopterSummaryDto ToSummaryDto(this Adopter adopter)
    {
        return new AdopterSummaryDto(
            Id: adopter.Id,
            FullName: adopter.FullName,
            Email: adopter.Email,
            Phone: adopter.Phone,
            Status: adopter.Status.ToString()
        );
    }

    public static AdoptionApplicationDto ToDto(
        this AdoptionApplication application,
        Adopter? adopter = null,
        Animal? animal = null)
    {
        return new AdoptionApplicationDto(
            Id: application.Id,
            AdopterId: application.AdopterId,
            AnimalId: application.AnimalId,
            Status: application.Status.ToString(),
            ApplicationDate: application.ApplicationDate,
            ReviewedByUserId: application.ReviewedByUserId,
            ReviewDate: application.ReviewDate,
            ReviewNotes: application.ReviewNotes,
            ScheduledVisitDate: application.ScheduledVisitDate,
            VisitDate: application.VisitDate,
            VisitNotes: application.VisitNotes,
            VisitAssessment: application.VisitAssessment,
            ContractNumber: application.ContractNumber,
            ContractSignedDate: application.ContractSignedDate,
            AdoptionMotivation: application.AdoptionMotivation,
            PetExperience: application.PetExperience,
            LivingConditions: application.LivingConditions,
            OtherPetsInfo: application.OtherPetsInfo,
            RejectionReason: application.RejectionReason,
            CompletionDate: application.CompletionDate,
            Adopter: adopter?.ToSummaryDto(),
            Animal: animal?.ToAdoptionSummaryDto(),
            PermittedActions: application.GetPermittedTriggers().Select(t => t.ToString())
        );
    }

    public static AdoptionApplicationListItemDto ToListItemDto(
        this AdoptionApplication application,
        string adopterName,
        string adopterEmail,
        string animalName,
        string animalRegistrationNumber)
    {
        return new AdoptionApplicationListItemDto(
            Id: application.Id,
            ApplicationNumber: application.Id.ToString("N")[..8].ToUpper(),
            AdopterId: application.AdopterId,
            AdopterName: adopterName,
            AdopterEmail: adopterEmail,
            AnimalId: application.AnimalId,
            AnimalName: animalName,
            AnimalRegistrationNumber: animalRegistrationNumber,
            Status: application.Status.ToString(),
            ApplicationDate: application.ApplicationDate,
            ScheduledVisitDate: application.ScheduledVisitDate,
            CompletionDate: application.CompletionDate
        );
    }

    public static AdoptionApplicationStatusChangeDto ToDto(this AdoptionApplicationStatusChange statusChange)
    {
        return new AdoptionApplicationStatusChangeDto(
            Id: statusChange.Id,
            PreviousStatus: statusChange.PreviousStatus.ToString(),
            NewStatus: statusChange.NewStatus.ToString(),
            Trigger: statusChange.Trigger.ToString(),
            ChangedBy: statusChange.ChangedBy,
            Reason: statusChange.Reason,
            ChangedAt: statusChange.ChangedAt
        );
    }

    public static AnimalSummaryForAdoptionDto ToAdoptionSummaryDto(this Animal animal)
    {
        var mainPhoto = animal.Photos.FirstOrDefault(p => p.IsMain) ?? animal.Photos.FirstOrDefault();

        return new AnimalSummaryForAdoptionDto(
            Id: animal.Id,
            RegistrationNumber: animal.RegistrationNumber,
            Name: animal.Name,
            Species: animal.Species.ToString(),
            Breed: animal.Breed,
            MainPhotoUrl: mainPhoto?.FilePath,
            Status: animal.Status.ToString()
        );
    }
}
