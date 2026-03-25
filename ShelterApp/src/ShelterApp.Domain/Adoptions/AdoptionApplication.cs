using ShelterApp.Domain.Adoptions.Events;
using ShelterApp.Domain.Common;
using Stateless;

namespace ShelterApp.Domain.Adoptions;

/// <summary>
/// Adoption application
/// </summary>
public class AdoptionApplication : AggregateRoot<Guid>
{
    private readonly List<AdoptionApplicationStatusChange> _statusHistory = new();
    private StateMachine<AdoptionApplicationStatus, AdoptionApplicationTrigger>? _stateMachine;

    /// <summary>
    /// Adopter identifier
    /// </summary>
    public Guid AdopterId { get; private set; }

    /// <summary>
    /// Animal identifier
    /// </summary>
    public Guid AnimalId { get; private set; }

    /// <summary>
    /// Application status
    /// </summary>
    public AdoptionApplicationStatus Status { get; private set; }

    /// <summary>
    /// Application submission date
    /// </summary>
    public DateTime ApplicationDate { get; private set; }

    #region Review Information

    /// <summary>
    /// ID of the staff member reviewing the application
    /// </summary>
    public Guid? ReviewedByUserId { get; private set; }

    /// <summary>
    /// Review date
    /// </summary>
    public DateTime? ReviewDate { get; private set; }

    /// <summary>
    /// Review notes
    /// </summary>
    public string? ReviewNotes { get; private set; }

    #endregion

    #region Visit Information

    /// <summary>
    /// Scheduled visit date
    /// </summary>
    public DateTime? ScheduledVisitDate { get; private set; }

    /// <summary>
    /// Actual visit date
    /// </summary>
    public DateTime? VisitDate { get; private set; }

    /// <summary>
    /// Visit notes
    /// </summary>
    public string? VisitNotes { get; private set; }

    /// <summary>
    /// Visit assessment (e.g. 1-5)
    /// </summary>
    public int? VisitAssessment { get; private set; }

    /// <summary>
    /// ID of the staff member conducting the visit
    /// </summary>
    public Guid? VisitConductedByUserId { get; private set; }

    #endregion

    #region Contract Information

    /// <summary>
    /// Contract generation date (kept for backward compatibility)
    /// </summary>
    public DateTime? ContractGeneratedDate { get; private set; }

    /// <summary>
    /// Adoption contract number (kept for backward compatibility)
    /// </summary>
    public string? ContractNumber { get; private set; }

    /// <summary>
    /// Contract signing date (kept for backward compatibility)
    /// </summary>
    public DateTime? ContractSignedDate { get; private set; }

    /// <summary>
    /// Contract file path (kept for backward compatibility)
    /// </summary>
    public string? ContractFilePath { get; private set; }

    /// <summary>
    /// Related adoption contract identifier
    /// </summary>
    public Guid? ContractId { get; private set; }

    #endregion

    #region Additional Information

    /// <summary>
    /// Adoption motivation
    /// </summary>
    public string? AdoptionMotivation { get; private set; }

    /// <summary>
    /// Pet care experience
    /// </summary>
    public string? PetExperience { get; private set; }

    /// <summary>
    /// Living conditions
    /// </summary>
    public string? LivingConditions { get; private set; }

    /// <summary>
    /// Information about other pets at home
    /// </summary>
    public string? OtherPetsInfo { get; private set; }

    /// <summary>
    /// Adopter's housing type (structured data for matching algorithm)
    /// </summary>
    public string? HousingType { get; private set; }

    /// <summary>
    /// Whether the adopter has children
    /// </summary>
    public bool? HasChildren { get; private set; }

    /// <summary>
    /// Whether the adopter has other animals
    /// </summary>
    public bool? HasOtherAnimals { get; private set; }

    /// <summary>
    /// Applicant's experience level (none/basic/intermediate/advanced)
    /// </summary>
    public string? ExperienceLevelApplicant { get; private set; }

    /// <summary>
    /// Available care time (lessThan1Hour/oneToThreeHours/moreThan3Hours)
    /// </summary>
    public string? AvailableCareTime { get; private set; }

    /// <summary>
    /// Rejection or cancellation reason
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Completion date (success or failure)
    /// </summary>
    public DateTime? CompletionDate { get; private set; }

