using FluentAssertions;
using ShelterApp.Domain.Chatbot;
using Xunit;

namespace ShelterApp.Tests.Chatbot;

public class ChatSessionTests
{
    #region Create Tests

    [Fact]
    public void Create_WithUserId_ShouldSetUserIdCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var session = ChatSession.Create(userId: userId);

        // Assert
        session.UserId.Should().Be(userId);
        session.AnonymousSessionId.Should().BeNull();
        session.State.Should().Be(ChatState.Initial);
    }

    [Fact]
    public void Create_WithAnonymousSessionId_ShouldSetAnonymousSessionIdCorrectly()
    {
        // Arrange
        var anonymousId = "session-12345";

        // Act
        var session = ChatSession.Create(anonymousSessionId: anonymousId);

        // Assert
        session.UserId.Should().BeNull();
        session.AnonymousSessionId.Should().Be(anonymousId);
        session.State.Should().Be(ChatState.Initial);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var session1 = ChatSession.Create();
        var session2 = ChatSession.Create();

        // Assert
        session1.Id.Should().NotBe(Guid.Empty);
        session2.Id.Should().NotBe(Guid.Empty);
        session1.Id.Should().NotBe(session2.Id);
    }

    [Fact]
    public void Create_ShouldInitializeEmptyMessagesList()
    {
        // Act
        var session = ChatSession.Create();

        // Assert
        session.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyProfile()
    {
        // Act
        var session = ChatSession.Create();

        // Assert
        session.Profile.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldSetCreatedAtAndLastActivityAt()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var session = ChatSession.Create();
        var after = DateTime.UtcNow;

        // Assert
        session.CreatedAt.Should().BeOnOrAfter(before);
        session.CreatedAt.Should().BeOnOrBefore(after);
        session.LastActivityAt.Should().BeOnOrAfter(before);
        session.LastActivityAt.Should().BeOnOrBefore(after);
    }

    #endregion

    #region AddMessage Tests

    [Fact]
    public void AddMessage_ShouldAddMessageToCollection()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        var message = session.AddMessage(ChatMessageRole.User, "Cześć!");

        // Assert
        session.Messages.Should().HaveCount(1);
        message.Content.Should().Be("Cześć!");
        message.Role.Should().Be(ChatMessageRole.User);
    }

    [Fact]
    public void AddMessage_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var session = ChatSession.Create();
        var initialActivityTime = session.LastActivityAt;
        Thread.Sleep(10);

        // Act
        session.AddMessage(ChatMessageRole.User, "Test");

        // Assert
        session.LastActivityAt.Should().BeOnOrAfter(initialActivityTime);
    }

    [Fact]
    public void AddUserMessage_ShouldAddMessageWithUserRole()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        var message = session.AddUserMessage("Szukam psa");

        // Assert
        message.Role.Should().Be(ChatMessageRole.User);
        message.Content.Should().Be("Szukam psa");
    }

    [Fact]
    public void AddAssistantMessage_ShouldAddMessageWithAssistantRole()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        var message = session.AddAssistantMessage("Mamy wiele psów do adopcji!");

        // Assert
        message.Role.Should().Be(ChatMessageRole.Assistant);
        message.Content.Should().Be("Mamy wiele psów do adopcji!");
    }

    [Fact]
    public void AddAssistantMessage_WithRecommendations_ShouldIncludeRecommendations()
    {
        // Arrange
        var session = ChatSession.Create();
        var recommendations = new List<AnimalRecommendation>
        {
            new(Guid.NewGuid(), "Burek", "Dog", "Labrador", null, 0.95, "Świetnie pasuje!")
        };

        // Act
        var message = session.AddAssistantMessage("Polecam tego psa:", recommendations);

        // Assert
        message.Recommendations.Should().HaveCount(1);
        message.Recommendations!.First().Name.Should().Be("Burek");
    }

    #endregion

    #region State Management Tests

    [Fact]
    public void SetState_ShouldChangeState()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        session.SetState(ChatState.ProfilingSpecies);

        // Assert
        session.State.Should().Be(ChatState.ProfilingSpecies);
    }

    [Fact]
    public void SetState_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var session = ChatSession.Create();
        var initialTime = session.LastActivityAt;
        Thread.Sleep(10);

        // Act
        session.SetState(ChatState.Conversing);

        // Assert
        session.LastActivityAt.Should().BeOnOrAfter(initialTime);
    }

    [Fact]
    public void AdvanceProfilingState_FromInitial_ShouldMoveToProfilingSpecies()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        session.AdvanceProfilingState();

        // Assert
        session.State.Should().Be(ChatState.ProfilingSpecies);
    }

    [Fact]
    public void AdvanceProfilingState_FromProfilingSpecies_ShouldMoveToProfilingExperience()
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingSpecies);

        // Act
        session.AdvanceProfilingState();

        // Assert
        session.State.Should().Be(ChatState.ProfilingExperience);
    }

    [Fact]
    public void AdvanceProfilingState_FromProfilingExperience_ShouldMoveToProfilingLiving()
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingExperience);

        // Act
        session.AdvanceProfilingState();

        // Assert
        session.State.Should().Be(ChatState.ProfilingLiving);
    }

    [Fact]
    public void AdvanceProfilingState_FullCycle_ShouldEndInConversing()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        session.AdvanceProfilingState(); // Initial -> ProfilingSpecies
        session.AdvanceProfilingState(); // ProfilingSpecies -> ProfilingExperience
        session.AdvanceProfilingState(); // ProfilingExperience -> ProfilingLiving
        session.AdvanceProfilingState(); // ProfilingLiving -> ProfilingLifestyle
        session.AdvanceProfilingState(); // ProfilingLifestyle -> ProfilingChildren
        session.AdvanceProfilingState(); // ProfilingChildren -> ProfilingPets
        session.AdvanceProfilingState(); // ProfilingPets -> ProfilingSize
        session.AdvanceProfilingState(); // ProfilingSize -> ProfilingComplete
        session.AdvanceProfilingState(); // ProfilingComplete -> Conversing

        // Assert
        session.State.Should().Be(ChatState.Conversing);
    }

    [Fact]
    public void AdvanceProfilingState_FromConversing_ShouldStayInConversing()
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.Conversing);

        // Act
        session.AdvanceProfilingState();

        // Assert
        session.State.Should().Be(ChatState.Conversing);
    }

    #endregion

    #region UpdateProfileFromAnswer Tests

    [Theory]
    [InlineData("pies", "Dog")]
    [InlineData("psa", "Dog")]
    [InlineData("dog", "Dog")]
    [InlineData("kot", "Cat")]
    [InlineData("kota", "Cat")]
    [InlineData("cat", "Cat")]
    [InlineData("something else", "Dog")] // default
    public void UpdateProfileFromAnswer_ProfilingSpecies_ShouldParseCorrectly(string answer, string expected)
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingSpecies);

        // Act
        session.UpdateProfileFromAnswer(answer);

        // Assert
        session.Profile.PreferredSpecies.Should().Be(expected);
    }

    [Theory]
    [InlineData("brak", "None")]
    [InlineData("nie mam", "None")]
    [InlineData("żadne", "None")]
    [InlineData("none", "None")]
    [InlineData("podstawowe", "Basic")]
    [InlineData("basic", "Basic")]
    [InlineData("trochę", "Basic")]
    [InlineData("zaawansowane", "Advanced")]
    [InlineData("duże", "Advanced")]
    [InlineData("advanced", "Advanced")]
    public void UpdateProfileFromAnswer_ProfilingExperience_ShouldParseCorrectly(string answer, string expected)
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingExperience);

        // Act
        session.UpdateProfileFromAnswer(answer);

        // Assert
        session.Profile.Experience.Should().Be(expected);
    }

    [Theory]
    [InlineData("mieszkanie", "Apartment")]
    [InlineData("apartment", "Apartment")]
    [InlineData("blok", "Apartment")]
    [InlineData("dom", "House")]
    [InlineData("house", "House")]
    [InlineData("ogród", "HouseWithGarden")]
    [InlineData("ogrodem", "HouseWithGarden")]
    [InlineData("garden", "HouseWithGarden")]
    public void UpdateProfileFromAnswer_ProfilingLiving_ShouldParseCorrectly(string answer, string expected)
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingLiving);

        // Act
        session.UpdateProfileFromAnswer(answer);

        // Assert
        session.Profile.LivingConditions.Should().Be(expected);
    }

    [Theory]
    [InlineData("spokojny", "Low")]
    [InlineData("niski", "Low")]
    [InlineData("low", "Low")]
    [InlineData("aktywny", "Medium")]
    [InlineData("umiarkowany", "Medium")]
    [InlineData("medium", "Medium")]
    [InlineData("bardzo aktywny", "High")]
    [InlineData("wysoki", "High")]
    [InlineData("sportowy", "High")]
    public void UpdateProfileFromAnswer_ProfilingLifestyle_ShouldParseCorrectly(string answer, string expected)
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingLifestyle);

        // Act
        session.UpdateProfileFromAnswer(answer);

        // Assert
        session.Profile.Lifestyle.Should().Be(expected);
    }

    [Theory]
    [InlineData("tak", true)]
    [InlineData("yes", true)]
    [InlineData("mam", true)]
    [InlineData("nie", false)]
    [InlineData("no", false)]
    public void UpdateProfileFromAnswer_ProfilingChildren_ShouldParseCorrectly(string answer, bool expected)
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingChildren);

        // Act
        session.UpdateProfileFromAnswer(answer);

        // Assert
        session.Profile.HasChildren.Should().Be(expected);
    }

    [Theory]
    [InlineData("tak", true)]
    [InlineData("nie", false)]
    public void UpdateProfileFromAnswer_ProfilingPets_ShouldParseCorrectly(string answer, bool expected)
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingPets);

        // Act
        session.UpdateProfileFromAnswer(answer);

        // Assert
        session.Profile.HasOtherPets.Should().Be(expected);
    }

    [Theory]
    [InlineData("mały", "Small")]
    [InlineData("small", "Small")]
    [InlineData("średni", "Medium")]
    [InlineData("medium", "Medium")]
    [InlineData("duży", "Large")]
    [InlineData("large", "Large")]
    [InlineData("big", "Large")]
    [InlineData("bez znaczenia", null)]
    public void UpdateProfileFromAnswer_ProfilingSize_ShouldParseCorrectly(string answer, string? expected)
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingSize);

        // Act
        session.UpdateProfileFromAnswer(answer);

        // Assert
        session.Profile.SizePreference.Should().Be(expected);
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_WhenWithinTimeout_ShouldReturnFalse()
    {
        // Arrange
        var session = ChatSession.Create();

        // Act
        var isExpired = session.IsExpired(30);

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_AfterActivity_ShouldReturnFalse()
    {
        // Arrange
        var session = ChatSession.Create();
        session.AddMessage(ChatMessageRole.User, "Test");

        // Act
        var isExpired = session.IsExpired(30);

        // Assert
        isExpired.Should().BeFalse();
    }

    #endregion

    #region ResetProfile Tests

    [Fact]
    public void ResetProfile_ShouldResetToInitialState()
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingSpecies);
        session.UpdateProfileFromAnswer("pies");

        // Act
        session.ResetProfile();

        // Assert
        session.State.Should().Be(ChatState.Initial);
        session.Profile.PreferredSpecies.Should().BeNull();
    }

    [Fact]
    public void ResetProfile_ShouldCreateNewProfileInstance()
    {
        // Arrange
        var session = ChatSession.Create();
        session.SetState(ChatState.ProfilingSpecies);
        session.UpdateProfileFromAnswer("pies");
        var oldProfile = session.Profile;

        // Act
        session.ResetProfile();

        // Assert
        session.Profile.Should().NotBeSameAs(oldProfile);
    }

    [Fact]
    public void ResetProfile_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var session = ChatSession.Create();
        var initialTime = session.LastActivityAt;
        Thread.Sleep(10);

        // Act
        session.ResetProfile();

        // Assert
        session.LastActivityAt.Should().BeOnOrAfter(initialTime);
    }

    #endregion
}
