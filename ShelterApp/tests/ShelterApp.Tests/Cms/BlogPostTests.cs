using FluentAssertions;
using ShelterApp.Domain.Cms;
using Xunit;

namespace ShelterApp.Tests.Cms;

public class BlogPostTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Act
        var result = BlogPost.Create(
            title: "Jak przygotować dom na adopcję psa",
            content: "Treść artykułu o przygotowaniu domu...",
            excerpt: "Krótki opis artykułu",
            author: "Jan Kowalski",
            category: BlogCategory.Adopcja,
            authorUserId: Guid.NewGuid(),
            imageUrl: "/images/dog-adoption.jpg"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Jak przygotować dom na adopcję psa");
        result.Value.Content.Should().Be("Treść artykułu o przygotowaniu domu...");
        result.Value.Excerpt.Should().Be("Krótki opis artykułu");
        result.Value.Author.Should().Be("Jan Kowalski");
        result.Value.Category.Should().Be(BlogCategory.Adopcja);
        result.Value.ImageUrl.Should().Be("/images/dog-adoption.jpg");
        result.Value.IsPublished.Should().BeFalse();
        result.Value.ViewCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldFail()
    {
        // Act
        var result = BlogPost.Create(
            title: "",
            content: "Treść",
            excerpt: "Opis",
            author: "Autor",
            category: BlogCategory.Porady
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Tytuł");
    }

    [Fact]
    public void Create_WithWhiteSpaceTitle_ShouldFail()
    {
        // Act
        var result = BlogPost.Create(
            title: "   ",
            content: "Treść",
            excerpt: "Opis",
            author: "Autor",
            category: BlogCategory.Porady
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Tytuł");
    }

    [Fact]
    public void Create_WithEmptyContent_ShouldFail()
    {
        // Act
        var result = BlogPost.Create(
            title: "Tytuł",
            content: "",
            excerpt: "Opis",
            author: "Autor",
            category: BlogCategory.Porady
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Treść");
    }

    [Fact]
    public void Create_ShouldGenerateSlugFromTitle()
    {
        // Act
        var result = BlogPost.Create(
            title: "Jak Przygotować Dom Na Adopcję Psa",
            content: "Treść",
            excerpt: "Opis",
            author: "Autor",
            category: BlogCategory.Adopcja
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("jak-przygotowac-dom-na-adopcje-psa");
    }

    [Fact]
    public void Create_WithPolishCharacters_ShouldGenerateCleanSlug()
    {
        // Act
        var result = BlogPost.Create(
            title: "Żółć świętą będę próżnić",
            content: "Treść",
            excerpt: "Opis",
            author: "Autor",
            category: BlogCategory.Porady
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().NotContain("ż");
        result.Value.Slug.Should().NotContain("ó");
        result.Value.Slug.Should().NotContain("ć");
        result.Value.Slug.Should().NotContain("ś");
        result.Value.Slug.Should().NotContain("ę");
    }

    [Fact]
    public void Create_WithEmptyExcerpt_ShouldGenerateFromContent()
    {
        // Arrange
        var content = "To jest bardzo długa treść artykułu, która ma wiele słów i zdań, aby przetestować automatyczne generowanie skrótu z zawartości...";

        // Act
        var result = BlogPost.Create(
            title: "Tytuł",
            content: content,
            excerpt: "",
            author: "Autor",
            category: BlogCategory.Porady
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Excerpt.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldCalculateReadTime()
    {
        // Arrange - approximately 400 words = 2 minutes
        var words = string.Join(" ", Enumerable.Repeat("słowo", 400));

        // Act
        var result = BlogPost.Create(
            title: "Tytuł",
            content: words,
            excerpt: "Opis",
            author: "Autor",
            category: BlogCategory.Porady
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReadTimeMinutes.Should().Be(2);
    }

    [Fact]
    public void Create_WithShortContent_ShouldHaveAtLeast1MinuteReadTime()
    {
        // Act
        var result = BlogPost.Create(
            title: "Tytuł",
            content: "Krótka treść",
            excerpt: "Opis",
            author: "Autor",
            category: BlogCategory.Porady
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReadTimeMinutes.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var result1 = BlogPost.Create("Tytuł 1", "Treść 1", "Opis 1", "Autor", BlogCategory.Porady);
        var result2 = BlogPost.Create("Tytuł 2", "Treść 2", "Opis 2", "Autor", BlogCategory.Porady);

        // Assert
        result1.Value.Id.Should().NotBe(Guid.Empty);
        result2.Value.Id.Should().NotBe(Guid.Empty);
        result1.Value.Id.Should().NotBe(result2.Value.Id);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateSpecifiedFields()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;

        // Act
        post.Update(
            title: "Nowy tytuł",
            content: "Nowa treść",
            excerpt: "Nowy opis",
            category: BlogCategory.Zdrowie,
            imageUrl: "/images/new.jpg"
        );

        // Assert
        post.Title.Should().Be("Nowy tytuł");
        post.Content.Should().Be("Nowa treść");
        post.Excerpt.Should().Be("Nowy opis");
        post.Category.Should().Be(BlogCategory.Zdrowie);
        post.ImageUrl.Should().Be("/images/new.jpg");
    }

    [Fact]
    public void Update_WithNewTitle_ShouldRegenerateSlug()
    {
        // Arrange
        var post = BlogPost.Create("Stary tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;
        var oldSlug = post.Slug;

        // Act
        post.Update(title: "Nowy inny tytuł");

        // Assert
        post.Slug.Should().NotBe(oldSlug);
        post.Slug.Should().Be("nowy-inny-tytul");
    }

    [Fact]
    public void Update_WithNewContent_ShouldRecalculateReadTime()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Krótka treść", "Opis", "Autor", BlogCategory.Porady).Value;
        var oldReadTime = post.ReadTimeMinutes;
        var longContent = string.Join(" ", Enumerable.Repeat("słowo", 1000));

        // Act
        post.Update(content: longContent);

        // Assert
        post.ReadTimeMinutes.Should().BeGreaterThan(oldReadTime);
    }

    [Fact]
    public void Update_WithNullValues_ShouldNotChangeExistingValues()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;
        var originalTitle = post.Title;
        var originalContent = post.Content;

        // Act
        post.Update(excerpt: "Nowy opis");

        // Assert
        post.Title.Should().Be(originalTitle);
        post.Content.Should().Be(originalContent);
        post.Excerpt.Should().Be("Nowy opis");
    }

    #endregion

    #region Publish Tests

    [Fact]
    public void Publish_UnpublishedPost_ShouldSetIsPublishedTrue()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;

        // Act
        post.Publish();

        // Assert
        post.IsPublished.Should().BeTrue();
        post.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public void Publish_ShouldSetPublishedAtToNow()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;
        var before = DateTime.UtcNow;

        // Act
        post.Publish();
        var after = DateTime.UtcNow;

        // Assert
        post.PublishedAt.Should().NotBeNull();
        post.PublishedAt.Should().BeOnOrAfter(before);
        post.PublishedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Publish_AlreadyPublishedPost_ShouldNotChangePublishedAt()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;
        post.Publish();
        var originalPublishedAt = post.PublishedAt;
        Thread.Sleep(10);

        // Act
        post.Publish();

        // Assert
        post.PublishedAt.Should().Be(originalPublishedAt);
    }

    [Fact]
    public void Unpublish_PublishedPost_ShouldSetIsPublishedFalse()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;
        post.Publish();

        // Act
        post.Unpublish();

        // Assert
        post.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Unpublish_UnpublishedPost_ShouldDoNothing()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;

        // Act
        post.Unpublish();

        // Assert
        post.IsPublished.Should().BeFalse();
    }

    #endregion

    #region ViewCount Tests

    [Fact]
    public void IncrementViewCount_ShouldIncreaseViewCount()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;

        // Act
        post.IncrementViewCount();

        // Assert
        post.ViewCount.Should().Be(1);
    }

    [Fact]
    public void IncrementViewCount_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var post = BlogPost.Create("Tytuł", "Treść", "Opis", "Autor", BlogCategory.Porady).Value;

        // Act
        post.IncrementViewCount();
        post.IncrementViewCount();
        post.IncrementViewCount();

        // Assert
        post.ViewCount.Should().Be(3);
    }

    #endregion
}
