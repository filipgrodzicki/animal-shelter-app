using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Cms;

public class FaqItem : Entity<Guid>
{
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;
    public FaqCategory Category { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPublished { get; private set; }

    private FaqItem() { }

    public static Result<FaqItem> Create(
        string question,
        string answer,
        FaqCategory category,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(question))
            return Result.Failure<FaqItem>(Error.Validation("Pytanie jest wymagane"));

        if (string.IsNullOrWhiteSpace(answer))
            return Result.Failure<FaqItem>(Error.Validation("Odpowiedź jest wymagana"));

        return Result.Success(new FaqItem
        {
            Id = Guid.NewGuid(),
            Question = question,
            Answer = answer,
            Category = category,
            DisplayOrder = displayOrder,
            IsPublished = true
        });
    }

    public void Update(
        string? question = null,
        string? answer = null,
        FaqCategory? category = null,
        int? displayOrder = null)
    {
        if (question is not null)
            Question = question;

        if (answer is not null)
            Answer = answer;

        if (category.HasValue)
            Category = category.Value;

        if (displayOrder.HasValue)
            DisplayOrder = displayOrder.Value;

        SetUpdatedAt();
    }

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
}

public enum FaqCategory
{
    Adopcja,            // Adoption process
    OpikaZwierzat,      // Pet care
    Wolontariat,        // Volunteering
    Darowizny,          // Donations
    Kontakt,            // Contact/General
    ProceduraAdopcji    // Adoption procedures
}
