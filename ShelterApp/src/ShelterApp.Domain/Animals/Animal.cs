using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Animals.Events;
using ShelterApp.Domain.Common;
using Stateless;

namespace ShelterApp.Domain.Animals;

public class Animal : AggregateRoot<Guid>
{
    private readonly List<AnimalPhoto> _photos = new();
    private readonly List<AnimalStatusChange> _statusHistory = new();
    private readonly List<MedicalRecord> _medicalRecords = new();

    private StateMachine<AnimalStatus, AnimalStatusTrigger>? _stateMachine;

    // Required by the Ministry of Agriculture regulation
    public string RegistrationNumber { get; private set; } = string.Empty;
    public Species Species { get; private set; }
    public string Breed { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int? AgeInMonths { get; private set; }
    public Gender Gender { get; private set; }
    public Size Size { get; private set; }
    public string Color { get; private set; } = string.Empty;
    public string? ChipNumber { get; private set; }
    public DateTime AdmissionDate { get; private set; }
    public string AdmissionCircumstances { get; private set; } = string.Empty;
    public AnimalStatus Status { get; private set; }
    public string? Description { get; private set; }

    // Surrendering person's data (if surrendered by owner)
    public string? SurrenderedByFirstName { get; private set; }
    public string? SurrenderedByLastName { get; private set; }
    public string? SurrenderedByPhone { get; private set; }

    // Distinguishing marks
    public string? DistinguishingMarks { get; private set; }

    // Additional adoption information
    public ExperienceLevel ExperienceLevel { get; private set; }
    public ChildrenCompatibility ChildrenCompatibility { get; private set; }
    public AnimalCompatibility AnimalCompatibility { get; private set; }
    public SpaceRequirement SpaceRequirement { get; private set; }
    public CareTime CareTime { get; private set; }

    /// <summary>
    /// Special needs of the animal (e.g. diet, medication, special care)
    /// </summary>
    public string? SpecialNeeds { get; private set; }

    // Navigation properties
    public IReadOnlyCollection<AnimalPhoto> Photos => _photos.AsReadOnly();
    public IReadOnlyCollection<AnimalStatusChange> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<MedicalRecord> MedicalRecords => _medicalRecords.AsReadOnly();

    private Animal() { }

    public static Animal Create(
        string registrationNumber,
        Species species,
        string breed,
        string name,
        Gender gender,
        Size size,
        string color,
        string admissionCircumstances,
        int? ageInMonths = null,
        string? chipNumber = null,
        DateTime? admissionDate = null,
        string? description = null,
        ExperienceLevel experienceLevel = ExperienceLevel.None,
        ChildrenCompatibility childrenCompatibility = ChildrenCompatibility.Yes,
        AnimalCompatibility animalCompatibility = AnimalCompatibility.Yes,
        SpaceRequirement spaceRequirement = SpaceRequirement.Apartment,
        CareTime careTime = CareTime.OneToThreeHours,
        string? surrenderedByFirstName = null,
        string? surrenderedByLastName = null,
        string? surrenderedByPhone = null,
        string? distinguishingMarks = null,
        string? specialNeeds = null)
    {
        var animal = new Animal
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = registrationNumber,
            Species = species,
            Breed = breed,
            Name = name,
            AgeInMonths = ageInMonths,
            Gender = gender,
            Size = size,
            Color = color,
            ChipNumber = chipNumber,
            AdmissionDate = admissionDate ?? DateTime.UtcNow,
            AdmissionCircumstances = admissionCircumstances,
            Status = AnimalStatus.Admitted,
            Description = description,
            ExperienceLevel = experienceLevel,
            ChildrenCompatibility = childrenCompatibility,
            AnimalCompatibility = animalCompatibility,
            SpaceRequirement = spaceRequirement,
            CareTime = careTime,
            SurrenderedByFirstName = surrenderedByFirstName,
            SurrenderedByLastName = surrenderedByLastName,
            SurrenderedByPhone = surrenderedByPhone,
            DistinguishingMarks = distinguishingMarks,
            SpecialNeeds = specialNeeds
        };

        animal.InitializeStateMachine();
        animal.AddDomainEvent(new AnimalAdmittedEvent(animal.Id, registrationNumber, name));

        return animal;
    }

