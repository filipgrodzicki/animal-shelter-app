using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Chatbot;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// Service for matching animals to a user profile
/// Implements the scoring algorithm: SCORE = Σ(wi × zi)
/// </summary>
public class AnimalMatchingService : IAnimalMatchingService
{
    private readonly ShelterDbContext _context;
    private readonly MatchingWeights _weights;
    private readonly ILogger<AnimalMatchingService> _logger;

    public AnimalMatchingService(
        ShelterDbContext context,
        MatchingWeightsConfig weightsConfig,
        ILogger<AnimalMatchingService> logger)
    {
        _context = context;
        _weights = weightsConfig.MatchingWeights;
        _logger = logger;
    }

    public async Task<List<AnimalRecommendation>> GetMatchingAnimalsAsync(
        UserProfile profile,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating animal matches for profile: Species={Species}, Experience={Experience}, Living={Living}",
            profile.PreferredSpecies, profile.Experience, profile.LivingConditions);

        // Retrieve animals with Available status only
        var animalsQuery = _context.Animals
            .Include(a => a.Photos)
            .Where(a => a.Status == AnimalStatus.Available);

        // Filter by species if specified
        if (!string.IsNullOrEmpty(profile.PreferredSpecies))
        {
            var species = Enum.TryParse<Species>(profile.PreferredSpecies, true, out var parsedSpecies)
                ? parsedSpecies
                : Species.Dog;
            animalsQuery = animalsQuery.Where(a => a.Species == species);
        }

