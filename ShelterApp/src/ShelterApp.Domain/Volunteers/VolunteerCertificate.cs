using ShelterApp.Domain.Common;

namespace ShelterApp.Domain.Volunteers;

/// <summary>
/// Volunteer certificate type
/// </summary>
public enum CertificateType
{
    /// <summary>Basic training</summary>
    BasicTraining,

    /// <summary>Animal first aid</summary>
    AnimalFirstAid,

    /// <summary>Aggressive dog handling</summary>
    AggressiveDogHandling,

    /// <summary>Exotic animal handling</summary>
    ExoticAnimalHandling,

    /// <summary>Behavioral training</summary>
    BehavioralTraining,

    /// <summary>Other</summary>
    Other
}

/// <summary>
/// Volunteer certificate/credential (per ERD)
/// </summary>
public class VolunteerCertificate : Entity<Guid>
{
    /// <summary>
    /// Volunteer identifier
    /// </summary>
    public Guid VolunteerId { get; private set; }

    /// <summary>
    /// Certificate type
    /// </summary>
    public CertificateType Type { get; private set; }

    /// <summary>
    /// Certificate name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Certificate number
    /// </summary>
    public string? CertificateNumber { get; private set; }

    /// <summary>
    /// Issue date
    /// </summary>
    public DateTime IssueDate { get; private set; }

    /// <summary>
    /// Expiry date (if applicable)
    /// </summary>
    public DateTime? ExpiryDate { get; private set; }

    /// <summary>
    /// Issuing organization
    /// </summary>
    public string IssuingOrganization { get; private set; } = string.Empty;

    /// <summary>
    /// Certificate file path (scan)
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Whether the certificate is active (not expired)
    /// </summary>
    public bool IsActive => !ExpiryDate.HasValue || ExpiryDate.Value > DateTime.UtcNow;

    private VolunteerCertificate() { }

    /// <summary>
    /// Creates a new volunteer certificate
    /// </summary>
    public static VolunteerCertificate Create(
        Guid volunteerId,
        CertificateType type,
        string name,
        string issuingOrganization,
        DateTime issueDate,
        string? certificateNumber = null,
        DateTime? expiryDate = null,
        string? filePath = null,
        string? notes = null)
    {
        return new VolunteerCertificate
        {
            Id = Guid.NewGuid(),
            VolunteerId = volunteerId,
            Type = type,
            Name = name,
            CertificateNumber = certificateNumber,
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            IssuingOrganization = issuingOrganization,
            FilePath = filePath,
            Notes = notes
        };
    }

    /// <summary>
    /// Updates certificate data
    /// </summary>
    public void Update(
        string? name = null,
        string? certificateNumber = null,
        DateTime? expiryDate = null,
        string? issuingOrganization = null,
        string? filePath = null,
        string? notes = null)
    {
        if (name is not null) Name = name;
        if (certificateNumber is not null) CertificateNumber = certificateNumber;
        if (expiryDate.HasValue) ExpiryDate = expiryDate;
        if (issuingOrganization is not null) IssuingOrganization = issuingOrganization;
        if (filePath is not null) FilePath = filePath;
        if (notes is not null) Notes = notes;
        SetUpdatedAt();
    }
}
