using ShelterApp.Api.Features.Chatbot.Shared;
using ShelterApp.Domain.Chatbot;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Services.Chatbot;

namespace ShelterApp.Api.Features.Chatbot.Commands;

/// <summary>
/// Komenda rozpoczęcia nowej sesji czatu
/// </summary>
public record StartSessionCommand(
    Guid? UserId,
    string? AnonymousSessionId
) : ICommand<Result<ChatSessionDto>>;

public class StartSessionHandler : ICommandHandler<StartSessionCommand, Result<ChatSessionDto>>
{
    private readonly IChatSessionManager _sessionManager;
    private readonly IOpenAiChatService _openAiService;

    public StartSessionHandler(
        IChatSessionManager sessionManager,
        IOpenAiChatService openAiService)
    {
        _sessionManager = sessionManager;
        _openAiService = openAiService;
    }

    public async Task<Result<ChatSessionDto>> Handle(
        StartSessionCommand request,
        CancellationToken cancellationToken)
    {
        // Utwórz nową sesję
        var session = await _sessionManager.GetOrCreateSessionAsync(
            request.UserId, request.AnonymousSessionId, cancellationToken);

        // Powitanie - normalna konwersacja, bez wymuszania profilowania
        var greeting = "Dzień dobry! Jestem asystentem schroniska. Mogę odpowiedzieć na pytania o adopcję, " +
                       "schronisko, dostępne zwierzęta lub pomóc Państwu znaleźć odpowiednie zwierzę.\n\n" +
                       "W czym mogę Państwu pomóc?";

        session.AddAssistantMessage(greeting);
        session.SetState(ChatState.Conversing);

        await _sessionManager.UpdateSessionAsync(session, cancellationToken);

        return Result.Success(session.ToDto());
    }
}
