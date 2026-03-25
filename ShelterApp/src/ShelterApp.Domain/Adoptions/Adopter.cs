using ShelterApp.Domain.Adoptions.Events;
using ShelterApp.Domain.Common;
using Stateless;

namespace ShelterApp.Domain.Adoptions;

/// <summary>
/// Person adopting an animal
/// </summary>
public class Adopter : AggregateRoot<Guid>
{
    private readonly List<AdopterStatusChange> _statusHistory = new();
    private StateMachine<AdopterStatus, AdopterStatusTrigger>? _stateMachine;

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
    /// Date of birth
    /// </summary>
    public DateTime DateOfBirth { get; private set; }

    /// <summary>
    /// Adopter's age
    /// </summary>
    public int Age => CalculateAge(DateOfBirth);

    /// <summary>
    /// Adopter status
    /// </summary>
    public AdopterStatus Status { get; private set; }

    /// <summary>
    /// GDPR consent date
    /// </summary>
    public DateTime? RodoConsentDate { get; private set; }

    /// <summary>
    /// Status change history
    /// </summary>
    public IReadOnlyCollection<AdopterStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    private Adopter() { }

    /// <summary>
    /// Creates a new anonymous adopter
    /// </summary>
    public static Adopter CreateAnonymous()
    {
        var adopter = new Adopter
        {
            Id = Guid.NewGuid(),
            Status = AdopterStatus.Anonymous
        };

        adopter.InitializeStateMachine();
        return adopter;
    }

    /// <summary>
    /// Creates a new registered adopter
    /// </summary>
    public static Result<Adopter> Create(
        Guid userId,
        string firstName,
        string lastName,
        string email,
        string phone,
        DateTime dateOfBirth,
        string? address = null,
        string? city = null,
        string? postalCode = null,
        DateTime? rodoConsentDate = null)
    {
        // Age validation - must be >= 18 years old
        var age = CalculateAge(dateOfBirth);
        if (age < 18)
        {
            return Result.Failure<Adopter>(
                Error.Validation("Osoba adoptująca musi mieć ukończone 18 lat"));
        }

        var adopter = new Adopter
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            DateOfBirth = DateTime.SpecifyKind(dateOfBirth.Date, DateTimeKind.Utc),
            Address = address,
            City = city,
            PostalCode = postalCode,
            RodoConsentDate = rodoConsentDate ?? DateTime.UtcNow,
            Status = AdopterStatus.Registered
        };

        adopter.InitializeStateMachine();
        adopter.AddDomainEvent(new AdopterRegisteredEvent(
            adopter.Id, userId, email, adopter.FullName));

