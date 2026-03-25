using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Chatbot.Commands;
using ShelterApp.Api.Features.Chatbot.Queries;
using ShelterApp.Api.Features.Chatbot.Shared;

namespace ShelterApp.Api.Features.Chatbot;

/// <summary>
/// Kontroler API chatbota
/// </summary>
[Route("api/chatbot")]
[AllowAnonymous]
public class ChatbotController : ApiController
{
    /// <summary>
    /// Wysyła wiadomość do chatbota
    /// </summary>
    /// <param name="request">Treść wiadomości i opcjonalnie ID sesji</param>
    /// <returns>Odpowiedź asystenta</returns>
    [HttpPost("message")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = GetUserIdOrNull();
        var anonymousId = GetAnonymousSessionId();

        var command = new SendMessageCommand(
            Message: request.Message,
            SessionId: request.SessionId,
            UserId: userId,
            AnonymousSessionId: anonymousId
        );

        var result = await Sender.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Rozpoczyna nową sesję czatu
    /// </summary>
    /// <returns>Nowa sesja z powitaniem</returns>
    [HttpPost("session")]
    [ProducesResponseType(typeof(ChatSessionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> StartSession()
    {
        var userId = GetUserIdOrNull();
        var anonymousId = GetAnonymousSessionId();

        var command = new StartSessionCommand(userId, anonymousId);
        var result = await Sender.Send(command);

        return HandleCreatedResult(
            result,
            nameof(GetSession),
            (ChatSessionDto session) => new { sessionId = session.SessionId }
        );
    }

    /// <summary>
    /// Pobiera istniejącą sesję czatu
    /// </summary>
    /// <param name="sessionId">ID sesji</param>
    /// <returns>Dane sesji</returns>
    [HttpGet("session/{sessionId:guid}")]
    [ProducesResponseType(typeof(ChatSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        var query = new GetSessionQuery(sessionId);
        var result = await Sender.Send(query);
        return HandleResult(result);
    }

    #region Helper methods

    private Guid? GetUserIdOrNull()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetAnonymousSessionId()
    {
        // Pobierz z headera lub wygeneruj nowy
        var headerValue = Request.Headers["X-Anonymous-Session-Id"].FirstOrDefault();
        return headerValue ?? Guid.NewGuid().ToString();
    }

    #endregion
}
