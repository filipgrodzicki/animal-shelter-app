using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;

namespace ShelterApp.Infrastructure.Services.Chatbot;

/// <summary>
/// RAG service implementation for the chatbot
/// Searches FAQ, ContentPages, and BlogPosts in the CMS
/// </summary>
public class ChatbotRagService : IChatbotRagService
{
    private readonly ShelterDbContext _context;
    private readonly ShelterOptions _shelterOptions;

    public ChatbotRagService(
        ShelterDbContext context,
        IOptions<ShelterOptions> shelterOptions)
    {
        _context = context;
        _shelterOptions = shelterOptions.Value;
    }

    public async Task<string> GetRelevantContextAsync(string query, CancellationToken cancellationToken = default)
    {
        var contextParts = new List<string>();
        var queryLower = query.ToLowerInvariant();
        var keywords = ExtractKeywords(queryLower);

        // 1. Search FAQ
        var faqItems = await SearchFaqAsync(keywords, cancellationToken);
        if (faqItems.Any())
        {
            contextParts.Add("=== FAQ ===");
            foreach (var faq in faqItems)
            {
                contextParts.Add($"Pytanie: {faq.Question}");
                contextParts.Add($"Odpowiedź: {faq.Answer}");
                contextParts.Add("");
            }
        }

        // 2. Search ContentPages (e.g. regulations)
        var contentPages = await SearchContentPagesAsync(keywords, cancellationToken);
        if (contentPages.Any())
        {
            contextParts.Add("=== Informacje ze strony ===");
            foreach (var page in contentPages)
            {
                contextParts.Add($"Tytuł: {page.Title}");
                contextParts.Add($"Treść: {TruncateContent(page.Content, 500)}");
                contextParts.Add("");
            }
        }

        // 3. Add shelter info if the question is about contact/hours
        if (IsAboutShelterInfo(queryLower))
        {
            contextParts.Add(await GetShelterInfoAsync(cancellationToken));
        }

        // 4. If no context found, add basic information
        if (contextParts.Count == 0)
        {
            contextParts.Add(await GetShelterInfoAsync(cancellationToken));
        }

        return string.Join("\n", contextParts);
    }

    public async Task<string?> GetAnimalInfoAsync(Guid animalId, CancellationToken cancellationToken = default)
    {
        var animal = await _context.Animals
            .Include(a => a.Photos)
            .Include(a => a.MedicalRecords)
            .FirstOrDefaultAsync(a => a.Id == animalId, cancellationToken);

        if (animal == null) return null;

        var info = new List<string>
        {
            $"=== Informacje o zwierzęciu: {animal.Name} ===",
            $"Numer rejestracyjny: {animal.RegistrationNumber}",
            $"Gatunek: {TranslateSpecies(animal.Species)}",
            $"Rasa: {animal.Breed}",
            $"Wiek: {FormatAge(animal.AgeInMonths)}",
            $"Płeć: {TranslateGender(animal.Gender)}",
            $"Rozmiar: {TranslateSize(animal.Size)}",
            $"Kolor: {animal.Color}",
            $"Status: {TranslateStatus(animal.Status)}",
            $"Data przyjęcia: {animal.AdmissionDate:dd.MM.yyyy}",
            "",
            "Charakterystyka:",
            $"- Wymagane doświadczenie: {TranslateExperienceLevel(animal.ExperienceLevel)}",
            $"- Zgodność z dziećmi: {TranslateChildrenCompatibility(animal.ChildrenCompatibility)}",
            $"- Zgodność z innymi zwierzętami: {TranslateAnimalCompatibility(animal.AnimalCompatibility)}",
            $"- Wymagana przestrzeń: {TranslateSpaceRequirement(animal.SpaceRequirement)}",
            $"- Wymagany czas opieki: {TranslateCareTime(animal.CareTime)}"
        };

        if (!string.IsNullOrEmpty(animal.Description))
        {
            info.Add("");
            info.Add($"Opis: {animal.Description}");
        }

        return string.Join("\n", info);
    }