    #endregion

    /// <summary>
    /// Status change history
    /// </summary>
    public IReadOnlyCollection<AdoptionApplicationStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    private AdoptionApplication() { }

    /// <summary>
    /// Creates a new adoption application
    /// </summary>
    public static Result<AdoptionApplication> Create(
        Guid adopterId,
        Guid animalId,
        string? adoptionMotivation = null,
        string? petExperience = null,
        string? livingConditions = null,
        string? otherPetsInfo = null,
        string? housingType = null,
        bool? hasChildren = null,
        bool? hasOtherAnimals = null,
        string? experienceLevelApplicant = null,
        string? availableCareTime = null,
        DateTime? applicationDate = null)
    {
        var application = new AdoptionApplication
        {
            Id = Guid.NewGuid(),
            AdopterId = adopterId,
            AnimalId = animalId,
            Status = AdoptionApplicationStatus.New,
            ApplicationDate = applicationDate ?? DateTime.UtcNow,
            AdoptionMotivation = adoptionMotivation,
            PetExperience = petExperience,
            LivingConditions = livingConditions,
            OtherPetsInfo = otherPetsInfo,
            HousingType = housingType,
            HasChildren = hasChildren,
            HasOtherAnimals = hasOtherAnimals,
            ExperienceLevelApplicant = experienceLevelApplicant,
            AvailableCareTime = availableCareTime
        };

        application.InitializeStateMachine();
        application.AddDomainEvent(new AdoptionApplicationCreatedEvent(
            application.Id, adopterId, animalId));

        return Result.Success(application);
    }

    #region State Machine

    private void InitializeStateMachine()
    {
        _stateMachine = new StateMachine<AdoptionApplicationStatus, AdoptionApplicationTrigger>(
            () => Status,
            s => Status = s);

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        if (_stateMachine is null) return;

        // New -> Submitted (application is submitted)
        _stateMachine.Configure(AdoptionApplicationStatus.New)
            .Permit(AdoptionApplicationTrigger.ZlozenieZgloszenia, AdoptionApplicationStatus.Submitted);

        // Submitted -> UnderReview or Cancelled
        _stateMachine.Configure(AdoptionApplicationStatus.Submitted)
            .Permit(AdoptionApplicationTrigger.PodjęciePrzezPracownika, AdoptionApplicationStatus.UnderReview)
            .Permit(AdoptionApplicationTrigger.AnulowanePrzezUzytkownika, AdoptionApplicationStatus.Cancelled);

        // UnderReview -> Accepted or Rejected
        _stateMachine.Configure(AdoptionApplicationStatus.UnderReview)
            .Permit(AdoptionApplicationTrigger.PozytywnaWeryfikacjaDanych, AdoptionApplicationStatus.Accepted)
            .Permit(AdoptionApplicationTrigger.NegatywnaWeryfikacja, AdoptionApplicationStatus.Rejected);

        // Accepted -> VisitScheduled or Cancelled
        _stateMachine.Configure(AdoptionApplicationStatus.Accepted)
            .Permit(AdoptionApplicationTrigger.RezerwacjaTerminuWizyty, AdoptionApplicationStatus.VisitScheduled)
            .Permit(AdoptionApplicationTrigger.RezygnacjaPoAkceptacji, AdoptionApplicationStatus.Cancelled);

        // VisitScheduled -> VisitCompleted or Cancelled
        _stateMachine.Configure(AdoptionApplicationStatus.VisitScheduled)
            .Permit(AdoptionApplicationTrigger.StawienieSieNaWizyte, AdoptionApplicationStatus.VisitCompleted)
            .Permit(AdoptionApplicationTrigger.NiestawienieSieNaWizyte, AdoptionApplicationStatus.Cancelled);

        // VisitCompleted -> PendingFinalization or Rejected
        _stateMachine.Configure(AdoptionApplicationStatus.VisitCompleted)
            .Permit(AdoptionApplicationTrigger.PozytywnaOcenaWizyty, AdoptionApplicationStatus.PendingFinalization)
            .Permit(AdoptionApplicationTrigger.NegatywnaOcenaWizyty, AdoptionApplicationStatus.Rejected);

        // PendingFinalization -> Completed or Cancelled
        _stateMachine.Configure(AdoptionApplicationStatus.PendingFinalization)
            .Permit(AdoptionApplicationTrigger.PodpisanieUmowy, AdoptionApplicationStatus.Completed)
            .Permit(AdoptionApplicationTrigger.RezygnacjaPrzedPodpisaniem, AdoptionApplicationStatus.Cancelled);

        // Terminal states - no further transitions
        _stateMachine.Configure(AdoptionApplicationStatus.Completed);
        _stateMachine.Configure(AdoptionApplicationStatus.Rejected);
        _stateMachine.Configure(AdoptionApplicationStatus.Cancelled);
    }

