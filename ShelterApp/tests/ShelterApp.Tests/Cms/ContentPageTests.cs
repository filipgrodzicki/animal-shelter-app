using FluentAssertions;
using ShelterApp.Domain.Cms;
using Xunit;

namespace ShelterApp.Tests.Cms;

public class ContentPageTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Act
        var result = ContentPage.Create(
            title: "O nas",
            slug: "o-nas",
            content: "Treść strony o nas...",
            metaDescription: "Opis strony dla wyszukiwarek",
            metaKeywords: "schronisko, adopcja, zwierzęta"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("O nas");
        result.Value.Slug.Should().Be("o-nas");
        result.Value.Content.Should().Be("Treść strony o nas...");
        result.Value.MetaDescription.Should().Be("Opis strony dla wyszukiwarek");
        result.Value.MetaKeywords.Should().Be("schronisko, adopcja, zwierzęta");
        result.Value.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldFail()
    {
        // Act
        var result = ContentPage.Create(
            title: "",
            slug: "slug",
            content: "Treść"
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Tytuł");
    }

    [Fact]
    public void Create_WithWhiteSpaceTitle_ShouldFail()
    {
        // Act
        var result = ContentPage.Create(
            title: "   ",
            slug: "slug",
            content: "Treść"
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Tytuł");
    }

    [Fact]
    public void Create_WithEmptySlug_ShouldFail()
    {
        // Act
        var result = ContentPage.Create(
            title: "Tytuł",
            slug: "",
            content: "Treść"
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Slug");
    }

    [Fact]
    public void Create_WithWhiteSpaceSlug_ShouldFail()
    {
        // Act
        var result = ContentPage.Create(
            title: "Tytuł",
            slug: "   ",
            content: "Treść"
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Slug");
    }

    [Fact]
    public void Create_ShouldNormalizeSlugToLowerCase()
    {
        // Act
        var result = ContentPage.Create(
            title: "Tytuł",
            slug: "ABOUT-US",
            content: "Treść"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("about-us");
    }

    [Fact]
    public void Create_ShouldTrimSlug()
    {
        // Act
        var result = ContentPage.Create(
            title: "Tytuł",
            slug: "  about-us  ",
            content: "Treść"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("about-us");
    }

    [Fact]
    public void Create_WithNullContent_ShouldSetEmptyString()
    {
        // Act
        var result = ContentPage.Create(
            title: "Tytuł",
            slug: "slug",
            content: null!
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be(string.Empty);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var result1 = ContentPage.Create("Tytuł 1", "slug-1", "Treść 1");
        var result2 = ContentPage.Create("Tytuł 2", "slug-2", "Treść 2");

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
        var page = ContentPage.Create("Tytuł", "slug", "Treść").Value;
        var userId = Guid.NewGuid();

        // Act
        page.Update(
            title: "Nowy tytuł",
            content: "Nowa treść",
            metaDescription: "Nowy opis",
            metaKeywords: "nowe, słowa, kluczowe",
            editedBy: "Jan Kowalski",
            editedByUserId: userId
        );

        // Assert
        page.Title.Should().Be("Nowy tytuł");
        page.Content.Should().Be("Nowa treść");
        page.MetaDescription.Should().Be("Nowy opis");
        page.MetaKeywords.Should().Be("nowe, słowa, kluczowe");
        page.LastEditedBy.Should().Be("Jan Kowalski");
        page.LastEditedByUserId.Should().Be(userId);
    }

    [Fact]
    public void Update_WithNullValues_ShouldNotChangeExistingValues()
    {
        // Arrange
        var page = ContentPage.Create("Tytuł", "slug", "Treść", "Opis", "Słowa").Value;
        var originalTitle = page.Title;
        var originalContent = page.Content;

        // Act
        page.Update(metaDescription: "Nowy opis");

        // Assert
        page.Title.Should().Be(originalTitle);
        page.Content.Should().Be(originalContent);
        page.MetaDescription.Should().Be("Nowy opis");
    }

    [Fact]
    public void Update_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var page = ContentPage.Create("Tytuł", "slug", "Treść").Value;
        page.Update(title: "Początkowy tytuł"); // Ensure UpdatedAt is set
        var initialUpdatedAt = page.UpdatedAt!.Value;
        Thread.Sleep(10);

        // Act
        page.Update(title: "Nowy tytuł");

        // Assert
        page.UpdatedAt.Should().NotBeNull();
        page.UpdatedAt!.Value.Should().BeOnOrAfter(initialUpdatedAt);
    }

    #endregion

    #region Publish Tests

    [Fact]
    public void Publish_UnpublishedPage_ShouldSetIsPublishedTrue()
    {
        // Arrange
        var page = ContentPage.Create("Tytuł", "slug", "Treść").Value;

        // Act
        page.Publish();

        // Assert
        page.IsPublished.Should().BeTrue();
        page.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public void Publish_ShouldSetPublishedAtToNow()
    {
        // Arrange
        var page = ContentPage.Create("Tytuł", "slug", "Treść").Value;
        var before = DateTime.UtcNow;

        // Act
        page.Publish();
        var after = DateTime.UtcNow;

        // Assert
        page.PublishedAt.Should().BeOnOrAfter(before);
        page.PublishedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Publish_AlreadyPublishedPage_ShouldNotChangePublishedAt()
    {
        // Arrange
        var page = ContentPage.Create("Tytuł", "slug", "Treść").Value;
        page.Publish();
        var originalPublishedAt = page.PublishedAt;
        Thread.Sleep(10);

        // Act
        page.Publish();

        // Assert
        page.PublishedAt.Should().Be(originalPublishedAt);
    }

    [Fact]
    public void Unpublish_PublishedPage_ShouldSetIsPublishedFalse()
    {
        // Arrange
        var page = ContentPage.Create("Tytuł", "slug", "Treść").Value;
        page.Publish();

        // Act
        page.Unpublish();

        // Assert
        page.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Unpublish_UnpublishedPage_ShouldDoNothing()
    {
        // Arrange
        var page = ContentPage.Create("Tytuł", "slug", "Treść").Value;

        // Act
        page.Unpublish();

        // Assert
        page.IsPublished.Should().BeFalse();
    }

    #endregion
}
