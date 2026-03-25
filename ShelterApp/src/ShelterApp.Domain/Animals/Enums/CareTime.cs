namespace ShelterApp.Domain.Animals.Enums;

/// <summary>
/// Required daily care time for the animal
/// </summary>
public enum CareTime
{
    /// <summary>
    /// Less than one hour per day
    /// </summary>
    LessThan1Hour,

    /// <summary>
    /// 1-3 hours per day
    /// </summary>
    OneToThreeHours,

    /// <summary>
    /// More than 3 hours per day
    /// </summary>
    MoreThan3Hours
}