    private void EnsureStateMachineInitialized()
    {
        if (_stateMachine is null)
        {
            InitializeStateMachine();
        }
    }

    #endregion

    #region Status Operations

    /// <summary>
    /// Changes the application status
    /// </summary>
    public Result ChangeStatus(AdoptionApplicationTrigger trigger, string changedBy, string? reason = null)
    {
        EnsureStateMachineInitialized();

        if (!_stateMachine!.CanFire(trigger))
        {
            return Result.Failure(Error.Validation(
                $"Nie można wykonać akcji '{trigger}' dla zgłoszenia w statusie '{Status}'"));
        }

        var previousStatus = Status;
        _stateMachine.Fire(trigger);

        var statusChange = AdoptionApplicationStatusChange.Create(
            Id, previousStatus, Status, trigger, changedBy, reason);
        _statusHistory.Add(statusChange);

        SetUpdatedAt();

        AddDomainEvent(new AdoptionApplicationStatusChangedEvent(
            Id, AdopterId, AnimalId, previousStatus, Status, trigger, changedBy, reason));

        // Additional events for terminal states
        if (Status == AdoptionApplicationStatus.Completed)
        {
            CompletionDate = DateTime.UtcNow;
            AddDomainEvent(new AdoptionCompletedEvent(Id, AdopterId, AnimalId, ContractNumber));
        }
        else if (Status == AdoptionApplicationStatus.Rejected || Status == AdoptionApplicationStatus.Cancelled)
        {
            CompletionDate = DateTime.UtcNow;
            RejectionReason = reason;
        }

        return Result.Success();
    }

    /// <summary>
    /// Checks whether a given action can be performed
    /// </summary>
    public bool CanChangeStatus(AdoptionApplicationTrigger trigger)
    {
        EnsureStateMachineInitialized();
        return _stateMachine!.CanFire(trigger);
    }

    /// <summary>
    /// Returns permitted actions for the current status
    /// </summary>
    public IEnumerable<AdoptionApplicationTrigger> GetPermittedTriggers()
    {
        EnsureStateMachineInitialized();
        return _stateMachine!.PermittedTriggers;
    }

    #endregion

    #region Business Operations

