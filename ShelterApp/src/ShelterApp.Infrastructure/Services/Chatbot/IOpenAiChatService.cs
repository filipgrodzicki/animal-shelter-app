using ShelterApp.Domain.Chatbot;

namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// Interface for the OpenAI service that generates chatbot responses
/// </summary>
public interface IOpenAiChatService
{
    /// <summary>
    /// Generates a response based on message history and context
    /// </summary>
    /// <param name="messages">Conversation message history</param>
    /// <param name="context">RAG context (FAQ, animal information, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated response</returns>
    Task<string> GetCompletionAsync(
        IEnumerable<ChatMessage> messages,
        string? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a response for a profiling question
    /// </summary>
    Task<string> GetProfilingResponseAsync(
        ChatState currentState,
        string userAnswer,
        CancellationToken cancellationToken = default);
}