    #region State Machine

    private void InitializeStateMachine()
    {
        _stateMachine = new StateMachine<AnimalStatus, AnimalStatusTrigger>(
            () => Status,
            s => Status = s);

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        if (_stateMachine is null) return;

        // Admitted
        _stateMachine.Configure(AnimalStatus.Admitted)
            .Permit(AnimalStatusTrigger.SkierowanieNaKwarantanne, AnimalStatus.Quarantine)
            .Permit(AnimalStatusTrigger.DopuszczenieDoAdopcji, AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.Zgon, AnimalStatus.Deceased);

        // Quarantine
        _stateMachine.Configure(AnimalStatus.Quarantine)
            .Permit(AnimalStatusTrigger.WykrycieChoroby, AnimalStatus.Treatment)
            .Permit(AnimalStatusTrigger.ZakonczenieKwarantanny, AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.DopuszczenieDoAdopcji, AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.Zgon, AnimalStatus.Deceased);

        // Treatment
        _stateMachine.Configure(AnimalStatus.Treatment)
            .Permit(AnimalStatusTrigger.Wyleczenie, AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.DopuszczenieDoAdopcji, AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.Zgon, AnimalStatus.Deceased);

        // Available
        _stateMachine.Configure(AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.Zachorowanie, AnimalStatus.Treatment)
            .Permit(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, AnimalStatus.Reserved)
            .Permit(AnimalStatusTrigger.Zgon, AnimalStatus.Deceased);

        // Reserved
        _stateMachine.Configure(AnimalStatus.Reserved)
            .Permit(AnimalStatusTrigger.AnulowanieZgloszenia, AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.Rezygnacja, AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.ZatwierdznieZgloszenia, AnimalStatus.InAdoptionProcess)
            .Permit(AnimalStatusTrigger.Zgon, AnimalStatus.Deceased);

        // InAdoptionProcess
        _stateMachine.Configure(AnimalStatus.InAdoptionProcess)
            .Permit(AnimalStatusTrigger.NegatywnaOcena, AnimalStatus.Available)
            .Permit(AnimalStatusTrigger.PodpisanieUmowy, AnimalStatus.Adopted)
            .Permit(AnimalStatusTrigger.Zachorowanie, AnimalStatus.Treatment)
            .Permit(AnimalStatusTrigger.Zgon, AnimalStatus.Deceased);

        // Adopted (terminal state)
        _stateMachine.Configure(AnimalStatus.Adopted);

        // Deceased (terminal state)
        _stateMachine.Configure(AnimalStatus.Deceased);
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

    public Result ChangeStatus(AnimalStatusTrigger trigger, string changedBy, string? reason = null)
    {
        EnsureStateMachineInitialized();

        if (!_stateMachine!.CanFire(trigger))
        {
            return Result.Failure(Error.Validation(
                $"Nie można wykonać akcji '{trigger}' dla zwierzęcia w statusie '{Status}'"));
        }

        var previousStatus = Status;
        _stateMachine.Fire(trigger);

        var statusChange = AnimalStatusChange.Create(
            Id, previousStatus, Status, trigger, changedBy, reason);
        _statusHistory.Add(statusChange);

        SetUpdatedAt();

        AddDomainEvent(new AnimalStatusChangedEvent(
            Id, previousStatus, Status, trigger, changedBy, reason));

        if (Status == AnimalStatus.Adopted)
        {
            AddDomainEvent(new AnimalAdoptedEvent(Id, RegistrationNumber, Name, changedBy));
        }

        if (Status == AnimalStatus.Deceased)
        {
            AddDomainEvent(new AnimalDeceasedEvent(Id, RegistrationNumber, Name, reason));
        }

        return Result.Success();
    }

    public bool CanChangeStatus(AnimalStatusTrigger trigger)
    {
        EnsureStateMachineInitialized();
        return _stateMachine!.CanFire(trigger);
    }

    public IEnumerable<AnimalStatusTrigger> GetPermittedTriggers()
    {
        EnsureStateMachineInitialized();
        return _stateMachine!.PermittedTriggers;
    }

    #endregion

    #region Medical Records

    public Result<MedicalRecord> AddMedicalRecord(
        MedicalRecordType type,
        string title,
        string description,
        string veterinarianName,
        DateTime? recordDate = null,
        string? diagnosis = null,
        string? treatment = null,
        string? medications = null,
        DateTime? nextVisitDate = null,
        string? notes = null,
        decimal? cost = null,
        string enteredBy = "System",
        Guid? enteredByUserId = null)
    {
        var record = MedicalRecord.Create(
            animalId: Id,
            type: type,
            title: title,
            description: description,
            veterinarianName: veterinarianName,
            enteredBy: enteredBy,
            enteredByUserId: enteredByUserId,
            recordDate: recordDate,
            diagnosis: diagnosis,
            treatment: treatment,
            medications: medications,
            nextVisitDate: nextVisitDate,
            notes: notes,
            cost: cost);

        _medicalRecords.Add(record);
        SetUpdatedAt();

        AddDomainEvent(new MedicalRecordAddedEvent(Id, record.Id, type.ToString(), title));

        return Result.Success(record);
    }

    #endregion

    #region Photos

    public Result<AnimalPhoto> AddPhoto(
        string fileName,
        string filePath,
        string? contentType,
        long fileSize,
        bool isMain = false,
        string? description = null)
    {
        var displayOrder = _photos.Count;

        if (isMain)
        {
            foreach (var existingPhoto in _photos.Where(p => p.IsMain))
            {
                existingPhoto.UnsetAsMain();
            }
        }
        else if (!_photos.Any())
        {
            isMain = true;
        }

        var photo = AnimalPhoto.Create(
            Id, fileName, filePath, contentType, fileSize, isMain, description, displayOrder);

        _photos.Add(photo);
        SetUpdatedAt();

        AddDomainEvent(new AnimalPhotoAddedEvent(Id, photo.Id, fileName, isMain));

        return Result.Success(photo);
    }

    public Result SetMainPhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null)
        {
            return Result.Failure(Error.NotFound("AnimalPhoto", photoId));
        }