    /// <summary>
    /// Staff member takes the application for review
    /// </summary>
    public Result TakeForReview(Guid reviewerUserId, string reviewerName)
    {
        var result = ChangeStatus(
            AdoptionApplicationTrigger.PodjęciePrzezPracownika,
            reviewerName);

        if (result.IsSuccess)
        {
            ReviewedByUserId = reviewerUserId;
            ReviewDate = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Approves the application after positive data verification
    /// </summary>
    public Result ApproveApplication(string reviewerName, string? notes = null)
    {
        var result = ChangeStatus(
            AdoptionApplicationTrigger.PozytywnaWeryfikacjaDanych,
            reviewerName);

        if (result.IsSuccess)
        {
            ReviewNotes = notes;
            ReviewDate = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Rejects the application
    /// </summary>
    public Result RejectApplication(string reviewerName, string reason)
    {
        var result = ChangeStatus(
            AdoptionApplicationTrigger.NegatywnaWeryfikacja,
            reviewerName,
            reason);

        if (result.IsSuccess)
        {
            ReviewNotes = reason;
            ReviewDate = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Schedules a visit
    /// </summary>
    public Result ScheduleVisit(DateTime visitDate, string scheduledBy)
    {
        if (visitDate <= DateTime.UtcNow)
        {
            return Result.Failure(Error.Validation("Data wizyty musi być w przyszłości"));
        }

        var result = ChangeStatus(
            AdoptionApplicationTrigger.RezerwacjaTerminuWizyty,
            scheduledBy);

        if (result.IsSuccess)
        {
            ScheduledVisitDate = visitDate;
        }

        return result;
    }

    /// <summary>
    /// Records visit attendance
    /// </summary>
    public Result RecordVisitAttendance(Guid conductedByUserId, string conductedByName)
    {
        var result = ChangeStatus(
            AdoptionApplicationTrigger.StawienieSieNaWizyte,
            conductedByName);

        if (result.IsSuccess)
        {
            VisitDate = DateTime.UtcNow;
            VisitConductedByUserId = conductedByUserId;
        }

        return result;
    }

    /// <summary>
    /// Records the visit result
    /// </summary>
    public Result RecordVisitResult(
        bool isPositive,
        int assessment,
        string notes,
        string recordedBy)
    {
        if (assessment < 1 || assessment > 5)
        {
            return Result.Failure(Error.Validation("Ocena musi być w skali 1-5"));
        }

        VisitAssessment = assessment;
        VisitNotes = notes;

        var trigger = isPositive
            ? AdoptionApplicationTrigger.PozytywnaOcenaWizyty
            : AdoptionApplicationTrigger.NegatywnaOcenaWizyty;

        return ChangeStatus(trigger, recordedBy, isPositive ? null : notes);
    }

    /// <summary>
    /// Generates the adoption contract
    /// </summary>
    public Result GenerateContract(string contractNumber, string generatedBy)
    {
        if (Status != AdoptionApplicationStatus.PendingFinalization)
        {
            return Result.Failure(Error.Validation("Umowę można wygenerować tylko dla zgłoszeń oczekujących na finalizację"));
        }

        ContractNumber = contractNumber;
        ContractGeneratedDate = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Finalizes the adoption (contract signing)
    /// </summary>
    public Result FinalizeAdoption(string contractFilePath, string signedBy)
    {
        if (string.IsNullOrWhiteSpace(ContractNumber))
        {
            return Result.Failure(Error.Validation("Przed podpisaniem należy wygenerować umowę"));
        }

        var result = ChangeStatus(
            AdoptionApplicationTrigger.PodpisanieUmowy,
            signedBy);

        if (result.IsSuccess)
        {
            ContractFilePath = contractFilePath;
            ContractSignedDate = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Cancels the application by the user
    /// </summary>
    public Result CancelByUser(string reason, string userName)
    {
        AdoptionApplicationTrigger trigger = Status switch
        {
            AdoptionApplicationStatus.New => AdoptionApplicationTrigger.AnulowanePrzezUzytkownika,
            AdoptionApplicationStatus.Submitted => AdoptionApplicationTrigger.AnulowanePrzezUzytkownika,
            AdoptionApplicationStatus.Accepted => AdoptionApplicationTrigger.RezygnacjaPoAkceptacji,
            AdoptionApplicationStatus.VisitScheduled => AdoptionApplicationTrigger.NiestawienieSieNaWizyte,
            AdoptionApplicationStatus.PendingFinalization => AdoptionApplicationTrigger.RezygnacjaPrzedPodpisaniem,
            _ => throw new InvalidOperationException($"Nie można anulować zgłoszenia w statusie {Status}")
        };

        return ChangeStatus(trigger, userName, reason);
    }

    #endregion
}

/// <summary>
/// Adoption application status change history
/// </summary>
public class AdoptionApplicationStatusChange : Entity<Guid>
{
    public Guid ApplicationId { get; private set; }
    public AdoptionApplicationStatus PreviousStatus { get; private set; }
    public AdoptionApplicationStatus NewStatus { get; private set; }
    public AdoptionApplicationTrigger Trigger { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private AdoptionApplicationStatusChange() { }

    public static AdoptionApplicationStatusChange Create(
        Guid applicationId,
        AdoptionApplicationStatus previousStatus,
        AdoptionApplicationStatus newStatus,
        AdoptionApplicationTrigger trigger,
        string changedBy,
        string? reason)
    {
        return new AdoptionApplicationStatusChange
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Trigger = trigger,
            ChangedBy = changedBy,
            Reason = reason,
            ChangedAt = DateTime.UtcNow
        };
    }
}
