using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Infrastructure.Services.Chatbot;

namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Result of calculating the match between an adoption application and an animal
/// </summary>
public record MatchScoreResult(
    double TotalScore,
    double ExperienceScore,
    double SpaceScore,
    double CareTimeScore,
    double ChildrenScore,
    double OtherAnimalsScore,
    MatchingWeights Weights
);

/// <summary>
/// Interface for the adoption matching calculation service
/// </summary>
public interface IAdoptionMatchingService
{
    MatchScoreResult? CalculateMatchScore(AdoptionApplication application, Animal animal);
}

/// <summary>
/// Service that calculates the percentage match between an applicant and an animal.
/// Uses the same algorithm as AnimalMatchingService: SCORE = Σ(wi × zi)
/// </summary>
public class AdoptionMatchingService : IAdoptionMatchingService
{
    private readonly MatchingWeights _weights;

    public AdoptionMatchingService(MatchingWeightsConfig weightsConfig)
    {
        _weights = weightsConfig.MatchingWeights;
    }

    public MatchScoreResult? CalculateMatchScore(AdoptionApplication application, Animal animal)
    {
        // Cannot calculate if structured data is missing
        if (application.HousingType is null &&
            application.HasChildren is null &&
            application.HasOtherAnimals is null &&
            application.ExperienceLevelApplicant is null &&
            application.AvailableCareTime is null)
        {
            return null;
        }

        // z1: experience match (weight 0.30)
        var z1 = CalculateExperienceMatch(animal.ExperienceLevel, application.ExperienceLevelApplicant);

        // z2: space match (weight 0.20)
        var z2 = CalculateSpaceMatch(animal.SpaceRequirement, application.HousingType);

        // z3: care time match (weight 0.20)
        var z3 = CalculateCareTimeMatch(animal.CareTime, application.AvailableCareTime);

        // z4: children compatibility (weight 0.15)
        var z4 = CalculateChildrenMatch(animal.ChildrenCompatibility, application.HasChildren);

        // z5: other animals compatibility (weight 0.15)
        var z5 = CalculateOtherAnimalsMatch(animal.AnimalCompatibility, application.HasOtherAnimals);

        var totalScore = Math.Round(
            _weights.Experience * z1 +
            _weights.Space * z2 +
            _weights.CareTime * z3 +
            _weights.Children * z4 +
            _weights.OtherAnimals * z5, 2);

        return new MatchScoreResult(
            TotalScore: totalScore,
            ExperienceScore: Math.Round(z1, 2),
            SpaceScore: Math.Round(z2, 2),
            CareTimeScore: Math.Round(z3, 2),
            ChildrenScore: Math.Round(z4, 2),
            OtherAnimalsScore: Math.Round(z5, 2),
            Weights: _weights
        );
    }

    /// <summary>
    /// z1: Experience match (applicant level vs animal requirements)
    /// If the applicant has sufficient or higher experience → 1.0
    /// </summary>
    private static double CalculateExperienceMatch(ExperienceLevel animalRequirement, string? applicantLevel)
    {
        if (string.IsNullOrEmpty(applicantLevel)) return 0.5;

        var mapped = applicantLevel switch
        {
            "none" => ExperienceLevel.None,
            "basic" => ExperienceLevel.Basic,
            "intermediate" => ExperienceLevel.Basic,
            "advanced" => ExperienceLevel.Advanced,
            _ => ExperienceLevel.None
        };

        // Applicant has sufficient or higher experience
        if ((int)mapped >= (int)animalRequirement) return 1.0;

        // Missing one experience level
        var diff = (int)animalRequirement - (int)mapped;
        return diff == 1 ? 0.5 : 0.0;
    }

    /// <summary>
    /// z2: Space match (housing conditions vs animal requirements)
    /// </summary>
    private static double CalculateSpaceMatch(SpaceRequirement animalRequirement, string? housingType)
    {
        if (string.IsNullOrEmpty(housingType)) return 0.5;

        var userSpace = housingType switch
        {
            "apartment" => SpaceRequirement.Apartment,
            "house" => SpaceRequirement.House,
            "houseWithGarden" => SpaceRequirement.HouseWithGarden,
            _ => SpaceRequirement.Apartment
        };

        if ((int)userSpace >= (int)animalRequirement) return 1.0;
        if ((int)animalRequirement - (int)userSpace == 1) return 0.5;
        return 0.0;
    }

    /// <summary>
    /// z3: Care time match (available time vs animal needs)
    /// </summary>
    private static double CalculateCareTimeMatch(CareTime animalCareTime, string? availableCareTime)
    {
        if (string.IsNullOrEmpty(availableCareTime)) return 0.5;

        var timeScore = availableCareTime switch
        {
            "lessThan1Hour" => 0,
            "oneToThreeHours" => 1,
            "moreThan3Hours" => 2,
            _ => 1
        };

        var animalScore = (int)animalCareTime;

        if (timeScore >= animalScore) return 1.0;
        if (animalScore - timeScore == 1) return 0.5;
        return 0.0;
    }

    /// <summary>
    /// z4: Children compatibility
    /// </summary>
    private static double CalculateChildrenMatch(ChildrenCompatibility compatibility, bool? hasChildren)
    {
        if (hasChildren != true) return 1.0;

        return compatibility switch
        {
            ChildrenCompatibility.Yes => 1.0,
            ChildrenCompatibility.Partially => 0.5,
            ChildrenCompatibility.No => 0.0,
            _ => 0.5
        };
    }

    /// <summary>
    /// z5: Other animals compatibility
    /// </summary>
    private static double CalculateOtherAnimalsMatch(AnimalCompatibility compatibility, bool? hasOtherAnimals)
    {
        if (hasOtherAnimals != true) return 1.0;

        return compatibility switch
        {
            AnimalCompatibility.Yes => 1.0,
            AnimalCompatibility.Partially => 0.5,
            AnimalCompatibility.No => 0.0,
            _ => 0.5
        };
    }
}
