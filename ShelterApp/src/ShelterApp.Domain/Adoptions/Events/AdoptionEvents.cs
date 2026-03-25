using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Adoptions.Events;

#region Adopter Events

/// <summary>
/// Event: Adopter was registered
/// </summary>
public sealed record AdopterRegisteredEvent(
    Guid AdopterId,
    Guid UserId,
    string Email,
    string FullName
) : DomainEvent;

/// <summary>
/// Event: Adopter status was changed
/// </summary>
public sealed record AdopterStatusChangedEvent(
    Guid AdopterId,
    AdopterStatus PreviousStatus,
    AdopterStatus NewStatus,
    AdopterStatusTrigger Trigger,
    string ChangedBy,
    string? Reason
) : DomainEvent;

/// <summary>
/// Event: Adopter information was updated
/// </summary>
public sealed record AdopterInfoUpdatedEvent(
    Guid AdopterId,
    string FullName,
    string Email
) : DomainEvent;

#endregion

#region Adoption Application Events

/// <summary>
/// Event: Adoption application was created
/// </summary>
public sealed record AdoptionApplicationCreatedEvent(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId
) : DomainEvent;

/// <summary>
/// Event: Adoption application status was changed
/// </summary>
public sealed record AdoptionApplicationStatusChangedEvent(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId,
    AdoptionApplicationStatus PreviousStatus,
    AdoptionApplicationStatus NewStatus,
    AdoptionApplicationTrigger Trigger,
    string ChangedBy,
    string? Reason
) : DomainEvent;

/// <summary>
/// Event: Adoption application was taken for review by staff
/// </summary>
public sealed record ApplicationTakenForReviewEvent(
    Guid ApplicationId,
    Guid ReviewerUserId,
    string ReviewerName
) : DomainEvent;

/// <summary>
/// Event: Visit was scheduled
/// </summary>
public sealed record VisitScheduledEvent(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId,
    DateTime ScheduledDate
) : DomainEvent;

/// <summary>
/// Event: Visit was conducted
/// </summary>
public sealed record VisitCompletedEvent(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId,
    bool IsPositive,
    int Assessment,
    string Notes
) : DomainEvent;

/// <summary>
/// Event: Adoption contract was generated
/// </summary>
public sealed record ContractGeneratedEvent(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId,
    string ContractNumber
) : DomainEvent;

/// <summary>
/// Event: Adoption was completed successfully
/// </summary>
public sealed record AdoptionCompletedEvent(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId,
    string? ContractNumber
) : DomainEvent;

/// <summary>
/// Event: Adoption application was rejected
/// </summary>
public sealed record ApplicationRejectedEvent(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId,
    string Reason,
    string RejectedBy
) : DomainEvent;

/// <summary>
/// Event: Adoption application was cancelled
/// </summary>
public sealed record ApplicationCancelledEvent(
    Guid ApplicationId,
    Guid AdopterId,
    Guid AnimalId,
    string Reason,
    string CancelledBy
) : DomainEvent;

#endregion
