using ShelterApp.Domain.Chatbot;

namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// Interface for chat session management
/// </summary>
public interface IChatSessionManager
{
    /// <summary>
    /// Gets or creates a chat session
    /// </summary>
    Task<ChatSession> GetOrCreateSessionAsync(
        Guid? userId,
        string? anonymousSessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session by ID
    /// </summary>
    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a session
    /// </summary>
    Task UpdateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session
    /// </summary>
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
