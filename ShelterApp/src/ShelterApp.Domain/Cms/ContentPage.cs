using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Cms;

public class ContentPage : Entity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? MetaDescription { get; private set; }
    public string? MetaKeywords { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public string? LastEditedBy { get; private set; }
    public Guid? LastEditedByUserId { get; private set; }

    private ContentPage() { }

    public static Result<ContentPage> Create(
        string title,
        string slug,
        string content,
        string? metaDescription = null,
        string? metaKeywords = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<ContentPage>(Error.Validation("Tytuł jest wymagany"));

        if (string.IsNullOrWhiteSpace(slug))
            return Result.Failure<ContentPage>(Error.Validation("Slug jest wymagany"));

        return Result.Success(new ContentPage
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug.ToLowerInvariant().Trim(),
            Content = content ?? string.Empty,
            MetaDescription = metaDescription,
            MetaKeywords = metaKeywords,
            IsPublished = false
        });
    }

    public void Update(
        string? title = null,
        string? content = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        string? editedBy = null,
        Guid? editedByUserId = null)
    {
        if (title is not null)
            Title = title;

        if (content is not null)
            Content = content;

        MetaDescription = metaDescription ?? MetaDescription;
        MetaKeywords = metaKeywords ?? MetaKeywords;

        if (editedBy is not null)
        {
            LastEditedBy = editedBy;
            LastEditedByUserId = editedByUserId;
        }

        SetUpdatedAt();
    }

    public void Publish()
    {
        if (!IsPublished)
        {
            IsPublished = true;
            PublishedAt = DateTime.UtcNow;
            SetUpdatedAt();
        }
    }

    public void Unpublish()
    {
        if (IsPublished)
        {
            IsPublished = false;
            SetUpdatedAt();
        }
    }
}
