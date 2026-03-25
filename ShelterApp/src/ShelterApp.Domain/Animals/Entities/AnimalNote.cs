using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Animals.Entities;

/// <summary>
/// Animal note type
/// </summary>
public enum AnimalNoteType
{
    /// <summary>Behavior observation</summary>
    BehaviorObservation,

    /// <summary>Health observation</summary>
    HealthObservation,

    /// <summary>Feeding</summary>
    Feeding,

    /// <summary>Walk/Activity</summary>
    WalkActivity,

    /// <summary>Interaction with other animals</summary>
    AnimalInteraction,

    /// <summary>Interaction with humans</summary>
    HumanInteraction,

    /// <summary>Grooming</summary>
    Grooming,

    /// <summary>Training</summary>
    Training,

    /// <summary>General note</summary>
    General,

    /// <summary>Urgent</summary>
    Urgent
}

/// <summary>
/// Animal note added by a volunteer or staff member (WB-19)
/// </summary>
public class AnimalNote : Entity<Guid>
{
    /// <summary>
    /// Animal ID
    /// </summary>
    public Guid AnimalId { get; private set; }

    /// <summary>
    /// ID of the volunteer who added the note (optional)
    /// </summary>
    public Guid? VolunteerId { get; private set; }

    /// <summary>
    /// ID of the user who added the note
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Author's full name
    /// </summary>
    public string AuthorName { get; private set; } = string.Empty;

    /// <summary>
    /// Note type
    /// </summary>
    public AnimalNoteType NoteType { get; private set; }

    /// <summary>
    /// Note title
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Note content
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the note is important/urgent
    /// </summary>
    public bool IsImportant { get; private set; }

    /// <summary>
    /// Observation date (may differ from creation date)
    /// </summary>
    public DateTime ObservationDate { get; private set; }

    private AnimalNote() { }

    /// <summary>
    /// Creates a new animal note
    /// </summary>
    public static Result<AnimalNote> Create(
        Guid animalId,
        AnimalNoteType noteType,
        string title,
        string content,
        string authorName,
        Guid? volunteerId = null,
        Guid? userId = null,
        bool isImportant = false,
        DateTime? observationDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<AnimalNote>(
                Error.Validation("Tytuł notatki jest wymagany"));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Failure<AnimalNote>(
                Error.Validation("Treść notatki jest wymagana"));
        }

        if (string.IsNullOrWhiteSpace(authorName))
        {
            return Result.Failure<AnimalNote>(
                Error.Validation("Imię autora jest wymagane"));
        }

        var note = new AnimalNote
        {
            Id = Guid.NewGuid(),
            AnimalId = animalId,
            VolunteerId = volunteerId,
            UserId = userId,
            AuthorName = authorName,
            NoteType = noteType,
            Title = title.Trim(),
            Content = content.Trim(),
            IsImportant = isImportant || noteType == AnimalNoteType.Urgent,
            ObservationDate = observationDate ?? DateTime.UtcNow
        };

        return Result.Success(note);
    }

    /// <summary>
    /// Updates the note content
    /// </summary>
    public void UpdateContent(string title, string content)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            Content = content.Trim();
        }

        SetUpdatedAt();
    }

    /// <summary>
    /// Marks the note as important
    /// </summary>
    public void MarkAsImportant()
    {
        IsImportant = true;
        SetUpdatedAt();
    }

    /// <summary>
    /// Removes the important flag
    /// </summary>
    public void UnmarkAsImportant()
    {
        IsImportant = false;
        SetUpdatedAt();
    }
}
