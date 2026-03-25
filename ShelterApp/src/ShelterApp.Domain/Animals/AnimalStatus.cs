namespace ShelterApp.Domain.Animals;

public enum AnimalStatus
{
    Admitted,           // Admitted
    Quarantine,         // Quarantine
    Treatment,          // Treatment
    Available,          // Available for adoption
    Reserved,           // Reserved
    InAdoptionProcess,  // In adoption process
    Adopted,            // Adopted
    Deceased            // Deceased
}

public enum AnimalStatusTrigger
{
    SkierowanieNaKwarantanne,       // Admitted -> Quarantine
    DopuszczenieDoAdopcji,          // Admitted -> Available, Quarantine -> Available, Treatment -> Available
    WykrycieChoroby,                // Quarantine -> Treatment
    ZakonczenieKwarantanny,         // Quarantine -> Available
    Wyleczenie,                     // Treatment -> Available
    Zachorowanie,                   // Available -> Treatment, InAdoptionProcess -> Treatment
    ZlozenieZgloszeniaAdopcyjnego,  // Available -> Reserved
    AnulowanieZgloszenia,           // Reserved -> Available
    Rezygnacja,                     // Reserved -> Available
    ZatwierdznieZgloszenia,         // Reserved -> InAdoptionProcess
    NegatywnaOcena,                 // InAdoptionProcess -> Available
    PodpisanieUmowy,                // InAdoptionProcess -> Adopted
    Zgon                            // Any (except Adopted) -> Deceased
}
