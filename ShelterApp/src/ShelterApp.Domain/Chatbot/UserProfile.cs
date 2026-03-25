namespace ShelterApp.Domain.Chatbot;

/// <summary>
/// User preference profile collected during the profiling dialog
/// </summary>
public class UserProfile
{
    /// <summary>
    /// Preferred species (Dog, Cat)
    /// </summary>
    public string? PreferredSpecies { get; set; }

    /// <summary>
    /// Pet care experience (None, Basic, Advanced)
    /// </summary>
    public string? Experience { get; set; }

    /// <summary>
    /// Living conditions (Apartment, House, HouseWithGarden)
    /// </summary>
    public string? LivingConditions { get; set; }

    /// <summary>
    /// Lifestyle / activity level (Low, Medium, High)
    /// </summary>
    public string? Lifestyle { get; set; }

    /// <summary>
    /// Whether the user has children
    /// </summary>
    public bool? HasChildren { get; set; }

    /// <summary>
    /// Whether the user has other pets
    /// </summary>
    public bool? HasOtherPets { get; set; }

    /// <summary>
    /// Preferred animal size (Small, Medium, Large, null = no preference)
    /// </summary>
    public string? SizePreference { get; set; }

    /// <summary>
    /// Available daily care time (less than 1h, 1-3h, more than 3h)
    /// </summary>
    public string? AvailableTime { get; set; }

    /// <summary>
    /// Checks whether the profile is complete (required fields filled)
    /// </summary>
    public bool IsComplete =>
        !string.IsNullOrEmpty(PreferredSpecies) &&
        !string.IsNullOrEmpty(Experience) &&
        !string.IsNullOrEmpty(LivingConditions) &&
        !string.IsNullOrEmpty(Lifestyle) &&
        HasChildren.HasValue &&
        HasOtherPets.HasValue;

    /// <summary>
    /// Creates a copy of the profile
    /// </summary>
    public UserProfile Clone()
    {
        return new UserProfile
        {
            PreferredSpecies = PreferredSpecies,
            Experience = Experience,
            LivingConditions = LivingConditions,
            Lifestyle = Lifestyle,
            HasChildren = HasChildren,
            HasOtherPets = HasOtherPets,
            SizePreference = SizePreference,
            AvailableTime = AvailableTime
        };
    }
}
