using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Chatbot;

namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// In-memory implementation of chat session management
/// </summary>
public class InMemoryChatSessionManager : IChatSessionManager
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _sessions = new();
    private readonly ConcurrentDictionary<string, Guid> _anonymousSessionIndex = new();
    private readonly ConcurrentDictionary<Guid, Guid> _userSessionIndex = new();
    private readonly ChatbotSettings _settings;

    public InMemoryChatSessionManager(IOptions<ChatbotSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<ChatSession> GetOrCreateSessionAsync(
        Guid? userId,
        string? anonymousSessionId,
        CancellationToken cancellationToken = default)
    {
        // Check if the user already has a session
        if (userId.HasValue && _userSessionIndex.TryGetValue(userId.Value, out var existingSessionId))
        {
            if (_sessions.TryGetValue(existingSessionId, out var existingSession) &&
                !existingSession.IsExpired(_settings.SessionTimeoutMinutes))
            {
                return Task.FromResult(existingSession);
            }
            // Session expired - remove
            _sessions.TryRemove(existingSessionId, out _);
            _userSessionIndex.TryRemove(userId.Value, out _);
        }

        // Check if the anonymous user already has a session
        if (!string.IsNullOrEmpty(anonymousSessionId) &&
            _anonymousSessionIndex.TryGetValue(anonymousSessionId, out existingSessionId))
        {
            if (_sessions.TryGetValue(existingSessionId, out var existingSession) &&
                !existingSession.IsExpired(_settings.SessionTimeoutMinutes))
            {
                return Task.FromResult(existingSession);
            }
            // Session expired - remove
            _sessions.TryRemove(existingSessionId, out _);
            _anonymousSessionIndex.TryRemove(anonymousSessionId, out _);
        }

        // Create a new session
        var session = ChatSession.Create(userId, anonymousSessionId);
        _sessions[session.Id] = session;

        if (userId.HasValue)
        {
            _userSessionIndex[userId.Value] = session.Id;
        }
        else if (!string.IsNullOrEmpty(anonymousSessionId))
        {
            _anonymousSessionIndex[anonymousSessionId] = session.Id;
        }

        return Task.FromResult(session);
    }

    public Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            if (session.IsExpired(_settings.SessionTimeoutMinutes))
            {
                // Session expired - remove
                _sessions.TryRemove(sessionId, out _);
                return Task.FromResult<ChatSession?>(null);
            }
            return Task.FromResult<ChatSession?>(session);
        }

        return Task.FromResult<ChatSession?>(null);
    }

    public Task UpdateSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            if (session.UserId.HasValue)
            {
                _userSessionIndex.TryRemove(session.UserId.Value, out _);
            }
            if (!string.IsNullOrEmpty(session.AnonymousSessionId))
            {
                _anonymousSessionIndex.TryRemove(session.AnonymousSessionId, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredSessionIds = _sessions
            .Where(kvp => kvp.Value.IsExpired(_settings.SessionTimeoutMinutes))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in expiredSessionIds)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                if (session.UserId.HasValue)
                {
                    _userSessionIndex.TryRemove(session.UserId.Value, out _);
                }
                if (!string.IsNullOrEmpty(session.AnonymousSessionId))
                {
                    _anonymousSessionIndex.TryRemove(session.AnonymousSessionId, out _);
                }
            }
        }

        return Task.CompletedTask;
    }
}
