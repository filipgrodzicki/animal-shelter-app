using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Animals.Entities;

public class MedicalRecordAttachment : Entity<Guid>
{
    public Guid MedicalRecordId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string? ContentType { get; private set; }
    public long FileSize { get; private set; }
    public string? Description { get; private set; }

    private MedicalRecordAttachment() { }

    internal static MedicalRecordAttachment Create(
        Guid medicalRecordId,
        string fileName,
        string filePath,
        string? contentType,
        long fileSize,
        string? description = null)
    {
        return new MedicalRecordAttachment
        {
            Id = Guid.NewGuid(),
            MedicalRecordId = medicalRecordId,
            FileName = fileName,
            FilePath = filePath,
            ContentType = contentType,
            FileSize = fileSize,
            Description = description
        };
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        SetUpdatedAt();
    }
}
