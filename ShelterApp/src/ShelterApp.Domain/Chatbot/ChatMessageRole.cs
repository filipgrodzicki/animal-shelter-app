namespace ShelterApp.Domain.Chatbot;

/// <summary>
/// Rola autora wiadomości w czacie
/// </summary>
public enum ChatMessageRole
{
    /// <summary>
    /// Wiadomość od użytkownika
    /// </summary>
    User,

    /// <summary>
    /// Wiadomość od asystenta AI
    /// </summary>
    Assistant,

    /// <summary>
    /// Wiadomość systemowa
    /// </summary>
    System
}
