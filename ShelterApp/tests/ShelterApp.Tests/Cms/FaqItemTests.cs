using FluentAssertions;
using ShelterApp.Domain.Cms;
using Xunit;

namespace ShelterApp.Tests.Cms;

public class FaqItemTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Act
        var result = FaqItem.Create(
            question: "Jak mogę zaadoptować zwierzę?",
            answer: "Proces adopcji jest prosty...",
            category: FaqCategory.Adopcja,
            displayOrder: 1
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Question.Should().Be("Jak mogę zaadoptować zwierzę?");
        result.Value.Answer.Should().Be("Proces adopcji jest prosty...");
        result.Value.Category.Should().Be(FaqCategory.Adopcja);
        result.Value.DisplayOrder.Should().Be(1);
        result.Value.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyQuestion_ShouldFail()
    {
        // Act
        var result = FaqItem.Create(
            question: "",
            answer: "Odpowiedź",
            category: FaqCategory.Adopcja
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Pytanie");
    }

    [Fact]
    public void Create_WithWhiteSpaceQuestion_ShouldFail()
    {
        // Act
        var result = FaqItem.Create(
            question: "   ",
            answer: "Odpowiedź",
            category: FaqCategory.Adopcja
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Pytanie");
    }

    [Fact]
    public void Create_WithEmptyAnswer_ShouldFail()
    {
        // Act
        var result = FaqItem.Create(
            question: "Pytanie?",
            answer: "",
            category: FaqCategory.Adopcja
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Odpowiedź");
    }

    [Fact]
    public void Create_WithWhiteSpaceAnswer_ShouldFail()
    {
        // Act
        var result = FaqItem.Create(
            question: "Pytanie?",
            answer: "   ",
            category: FaqCategory.Adopcja
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Odpowiedź");
    }

    [Fact]
    public void Create_WithDefaultDisplayOrder_ShouldBeZero()
    {
        // Act
        var result = FaqItem.Create(
            question: "Pytanie?",
            answer: "Odpowiedź",
            category: FaqCategory.Adopcja
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayOrder.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var result1 = FaqItem.Create("Pytanie 1?", "Odpowiedź 1", FaqCategory.Adopcja);
        var result2 = FaqItem.Create("Pytanie 2?", "Odpowiedź 2", FaqCategory.Adopcja);

        // Assert
        result1.Value.Id.Should().NotBe(Guid.Empty);
        result2.Value.Id.Should().NotBe(Guid.Empty);
        result1.Value.Id.Should().NotBe(result2.Value.Id);
    }

    [Fact]
    public void Create_ShouldSetIsPublishedToTrue()
    {
        // Act
        var result = FaqItem.Create(
            question: "Pytanie?",
            answer: "Odpowiedź",
            category: FaqCategory.Adopcja
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsPublished.Should().BeTrue();
    }

    [Theory]
    [InlineData(FaqCategory.Adopcja)]
    [InlineData(FaqCategory.OpikaZwierzat)]
    [InlineData(FaqCategory.Wolontariat)]
    [InlineData(FaqCategory.Darowizny)]
    [InlineData(FaqCategory.Kontakt)]
    [InlineData(FaqCategory.ProceduraAdopcji)]
    public void Create_WithDifferentCategories_ShouldSucceed(FaqCategory category)
    {
        // Act
        var result = FaqItem.Create(
            question: "Pytanie?",
            answer: "Odpowiedź",
            category: category
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be(category);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateSpecifiedFields()
    {
        // Arrange
        var faq = FaqItem.Create("Pytanie?", "Odpowiedź", FaqCategory.Adopcja).Value;

        // Act
        faq.Update(
            question: "Nowe pytanie?",
            answer: "Nowa odpowiedź",
            category: FaqCategory.Wolontariat,
            displayOrder: 5
        );

        // Assert
        faq.Question.Should().Be("Nowe pytanie?");
        faq.Answer.Should().Be("Nowa odpowiedź");
        faq.Category.Should().Be(FaqCategory.Wolontariat);
        faq.DisplayOrder.Should().Be(5);
    }

    [Fact]
    public void Update_WithNullValues_ShouldNotChangeExistingValues()
    {
        // Arrange
        var faq = FaqItem.Create("Pytanie?", "Odpowiedź", FaqCategory.Adopcja, 1).Value;
        var originalQuestion = faq.Question;
        var originalAnswer = faq.Answer;

        // Act
        faq.Update(displayOrder: 10);

        // Assert
        faq.Question.Should().Be(originalQuestion);
        faq.Answer.Should().Be(originalAnswer);
        faq.DisplayOrder.Should().Be(10);
    }

    [Fact]
    public void Update_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var faq = FaqItem.Create("Pytanie?", "Odpowiedź", FaqCategory.Adopcja).Value;
        faq.Update(question: "Początkowe pytanie?"); // Ensure UpdatedAt is set
        var initialUpdatedAt = faq.UpdatedAt!.Value;
        Thread.Sleep(10);

        // Act
        faq.Update(question: "Nowe pytanie?");

        // Assert
        faq.UpdatedAt.Should().NotBeNull();
        faq.UpdatedAt!.Value.Should().BeOnOrAfter(initialUpdatedAt);
    }

    [Fact]
    public void Update_OnlyQuestion_ShouldOnlyChangeQuestion()
    {
        // Arrange
        var faq = FaqItem.Create("Pytanie?", "Odpowiedź", FaqCategory.Adopcja, 1).Value;
        var originalAnswer = faq.Answer;
        var originalCategory = faq.Category;
        var originalOrder = faq.DisplayOrder;

        // Act
        faq.Update(question: "Nowe pytanie?");

        // Assert
        faq.Question.Should().Be("Nowe pytanie?");
        faq.Answer.Should().Be(originalAnswer);
        faq.Category.Should().Be(originalCategory);
        faq.DisplayOrder.Should().Be(originalOrder);
    }

    #endregion

    #region Publish Tests

    [Fact]
    public void Publish_ShouldSetIsPublishedTrue()
    {
        // Arrange
        var faq = FaqItem.Create("Pytanie?", "Odpowiedź", FaqCategory.Adopcja).Value;
        faq.Unpublish();

        // Act
        faq.Publish();

        // Assert
        faq.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Unpublish_ShouldSetIsPublishedFalse()
    {
        // Arrange
        var faq = FaqItem.Create("Pytanie?", "Odpowiedź", FaqCategory.Adopcja).Value;

        // Act
        faq.Unpublish();

        // Assert
        faq.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void PublishUnpublish_MultipleTimes_ShouldWorkCorrectly()
    {
        // Arrange
        var faq = FaqItem.Create("Pytanie?", "Odpowiedź", FaqCategory.Adopcja).Value;

        // Act & Assert
        faq.IsPublished.Should().BeTrue();

        faq.Unpublish();
        faq.IsPublished.Should().BeFalse();

        faq.Publish();
        faq.IsPublished.Should().BeTrue();

        faq.Unpublish();
        faq.IsPublished.Should().BeFalse();
    }

    #endregion
}
