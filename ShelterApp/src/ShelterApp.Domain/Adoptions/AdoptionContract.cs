using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Adoptions;

/// <summary>
/// Adoption contract (separate entity per ERD)
/// </summary>
public class AdoptionContract : Entity<Guid>
{
    /// <summary>
    /// Adoption application identifier
    /// </summary>
    public Guid AdoptionApplicationId { get; private set; }

    /// <summary>
    /// Adoption contract number
    /// </summary>
    public string ContractNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Contract generation date
    /// </summary>
    public DateTime GeneratedDate { get; private set; }

    /// <summary>
    /// Contract signing date
    /// </summary>
    public DateTime? SignedDate { get; private set; }

    /// <summary>
    /// Contract file path (PDF)
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Shelter signatory's full name
    /// </summary>
    public string? ShelterSignatory { get; private set; }

    /// <summary>
    /// Adopter signatory's full name
    /// </summary>
    public string? AdopterSignatory { get; private set; }

    /// <summary>
    /// Adoption fee amount
    /// </summary>
    public decimal? AdoptionFee { get; private set; }

    /// <summary>
    /// Whether the fee has been paid
    /// </summary>
    public bool FeePaid { get; private set; }

    /// <summary>
    /// Fee payment date
    /// </summary>
    public DateTime? FeePaidDate { get; private set; }

    /// <summary>
    /// Additional contract terms
    /// </summary>
    public string? AdditionalTerms { get; private set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Whether the contract is signed
    /// </summary>
    public bool IsSigned => SignedDate.HasValue;

    private AdoptionContract() { }

    /// <summary>
    /// Creates a new adoption contract
    /// </summary>
    public static AdoptionContract Create(
        Guid adoptionApplicationId,
        string contractNumber,
        decimal? adoptionFee = null,
        string? additionalTerms = null,
        string? notes = null)
    {
        return new AdoptionContract
        {
            Id = Guid.NewGuid(),
            AdoptionApplicationId = adoptionApplicationId,
            ContractNumber = contractNumber,
            GeneratedDate = DateTime.UtcNow,
            AdoptionFee = adoptionFee,
            AdditionalTerms = additionalTerms,
            Notes = notes,
            FeePaid = false
        };
    }

    /// <summary>
    /// Sets the generated PDF file path
    /// </summary>
    public void SetFilePath(string filePath)
    {
        FilePath = filePath;
        SetUpdatedAt();
    }

    /// <summary>
    /// Signs the contract
    /// </summary>
    public Result Sign(string shelterSignatory, string adopterSignatory)
    {
        if (IsSigned)
        {
            return Result.Failure(Error.Validation("Umowa została już podpisana"));
        }

        ShelterSignatory = shelterSignatory;
        AdopterSignatory = adopterSignatory;
        SignedDate = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Records the adoption fee payment
    /// </summary>
    public Result RecordFeePayment()
    {
        if (!AdoptionFee.HasValue || AdoptionFee.Value <= 0)
        {
            return Result.Failure(Error.Validation("Brak określonej opłaty adopcyjnej"));
        }

        if (FeePaid)
        {
            return Result.Failure(Error.Validation("Opłata została już uiszczona"));
        }

        FeePaid = true;
        FeePaidDate = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>
    /// Updates additional contract terms
    /// </summary>
    public void UpdateTerms(string? additionalTerms, string? notes = null)
    {
        AdditionalTerms = additionalTerms;
        if (notes is not null) Notes = notes;
        SetUpdatedAt();
    }
}
