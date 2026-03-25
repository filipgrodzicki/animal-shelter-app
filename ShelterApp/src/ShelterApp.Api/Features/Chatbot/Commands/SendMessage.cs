using FluentValidation;
using ShelterApp.Api.Features.Chatbot.Shared;
using ShelterApp.Domain.Chatbot;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Services.Chatbot;

namespace ShelterApp.Api.Features.Chatbot.Commands;

/// <summary>
/// Command for sending a message to the chatbot
/// </summary>
public record SendMessageCommand(
    string Message,
    Guid? SessionId,
    Guid? UserId,
    string? AnonymousSessionId
) : ICommand<Result<SendMessageResponse>>;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Wiadomość nie może być pusta")
            .MaximumLength(2000).WithMessage("Wiadomość nie może przekraczać 2000 znaków");
    }
}

public class SendMessageHandler : ICommandHandler<SendMessageCommand, Result<SendMessageResponse>>
{
    private readonly IChatSessionManager _sessionManager;
    private readonly IOpenAiChatService _openAiService;
    private readonly IChatbotRagService _ragService;
    private readonly IAnimalMatchingService _matchingService;

    public SendMessageHandler(
        IChatSessionManager sessionManager,
        IOpenAiChatService openAiService,
        IChatbotRagService ragService,
        IAnimalMatchingService matchingService)
    {
        _sessionManager = sessionManager;
        _openAiService = openAiService;
        _ragService = ragService;
        _matchingService = matchingService;
    }

    public async Task<Result<SendMessageResponse>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get or create session
        ChatSession session;
        if (request.SessionId.HasValue)
        {
            var existingSession = await _sessionManager.GetSessionAsync(request.SessionId.Value, cancellationToken);
            if (existingSession == null)
            {
                session = await _sessionManager.GetOrCreateSessionAsync(
                    request.UserId, request.AnonymousSessionId, cancellationToken);
            }
            else
            {
                session = existingSession;
            }
        }
        else
        {
            session = await _sessionManager.GetOrCreateSessionAsync(
                request.UserId, request.AnonymousSessionId, cancellationToken);
        }

        // 2. Add user message
        session.AddUserMessage(request.Message);

        // 3. If in profiling mode - continue with questions
        if (IsProfilingState(session.State))
        {
            return await HandleProfilingFlowAsync(session, request.Message, cancellationToken);
        }

        // 4. Check if the question is about adoption procedure - pre-built response (bypass LLM)
        //    Must be BEFORE checking matching, because keywords overlap (e.g. "adoptować")
        var adoptionResponse = TryGetAdoptionProcedureResponse(request.Message);
        if (adoptionResponse != null)
        {
            var adoptionMessage = session.AddAssistantMessage(adoptionResponse);
            await _sessionManager.UpdateSessionAsync(session, cancellationToken);
            return Result.Success(new SendMessageResponse(
                SessionId: session.Id,
                AssistantMessage: adoptionMessage.ToDto(),
                NextProfilingQuestion: null
            ));
        }

        // 5. Check if user wants animal matching
        if (IsAskingForMatchingHelp(request.Message))
        {
            // Always start profiling from scratch
            session.ResetProfile();
            session.SetState(ChatState.ProfilingSpecies);
            var profilingStart = "Chętnie pomogę Ci znaleźć idealnego pupila! " +
                                 "Zadam Ci kilka pytań, żeby dopasować zwierzę do Twoich potrzeb.\n\n" +
                                 "Jakiego zwierzaka szukasz - psa czy kota?";
            var startMessage = session.AddAssistantMessage(profilingStart);

            await _sessionManager.UpdateSessionAsync(session, cancellationToken);

            return Result.Success(new SendMessageResponse(
                SessionId: session.Id,
                AssistantMessage: startMessage.ToDto(),
                NextProfilingQuestion: null
            ));
        }

        // 6. Normal conversation - fetch RAG context and generate response
        var context = await _ragService.GetRelevantContextAsync(request.Message, cancellationToken);
        var response = await _openAiService.GetCompletionAsync(session.Messages, context, cancellationToken);

        // 7. Add assistant response
        var assistantMessage = session.AddAssistantMessage(response);

        // 8. Save session
        await _sessionManager.UpdateSessionAsync(session, cancellationToken);

