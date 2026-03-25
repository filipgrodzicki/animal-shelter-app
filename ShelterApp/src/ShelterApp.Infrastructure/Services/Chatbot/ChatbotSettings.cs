namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// Ustawienia chatbota z appsettings.json
/// </summary>
public class ChatbotSettings
{
    public const string SectionName = "Chatbot";

    /// <summary>
    /// Klucz API OpenAI
    /// </summary>
    public string OpenAiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model OpenAI do użycia (np. gpt-4-turbo-preview, gpt-3.5-turbo)
    /// </summary>
    public string Model { get; set; } = "gpt-4-turbo-preview";

    /// <summary>
    /// Maksymalna liczba tokenów w odpowiedzi
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// Temperatura (kreatywność) odpowiedzi (0.0 - 2.0)
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Czas wygaśnięcia sesji w minutach
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;
}

/// <summary>
/// Konfiguracja promptu systemowego z pliku JSON
/// </summary>
public class SystemPromptConfig
{
    public SystemPromptContent SystemPrompt { get; set; } = new();
}

public class SystemPromptContent
{
    public string Role { get; set; } = string.Empty;
    public List<string> AllowedTopics { get; set; } = new();
    public List<string> Rules { get; set; } = new();
    public string FallbackMessage { get; set; } = string.Empty;
    public string OffTopicMessage { get; set; } = string.Empty;
}

/// <summary>
/// Konfiguracja wag dopasowania z pliku JSON
/// </summary>
public class MatchingWeightsConfig
{
    public MatchingWeights MatchingWeights { get; set; } = new();
}

public class MatchingWeights
{
    public double Experience { get; set; } = 0.30;
    public double Space { get; set; } = 0.20;
    public double CareTime { get; set; } = 0.20;
    public double Children { get; set; } = 0.15;
    public double OtherAnimals { get; set; } = 0.15;
}

/// <summary>
/// Provider dla dynamicznego odczytu konfiguracji promptu systemowego
/// </summary>
public interface ISystemPromptProvider
{
    SystemPromptContent GetSystemPrompt();
}

/// <summary>
/// Implementacja providera - czyta z pliku przy każdym żądaniu
/// </summary>
public class FileSystemPromptProvider : ISystemPromptProvider
{
    private readonly string _filePath;

    public FileSystemPromptProvider(string filePath)
    {
        _filePath = filePath;
    }

    public SystemPromptContent GetSystemPrompt()
    {
        try
        {
            if (System.IO.File.Exists(_filePath))
            {
                var json = System.IO.File.ReadAllText(_filePath);
                var config = System.Text.Json.JsonSerializer.Deserialize<SystemPromptConfig>(
                    json,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                    }
                );
                return config?.SystemPrompt ?? new SystemPromptContent();
            }
        }
        catch
        {
            // W razie błędu zwróć domyślną konfigurację
        }
        return new SystemPromptContent();
    }
}
