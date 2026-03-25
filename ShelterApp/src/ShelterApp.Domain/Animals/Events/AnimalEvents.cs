using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Animals.Events;

public sealed record AnimalAdmittedEvent(
    Guid AnimalId,
    string RegistrationNumber,
    string Name
) : DomainEvent;

public sealed record AnimalStatusChangedEvent(
    Guid AnimalId,
    AnimalStatus PreviousStatus,
    AnimalStatus NewStatus,
    AnimalStatusTrigger Trigger,
    string ChangedBy,
    string? Reason
) : DomainEvent;

public sealed record AnimalAdoptedEvent(
    Guid AnimalId,
    string RegistrationNumber,
    string Name,
    string AdoptedBy
) : DomainEvent;

public sealed record AnimalDeceasedEvent(
    Guid AnimalId,
    string RegistrationNumber,
    string Name,
    string? Reason
) : DomainEvent;

public sealed record MedicalRecordAddedEvent(
    Guid AnimalId,
    Guid MedicalRecordId,
    string RecordType,
    string Title
) : DomainEvent;

public sealed record AnimalPhotoAddedEvent(
    Guid AnimalId,
    Guid PhotoId,
    string FileName,
    bool IsMain
) : DomainEvent;