        return Result.Success(new SendMessageResponse(
            SessionId: session.Id,
            AssistantMessage: assistantMessage.ToDto(),
            NextProfilingQuestion: null
        ));
    }

    private async Task<Result<SendMessageResponse>> HandleProfilingFlowAsync(
        ChatSession session,
        string userAnswer,
        CancellationToken cancellationToken)
    {
        // Update profile based on user answer
        var recognized = session.UpdateProfileFromAnswer(userAnswer);

        if (!recognized)
        {
            // Answer not recognized - repeat question with a hint
            var hint = GetValidationHint(session.State);
            var retryMessage = session.AddAssistantMessage(hint);
            await _sessionManager.UpdateSessionAsync(session, cancellationToken);

            return Result.Success(new SendMessageResponse(
                SessionId: session.Id,
                AssistantMessage: retryMessage.ToDto(),
                NextProfilingQuestion: null
            ));
        }

        // Advance to next state
        session.AdvanceProfilingState();

        // Get next question
        var response = await _openAiService.GetProfilingResponseAsync(
            session.State, userAnswer, cancellationToken);

        // If profile is complete, add recommendations
        List<AnimalRecommendation>? recommendations = null;
        if (session.State == ChatState.ProfilingComplete && session.Profile.IsComplete)
        {
            recommendations = await _matchingService.GetMatchingAnimalsAsync(
                session.Profile, 5, cancellationToken);

            // Add recommendation info to response
            if (recommendations.Any())
            {
                response += "\n\nOto zwierzęta najlepiej dopasowane do Twoich preferencji:";
            }
        }

        var assistantMessage = session.AddAssistantMessage(response, recommendations);

        // If profiling is done, switch to normal conversation
        if (session.State == ChatState.ProfilingComplete)
        {
            session.SetState(ChatState.Conversing);
        }

        await _sessionManager.UpdateSessionAsync(session, cancellationToken);

        return Result.Success(new SendMessageResponse(
            SessionId: session.Id,
            AssistantMessage: assistantMessage.ToDto(),
            NextProfilingQuestion: IsProfilingState(session.State)
                ? await _openAiService.GetProfilingResponseAsync(session.State, "", cancellationToken)
                : null
        ));
    }

    private async Task<Result<SendMessageResponse>> HandleRecommendationsRequestAsync(
        ChatSession session,
        CancellationToken cancellationToken)
    {
        var recommendations = await _matchingService.GetMatchingAnimalsAsync(
            session.Profile, 5, cancellationToken);

        var response = recommendations.Any()
            ? "Oto zwierzęta najlepiej dopasowane do Twoich preferencji:"
            : "Niestety nie znalazłem zwierząt pasujących do Twoich preferencji. " +
              "Możesz zmienić kryteria lub odwiedzić schronisko osobiście.";

        var assistantMessage = session.AddAssistantMessage(response, recommendations);

        await _sessionManager.UpdateSessionAsync(session, cancellationToken);

        return Result.Success(new SendMessageResponse(
            SessionId: session.Id,
            AssistantMessage: assistantMessage.ToDto(),
            NextProfilingQuestion: null
        ));
    }

    private static bool IsProfilingState(ChatState state)
    {
        return state is ChatState.ProfilingSpecies
            or ChatState.ProfilingExperience
            or ChatState.ProfilingLiving
            or ChatState.ProfilingLifestyle
            or ChatState.ProfilingChildren
            or ChatState.ProfilingPets;
    }

    /// <summary>
    /// Checks if the user is asking for help choosing an animal (triggers profiling or recommendations)
    /// </summary>
    private static bool IsAskingForMatchingHelp(string message)
    {
        var normalized = StripDiacritics(message.ToLowerInvariant());
        var keywords = new[]
        {
            "szukam", "znajdz", "dopasuj", "polec",
            "rekomendacj", "pomoz", "wybrac", "wybierz",
            "chce adoptowac", "chcialbym adoptowac",
            "pokaz zwierzeta", "pokaz psy", "pokaz koty",
            "jakie zwierze", "jakie zwierzeta", "jakiego psa", "jakiego kota",
            "dobierz", "dobrac", "idealn", "pasujace",
            "chce psa", "chce kota",
            "zaadoptowac", "adoptowac",
            "pomoz znalezc", "pomoz wybrac",
            "doradzic", "doradz", "zaproponuj",
            "zwierzeta do adopcji", "dostepne zwierzeta",
            "lista zwierzat", "macie zwierzeta", "jakie macie"
        };
        return keywords.Any(k => normalized.Contains(k));
    }

    /// <summary>
    /// If the question is about the adoption procedure, returns a pre-built response (bypass LLM).
    /// </summary>
    private static string? TryGetAdoptionProcedureResponse(string message)
    {
        var normalized = StripDiacritics(message.ToLowerInvariant());
        var adoptionKeywords = new[]
        {
            "jak adoptowac", "procedura adopc", "kroki adopc", "etapy adopc",
            "jak przebiega adopcja", "jak wyglada adopcja", "co trzeba zrobic zeby adoptowac",
            "jak moge adoptowac", "chce adoptowac", "jak zaadoptowac",
            "proces adopcji", "jak wziąć", "jak wziac"
        };

        if (!adoptionKeywords.Any(k => normalized.Contains(k)))
            return null;

        return "Adopcja online:\n" +
               "1. Przeglądanie zwierząt - na naszej stronie znajdziesz profile wszystkich podopiecznych dostępnych do adopcji.\n" +
               "2. Rejestracja i logowanie - aby złożyć wniosek, musisz posiadać konto w naszym systemie.\n" +
               "3. Złożenie wniosku adopcyjnego - wypełniasz formularz z danymi osobowymi, warunkami mieszkaniowymi i motywacją do adopcji.\n" +
               "4. Rezerwacja zwierzęcia - po złożeniu wniosku wybrane zwierzę zostaje tymczasowo zarezerwowane.\n" +
               "5. Weryfikacja zgłoszenia - nasz pracownik sprawdza Twoje warunki i doświadczenie.\n" +
               "6. Umówienie wizyty - po pozytywnej weryfikacji wybierasz termin wizyty w schronisku.\n" +
               "7. Wizyta i decyzja - spotykasz się ze zwierzęciem i podejmujesz ostateczną decyzję.\n" +
               "8. Podpisanie umowy - finalizujemy adopcję i Twój nowy przyjaciel jedzie do domu!\n\n" +
               "Adopcja stacjonarna:\n" +
               "1. Wizyta w schronisku - odwiedzasz nas osobiście i poznajesz zwierzęta na miejscu.\n" +
               "2. Rozmowa z pracownikiem - przeprowadzamy wstępną rozmowę o Twoich warunkach i oczekiwaniach.\n" +
               "3. Złożenie wniosku - pracownik wprowadza Twoje dane do systemu.\n" +
               "4. Weryfikacja - oceniamy dopasowanie do wybranego zwierzęcia.\n" +
               "5. Podpisanie umowy - jeśli wszystko się zgadza, finalizujemy adopcję tego samego dnia lub umawiamy kolejną wizytę.\n\n" +
               "Jeśli masz pytania, zadzwoń pod numer +48 123 456 789 lub napisz na kontakt@schronisko.pl";
    }

    /// <summary>
    /// Returns a hint when the user's answer was not recognized
    /// </summary>
    private static string GetValidationHint(ChatState state)
    {
        return state switch
        {
            ChatState.ProfilingSpecies =>
                "Nie rozumiem Twojej odpowiedzi. Proszę, napisz czy szukasz psa czy kota.",
            ChatState.ProfilingExperience =>
                "Nie rozumiem Twojej odpowiedzi. Jakie masz doświadczenie w opiece nad zwierzętami?\nNapisz: brak, podstawowe lub zaawansowane.",
            ChatState.ProfilingLiving =>
                "Nie rozumiem Twojej odpowiedzi. Jakie masz warunki mieszkaniowe?\nNapisz: mieszkanie, dom lub dom z ogrodem.",
            ChatState.ProfilingLifestyle =>
                "Nie rozumiem Twojej odpowiedzi. Ile czasu dziennie możesz poświęcić na opiekę?\nNapisz: poniżej 1 godziny, 1-3 godziny lub powyżej 3 godzin.",
            ChatState.ProfilingChildren =>
                "Nie rozumiem Twojej odpowiedzi. Czy masz dzieci w domu? Napisz: tak lub nie.",
            ChatState.ProfilingPets =>
                "Nie rozumiem Twojej odpowiedzi. Czy masz już inne zwierzęta? Napisz: tak lub nie.",
            _ => "Nie rozumiem Twojej odpowiedzi. Spróbuj ponownie."
        };
    }

    /// <summary>
    /// Strips Polish diacritics to their ASCII equivalents
    /// </summary>
    private static string StripDiacritics(string text)
    {
        return text
            .Replace('ą', 'a').Replace('ć', 'c').Replace('ę', 'e')
            .Replace('ł', 'l').Replace('ń', 'n').Replace('ó', 'o')
            .Replace('ś', 's').Replace('ź', 'z').Replace('ż', 'z');
    }
}
