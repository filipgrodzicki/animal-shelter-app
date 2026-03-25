using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers.Events;
using Stateless;

namespace ShelterApp.Domain.Volunteers;

#region Enums

/// <summary>
/// Volunteer status in the system
/// </summary>
public enum VolunteerStatus
{
    /// <summary>Candidate - application submitted</summary>
    Candidate = 0,

    /// <summary>In training</summary>
    InTraining = 1,

    /// <summary>Active volunteer</summary>
    Active = 2,

    /// <summary>Suspended</summary>
    Suspended = 3,

    /// <summary>Inactive</summary>
    Inactive = 4
}

/// <summary>
/// Volunteer status change triggers
/// </summary>
public enum VolunteerStatusTrigger
{
    /// <summary>Candidate application accepted</summary>
    AkceptacjaZgloszenia,

    /// <summary>Candidate application rejected</summary>
    OdrzucenieZgloszenia,

    /// <summary>Training completed</summary>
    UkonczenieSzkolenia,

    /// <summary>Training resignation or failure to complete</summary>
    RezygnacjaZeSzkolenia,

    /// <summary>Activity suspension</summary>
    ZawieszenieAktywnosci,

    /// <summary>Volunteering resignation</summary>
    Rezygnacja,

    /// <summary>Resumption after suspension</summary>
    Wznowienie,

    /// <summary>Reapplication by inactive volunteer</summary>
    PonowneZgloszenie
}

#endregion

/// <summary>
/// Shelter volunteer
/// </summary>
public class Volunteer : AggregateRoot<Guid>
{
    private readonly List<VolunteerStatusChange> _statusHistory = new();
    private readonly List<VolunteerCertificate> _certificates = new();
    private readonly List<string> _skills = new();
    private readonly List<DayOfWeek> _availability = new();
    private StateMachine<VolunteerStatus, VolunteerStatusTrigger>? _stateMachine;

    #region Properties

    /// <summary>
    /// User identifier in the Identity system
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string Phone { get; private set; } = string.Empty;

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime DateOfBirth { get; private set; }

    /// <summary>
    /// Volunteer's age
    /// </summary>
    public int Age => CalculateAge(DateOfBirth);

