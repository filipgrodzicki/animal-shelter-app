using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Common;
using ShelterApp.Domain.Cms;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Cms;

/// <summary>
/// Zarządzanie treścią CMS (WF-27)
/// </summary>
[Route("api/cms")]
[Produces("application/json")]
[ApiController]
public class CmsController : ApiController
{
    private readonly ShelterDbContext _context;

    public CmsController(ShelterDbContext context)
    {
        _context = context;
    }

    #region Blog Posts

    /// <summary>
    /// Pobiera listę wpisów blogowych
    /// </summary>
    [HttpGet("blog")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<BlogPostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlogPosts(
        [FromQuery] string? category = null,
        [FromQuery] bool? published = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BlogPosts.AsQueryable();

        // Dla anonimowych - tylko opublikowane
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            query = query.Where(p => p.IsPublished);
        }
        else if (published.HasValue)
        {
            query = query.Where(p => p.IsPublished == published.Value);
        }

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<BlogCategory>(category, out var cat))
        {
            query = query.Where(p => p.Category == cat);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var posts = await query
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new BlogPostDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Excerpt,
                p.Content,
                p.ImageUrl,
                p.Author,
                p.Category.ToString(),
                p.IsPublished,
                p.PublishedAt,
                p.ReadTimeMinutes,
                p.ViewCount,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<BlogPostDto>(posts, totalCount, page, pageSize));
    }

    /// <summary>
    /// Pobiera wpis blogowy po slug
    /// </summary>
    [HttpGet("blog/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BlogPostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBlogPostBySlug(string slug, CancellationToken cancellationToken)
    {
        var post = await _context.BlogPosts
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

        if (post is null)
            return NotFound();

        // Dla anonimowych - tylko opublikowane
        if ((!User.Identity?.IsAuthenticated ?? true) && !post.IsPublished)
            return NotFound();

        // Increment view count
        post.IncrementViewCount();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new BlogPostDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Excerpt,
            post.Content,
            post.ImageUrl,
            post.Author,
            post.Category.ToString(),
            post.IsPublished,
            post.PublishedAt,
            post.ReadTimeMinutes,
            post.ViewCount,
            post.CreatedAt,
            post.UpdatedAt
        ));
    }

    /// <summary>
    /// Tworzy nowy wpis blogowy (Admin)
    /// </summary>
    [HttpPost("blog")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BlogPostDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBlogPost(
        [FromBody] CreateBlogPostRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BlogCategory>(request.Category, out var category))
            return BadRequest("Nieprawidłowa kategoria");

        var result = BlogPost.Create(
            request.Title,
            request.Content,
            request.Excerpt ?? string.Empty,
            request.Author,
            category,
            imageUrl: request.ImageUrl
        );

        if (result.IsFailure)
            return BadRequest(result.Error.Message);

        var post = result.Value;

        if (request.Publish)
            post.Publish();

        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBlogPostBySlug), new { slug = post.Slug },
            new BlogPostDto(
                post.Id, post.Title, post.Slug, post.Excerpt, post.Content,
                post.ImageUrl, post.Author, post.Category.ToString(),
                post.IsPublished, post.PublishedAt, post.ReadTimeMinutes,
                post.ViewCount, post.CreatedAt, post.UpdatedAt
            ));
    }

    /// <summary>
    /// Aktualizuje wpis blogowy (Admin)
    /// </summary>
    [HttpPut("blog/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BlogPostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBlogPost(
        Guid id,
        [FromBody] UpdateBlogPostRequest request,
        CancellationToken cancellationToken)
    {
        var post = await _context.BlogPosts.FindAsync(new object[] { id }, cancellationToken);

        if (post is null)
            return NotFound();

        BlogCategory? category = null;
        if (!string.IsNullOrEmpty(request.Category) && Enum.TryParse<BlogCategory>(request.Category, out var cat))
            category = cat;

        post.Update(request.Title, request.Content, request.Excerpt, category, request.ImageUrl);

        if (request.Publish == true && !post.IsPublished)
            post.Publish();
        else if (request.Publish == false && post.IsPublished)
            post.Unpublish();

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new BlogPostDto(
            post.Id, post.Title, post.Slug, post.Excerpt, post.Content,
            post.ImageUrl, post.Author, post.Category.ToString(),
            post.IsPublished, post.PublishedAt, post.ReadTimeMinutes,
            post.ViewCount, post.CreatedAt, post.UpdatedAt
        ));
    }

    /// <summary>
    /// Usuwa wpis blogowy (Admin)
    /// </summary>
    [HttpDelete("blog/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBlogPost(Guid id, CancellationToken cancellationToken)
    {
        var post = await _context.BlogPosts.FindAsync(new object[] { id }, cancellationToken);

        if (post is null)
            return NotFound();

        _context.BlogPosts.Remove(post);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    #region FAQ

    /// <summary>
    /// Pobiera listę FAQ
    /// </summary>
    [HttpGet("faq")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<FaqItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFaqItems(
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FaqItems.AsQueryable();

        // Dla anonimowych - tylko opublikowane
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            query = query.Where(f => f.IsPublished);
        }

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<FaqCategory>(category, out var cat))
        {
            query = query.Where(f => f.Category == cat);
        }

        var items = await query
            .OrderBy(f => f.Category)
            .ThenBy(f => f.DisplayOrder)
            .Select(f => new FaqItemDto(
                f.Id,
                f.Question,
                f.Answer,
                f.Category.ToString(),
                f.DisplayOrder,
                f.IsPublished
            ))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    /// <summary>
    /// Tworzy nowy element FAQ (Admin)
    /// </summary>
    [HttpPost("faq")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(FaqItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFaqItem(
        [FromBody] CreateFaqItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FaqCategory>(request.Category, out var category))
            return BadRequest("Nieprawidłowa kategoria");

        var result = FaqItem.Create(
            request.Question,
            request.Answer,
            category,
            request.DisplayOrder
        );

        if (result.IsFailure)
            return BadRequest(result.Error.Message);

        var item = result.Value;

        _context.FaqItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetFaqItems), null,
            new FaqItemDto(item.Id, item.Question, item.Answer,
                item.Category.ToString(), item.DisplayOrder, item.IsPublished));
    }

    /// <summary>
    /// Aktualizuje element FAQ (Admin)
    /// </summary>
    [HttpPut("faq/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(FaqItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFaqItem(
        Guid id,
        [FromBody] UpdateFaqItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _context.FaqItems.FindAsync(new object[] { id }, cancellationToken);

        if (item is null)
            return NotFound();

        FaqCategory? category = null;
        if (!string.IsNullOrEmpty(request.Category) && Enum.TryParse<FaqCategory>(request.Category, out var cat))
            category = cat;

        item.Update(request.Question, request.Answer, category, request.DisplayOrder);

        if (request.IsPublished == true)
            item.Publish();
        else if (request.IsPublished == false)
            item.Unpublish();

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new FaqItemDto(item.Id, item.Question, item.Answer,
            item.Category.ToString(), item.DisplayOrder, item.IsPublished));
    }

    /// <summary>
    /// Usuwa element FAQ (Admin)
    /// </summary>
    [HttpDelete("faq/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteFaqItem(Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.FaqItems.FindAsync(new object[] { id }, cancellationToken);

        if (item is null)
            return NotFound();

        _context.FaqItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    #region Content Pages

    /// <summary>
    /// Pobiera stronę po slug
    /// </summary>
    [HttpGet("pages/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ContentPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageBySlug(string slug, CancellationToken cancellationToken)
    {
        var page = await _context.ContentPages
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

        if (page is null)
            return NotFound();

        if ((!User.Identity?.IsAuthenticated ?? true) && !page.IsPublished)
            return NotFound();

        return Ok(new ContentPageDto(
            page.Id, page.Title, page.Slug, page.Content,
            page.MetaDescription, page.MetaKeywords,
            page.IsPublished, page.PublishedAt,
            page.LastEditedBy, page.CreatedAt, page.UpdatedAt
        ));
    }

    /// <summary>
    /// Pobiera wszystkie strony (Admin)
    /// </summary>
    [HttpGet("pages")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<ContentPageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPages(CancellationToken cancellationToken)
    {
        var pages = await _context.ContentPages
            .OrderBy(p => p.Title)
            .Select(p => new ContentPageDto(
                p.Id, p.Title, p.Slug, p.Content,
                p.MetaDescription, p.MetaKeywords,
                p.IsPublished, p.PublishedAt,
                p.LastEditedBy, p.CreatedAt, p.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(pages);
    }

    /// <summary>
    /// Tworzy nową stronę (Admin)
    /// </summary>
    [HttpPost("pages")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ContentPageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePage(
        [FromBody] CreatePageRequest request,
        CancellationToken cancellationToken)
    {
        // Check if slug already exists
        var exists = await _context.ContentPages.AnyAsync(p => p.Slug == request.Slug, cancellationToken);
        if (exists)
            return BadRequest("Strona o podanym slug już istnieje");

        var result = ContentPage.Create(
            request.Title,
            request.Slug,
            request.Content,
            request.MetaDescription,
            request.MetaKeywords
        );

        if (result.IsFailure)
            return BadRequest(result.Error.Message);

        var page = result.Value;

        if (request.Publish)
            page.Publish();

        _context.ContentPages.Add(page);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPageBySlug), new { slug = page.Slug },
            new ContentPageDto(
                page.Id, page.Title, page.Slug, page.Content,
                page.MetaDescription, page.MetaKeywords,
                page.IsPublished, page.PublishedAt,
                page.LastEditedBy, page.CreatedAt, page.UpdatedAt
            ));
    }

    /// <summary>
    /// Aktualizuje stronę (Admin)
    /// </summary>
    [HttpPut("pages/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ContentPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePage(
        Guid id,
        [FromBody] UpdatePageRequest request,
        CancellationToken cancellationToken)
    {
        var page = await _context.ContentPages.FindAsync(new object[] { id }, cancellationToken);

        if (page is null)
            return NotFound();

        page.Update(request.Title, request.Content, request.MetaDescription, request.MetaKeywords);

        if (request.Publish == true && !page.IsPublished)
            page.Publish();
        else if (request.Publish == false && page.IsPublished)
            page.Unpublish();

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new ContentPageDto(
            page.Id, page.Title, page.Slug, page.Content,
            page.MetaDescription, page.MetaKeywords,
            page.IsPublished, page.PublishedAt,
            page.LastEditedBy, page.CreatedAt, page.UpdatedAt
        ));
    }

    /// <summary>
    /// Usuwa stronę (Admin)
    /// </summary>
    [HttpDelete("pages/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePage(Guid id, CancellationToken cancellationToken)
    {
        var page = await _context.ContentPages.FindAsync(new object[] { id }, cancellationToken);

        if (page is null)
            return NotFound();

        _context.ContentPages.Remove(page);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    #endregion
}

// ============================================
// DTOs
// ============================================

public record BlogPostDto(
    Guid Id,
    string Title,
    string Slug,
    string Excerpt,
    string Content,
    string? ImageUrl,
    string Author,
    string Category,
    bool IsPublished,
    DateTime? PublishedAt,
    int ReadTimeMinutes,
    int ViewCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record FaqItemDto(
    Guid Id,
    string Question,
    string Answer,
    string Category,
    int DisplayOrder,
    bool IsPublished
);

public record ContentPageDto(
    Guid Id,
    string Title,
    string Slug,
    string Content,
    string? MetaDescription,
    string? MetaKeywords,
    bool IsPublished,
    DateTime? PublishedAt,
    string? LastEditedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ============================================
// Request DTOs
// ============================================

public record CreateBlogPostRequest(
    string Title,
    string Content,
    string? Excerpt,
    string Author,
    string Category,
    string? ImageUrl,
    bool Publish = false
);

public record UpdateBlogPostRequest(
    string? Title,
    string? Content,
    string? Excerpt,
    string? Category,
    string? ImageUrl,
    bool? Publish
);

public record CreateFaqItemRequest(
    string Question,
    string Answer,
    string Category,
    int DisplayOrder = 0
);

public record UpdateFaqItemRequest(
    string? Question,
    string? Answer,
    string? Category,
    int? DisplayOrder,
    bool? IsPublished
);

public record CreatePageRequest(
    string Title,
    string Slug,
    string Content,
    string? MetaDescription,
    string? MetaKeywords,
    bool Publish = false
);

public record UpdatePageRequest(
    string? Title,
    string? Content,
    string? MetaDescription,
    string? MetaKeywords,
    bool? Publish
);
