namespace ShelterApp.Domain.Chatbot;

/// <summary>
/// Chat session with a user
/// </summary>
public class ChatSession
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string? AnonymousSessionId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public ChatState State { get; private set; }
    public UserProfile Profile { get; private set; } = new();

    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private ChatSession() { }

    /// <summary>
    /// Creates a new chat session
    /// </summary>
    public static ChatSession Create(Guid? userId = null, string? anonymousSessionId = null)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AnonymousSessionId = anonymousSessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            State = ChatState.Initial,
            Profile = new UserProfile()
        };

        return session;
    }

    /// <summary>
    /// Adds a message to the session
    /// </summary>
    public ChatMessage AddMessage(ChatMessageRole role, string content, List<AnimalRecommendation>? recommendations = null)
    {
        var message = ChatMessage.Create(role, content, recommendations);
        _messages.Add(message);
        LastActivityAt = DateTime.UtcNow;
        return message;
    }

    /// <summary>
    /// Adds a user message
    /// </summary>
    public ChatMessage AddUserMessage(string content)
    {
        return AddMessage(ChatMessageRole.User, content);
    }

    /// <summary>
    /// Adds an assistant message
    /// </summary>
    public ChatMessage AddAssistantMessage(string content, List<AnimalRecommendation>? recommendations = null)
    {
        return AddMessage(ChatMessageRole.Assistant, content, recommendations);
    }

    /// <summary>
    /// Changes the session state
    /// </summary>
    public void SetState(ChatState newState)
    {
        State = newState;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Advances to the next profiling step
    /// </summary>
    public void AdvanceProfilingState()
    {
        State = State switch
        {
            ChatState.Initial => ChatState.ProfilingSpecies,
            ChatState.ProfilingSpecies => ChatState.ProfilingExperience,
            ChatState.ProfilingExperience => ChatState.ProfilingLiving,
            ChatState.ProfilingLiving => ChatState.ProfilingLifestyle,
            ChatState.ProfilingLifestyle => ChatState.ProfilingChildren,
            ChatState.ProfilingChildren => ChatState.ProfilingPets,
            ChatState.ProfilingPets => ChatState.ProfilingComplete,
            ChatState.ProfilingComplete => ChatState.Conversing,
            _ => State
        };
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user profile based on the current state.
    /// Returns true if the answer was recognized, false otherwise.
    /// </summary>
    public bool UpdateProfileFromAnswer(string answer)
    {
        var normalizedAnswer = answer.Trim().ToLowerInvariant();
        LastActivityAt = DateTime.UtcNow;

        switch (State)
        {
            case ChatState.ProfilingSpecies:
                Profile.PreferredSpecies = TryParseSpecies(normalizedAnswer);
                return true;
            case ChatState.ProfilingExperience:
                var experience = TryParseExperience(normalizedAnswer);
                if (experience == null) return false;
                Profile.Experience = experience;
                return true;
            case ChatState.ProfilingLiving:
                var living = TryParseLivingConditions(normalizedAnswer);
                if (living == null) return false;
                Profile.LivingConditions = living;
                return true;
            case ChatState.ProfilingLifestyle:
                var careTime = TryParseCareTime(normalizedAnswer);
                if (careTime == null) return false;
                Profile.AvailableTime = careTime;
                Profile.Lifestyle = careTime switch
                {
                    "lessThan1h" => "Low",
                    "1to3h" => "Medium",
                    "moreThan3h" => "High",
                    _ => "Medium"
                };
                return true;
            case ChatState.ProfilingChildren:
                var children = TryParseYesNo(normalizedAnswer);
                if (children == null) return false;
                Profile.HasChildren = children.Value;
                return true;
            case ChatState.ProfilingPets:
                var pets = TryParseYesNo(normalizedAnswer);
                if (pets == null) return false;
                Profile.HasOtherPets = pets.Value;
                return true;
            case ChatState.ProfilingSize:
                Profile.SizePreference = TryParseSize(normalizedAnswer);
                return true;
            default:
                return true;
        }
    }

    /// <summary>
    /// Checks whether the session has expired
    /// </summary>
    public bool IsExpired(int timeoutMinutes)
    {
        return DateTime.UtcNow - LastActivityAt > TimeSpan.FromMinutes(timeoutMinutes);
    }

    /// <summary>
    /// Resets the profile and restarts profiling from scratch
    /// </summary>
    public void ResetProfile()
    {
        Profile = new UserProfile();
        State = ChatState.Initial;
        LastActivityAt = DateTime.UtcNow;
    }

    #region Answer parsers (TryParse - return null when not recognized)

    private static string TryParseSpecies(string answer)
    {
        if (answer.Contains("kot") || answer.Contains("kota") || answer.Contains("koty") || answer.Contains("cat"))
            return "Cat";
        return "Dog";
    }

    private static string? TryParseExperience(string answer)
    {
        if (answer.Contains("brak") || answer.Contains("nie mam") || answer.Contains("żadne") || answer.Contains("none") || answer.Contains("nie miałem") || answer.Contains("nie miałam"))
            return "None";
        if (answer.Contains("zaawansowane") || answer.Contains("duże") || answer.Contains("advanced") || answer.Contains("dużo") || answer.Contains("dużą"))
            return "Advanced";
        if (answer.Contains("podstawowe") || answer.Contains("basic") || answer.Contains("trochę") || answer.Contains("niewielkie") || answer.Contains("małe"))
            return "Basic";
        return null;
    }

    private static string? TryParseLivingConditions(string answer)
    {
        if (answer.Contains("ogród") || answer.Contains("ogrodem") || answer.Contains("garden") || answer.Contains("ogrod"))
            return "HouseWithGarden";
        if (answer.Contains("dom") || answer.Contains("house"))
            return "House";
        if (answer.Contains("mieszkanie") || answer.Contains("apartment") || answer.Contains("blok") || answer.Contains("mieszkaniu"))
            return "Apartment";
        return null;
    }

    private static string? TryParseCareTime(string answer)
    {
        if (answer.Contains("powyżej") || answer.Contains("więcej") || answer.Contains("dużo") || answer.Contains("moreThan") || answer.Contains("powyzej") || answer.Contains("wiecej") || answer.Contains("sportowy") || answer.Contains("wysoki") || answer.Contains("high") || answer.Contains("bardzo"))
            return "moreThan3h";
        if (answer.Contains("1-3") || answer.Contains("średni") || answer.Contains("umiarkow") || answer.Contains("1to3") || answer.Contains("sredni") || answer.Contains("aktywny") || answer.Contains("medium"))
            return "1to3h";
        if (answer.Contains("poniżej") || answer.Contains("mniej") || answer.Contains("mało") || answer.Contains("lessThan") || answer.Contains("ponizej") || answer.Contains("spokojny") || answer.Contains("niski") || answer.Contains("low"))
            return "lessThan1h";
        return null;
    }

    private static string? TryParseSize(string answer)
    {
        if (answer.Contains("mały") || answer.Contains("small") || answer.Contains("maly"))
            return "Small";
        if (answer.Contains("średni") || answer.Contains("medium") || answer.Contains("sredni"))
            return "Medium";
        if (answer.Contains("duży") || answer.Contains("large") || answer.Contains("big") || answer.Contains("duzy"))
            return "Large";
        if (answer.Contains("bez znaczenia") || answer.Contains("wszystko jedno") || answer.Contains("obojętne") || answer.Contains("any") || answer.Contains("dowolny") || answer.Contains("obojetne"))
            return "Any";
        return null;
    }

    private static bool? TryParseYesNo(string answer)
    {
        if (answer.Contains("tak") || answer.Contains("yes") || answer.Contains("mam"))
            return true;
        if (answer.Contains("nie") || answer.Contains("no") || answer.Contains("nie mam"))
            return false;
        return null;
    }

    #endregion
}
