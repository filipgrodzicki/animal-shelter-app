using ShelterApp.Domain.Chatbot;

namespace ShelterApp.Api.Features.Chatbot.Shared;

/// <summary>
/// DTO sesji czatu
/// </summary>
public record ChatSessionDto(
    Guid SessionId,
    string State,
    UserProfileDto? Profile,
    List<ChatMessageDto> Messages
);

/// <summary>
/// DTO wiadomości czatu
/// </summary>
public record ChatMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime Timestamp,
    List<AnimalRecommendationDto>? Recommendations
);

/// <summary>
/// DTO profilu użytkownika
/// </summary>
public record UserProfileDto(
    string? PreferredSpecies,
    string? Experience,
    string? LivingConditions,
    string? Lifestyle,
    bool? HasChildren,
    bool? HasOtherPets,
    string? SizePreference
);

/// <summary>
/// DTO rekomendacji zwierzęcia
/// </summary>
public record AnimalRecommendationDto(
    Guid Id,
    string Name,
    string Species,
    string Breed,
    string? PhotoUrl,
    double MatchScore,
    string MatchReason
);

/// <summary>
/// Request wysłania wiadomości
/// </summary>
public record SendMessageRequest(
    string Message,
    Guid? SessionId
);

/// <summary>
/// Response wysłania wiadomości
/// </summary>
public record SendMessageResponse(
    Guid SessionId,
    ChatMessageDto AssistantMessage,
    string? NextProfilingQuestion
);

/// <summary>
/// Rozszerzenia mapujące
/// </summary>
public static class ChatMappingExtensions
{
    public static ChatSessionDto ToDto(this ChatSession session)
    {
        return new ChatSessionDto(
            SessionId: session.Id,
            State: session.State.ToString(),
            Profile: session.Profile.ToDto(),
            Messages: session.Messages.Select(m => m.ToDto()).ToList()
        );
    }

    public static ChatMessageDto ToDto(this ChatMessage message)
    {
        return new ChatMessageDto(
            Id: message.Id,
            Role: message.Role.ToString().ToLowerInvariant(),
            Content: message.Content,
            Timestamp: message.Timestamp,
            Recommendations: message.Recommendations?.Select(r => r.ToDto()).ToList()
        );
    }

    public static UserProfileDto ToDto(this UserProfile profile)
    {
        return new UserProfileDto(
            PreferredSpecies: profile.PreferredSpecies,
            Experience: profile.Experience,
            LivingConditions: profile.LivingConditions,
            Lifestyle: profile.Lifestyle,
            HasChildren: profile.HasChildren,
            HasOtherPets: profile.HasOtherPets,
            SizePreference: profile.SizePreference
        );
    }

    public static AnimalRecommendationDto ToDto(this AnimalRecommendation recommendation)
    {
        return new AnimalRecommendationDto(
            Id: recommendation.AnimalId,
            Name: recommendation.Name,
            Species: recommendation.Species,
            Breed: recommendation.Breed,
            PhotoUrl: recommendation.PhotoUrl,
            MatchScore: recommendation.MatchScore,
            MatchReason: recommendation.MatchReason
        );
    }
}