        var animals = await animalsQuery.ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} animals matching basic criteria", animals.Count);

        // Calculate score for each animal
        var scoredAnimals = animals
            .Select(animal => new
            {
                Animal = animal,
                Score = CalculateMatchScore(animal, profile),
                Reasons = GetMatchReasons(animal, profile)
            })
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .ToList();

        return scoredAnimals.Select(x => new AnimalRecommendation(
            AnimalId: x.Animal.Id,
            Name: x.Animal.Name,
            Species: x.Animal.Species.ToString(),
            Breed: x.Animal.Breed,
            PhotoUrl: x.Animal.Photos.FirstOrDefault(p => p.IsMain)?.FilePath
                ?? x.Animal.Photos.FirstOrDefault()?.FilePath,
            MatchScore: x.Score,
            MatchReason: x.Reasons
        )).ToList();
    }

    /// <summary>
    /// Calculates the match score using the formula: SCORE = Σ(wi × zi)
    /// </summary>
    private double CalculateMatchScore(Animal animal, UserProfile profile)
    {
        double score = 0;

        // z1: experience match (user experience vs animal requirements) - weight 0.30
        var z1 = CalculateExperienceMatch(animal.ExperienceLevel, profile.Experience);
        score += _weights.Experience * z1;

        // z2: space match (housing conditions vs requirements) - weight 0.20
        var z2 = CalculateSpaceMatch(animal.SpaceRequirement, profile.LivingConditions);
        score += _weights.Space * z2;

        // z3: care time match (available time vs needs) - weight 0.20
        var z3 = CalculateCareTimeMatch(animal.CareTime, profile.AvailableTime);
        score += _weights.CareTime * z3;

        // z4: children compatibility - weight 0.15
        var z4 = CalculateChildrenMatch(animal.ChildrenCompatibility, profile.HasChildren);
        score += _weights.Children * z4;

        // z5: other animals compatibility - weight 0.15
        var z5 = CalculateOtherAnimalsMatch(animal.AnimalCompatibility, profile.HasOtherPets);
        score += _weights.OtherAnimals * z5;

        return Math.Round(score, 2);
    }

    /// <summary>
    /// z1: Experience match (user experience vs animal requirements)
    /// If the user has sufficient or higher experience → 1.0
    /// </summary>
    private static double CalculateExperienceMatch(ExperienceLevel animalRequirement, string? userExperience)
    {
        if (string.IsNullOrEmpty(userExperience)) return 0.5;

        var userLevel = userExperience switch
        {
            "None" => ExperienceLevel.None,
            "Basic" => ExperienceLevel.Basic,
            "Advanced" => ExperienceLevel.Advanced,
            _ => ExperienceLevel.None
        };

        // User has sufficient or higher experience
        if ((int)userLevel >= (int)animalRequirement) return 1.0;

        // Missing one experience level
        var diff = (int)animalRequirement - (int)userLevel;
        return diff == 1 ? 0.5 : 0.0;
    }

    /// <summary>
    /// z2: Space match (housing conditions vs animal requirements)
    /// </summary>
    private static double CalculateSpaceMatch(SpaceRequirement animalRequirement, string? userLiving)
    {
        if (string.IsNullOrEmpty(userLiving)) return 0.5;

        var userSpace = userLiving switch
        {
            "Apartment" => SpaceRequirement.Apartment,
            "House" => SpaceRequirement.House,
            "HouseWithGarden" => SpaceRequirement.HouseWithGarden,
            _ => SpaceRequirement.Apartment
        };

        // If the user has equal or more space than the animal requires
        if ((int)userSpace >= (int)animalRequirement) return 1.0;

        // If missing one level
        if ((int)animalRequirement - (int)userSpace == 1) return 0.5;

        return 0.0;
    }

    /// <summary>
    /// z3: Care time match (user's available time vs animal needs)
    /// </summary>
    private static double CalculateCareTimeMatch(CareTime animalCareTime, string? availableTime)
    {
        // If no time specified, assume average
        if (string.IsNullOrEmpty(availableTime)) return 0.5;

        // Map time to requirements
        var timeScore = availableTime switch
        {
            "lessThan1h" => 0, // below 1h
            "1to3h" => 1,      // 1-3h
            "moreThan3h" => 2, // above 3h
            _ => 1
        };

        var careTimeScore = (int)animalCareTime;

        if (timeScore >= careTimeScore) return 1.0;
        if (careTimeScore - timeScore == 1) return 0.5;
        return 0.0;
    }

    /// <summary>
    /// z4: Children compatibility
    /// </summary>
    private static double CalculateChildrenMatch(ChildrenCompatibility childrenCompatibility, bool? hasChildren)
    {
        // If user has no children, full compatibility
        if (hasChildren != true) return 1.0;

        // User has children
        return childrenCompatibility switch
        {
            ChildrenCompatibility.Yes => 1.0,       // ideal for families with children
            ChildrenCompatibility.Partially => 0.5, // tolerates older children
            ChildrenCompatibility.No => 0.0,        // not recommended for families with children
            _ => 0.5
        };
    }

    /// <summary>
    /// z5: Other animals compatibility
    /// </summary>
    private static double CalculateOtherAnimalsMatch(AnimalCompatibility animalCompatibility, bool? hasOtherPets)
    {
        // If user has no other pets, full compatibility
        if (hasOtherPets != true) return 1.0;

        // User has other pets
        return animalCompatibility switch
        {
            AnimalCompatibility.Yes => 1.0,       // friendly to other animals
            AnimalCompatibility.Partially => 0.5, // tolerates other animals
            AnimalCompatibility.No => 0.0,        // does not tolerate other animals
            _ => 0.5
        };
    }

    /// <summary>
    /// Generates recommendation reasoning
    /// </summary>
    private string GetMatchReasons(Animal animal, UserProfile profile)
    {
        var reasons = new List<string>();

        // Experience
        if (CalculateExperienceMatch(animal.ExperienceLevel, profile.Experience) >= 0.8)
        {
            reasons.Add("Twoje doświadczenie odpowiada wymaganiom tego zwierzęcia");
        }

        // Space
        if (CalculateSpaceMatch(animal.SpaceRequirement, profile.LivingConditions) >= 0.8)
        {
            reasons.Add("Odpowiedni dla Twoich warunków mieszkaniowych");
        }

        // Children
        if (profile.HasChildren == true && animal.ChildrenCompatibility == ChildrenCompatibility.Yes)
        {
            reasons.Add("Idealny dla rodzin z dziećmi");
        }
        else if (profile.HasChildren == true && animal.ChildrenCompatibility == ChildrenCompatibility.Partially)
        {
            reasons.Add("Toleruje starsze dzieci");
        }

        // Other animals
        if (profile.HasOtherPets == true && animal.AnimalCompatibility == AnimalCompatibility.Yes)
        {
            reasons.Add("Przyjazny innym zwierzętom");
        }
        else if (profile.HasOtherPets == true && animal.AnimalCompatibility == AnimalCompatibility.Partially)
        {
            reasons.Add("Toleruje inne zwierzęta");
        }

        // Care time
        var careTimeLabel = animal.CareTime switch
        {
            CareTime.LessThan1Hour => "wymaga mniej niż godziny opieki dziennie",
            CareTime.OneToThreeHours => "wymaga 1-3 godzin opieki dziennie",
            CareTime.MoreThan3Hours => "wymaga ponad 3 godzin opieki dziennie",
            _ => ""
        };
        if (!string.IsNullOrEmpty(careTimeLabel))
        {
            reasons.Add(char.ToUpper(careTimeLabel[0]) + careTimeLabel[1..]);
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Może być dobrym towarzyszem");
        }

        return string.Join(". ", reasons) + ".";
    }
}
