namespace ShelterApp.Domain.Adoptions;

/// <summary>
/// Person's status in the adoption system
/// </summary>
public enum AdopterStatus
{
    /// <summary>
    /// Anonymous user (not registered)
    /// </summary>
    Anonymous,

    /// <summary>
    /// Registered user
    /// </summary>
    Registered,

    /// <summary>
    /// User submitting an adoption application
    /// </summary>
    Applying,

    /// <summary>
    /// User in the verification process
    /// </summary>
    Verified,

    /// <summary>
    /// Active adopter (in the adoption process)
    /// </summary>
    Adopter
}

/// <summary>
/// Adopter status change triggers
/// </summary>
public enum AdopterStatusTrigger
{
    /// <summary>
    /// Anonymous -> Registered
    /// </summary>
    RejestracjaKonta,

    /// <summary>
    /// Registered -> Applying
    /// </summary>
    ZlozenieZgloszeniaAdopcyjnego,

    /// <summary>
    /// Applying -> Registered (cancelled by user)
    /// </summary>
    AnulowanieZgloszenia,

    /// <summary>
    /// Applying -> Registered (rejected by staff)
    /// </summary>
    OdrzucenieZgloszenia,

    /// <summary>
    /// Applying -> Verified
    /// </summary>
    ZatwierdznieZgloszenia,

    /// <summary>
    /// Verified -> Registered (negative verification)
    /// </summary>
    NegatywnaWeryfikacja,

    /// <summary>
    /// Verified -> Adopter (positive verification)
    /// </summary>
    PozytywnaWeryfikacja,

    /// <summary>
    /// Adopter -> Registered (after signing adoption contract)
    /// </summary>
    PodpisanieUmowy
}
