using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Animals.Entities;

public class AnimalPhoto : Entity<Guid>
{
    public Guid AnimalId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string? ContentType { get; private set; }
    public long FileSize { get; private set; }
    public bool IsMain { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }

    private AnimalPhoto() { }

    internal static AnimalPhoto Create(
        Guid animalId,
        string fileName,
        string filePath,
        string? contentType,
        long fileSize,
        bool isMain = false,
        string? description = null,
        int displayOrder = 0)
    {
        return new AnimalPhoto
        {
            Id = Guid.NewGuid(),
            AnimalId = animalId,
            FileName = fileName,
            FilePath = filePath,
            ContentType = contentType,
            FileSize = fileSize,
            IsMain = isMain,
            Description = description,
            DisplayOrder = displayOrder
        };
    }

    internal void SetAsMain()
    {
        IsMain = true;
        SetUpdatedAt();
    }

    internal void UnsetAsMain()
    {
        IsMain = false;
        SetUpdatedAt();
    }

    internal void UpdateDisplayOrder(int order)
    {
        DisplayOrder = order;
        SetUpdatedAt();
    }
}
