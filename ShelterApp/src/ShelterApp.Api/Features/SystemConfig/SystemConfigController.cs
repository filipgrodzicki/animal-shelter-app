using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;

namespace ShelterApp.Api.Features.SystemConfig;

/// <summary>
/// Konfiguracja systemu (wyłączne uprawnienie Administratora)
/// </summary>
/// <remarks>
/// Kontroler obsługuje konfigurację modułu AI:
/// - Prompt systemowy chatbota
/// - Parametry algorytmu dopasowania zwierząt (wagi)
/// </remarks>
[Route("api/system-config")]
[Produces("application/json")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SystemConfigController : ApiController
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SystemConfigController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SystemConfigController(
        IWebHostEnvironment environment,
        ILogger<SystemConfigController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    #region System Prompt

    /// <summary>
    /// Pobiera aktualny prompt systemowy chatbota
    /// </summary>
    /// <returns>Konfiguracja prompta systemowego</returns>
    [HttpGet("chatbot/prompt")]
    [ProducesResponseType(typeof(SystemPromptConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSystemPrompt()
    {
        try
        {
            var filePath = GetSystemPromptPath();

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Config.NotFound",
                    Detail = "Plik konfiguracji prompta systemowego nie istnieje",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<SystemPromptWrapper>(json, JsonOptions);

            return Ok(config?.SystemPrompt ?? new SystemPromptConfig());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odczytu konfiguracji prompta systemowego");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Config.ReadError",
                Detail = "Wystąpił błąd podczas odczytu konfiguracji",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Aktualizuje prompt systemowy chatbota
    /// </summary>
    /// <param name="request">Nowa konfiguracja prompta</param>
    /// <returns>Zaktualizowana konfiguracja</returns>
    [HttpPut("chatbot/prompt")]
    [ProducesResponseType(typeof(SystemPromptConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSystemPrompt([FromBody] SystemPromptConfig request)
    {
        try
        {
            // Walidacja
            if (string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Config.ValidationError",
                    Detail = "Pole 'role' jest wymagane",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var filePath = GetSystemPromptPath();

            // Utwórz backup
            await CreateBackup(filePath, "system-prompt");

            var wrapper = new SystemPromptWrapper { SystemPrompt = request };
            var json = JsonSerializer.Serialize(wrapper, JsonOptions);

            await System.IO.File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Zaktualizowano konfigurację prompta systemowego");

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji prompta systemowego");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Config.WriteError",
                Detail = "Wystąpił błąd podczas zapisu konfiguracji",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    #endregion

    #region Matching Weights

    /// <summary>
    /// Pobiera aktualne wagi algorytmu dopasowania
    /// </summary>
    /// <returns>Konfiguracja wag dopasowania</returns>
    [HttpGet("matching/weights")]
    [ProducesResponseType(typeof(MatchingWeightsConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMatchingWeights()
    {
        try
        {
            var filePath = GetMatchingWeightsPath();

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Config.NotFound",
                    Detail = "Plik konfiguracji wag dopasowania nie istnieje",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<MatchingWeightsWrapper>(json, JsonOptions);

            return Ok(config?.MatchingWeights ?? new MatchingWeightsConfig());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odczytu konfiguracji wag dopasowania");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Config.ReadError",
                Detail = "Wystąpił błąd podczas odczytu konfiguracji",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Aktualizuje wagi algorytmu dopasowania
    /// </summary>
    /// <param name="request">Nowa konfiguracja wag</param>
    /// <returns>Zaktualizowana konfiguracja</returns>
    /// <remarks>
    /// Wagi muszą sumować się do 1.0 (100%).
    ///
    /// Dostępne parametry:
    /// - experience: waga dla poziomu doświadczenia (domyślnie 0.30)
    /// - space: waga dla wymagań przestrzennych (domyślnie 0.20)
    /// - care_time: waga dla czasu opieki (domyślnie 0.20)
    /// - children: waga dla kompatybilności z dziećmi (domyślnie 0.15)
    /// - other_animals: waga dla kompatybilności z innymi zwierzętami (domyślnie 0.15)
    /// </remarks>
    [HttpPut("matching/weights")]
    [ProducesResponseType(typeof(MatchingWeightsConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMatchingWeights([FromBody] MatchingWeightsConfig request)
    {
        try
        {
            // Walidacja - wagi muszą być nieujemne
            if (request.Experience < 0 || request.Space < 0 || request.CareTime < 0 ||
                request.Children < 0 || request.OtherAnimals < 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Config.ValidationError",
                    Detail = "Wagi nie mogą być ujemne",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Walidacja - suma wag powinna wynosić 1.0 (z tolerancją)
            var sum = request.Experience + request.Space + request.CareTime +
                      request.Children + request.OtherAnimals;

            if (Math.Abs(sum - 1.0) > 0.01)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Config.ValidationError",
                    Detail = $"Suma wag musi wynosić 1.0 (100%). Aktualna suma: {sum:F2}",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var filePath = GetMatchingWeightsPath();

            // Utwórz backup
            await CreateBackup(filePath, "matching-weights");

            var wrapper = new MatchingWeightsWrapper { MatchingWeights = request };
            var json = JsonSerializer.Serialize(wrapper, JsonOptions);

            await System.IO.File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Zaktualizowano konfigurację wag dopasowania: Experience={Experience}, Space={Space}, CareTime={CareTime}, Children={Children}, OtherAnimals={OtherAnimals}",
                request.Experience, request.Space, request.CareTime, request.Children, request.OtherAnimals);

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji wag dopasowania");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Config.WriteError",
                Detail = "Wystąpił błąd podczas zapisu konfiguracji",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    #endregion

    #region Full Configuration

    /// <summary>
    /// Pobiera całą konfigurację AI (prompt + wagi)
    /// </summary>
    /// <returns>Pełna konfiguracja AI</returns>
    [HttpGet("ai")]
    [ProducesResponseType(typeof(FullAiConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFullAiConfig()
    {
        try
        {
            SystemPromptConfig? systemPrompt = null;
            MatchingWeightsConfig? matchingWeights = null;

            // Odczytaj prompt systemowy
            var promptPath = GetSystemPromptPath();
            if (System.IO.File.Exists(promptPath))
            {
                var promptJson = await System.IO.File.ReadAllTextAsync(promptPath);
                var promptWrapper = JsonSerializer.Deserialize<SystemPromptWrapper>(promptJson, JsonOptions);
                systemPrompt = promptWrapper?.SystemPrompt;
            }

            // Odczytaj wagi
            var weightsPath = GetMatchingWeightsPath();
            if (System.IO.File.Exists(weightsPath))
            {
                var weightsJson = await System.IO.File.ReadAllTextAsync(weightsPath);
                var weightsWrapper = JsonSerializer.Deserialize<MatchingWeightsWrapper>(weightsJson, JsonOptions);
                matchingWeights = weightsWrapper?.MatchingWeights;
            }

            return Ok(new FullAiConfig(
                systemPrompt ?? new SystemPromptConfig(),
                matchingWeights ?? new MatchingWeightsConfig()
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odczytu pełnej konfiguracji AI");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Config.ReadError",
                Detail = "Wystąpił błąd podczas odczytu konfiguracji",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    #endregion

    #region Helpers

    private string GetSystemPromptPath()
    {
        return Path.Combine(_environment.ContentRootPath, "system-prompt.json");
    }

    private string GetMatchingWeightsPath()
    {
        return Path.Combine(_environment.ContentRootPath, "matching-weights.json");
    }

    private Task CreateBackup(string filePath, string prefix)
    {
        // Backup disabled in Docker (read-only filesystem)
        // In production, config should be stored in database
        _logger.LogDebug("Backup skipped for {FilePath}", filePath);
        return Task.CompletedTask;
    }

    #endregion
}

#region DTOs

/// <summary>
/// Wrapper dla prompta systemowego (struktura pliku JSON)
/// </summary>
public class SystemPromptWrapper
{
    public SystemPromptConfig SystemPrompt { get; set; } = new();
}

/// <summary>
/// Konfiguracja prompta systemowego chatbota
/// </summary>
public class SystemPromptConfig
{
    /// <summary>
    /// Rola/opis asystenta AI
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Lista dozwolonych tematów rozmowy
    /// </summary>
    public List<string> AllowedTopics { get; set; } = new();

    /// <summary>
    /// Lista reguł dla asystenta
    /// </summary>
    public List<string> Rules { get; set; } = new();

    /// <summary>
    /// Wiadomość gdy asystent nie zna odpowiedzi
    /// </summary>
    public string FallbackMessage { get; set; } = string.Empty;

    /// <summary>
    /// Wiadomość gdy użytkownik pyta o niedozwolony temat
    /// </summary>
    public string OffTopicMessage { get; set; } = string.Empty;
}

/// <summary>
/// Wrapper dla wag dopasowania (struktura pliku JSON)
/// </summary>
public class MatchingWeightsWrapper
{
    public MatchingWeightsConfig MatchingWeights { get; set; } = new();
}

/// <summary>
/// Konfiguracja wag algorytmu dopasowania zwierząt
/// </summary>
public class MatchingWeightsConfig
{
    /// <summary>
    /// Waga dla poziomu doświadczenia (0.0 - 1.0)
    /// </summary>
    public double Experience { get; set; } = 0.30;

    /// <summary>
    /// Waga dla wymagań przestrzennych (0.0 - 1.0)
    /// </summary>
    public double Space { get; set; } = 0.20;

    /// <summary>
    /// Waga dla czasu opieki (0.0 - 1.0)
    /// </summary>
    public double CareTime { get; set; } = 0.20;

    /// <summary>
    /// Waga dla kompatybilności z dziećmi (0.0 - 1.0)
    /// </summary>
    public double Children { get; set; } = 0.15;

    /// <summary>
    /// Waga dla kompatybilności z innymi zwierzętami (0.0 - 1.0)
    /// </summary>
    public double OtherAnimals { get; set; } = 0.15;
}

/// <summary>
/// Pełna konfiguracja AI
/// </summary>
public record FullAiConfig(
    SystemPromptConfig SystemPrompt,
    MatchingWeightsConfig MatchingWeights
);

#endregion