    public Task<string> GetShelterInfoAsync(CancellationToken cancellationToken = default)
    {
        var info = new List<string>
        {
            "=== Informacje o schronisku ===",
            "Nazwa schroniska: schronisko (pisz małą literą w środku zdania, nie dodawaj żadnych dopisków do nazwy)",
            $"Adres: {_shelterOptions.Address}, {_shelterOptions.PostalCode} {_shelterOptions.City}",
            $"Telefon: {_shelterOptions.Phone}",
            $"Email: {_shelterOptions.Email}",
            "",
            "Godziny otwarcia:",
            "Poniedziałek - Piątek: 10:00 - 18:00",
            "Sobota - Niedziela: 10:00 - 16:00",
            "",
            "Procedura adopcji online:",
            "1. Przeglądanie zwierząt - na stronie znajdziesz profile wszystkich podopiecznych dostępnych do adopcji.",
            "2. Rejestracja i logowanie - aby złożyć wniosek, musisz posiadać konto w systemie.",
            "3. Złożenie wniosku adopcyjnego - wypełniasz formularz z danymi osobowymi, warunkami mieszkaniowymi i motywacją do adopcji.",
            "4. Rezerwacja zwierzęcia - po złożeniu wniosku wybrane zwierzę zostaje tymczasowo zarezerwowane.",
            "5. Weryfikacja zgłoszenia - pracownik sprawdza warunki i doświadczenie.",
            "6. Umówienie wizyty - po pozytywnej weryfikacji wybierasz termin wizyty w schronisku.",
            "7. Wizyta i decyzja - spotykasz się ze zwierzęciem i podejmujesz ostateczną decyzję.",
            "8. Podpisanie umowy - finalizacja adopcji i zwierzę jedzie do domu.",
            "",
            "Procedura adopcji stacjonarnej:",
            "1. Wizyta w schronisku - odwiedzasz osobiście i poznajesz zwierzęta na miejscu.",
            "2. Rozmowa z pracownikiem - wstępna rozmowa o warunkach i oczekiwaniach.",
            "3. Złożenie wniosku - pracownik wprowadza dane do systemu.",
            "4. Weryfikacja - ocena dopasowania do wybranego zwierzęcia.",
            "5. Podpisanie umowy - jeśli wszystko się zgadza, finalizacja adopcji tego samego dnia lub umówienie kolejnej wizyty.",
            "",
            "Algorytm dopasowania zwierząt:",
            "Wynik dopasowania obliczany jest na podstawie 5 kryteriów:",
            "- Doświadczenie (30%) - czy doświadczenie użytkownika odpowiada wymaganiom zwierzęcia",
            "- Czas opieki (20%) - czy użytkownik ma wystarczająco dużo czasu na opiekę",
            "- Przestrzeń (20%) - czy warunki mieszkaniowe użytkownika są odpowiednie",
            "- Dzieci (15%) - czy zwierzę jest przyjazne dzieciom (jeśli użytkownik ma dzieci)",
            "- Inne zwierzęta (15%) - czy zwierzę toleruje inne zwierzęta (jeśli użytkownik ma inne zwierzęta)",
            "Każde kryterium przyjmuje wartość: 1.0 (pełna zgodność), 0.5 (częściowa) lub 0.0 (brak). Wynik końcowy to średnia ważona tych wartości wyrażona w procentach."
        };

        return Task.FromResult(string.Join("\n", info));
    }

    #region Helper methods

