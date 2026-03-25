namespace ShelterApp.Domain.Chatbot;

/// <summary>
/// Animal recommendation with match score
/// </summary>
public record AnimalRecommendation(
    /// <summary>
    /// Animal ID
    /// </summary>
    Guid AnimalId,

    /// <summary>
    /// Animal name
    /// </summary>
    string Name,

    /// <summary>
    /// Species
    /// </summary>
    string Species,

    /// <summary>
    /// Breed
    /// </summary>
    string Breed,

    /// <summary>
    /// Main photo URL
    /// </summary>
    string? PhotoUrl,

    /// <summary>
    /// Match score (0.0 - 1.0)
    /// </summary>
    double MatchScore,

    /// <summary>
    /// Recommendation reasoning
    /// </summary>
    string MatchReason
);
