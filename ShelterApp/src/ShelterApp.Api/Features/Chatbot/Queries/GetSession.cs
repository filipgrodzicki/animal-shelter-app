using ShelterApp.Api.Features.Chatbot.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Services.Chatbot;

namespace ShelterApp.Api.Features.Chatbot.Queries;

/// <summary>
/// Query do pobrania sesji czatu
/// </summary>
public record GetSessionQuery(Guid SessionId) : IQuery<Result<ChatSessionDto>>;

public class GetSessionHandler : IQueryHandler<GetSessionQuery, Result<ChatSessionDto>>
{
    private readonly IChatSessionManager _sessionManager;

    public GetSessionHandler(IChatSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task<Result<ChatSessionDto>> Handle(
        GetSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionManager.GetSessionAsync(request.SessionId, cancellationToken);

        if (session == null)
        {
            return Result.Failure<ChatSessionDto>(
                Error.NotFound("ChatSession", request.SessionId));
        }

        return Result.Success(session.ToDto());
    }
}
