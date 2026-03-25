namespace ShelterApp.Domain.Common;

/// <summary>
/// Serwis do generowania umów adopcyjnych w PDF
/// </summary>
public interface IContractGeneratorService
{
    /// <summary>
    /// Generuje umowę adopcyjną w formacie PDF
    /// </summary>
    /// <param name="contractData">Dane do umowy</param>
    /// <returns>Zawartość pliku PDF jako tablica bajtów</returns>
    Task<byte[]> GenerateAdoptionContractAsync(AdoptionContractData contractData);
}

/// <summary>
/// Dane wymagane do wygenerowania umowy adopcyjnej
/// </summary>
public record AdoptionContractData
{
    // Dane umowy
    public required string ContractNumber { get; init; }
    public required DateTime ContractDate { get; init; }

    // Dane schroniska
    public required ShelterInfo Shelter { get; init; }

    // Dane adoptującego
    public required AdopterInfo Adopter { get; init; }

    // Dane zwierzęcia
    public required AnimalInfo Animal { get; init; }

    // Warunki adopcji
    public required AdoptionTerms Terms { get; init; }
}

/// <summary>
/// Informacje o schronisku
/// </summary>
public record ShelterInfo
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public required string Phone { get; init; }
    public required string Email { get; init; }
    public required string Nip { get; init; }
    public required string Regon { get; init; }
}

/// <summary>
/// Informacje o adoptującym
/// </summary>
public record AdopterInfo
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public required string Phone { get; init; }
    public required string Email { get; init; }
    public required string DocumentNumber { get; init; }
    public required DateTime DateOfBirth { get; init; }
}

/// <summary>
/// Informacje o zwierzęciu
/// </summary>
public record AnimalInfo
{
    public required string RegistrationNumber { get; init; }
    public required string Name { get; init; }
    public required string Species { get; init; }
    public required string Breed { get; init; }
    public required string Gender { get; init; }
    public required string Color { get; init; }
    public required DateTime? DateOfBirth { get; init; }
    public required string? ChipNumber { get; init; }
    public required bool IsSterilized { get; init; }
    public required bool IsVaccinated { get; init; }
    public required string? HealthNotes { get; init; }
}

/// <summary>
/// Warunki adopcji
/// </summary>
public record AdoptionTerms
{
    public required bool RequiresSterilization { get; init; }
    public required DateTime? SterilizationDeadline { get; init; }
    public required bool RequiresVaccination { get; init; }
    public required DateTime? VaccinationDeadline { get; init; }
    public required bool AllowsHomeVisits { get; init; }
    public required string? SpecialConditions { get; init; }
}
