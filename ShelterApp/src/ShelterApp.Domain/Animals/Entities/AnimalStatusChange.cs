using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Animals.Entities;

public class AnimalStatusChange : Entity<Guid>
{
    public Guid AnimalId { get; private set; }
    public AnimalStatus PreviousStatus { get; private set; }
    public AnimalStatus NewStatus { get; private set; }
    public AnimalStatusTrigger Trigger { get; private set; }
    public string? Reason { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTime ChangedAt { get; private set; }

    private AnimalStatusChange() { }

    internal static AnimalStatusChange Create(
        Guid animalId,
        AnimalStatus previousStatus,
        AnimalStatus newStatus,
        AnimalStatusTrigger trigger,
        string changedBy,
        string? reason = null)
    {
        return new AnimalStatusChange
        {
            Id = Guid.NewGuid(),
            AnimalId = animalId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Trigger = trigger,
            Reason = reason,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
    }
}
