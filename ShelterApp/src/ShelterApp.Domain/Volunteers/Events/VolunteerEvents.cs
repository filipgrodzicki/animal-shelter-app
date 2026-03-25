using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Volunteers.Events;

/// <summary>
/// Event: Volunteer application was submitted
/// </summary>
public sealed record VolunteerApplicationSubmittedEvent(
    Guid VolunteerId,
    string Email,
    string FullName
) : DomainEvent;

/// <summary>
/// Event: Volunteer status was changed
/// </summary>
public sealed record VolunteerStatusChangedEvent(
    Guid VolunteerId,
    VolunteerStatus PreviousStatus,
    VolunteerStatus NewStatus,
    VolunteerStatusTrigger Trigger,
    string ChangedBy,
    string? Reason
) : DomainEvent;

/// <summary>
/// Event: Volunteer was activated (completed training)
/// </summary>
public sealed record VolunteerActivatedEvent(
    Guid VolunteerId,
    string Email,
    string FullName,
    string ContractNumber
) : DomainEvent;

/// <summary>
/// Event: Volunteer work hours were recorded
/// </summary>
public sealed record VolunteerHoursRecordedEvent(
    Guid VolunteerId,
    decimal HoursAdded,
    decimal TotalHours,
    string RecordedBy
) : DomainEvent;

/// <summary>
/// Event: Volunteer was suspended
/// </summary>
public sealed record VolunteerSuspendedEvent(
    Guid VolunteerId,
    string Email,
    string FullName,
    string Reason,
    string SuspendedBy
) : DomainEvent;

/// <summary>
/// Event: Volunteer resigned
/// </summary>
public sealed record VolunteerResignedEvent(
    Guid VolunteerId,
    string Email,
    string FullName,
    string? Reason
) : DomainEvent;

/// <summary>
/// Event: Volunteer information was updated
/// </summary>
public sealed record VolunteerInfoUpdatedEvent(
    Guid VolunteerId,
    string FullName,
    string Email
) : DomainEvent;
