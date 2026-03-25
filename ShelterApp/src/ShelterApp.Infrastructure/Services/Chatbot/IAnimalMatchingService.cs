using ShelterApp.Domain.Chatbot;

namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// Interface for the animal-to-user-profile matching service
/// </summary>
public interface IAnimalMatchingService
{
    /// <summary>
    /// Gets the list of best-matched animals
    /// </summary>
    /// <param name="profile">User profile</param>
    /// <param name="maxResults">Maximum number of results (default 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recommendations sorted descending by match score</returns>
    Task<List<AnimalRecommendation>> GetMatchingAnimalsAsync(
        UserProfile profile,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
