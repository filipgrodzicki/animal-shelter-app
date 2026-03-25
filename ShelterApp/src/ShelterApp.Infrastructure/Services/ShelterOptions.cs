namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Opcje konfiguracyjne schroniska
/// </summary>
public class ShelterOptions
{
    /// <summary>
    /// Nazwa sekcji w konfiguracji
    /// </summary>
    public const string SectionName = "Shelter";

    /// <summary>
    /// Nazwa schroniska
    /// </summary>
    public string Name { get; set; } = "Schronisko";

    /// <summary>
    /// Adres
    /// </summary>
    public string Address { get; set; } = "ul. Schroniskowa 1";

    /// <summary>
    /// Miasto
    /// </summary>
    public string City { get; set; } = "Warszawa";

    /// <summary>
    /// Kod pocztowy
    /// </summary>
    public string PostalCode { get; set; } = "00-001";

    /// <summary>
    /// Numer telefonu
    /// </summary>
    public string Phone { get; set; } = "+48 22 123 45 67";

    /// <summary>
    /// Adres email
    /// </summary>
    public string Email { get; set; } = "kontakt@schronisko.pl";

    /// <summary>
    /// Adres strony internetowej
    /// </summary>
    public string Website { get; set; } = "https://schronisko.pl";

    /// <summary>
    /// NIP
    /// </summary>
    public string Nip { get; set; } = "123-456-78-90";

    /// <summary>
    /// REGON
    /// </summary>
    public string Regon { get; set; } = "123456789";
}
