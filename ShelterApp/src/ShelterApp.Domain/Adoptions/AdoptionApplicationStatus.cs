namespace ShelterApp.Domain.Adoptions;

/// <summary>
/// Adoption application status
/// </summary>
public enum AdoptionApplicationStatus
{
    /// <summary>
    /// New application (before submission)
    /// </summary>
    New,

    /// <summary>
    /// Application submitted, awaiting review
    /// </summary>
    Submitted,

    /// <summary>
    /// Application being reviewed by staff
    /// </summary>
    UnderReview,

    /// <summary>
    /// Application accepted
    /// </summary>
    Accepted,

    /// <summary>
    /// Visit scheduled
    /// </summary>
    VisitScheduled,

    /// <summary>
    /// Visit completed
    /// </summary>
    VisitCompleted,

    /// <summary>
    /// Pending finalization (awaiting contract signing)
    /// </summary>
    PendingFinalization,

    /// <summary>
    /// Completed (adoption finalized)
    /// </summary>
    Completed,

    /// <summary>
    /// Rejected
    /// </summary>
    Rejected,

    /// <summary>
    /// Cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Adoption application status change triggers
/// </summary>
public enum AdoptionApplicationTrigger
{
    /// <summary>
    /// New -> Submitted (application was submitted)
    /// </summary>
    ZlozenieZgloszenia,

    /// <summary>
    /// Submitted -> UnderReview (staff takes application for review)
    /// </summary>
    PodjęciePrzezPracownika,

    /// <summary>
    /// Submitted -> Cancelled (user cancels)
    /// </summary>
    AnulowanePrzezUzytkownika,

    /// <summary>
    /// UnderReview -> Accepted
    /// </summary>
    PozytywnaWeryfikacjaDanych,

    /// <summary>
    /// UnderReview -> Rejected
    /// </summary>
    NegatywnaWeryfikacja,

    /// <summary>
    /// Accepted -> VisitScheduled
    /// </summary>
    RezerwacjaTerminuWizyty,

    /// <summary>
    /// Accepted -> Cancelled (user withdraws)
    /// </summary>
    RezygnacjaPoAkceptacji,

    /// <summary>
    /// VisitScheduled -> VisitCompleted
    /// </summary>
    StawienieSieNaWizyte,

    /// <summary>
    /// VisitScheduled -> Cancelled (no-show or withdrawal)
    /// </summary>
    NiestawienieSieNaWizyte,

    /// <summary>
    /// VisitCompleted -> PendingFinalization
    /// </summary>
    PozytywnaOcenaWizyty,

    /// <summary>
    /// VisitCompleted -> Rejected
    /// </summary>
    NegatywnaOcenaWizyty,

    /// <summary>
    /// PendingFinalization -> Completed
    /// </summary>
    PodpisanieUmowy,

    /// <summary>
    /// PendingFinalization -> Cancelled (withdrawal before signing)
    /// </summary>
    RezygnacjaPrzedPodpisaniem
}
