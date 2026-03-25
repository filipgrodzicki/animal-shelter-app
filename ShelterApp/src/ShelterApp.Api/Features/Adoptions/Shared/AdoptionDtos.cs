using ShelterApp.Domain.Adoptions;

namespace ShelterApp.Api.Features.Adoptions.Shared;

// ============================================
// Adopter DTOs
// ============================================

public record AdopterDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Phone,
    string? Address,
    string? City,
    string? PostalCode,
    DateTime DateOfBirth,
    int Age,
    string Status,
    DateTime? RodoConsentDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record AdopterSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string Status
);

// ============================================
// Adoption Application DTOs
// ============================================

public record AdoptionApplicationDto(
    Guid Id,
    Guid AdopterId,
    Guid AnimalId,
    string Status,
    DateTime ApplicationDate,
    // Review Info
    Guid? ReviewedByUserId,
    DateTime? ReviewDate,
    string? ReviewNotes,
    // Visit Info
    DateTime? ScheduledVisitDate,
    DateTime? VisitDate,
    string? VisitNotes,
    int? VisitAssessment,
    // Contract Info
    string? ContractNumber,
    DateTime? ContractSignedDate,
    // Additional Info
    string? AdoptionMotivation,
    string? PetExperience,
    string? LivingConditions,
    string? OtherPetsInfo,
    string? RejectionReason,
    DateTime? CompletionDate,
    // Relations
    AdopterSummaryDto? Adopter,
    AnimalSummaryForAdoptionDto? Animal,
    IEnumerable<string> PermittedActions
);

public record AdoptionApplicationListItemDto(
    Guid Id,
    string ApplicationNumber,
    Guid AdopterId,
    string AdopterName,
    string AdopterEmail,
    Guid AnimalId,
    string AnimalName,
    string AnimalRegistrationNumber,
    string Status,
    DateTime ApplicationDate,
    DateTime? ScheduledVisitDate,
    DateTime? CompletionDate
);

public record AdoptionApplicationStatusChangeDto(
    Guid Id,
    string PreviousStatus,
    string NewStatus,
    string Trigger,
    string ChangedBy,
    string? Reason,
    DateTime ChangedAt
);

// ============================================
// Animal Summary for Adoption
// ============================================

public record AnimalSummaryForAdoptionDto(
    Guid Id,
    string RegistrationNumber,
    string Name,
    string Species,
    string Breed,
    string? MainPhotoUrl,
    string Status
);

// ============================================
// Submit Application Result
// ============================================

public record SubmitApplicationResultDto(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId,
    string ApplicationStatus,
    string AnimalStatus,
    string Message
);
