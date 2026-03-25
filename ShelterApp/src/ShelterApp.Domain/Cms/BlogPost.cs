using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Cms;

public class BlogPost : Entity<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public Guid? AuthorUserId { get; private set; }
    public BlogCategory Category { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public int ReadTimeMinutes { get; private set; }
    public int ViewCount { get; private set; }

    private BlogPost() { }

    public static Result<BlogPost> Create(
        string title,
        string content,
        string excerpt,
        string author,
        BlogCategory category,
        Guid? authorUserId = null,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<BlogPost>(Error.Validation("Tytuł jest wymagany"));

        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure<BlogPost>(Error.Validation("Treść jest wymagana"));

        var slug = GenerateSlug(title);
        var readTime = CalculateReadTime(content);

        return Result.Success(new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Content = content,
            Excerpt = string.IsNullOrWhiteSpace(excerpt) ? GenerateExcerpt(content) : excerpt,
            Author = author,
            AuthorUserId = authorUserId,
            Category = category,
            ImageUrl = imageUrl,
            IsPublished = false,
            ReadTimeMinutes = readTime,
            ViewCount = 0
        });
    }

    public void Update(
        string? title = null,
        string? content = null,
        string? excerpt = null,
        BlogCategory? category = null,
        string? imageUrl = null)
    {
        if (title is not null)
        {
            Title = title;
            Slug = GenerateSlug(title);
        }

        if (content is not null)
        {
            Content = content;
            ReadTimeMinutes = CalculateReadTime(content);
        }

        if (excerpt is not null)
            Excerpt = excerpt;

        if (category.HasValue)
            Category = category.Value;

        if (imageUrl is not null)
            ImageUrl = imageUrl;

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

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("ą", "a").Replace("ć", "c").Replace("ę", "e")
            .Replace("ł", "l").Replace("ń", "n").Replace("ó", "o")
            .Replace("ś", "s").Replace("ź", "z").Replace("ż", "z");

        // Remove non-alphanumeric except hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

    private static string GenerateExcerpt(string content, int maxLength = 200)
    {
        var plainText = System.Text.RegularExpressions.Regex.Replace(content, @"<[^>]+>", "");
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();

        if (plainText.Length <= maxLength)
            return plainText;

        return plainText.Substring(0, maxLength).TrimEnd() + "...";
    }

    private static int CalculateReadTime(string content)
    {
        var wordCount = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, wordCount / 200); // ~200 words per minute
    }
}

public enum BlogCategory
{
    Adopcja,        // Adoption tips
    Porady,         // Pet care advice
    Zdrowie,        // Health tips
    Historie,       // Success stories
    Wydarzenia,     // Events
    Aktualnosci     // News
}