        foreach (var existingPhoto in _photos.Where(p => p.IsMain))
        {
            existingPhoto.UnsetAsMain();
        }

        photo.SetAsMain();
        SetUpdatedAt();

        return Result.Success();
    }

    public Result RemovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null)
        {
            return Result.Failure(Error.NotFound("AnimalPhoto", photoId));
        }

        var wasMain = photo.IsMain;
        _photos.Remove(photo);

        if (wasMain && _photos.Any())
        {
            _photos.First().SetAsMain();
        }

        SetUpdatedAt();
        return Result.Success();
    }

    #endregion

    #region Update Methods

    public void UpdateBasicInfo(
        string? name = null,
        string? breed = null,
        int? ageInMonths = null,
        string? color = null,
        string? description = null)
    {
        if (name is not null) Name = name;
        if (breed is not null) Breed = breed;
        if (ageInMonths.HasValue) AgeInMonths = ageInMonths;
        if (color is not null) Color = color;
        if (description is not null) Description = description;
        SetUpdatedAt();
    }

    public void UpdateAdoptionInfo(
        ExperienceLevel? experienceLevel = null,
        ChildrenCompatibility? childrenCompatibility = null,
        AnimalCompatibility? animalCompatibility = null,
        SpaceRequirement? spaceRequirement = null,
        CareTime? careTime = null)
    {
        if (experienceLevel.HasValue) ExperienceLevel = experienceLevel.Value;
        if (childrenCompatibility.HasValue) ChildrenCompatibility = childrenCompatibility.Value;
        if (animalCompatibility.HasValue) AnimalCompatibility = animalCompatibility.Value;
        if (spaceRequirement.HasValue) SpaceRequirement = spaceRequirement.Value;
        if (careTime.HasValue) CareTime = careTime.Value;
        SetUpdatedAt();
    }

    public void UpdateChipNumber(string chipNumber)
    {
        ChipNumber = chipNumber;
        SetUpdatedAt();
    }

    /// <summary>
    /// Updates the animal's special needs
    /// </summary>
    public void UpdateSpecialNeeds(string? specialNeeds)
    {
        SpecialNeeds = specialNeeds;
        SetUpdatedAt();
    }

    #endregion
}