        return Result.Success(adopter);
    }

    #region State Machine

    private void InitializeStateMachine()
    {
        _stateMachine = new StateMachine<AdopterStatus, AdopterStatusTrigger>(
            () => Status,
            s => Status = s);

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        if (_stateMachine is null) return;

        // Anonymous -> Registered
        _stateMachine.Configure(AdopterStatus.Anonymous)
            .Permit(AdopterStatusTrigger.RejestracjaKonta, AdopterStatus.Registered);

        // Registered -> Applying
        _stateMachine.Configure(AdopterStatus.Registered)
            .Permit(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, AdopterStatus.Applying);

        // Applying -> Registered (cancellation or rejection) or -> Verified
        _stateMachine.Configure(AdopterStatus.Applying)
            .Permit(AdopterStatusTrigger.AnulowanieZgloszenia, AdopterStatus.Registered)
            .Permit(AdopterStatusTrigger.OdrzucenieZgloszenia, AdopterStatus.Registered)
            .Permit(AdopterStatusTrigger.ZatwierdznieZgloszenia, AdopterStatus.Verified);

        // Verified -> Registered (negative verification) or -> Adopter
        _stateMachine.Configure(AdopterStatus.Verified)
            .Permit(AdopterStatusTrigger.NegatywnaWeryfikacja, AdopterStatus.Registered)
            .Permit(AdopterStatusTrigger.PozytywnaWeryfikacja, AdopterStatus.Adopter);

        // Adopter -> Registered (after contract signing)
        _stateMachine.Configure(AdopterStatus.Adopter)
            .Permit(AdopterStatusTrigger.PodpisanieUmowy, AdopterStatus.Registered);
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
    /// Changes the adopter's status
    /// </summary>
    public Result ChangeStatus(AdopterStatusTrigger trigger, string changedBy, string? reason = null)
    {
        EnsureStateMachineInitialized();

        if (!_stateMachine!.CanFire(trigger))
        {
            return Result.Failure(Error.Validation(
                $"Nie można wykonać akcji '{trigger}' dla osoby w statusie '{Status}'"));
        }

        var previousStatus = Status;
        _stateMachine.Fire(trigger);

        var statusChange = AdopterStatusChange.Create(
            Id, previousStatus, Status, trigger, changedBy, reason);
        _statusHistory.Add(statusChange);

        SetUpdatedAt();

        AddDomainEvent(new AdopterStatusChangedEvent(
            Id, previousStatus, Status, trigger, changedBy, reason));

        return Result.Success();
    }

    /// <summary>
    /// Checks whether a given action can be performed
    /// </summary>
    public bool CanChangeStatus(AdopterStatusTrigger trigger)
    {
        EnsureStateMachineInitialized();
        return _stateMachine!.CanFire(trigger);
    }

    /// <summary>
    /// Returns permitted actions for the current status
    /// </summary>
    public IEnumerable<AdopterStatusTrigger> GetPermittedTriggers()
    {
        EnsureStateMachineInitialized();
        return _stateMachine!.PermittedTriggers;
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Registers an anonymous user
    /// </summary>
    public Result Register(
        Guid userId,
        string firstName,
        string lastName,
        string email,
        string phone,
        DateTime dateOfBirth,
        string? address = null,
        string? city = null,
        string? postalCode = null)
    {
        if (Status != AdopterStatus.Anonymous)
        {
            return Result.Failure(Error.Validation("Tylko anonimowy użytkownik może się zarejestrować"));
        }

        var age = CalculateAge(dateOfBirth);
        if (age < 18)
        {
            return Result.Failure(Error.Validation("Osoba adoptująca musi mieć ukończone 18 lat"));
        }

        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        DateOfBirth = DateTime.SpecifyKind(dateOfBirth.Date, DateTimeKind.Utc);
        Address = address;
        City = city;
        PostalCode = postalCode;
        RodoConsentDate = DateTime.UtcNow;

        return ChangeStatus(AdopterStatusTrigger.RejestracjaKonta, email);
    }

    /// <summary>
    /// Updates contact information
    /// </summary>
    public void UpdateContactInfo(
        string? phone = null,
        string? address = null,
        string? city = null,
        string? postalCode = null)
    {
        if (phone is not null) Phone = phone;
        if (address is not null) Address = address;
        if (city is not null) City = city;
        if (postalCode is not null) PostalCode = postalCode;
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
            if (age < 18)
            {
                return Result.Failure(Error.Validation("Osoba adoptująca musi mieć ukończone 18 lat"));
            }
            DateOfBirth = DateTime.SpecifyKind(dateOfBirth.Value.Date, DateTimeKind.Utc);
        }

        if (firstName is not null) FirstName = firstName;
        if (lastName is not null) LastName = lastName;
        if (email is not null) Email = email;

        SetUpdatedAt();
        return Result.Success();
    }

    /// <summary>
    /// Gives GDPR consent
    /// </summary>
    public void GiveRodoConsent()
    {
        RodoConsentDate = DateTime.UtcNow;
        SetUpdatedAt();
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
/// Adopter status change history
/// </summary>
public class AdopterStatusChange : Entity<Guid>
{
    public Guid AdopterId { get; private set; }
    public AdopterStatus PreviousStatus { get; private set; }
    public AdopterStatus NewStatus { get; private set; }
    public AdopterStatusTrigger Trigger { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private AdopterStatusChange() { }

    public static AdopterStatusChange Create(
        Guid adopterId,
        AdopterStatus previousStatus,
        AdopterStatus newStatus,
        AdopterStatusTrigger trigger,
        string changedBy,
        string? reason)
    {
        return new AdopterStatusChange
        {
            Id = Guid.NewGuid(),
            AdopterId = adopterId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Trigger = trigger,
            ChangedBy = changedBy,
            Reason = reason,
            ChangedAt = DateTime.UtcNow
        };
    }
}