    /// <summary>
    /// Residential address
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; private set; }

    /// <summary>
    /// Postal code
    /// </summary>
    public string? PostalCode { get; private set; }

    /// <summary>
    /// Volunteer status
    /// </summary>
    public VolunteerStatus Status { get; private set; }

    /// <summary>
    /// Application date
    /// </summary>
    public DateTime ApplicationDate { get; private set; }

    /// <summary>
    /// Training start date
    /// </summary>
    public DateTime? TrainingStartDate { get; private set; }

    /// <summary>
    /// Training end date
    /// </summary>
    public DateTime? TrainingEndDate { get; private set; }

    /// <summary>
    /// Volunteer contract signing date
    /// </summary>
    public DateTime? ContractSignedDate { get; private set; }

    /// <summary>
    /// Volunteer contract number
    /// </summary>
    public string? ContractNumber { get; private set; }

    /// <summary>
    /// Emergency contact name
    /// </summary>
    public string? EmergencyContactName { get; private set; }

    /// <summary>
    /// Emergency contact phone number
    /// </summary>
    public string? EmergencyContactPhone { get; private set; }

    /// <summary>
    /// Volunteer's skills
    /// </summary>
    public IReadOnlyList<string> Skills => _skills.AsReadOnly();

    /// <summary>
    /// Availability (days of week)
    /// </summary>
    public IReadOnlyList<DayOfWeek> Availability => _availability.AsReadOnly();

    /// <summary>
    /// Total hours worked
    /// </summary>
    public decimal TotalHoursWorked { get; private set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Status change history
    /// </summary>
    public IReadOnlyCollection<VolunteerStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    /// <summary>
    /// Volunteer's certificates and credentials
    /// </summary>
    public IReadOnlyCollection<VolunteerCertificate> Certificates => _certificates.AsReadOnly();

    #endregion

    private Volunteer() { }

    #region Factory Methods

    /// <summary>
    /// Creates a new volunteer candidate
    /// </summary>
    public static Result<Volunteer> Create(
        string firstName,
        string lastName,
        string email,
        string phone,
        DateTime dateOfBirth,
        string? address = null,
        string? city = null,
        string? postalCode = null,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null,
        IEnumerable<string>? skills = null,
        IEnumerable<DayOfWeek>? availability = null,
        string? notes = null)
    {
        // Age validation - must be >= 16 years old
        var age = CalculateAge(dateOfBirth);
        if (age < 16)
        {
            return Result.Failure<Volunteer>(
                Error.Validation("Wolontariusz musi mieć ukończone 16 lat"));
        }

        var volunteer = new Volunteer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Address = address,
            City = city,
            PostalCode = postalCode,
            EmergencyContactName = emergencyContactName,
            EmergencyContactPhone = emergencyContactPhone,
            Notes = notes,
            Status = VolunteerStatus.Candidate,
            ApplicationDate = DateTime.UtcNow,
            TotalHoursWorked = 0
        };

        if (skills is not null)
        {
            volunteer._skills.AddRange(skills);
        }

        if (availability is not null)
        {
            volunteer._availability.AddRange(availability);
        }

        volunteer.InitializeStateMachine();
        volunteer.AddDomainEvent(new VolunteerApplicationSubmittedEvent(
            volunteer.Id, volunteer.Email, volunteer.FullName));

        return Result.Success(volunteer);
    }

    /// <summary>
    /// Creates a volunteer with a linked user account
    /// </summary>
    public static Result<Volunteer> CreateWithUser(
        Guid userId,
        string firstName,
        string lastName,
        string email,
        string phone,
        DateTime dateOfBirth,
        string? address = null,
        string? city = null,
        string? postalCode = null,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null,
        IEnumerable<string>? skills = null,
        IEnumerable<DayOfWeek>? availability = null,
        string? notes = null)
    {
        var result = Create(
            firstName, lastName, email, phone, dateOfBirth,
            address, city, postalCode,
            emergencyContactName, emergencyContactPhone,
            skills, availability, notes);

        if (result.IsSuccess)
        {
            result.Value.UserId = userId;
        }

        return result;
    }

    #endregion

    #region State Machine

    private void InitializeStateMachine()
    {
        _stateMachine = new StateMachine<VolunteerStatus, VolunteerStatusTrigger>(
            () => Status,
            s => Status = s);

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        if (_stateMachine is null) return;

        // Candidate -> InTraining or -> Inactive
        _stateMachine.Configure(VolunteerStatus.Candidate)
            .Permit(VolunteerStatusTrigger.AkceptacjaZgloszenia, VolunteerStatus.InTraining)
            .Permit(VolunteerStatusTrigger.OdrzucenieZgloszenia, VolunteerStatus.Inactive);

        // InTraining -> Active or -> Inactive
        _stateMachine.Configure(VolunteerStatus.InTraining)
            .Permit(VolunteerStatusTrigger.UkonczenieSzkolenia, VolunteerStatus.Active)
            .Permit(VolunteerStatusTrigger.RezygnacjaZeSzkolenia, VolunteerStatus.Inactive);

        // Active -> Suspended or -> Inactive
        _stateMachine.Configure(VolunteerStatus.Active)
            .Permit(VolunteerStatusTrigger.ZawieszenieAktywnosci, VolunteerStatus.Suspended)
            .Permit(VolunteerStatusTrigger.Rezygnacja, VolunteerStatus.Inactive);

        // Suspended -> Active or -> Inactive
        _stateMachine.Configure(VolunteerStatus.Suspended)
            .Permit(VolunteerStatusTrigger.Wznowienie, VolunteerStatus.Active)
            .Permit(VolunteerStatusTrigger.Rezygnacja, VolunteerStatus.Inactive);

        // Inactive -> Candidate (reapplication)
        _stateMachine.Configure(VolunteerStatus.Inactive)
            .Permit(VolunteerStatusTrigger.PonowneZgloszenie, VolunteerStatus.Candidate);
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
    /// Changes the volunteer's status
    /// </summary>
    public Result ChangeStatus(VolunteerStatusTrigger trigger, string changedBy, string? reason = null)
    {
        EnsureStateMachineInitialized();

        if (!_stateMachine!.CanFire(trigger))
        {
            return Result.Failure(Error.Validation(
                $"Nie można wykonać akcji '{trigger}' dla wolontariusza w statusie '{Status}'"));
        }

        var previousStatus = Status;
        _stateMachine.Fire(trigger);

        var statusChange = VolunteerStatusChange.Create(
            Id, previousStatus, Status, trigger, changedBy, reason);
        _statusHistory.Add(statusChange);

        SetUpdatedAt();

        AddDomainEvent(new VolunteerStatusChangedEvent(
            Id, previousStatus, Status, trigger, changedBy, reason));

        return Result.Success();
    }

    /// <summary>
    /// Checks whether a given action can be performed
    /// </summary>
    public bool CanChangeStatus(VolunteerStatusTrigger trigger)
    {
        EnsureStateMachineInitialized();
        return _stateMachine!.CanFire(trigger);
    }

    /// <summary>
    /// Returns permitted actions for the current status
    /// </summary>
    public IEnumerable<VolunteerStatusTrigger> GetPermittedTriggers()
    {
        EnsureStateMachineInitialized();
        return _stateMachine!.PermittedTriggers;
    }

    #endregion

    #region Training Operations

    /// <summary>
    /// Accepts the application and starts training
    /// </summary>
    public Result AcceptAndStartTraining(string acceptedBy, DateTime? trainingStartDate = null)
    {
        var result = ChangeStatus(VolunteerStatusTrigger.AkceptacjaZgloszenia, acceptedBy);
        if (result.IsFailure)
            return result;

        TrainingStartDate = trainingStartDate ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Completes training and activates the volunteer
    /// </summary>
    public Result CompleteTraining(
        string completedBy,
        string contractNumber,
        DateTime? trainingEndDate = null)
    {
        if (string.IsNullOrWhiteSpace(contractNumber))
        {
            return Result.Failure(Error.Validation("Numer umowy jest wymagany"));
        }

        var result = ChangeStatus(VolunteerStatusTrigger.UkonczenieSzkolenia, completedBy);
        if (result.IsFailure)
            return result;

        TrainingEndDate = trainingEndDate ?? DateTime.UtcNow;
        ContractNumber = contractNumber;
        ContractSignedDate = DateTime.UtcNow;

        AddDomainEvent(new VolunteerActivatedEvent(Id, Email, FullName, ContractNumber));

        return Result.Success();
    }

    /// <summary>
    /// Rejects the application
    /// </summary>
    public Result RejectApplication(string rejectedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("Powód odrzucenia jest wymagany"));
        }

        return ChangeStatus(VolunteerStatusTrigger.OdrzucenieZgloszenia, rejectedBy, reason);
    }

    /// <summary>
    /// Resigns from training
    /// </summary>
    public Result ResignFromTraining(string resignedBy, string? reason = null)
    {
        return ChangeStatus(VolunteerStatusTrigger.RezygnacjaZeSzkolenia, resignedBy, reason);
    }

    #endregion

    #region Activity Operations

    /// <summary>
    /// Suspends the volunteer's activity
    /// </summary>
    public Result Suspend(string suspendedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("Powód zawieszenia jest wymagany"));
        }

        return ChangeStatus(VolunteerStatusTrigger.ZawieszenieAktywnosci, suspendedBy, reason);
    }

    /// <summary>
    /// Resumes activity after suspension
    /// </summary>
    public Result Resume(string resumedBy, string? notes = null)
    {
        return ChangeStatus(VolunteerStatusTrigger.Wznowienie, resumedBy, notes);
    }

    /// <summary>
    /// Resigns from volunteering
    /// </summary>
    public Result Resign(string resignedBy, string? reason = null)
    {
        return ChangeStatus(VolunteerStatusTrigger.Rezygnacja, resignedBy, reason);
    }

    /// <summary>
    /// Reapplies an inactive volunteer
    /// </summary>
    public Result Reapply(string reappliedBy)
    {
        var result = ChangeStatus(VolunteerStatusTrigger.PonowneZgloszenie, reappliedBy);
        if (result.IsSuccess)
        {
            ApplicationDate = DateTime.UtcNow;
            TrainingStartDate = null;
            TrainingEndDate = null;
        }
        return result;
    }

    #endregion

    #region Work Hours

    /// <summary>
    /// Adds worked hours
    /// </summary>
    public Result AddWorkHours(decimal hours, string recordedBy)
    {
        if (Status != VolunteerStatus.Active)
        {
            return Result.Failure(Error.Validation(
                "Godziny można dodawać tylko dla aktywnych wolontariuszy"));
        }

        if (hours <= 0)
        {
            return Result.Failure(Error.Validation(
                "Liczba godzin musi być większa od zera"));
        }

        TotalHoursWorked += hours;
        SetUpdatedAt();

        AddDomainEvent(new VolunteerHoursRecordedEvent(Id, hours, TotalHoursWorked, recordedBy));

        return Result.Success();
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Updates contact information
    /// </summary>
    public void UpdateContactInfo(
        string? phone = null,
        string? address = null,
        string? city = null,
        string? postalCode = null,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null)
    {
        if (phone is not null) Phone = phone;
        if (address is not null) Address = address;
        if (city is not null) City = city;
        if (postalCode is not null) PostalCode = postalCode;
        if (emergencyContactName is not null) EmergencyContactName = emergencyContactName;
        if (emergencyContactPhone is not null) EmergencyContactPhone = emergencyContactPhone;
        SetUpdatedAt();
    }

    /// <summary>
    /// Updates personal information
    /// </summary>
    public Result UpdatePersonalInfo(
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        DateTime? dateOfBirth = null)
    {
        if (dateOfBirth.HasValue)
        {
            var age = CalculateAge(dateOfBirth.Value);
            if (age < 16)
            {
                return Result.Failure(Error.Validation("Wolontariusz musi mieć ukończone 16 lat"));
            }
            DateOfBirth = dateOfBirth.Value;
        }

        if (firstName is not null) FirstName = firstName;
        if (lastName is not null) LastName = lastName;
        if (email is not null) Email = email;

        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Updates skills
    /// </summary>
    public void UpdateSkills(IEnumerable<string> skills)
    {
        _skills.Clear();
        _skills.AddRange(skills.Where(s => !string.IsNullOrWhiteSpace(s)));
        SetUpdatedAt();
    }

    /// <summary>
    /// Adds a skill
    /// </summary>
    public void AddSkill(string skill)
    {
        if (!string.IsNullOrWhiteSpace(skill) && !_skills.Contains(skill))
        {
            _skills.Add(skill);
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Removes a skill
    /// </summary>
    public void RemoveSkill(string skill)
    {
        if (_skills.Remove(skill))
        {
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Updates availability
    /// </summary>
    public void UpdateAvailability(IEnumerable<DayOfWeek> availability)
    {
        _availability.Clear();
        _availability.AddRange(availability.Distinct());
        SetUpdatedAt();
    }

    /// <summary>
    /// Updates notes
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        SetUpdatedAt();
    }

    /// <summary>
    /// Assigns a user account
    /// </summary>
    public void AssignUser(Guid userId)
    {
        UserId = userId;
        SetUpdatedAt();
    }

    #endregion

    #region Certificate Operations

    /// <summary>
    /// Adds a certificate to the volunteer
    /// </summary>
    public Result<VolunteerCertificate> AddCertificate(
        CertificateType type,
        string name,
        string issuingOrganization,
        DateTime issueDate,
        string? certificateNumber = null,
        DateTime? expiryDate = null,
        string? filePath = null,
        string? notes = null)
    {
        var certificate = VolunteerCertificate.Create(
            Id, type, name, issuingOrganization, issueDate,
            certificateNumber, expiryDate, filePath, notes);

        _certificates.Add(certificate);
        SetUpdatedAt();

        return Result.Success(certificate);
    }

    /// <summary>
    /// Removes a volunteer's certificate
    /// </summary>
    public Result RemoveCertificate(Guid certificateId)
    {
        var certificate = _certificates.FirstOrDefault(c => c.Id == certificateId);
        if (certificate is null)
        {
            return Result.Failure(Error.NotFound("VolunteerCertificate", certificateId));
        }

        _certificates.Remove(certificate);
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Returns active (non-expired) certificates
    /// </summary>
    public IEnumerable<VolunteerCertificate> GetActiveCertificates()
    {
        return _certificates.Where(c => c.IsActive);
    }

    #endregion

    #region Private Methods

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
            age--;
        return age;
    }

    #endregion
}

/// <summary>
/// Volunteer status change history
/// </summary>
public class VolunteerStatusChange : Entity<Guid>
{
    public Guid VolunteerId { get; private set; }
    public VolunteerStatus PreviousStatus { get; private set; }
    public VolunteerStatus NewStatus { get; private set; }
    public VolunteerStatusTrigger Trigger { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private VolunteerStatusChange() { }

    public static VolunteerStatusChange Create(
        Guid volunteerId,
        VolunteerStatus previousStatus,
        VolunteerStatus newStatus,
        VolunteerStatusTrigger trigger,
        string changedBy,
        string? reason)
    {
        return new VolunteerStatusChange
        {
            Id = Guid.NewGuid(),
            VolunteerId = volunteerId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Trigger = trigger,
            ChangedBy = changedBy,
            Reason = reason,
            ChangedAt = DateTime.UtcNow
        };
    }
}
