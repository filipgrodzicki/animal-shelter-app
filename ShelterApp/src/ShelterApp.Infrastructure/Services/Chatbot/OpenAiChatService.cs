using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Chatbot;

namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// LLM service implementation for generating chatbot responses
/// Uses Groq API (OpenAI-compatible)
/// </summary>
public class OpenAiChatService : IOpenAiChatService
{
    private readonly HttpClient _httpClient;
    private readonly ChatbotSettings _settings;
    private readonly ISystemPromptProvider _systemPromptProvider;
    private readonly ILogger<OpenAiChatService> _logger;

    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    public OpenAiChatService(
        HttpClient httpClient,
        IOptions<ChatbotSettings> settings,
        ISystemPromptProvider systemPromptProvider,
        ILogger<OpenAiChatService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _systemPromptProvider = systemPromptProvider;
        _logger = logger;

        // Configure HttpClient
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.OpenAiApiKey}");
    }

    public async Task<string> GetCompletionAsync(
        IEnumerable<ChatMessage> messages,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var openAiMessages = new List<OpenAiMessage>();

            // Add system prompt with context
            var systemContent = BuildSystemPrompt(context);
            openAiMessages.Add(new OpenAiMessage("system", systemContent));

            // Add message history
            foreach (var message in messages)
            {
                var role = message.Role switch
                {
                    ChatMessageRole.User => "user",
                    ChatMessageRole.Assistant => "assistant",
                    _ => "system"
                };
                openAiMessages.Add(new OpenAiMessage(role, message.Content));
            }

            var request = new OpenAiRequest
            {
                Model = _settings.Model,
                Messages = openAiMessages,
                MaxTokens = _settings.MaxTokens,
                Temperature = _settings.Temperature
            };

            _logger.LogInformation("Sending request to Groq API with {MessageCount} messages", openAiMessages.Count);

            var response = await _httpClient.PostAsJsonAsync(GroqApiUrl, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Groq API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return _systemPromptProvider.GetSystemPrompt().FallbackMessage;
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>(cancellationToken: cancellationToken);

            var assistantMessage = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrEmpty(assistantMessage))
            {
                _logger.LogWarning("Empty response from Groq");
                return _systemPromptProvider.GetSystemPrompt().FallbackMessage;
            }

            _logger.LogInformation("Received response from Groq: {Length} characters", assistantMessage.Length);
            return assistantMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Groq API");
            return _systemPromptProvider.GetSystemPrompt().FallbackMessage;
        }
    }

    public Task<string> GetProfilingResponseAsync(
        ChatState currentState,
        string userAnswer,
        CancellationToken cancellationToken = default)
    {
        // For simpler profiling questions we don't need OpenAI
        // Return predefined questions
        // Mapping: state → question FOR THAT state
        // After AdvanceProfilingState() the state is already new, so return the question for the new state
        var response = currentState switch
        {
            ChatState.Initial => "Dzień dobry! Jestem asystentem schroniska i pomogę Państwu znaleźć odpowiednie zwierzę. " +
                                 "Zadam kilka pytań, aby dopasować zwierzę do Państwa potrzeb.\n\n" +
                                 "Jakiego zwierzęcia Państwo szukają - psa czy kota?",

            ChatState.ProfilingSpecies => "Jakiego zwierzęcia Państwo szukają - psa czy kota?",

            ChatState.ProfilingExperience => "Jakie mają Państwo doświadczenie w opiece nad zwierzętami?\n" +
                                              "(brak / podstawowe / zaawansowane)",

            ChatState.ProfilingLiving => "Jakie mają Państwo warunki mieszkaniowe?\n" +
                                          "(mieszkanie / dom / dom z ogrodem)",

            ChatState.ProfilingLifestyle => "Ile czasu dziennie mogą Państwo poświęcić na opiekę nad zwierzęciem?\n" +
                                             "(poniżej 1 godziny / 1-3 godziny / powyżej 3 godzin)",

            ChatState.ProfilingChildren => "Czy mają Państwo dzieci w domu? (tak / nie)",

            ChatState.ProfilingPets => "Ostatnie pytanie - czy mają Państwo już inne zwierzęta w domu? (tak / nie)",

            ChatState.ProfilingComplete => "Dziękuję za odpowiedzi! Na podstawie Państwa preferencji przygotowuję listę " +
                                            "najlepiej dopasowanych zwierząt...",

            _ => "W czym mogę Państwu pomóc? Mogę odpowiedzieć na pytania o adopcję, pokazać dostępne zwierzęta " +
                 "lub udzielić informacji o schronisku."
        };

        return Task.FromResult(response);
    }

    private string BuildSystemPrompt(string? context)
    {
        // Pobierz aktualną konfigurację z pliku (dynamicznie)
        var systemPrompt = _systemPromptProvider.GetSystemPrompt();

        var sb = new StringBuilder();

        // Force Polish language (Llama 3.3 switches to Russian without this)
        sb.AppendLine("JĘZYK: Odpowiadaj WYŁĄCZNIE po polsku.");
        sb.AppendLine();
        sb.AppendLine(systemPrompt.Role);
        sb.AppendLine();

        // Allowed topics from JSON configuration
        sb.AppendLine("Dozwolone tematy: " + string.Join(", ", systemPrompt.AllowedTopics) + ".");
        sb.AppendLine();

        // Rules from JSON configuration
        sb.AppendLine("Zasady:");
        foreach (var rule in systemPrompt.Rules)
        {
            sb.AppendLine($"- {rule}");
        }
        sb.AppendLine();

        // RAG context
        if (!string.IsNullOrEmpty(context))
        {
            sb.AppendLine("=== KONTEKST ===");
            sb.AppendLine(context);
            sb.AppendLine("=== KONIEC KONTEKSTU ===");
            sb.AppendLine();
        }

        // Off-topic vs fallback logic
        sb.AppendLine("REAKCJA NA PYTANIA:");
        sb.AppendLine($"- Pytanie POZA tematem schroniska (np. pogoda, polityka, sport): odpowiedz \"{systemPrompt.OffTopicMessage}\"");
        sb.AppendLine($"- Pytanie o schronisko/adopcję ale BRAK odpowiedzi w kontekście: odpowiedz \"{systemPrompt.FallbackMessage}\"");

        return sb.ToString();
    }

    #region OpenAI DTOs

    private class OpenAiRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAiMessage> Messages { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private class OpenAiMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        public OpenAiMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    private class OpenAiResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiResponseMessage? Message { get; set; }
    }

    private class OpenAiResponseMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    #endregion
}
