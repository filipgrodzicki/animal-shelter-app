using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Animals.Entities;

public class MedicalRecord : Entity<Guid>
{
    private readonly List<MedicalRecordAttachment> _attachments = new();

    public Guid AnimalId { get; private set; }
    public MedicalRecordType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime RecordDate { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Treatment { get; private set; }
    public string? Medications { get; private set; }
    public DateTime? NextVisitDate { get; private set; }
    public string VeterinarianName { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public decimal? Cost { get; private set; }

    // Person entering the record into the system (required by WF-06)
    public string EnteredBy { get; private set; } = string.Empty;
    public Guid? EnteredByUserId { get; private set; }

    // Attachments (scanned veterinary documents)
    public IReadOnlyCollection<MedicalRecordAttachment> Attachments => _attachments.AsReadOnly();

    private MedicalRecord() { }

    internal static MedicalRecord Create(
        Guid animalId,
        MedicalRecordType type,
        string title,
        string description,
        string veterinarianName,
        string enteredBy,
        Guid? enteredByUserId = null,
        DateTime? recordDate = null,
        string? diagnosis = null,
        string? treatment = null,
        string? medications = null,
        DateTime? nextVisitDate = null,
        string? notes = null,
        decimal? cost = null)
    {
        return new MedicalRecord
        {
            Id = Guid.NewGuid(),
            AnimalId = animalId,
            Type = type,
            Title = title,
            Description = description,
            RecordDate = recordDate ?? DateTime.UtcNow,
            Diagnosis = diagnosis,
            Treatment = treatment,
            Medications = medications,
            NextVisitDate = nextVisitDate,
            VeterinarianName = veterinarianName,
            Notes = notes,
            Cost = cost,
            EnteredBy = enteredBy,
            EnteredByUserId = enteredByUserId
        };
    }

    public MedicalRecordAttachment AddAttachment(
        string fileName,
        string filePath,
        string? contentType,
        long fileSize,
        string? description = null)
    {
        var attachment = MedicalRecordAttachment.Create(
            Id, fileName, filePath, contentType, fileSize, description);
        _attachments.Add(attachment);
        SetUpdatedAt();
        return attachment;
    }

    public bool RemoveAttachment(Guid attachmentId)
    {
        var attachment = _attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment is null) return false;

        _attachments.Remove(attachment);
        SetUpdatedAt();
        return true;
    }

    internal void Update(
        string? diagnosis = null,
        string? treatment = null,
        string? medications = null,
        DateTime? nextVisitDate = null,
        string? notes = null)
    {
        if (diagnosis is not null) Diagnosis = diagnosis;
        if (treatment is not null) Treatment = treatment;
        if (medications is not null) Medications = medications;
        if (nextVisitDate.HasValue) NextVisitDate = nextVisitDate;
        if (notes is not null) Notes = notes;
        SetUpdatedAt();
    }
}

public enum MedicalRecordType
{
    Examination,        // Examination
    Vaccination,        // Vaccination
    Surgery,            // Surgery
    Treatment,          // Treatment
    Deworming,          // Deworming
    Sterilization,      // Sterilization/Neutering
    Microchipping,      // Microchipping
    DentalCare,         // Dental care
    Laboratory,         // Laboratory tests
    Other               // Other
}
