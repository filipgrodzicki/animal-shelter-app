namespace ShelterApp.Domain.Chatbot;

/// <summary>
/// Pojedyncza wiadomość w konwersacji czatbota
/// </summary>
public class ChatMessage
{
    public Guid Id { get; private set; }
    public ChatMessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public List<AnimalRecommendation>? Recommendations { get; private set; }

    private ChatMessage() { }

    /// <summary>
    /// Tworzy nową wiadomość
    /// </summary>
    public static ChatMessage Create(
        ChatMessageRole role,
        string content,
        List<AnimalRecommendation>? recommendations = null)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            Recommendations = recommendations
        };
    }

    /// <summary>
    /// Tworzy wiadomość użytkownika
    /// </summary>
    public static ChatMessage FromUser(string content)
    {
        return Create(ChatMessageRole.User, content);
    }

    /// <summary>
    /// Tworzy wiadomość asystenta
    /// </summary>
    public static ChatMessage FromAssistant(string content, List<AnimalRecommendation>? recommendations = null)
    {
        return Create(ChatMessageRole.Assistant, content, recommendations);
    }

    /// <summary>
    /// Tworzy wiadomość systemową
    /// </summary>
    public static ChatMessage FromSystem(string content)
    {
        return Create(ChatMessageRole.System, content);
    }
}
