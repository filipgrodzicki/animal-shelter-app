namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// Interface for the RAG (Retrieval-Augmented Generation) chatbot service
/// </summary>
public interface IChatbotRagService
{
    /// <summary>
    /// Retrieves relevant context from the CMS based on the user's query
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Context to pass to the LLM</returns>
    Task<string> GetRelevantContextAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves information about a specific animal
    /// </summary>
    /// <param name="animalId">Animal ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Animal information as text</returns>
    Task<string?> GetAnimalInfoAsync(Guid animalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves shelter information (opening hours, contact, etc.)
    /// </summary>
    Task<string> GetShelterInfoAsync(CancellationToken cancellationToken = default);
}