    private async Task<List<FaqItemResult>> SearchFaqAsync(
        List<string> keywords,
        CancellationToken cancellationToken)
    {
        var query = _context.FaqItems
            .Where(f => f.IsPublished);

        // Simple keyword search
        if (keywords.Any())
        {
            query = query.Where(f =>
                keywords.Any(k =>
                    f.Question.ToLower().Contains(k) ||
                    f.Answer.ToLower().Contains(k)));
        }

        return await query
            .OrderBy(f => f.DisplayOrder)
            .Take(3)
            .Select(f => new FaqItemResult(f.Question, f.Answer))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ContentPageResult>> SearchContentPagesAsync(
        List<string> keywords,
        CancellationToken cancellationToken)
    {
        var query = _context.ContentPages
            .Where(p => p.IsPublished);

        if (keywords.Any())
        {
            query = query.Where(p =>
                keywords.Any(k =>
                    p.Title.ToLower().Contains(k) ||
                    p.Content.ToLower().Contains(k)));
        }

        return await query
            .Take(2)
            .Select(p => new ContentPageResult(p.Title, p.Content))
            .ToListAsync(cancellationToken);
    }

    private static List<string> ExtractKeywords(string query)
    {
        // Keywords related to adoption and the shelter
        var relevantKeywords = new[]
        {
            "adopcja", "adoptować", "adoptowanie", "procedura", "dokumenty", "wymagania",
            "opłata", "koszt", "cena", "godziny", "otwarcie", "adres", "kontakt", "telefon",
            "pies", "kot", "zwierzę", "szczeniak", "kociak", "rasa", "wiek",
            "szczepienia", "sterylizacja", "kastracja", "chip", "umowa",
            "wizyta", "spotkanie", "rezerwacja", "wolontariat", "darowizna"
        };

        return relevantKeywords
            .Where(k => query.Contains(k))
            .ToList();
    }

    private static bool IsAboutShelterInfo(string query)
    {
        var shelterKeywords = new[]
        {
            "godziny", "otwarcie", "adres", "gdzie", "kontakt", "telefon",
            "email", "dojazd", "lokalizacja"
        };

        return shelterKeywords.Any(k => query.Contains(k));
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content[..maxLength] + "...";
    }

    private static string TranslateSpecies(Species species) => species switch
    {
        Species.Dog => "Pies",
        Species.Cat => "Kot",
        _ => "Inne"
    };

    private static string TranslateGender(Gender gender) => gender switch
    {
        Gender.Male => "Samiec",
        Gender.Female => "Samica",
        _ => "Nieznana"
    };

    private static string TranslateSize(Size size) => size switch
    {
        Size.Small => "Mały",
        Size.Medium => "Średni",
        Size.Large => "Duży",
        _ => "Nieznany"
    };

    private static string TranslateStatus(AnimalStatus status) => status switch
    {
        AnimalStatus.Available => "Dostępny do adopcji",
        AnimalStatus.Reserved => "Zarezerwowany",
        AnimalStatus.InAdoptionProcess => "W procesie adopcji",
        AnimalStatus.Adopted => "Zaadoptowany",
        _ => status.ToString()
    };

    private static string TranslateExperienceLevel(ExperienceLevel level) => level switch
    {
        ExperienceLevel.None => "Brak wymaganego doświadczenia",
        ExperienceLevel.Basic => "Podstawowe doświadczenie",
        ExperienceLevel.Advanced => "Duże doświadczenie",
        _ => "Nieznane"
    };

    private static string TranslateChildrenCompatibility(ChildrenCompatibility compat) => compat switch
    {
        ChildrenCompatibility.Yes => "Idealny dla rodzin z dziećmi",
        ChildrenCompatibility.Partially => "Toleruje starsze dzieci",
        ChildrenCompatibility.No => "Niezalecany dla rodzin z dziećmi",
        _ => "Nieznane"
    };

    private static string TranslateAnimalCompatibility(AnimalCompatibility compat) => compat switch
    {
        AnimalCompatibility.Yes => "Przyjazny innym zwierzętom",
        AnimalCompatibility.Partially => "Toleruje inne zwierzęta",
        AnimalCompatibility.No => "Nie toleruje innych zwierząt",
        _ => "Nieznane"
    };

    private static string TranslateCareTime(CareTime careTime) => careTime switch
    {
        CareTime.LessThan1Hour => "Poniżej godziny dziennie",
        CareTime.OneToThreeHours => "1-3 godziny dziennie",
        CareTime.MoreThan3Hours => "Powyżej 3 godzin dziennie",
        _ => "Nieznane"
    };

    private static string TranslateSpaceRequirement(SpaceRequirement req) => req switch
    {
        SpaceRequirement.Apartment => "Mieszkanie",
        SpaceRequirement.House => "Dom",
        SpaceRequirement.HouseWithGarden => "Dom z ogrodem",
        _ => "Nieznane"
    };

    private static string TranslateBool(bool? value) => value switch
    {
        true => "Tak",
        false => "Nie",
        _ => "Nieznane"
    };

    private static string FormatAge(int? ageInMonths)
    {
        if (!ageInMonths.HasValue) return "Nieznany";

        var years = ageInMonths.Value / 12;
        var months = ageInMonths.Value % 12;

        if (years == 0) return $"{months} mies.";
        if (months == 0) return $"{years} lat";
        return $"{years} lat {months} mies.";
    }

    #endregion

    private record FaqItemResult(string Question, string Answer);
    private record ContentPageResult(string Title, string Content);
}
