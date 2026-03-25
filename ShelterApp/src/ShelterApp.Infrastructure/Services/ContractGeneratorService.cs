using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShelterApp.Domain.Common;

namespace ShelterApp.Infrastructure.Services;

/// <summary>
/// Service for generating adoption contracts as PDF using QuestPDF
/// </summary>
public class ContractGeneratorService : IContractGeneratorService
{
    public Task<byte[]> GenerateAdoptionContractAsync(AdoptionContractData contractData)
    {
        // Set QuestPDF license (Community for open-source projects)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = new AdoptionContractDocument(contractData);
        var pdfBytes = document.GeneratePdf();

        return Task.FromResult(pdfBytes);
    }
}

/// <summary>
/// Adoption contract document
/// </summary>
internal class AdoptionContractDocument : IDocument
{
    private readonly AdoptionContractData _data;

    // Colors
    private static readonly string PrimaryColor = "#2E7D32"; // Green
    private static readonly string SecondaryColor = "#666666"; // Gray
    private static readonly string BorderColor = "#CCCCCC";

    public AdoptionContractDocument(AdoptionContractData data)
    {
        _data = data;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("DejaVu Sans"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            // Logo and shelter name
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(_data.Shelter.Name)
                        .FontSize(16)
                        .Bold()
                        .FontColor(PrimaryColor);

                    col.Item().Text($"{_data.Shelter.Address}")
                        .FontSize(9)
                        .FontColor(SecondaryColor);

                    col.Item().Text($"{_data.Shelter.PostalCode} {_data.Shelter.City}")
                        .FontSize(9)
                        .FontColor(SecondaryColor);

                    col.Item().Text($"Tel: {_data.Shelter.Phone} | Email: {_data.Shelter.Email}")
                        .FontSize(9)
                        .FontColor(SecondaryColor);
                });

                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text($"Umowa nr:")
                        .FontSize(9)
                        .FontColor(SecondaryColor);

                    col.Item().Text(_data.ContractNumber)
                        .FontSize(12)
                        .Bold();

                    col.Item().Text($"Data: {_data.ContractDate:dd.MM.yyyy}")
                        .FontSize(9);
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(BorderColor);

            // Document title
            column.Item().AlignCenter().PaddingVertical(10).Text("UMOWA ADOPCJI ZWIERZĘCIA")
                .FontSize(14)
                .Bold();
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(15);

            // Section 1: Contract parties
            column.Item().Element(c => ComposePartiesSection(c));

            // Section 2: Subject of the contract (animal data)
            column.Item().Element(c => ComposeAnimalSection(c));

            // Section 3: Adoption terms
            column.Item().Element(c => ComposeTermsSection(c));

            // Section 4: Adopter obligations
            column.Item().Element(c => ComposeObligationsSection(c));

            // Section 5: GDPR clause
            column.Item().Element(c => ComposeRodoSection(c));

            // Section 6: Final provisions
            column.Item().Element(c => ComposeFinalProvisionsSection(c));

            // Section 7: Signatures
            column.Item().Element(c => ComposeSignaturesSection(c));
        });
    }

    private void ComposePartiesSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("§ 1. STRONY UMOWY")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Text(text =>
            {
                text.Span("Umowa zawarta pomiędzy:").Bold();
            });

            // Shelter data
            column.Item().PaddingTop(5).Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
            {
                col.Item().Text("PRZEKAZUJĄCY (Schronisko):").Bold().FontSize(9);
                col.Item().PaddingTop(3).Text(_data.Shelter.Name).Bold();
                col.Item().Text($"Adres: {_data.Shelter.Address}, {_data.Shelter.PostalCode} {_data.Shelter.City}");
                col.Item().Text($"NIP: {_data.Shelter.Nip} | REGON: {_data.Shelter.Regon}");
                col.Item().Text($"Telefon: {_data.Shelter.Phone} | Email: {_data.Shelter.Email}");
            });

            column.Item().PaddingTop(5).Text("a");

            // Adopter data
            column.Item().PaddingTop(5).Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
            {
                col.Item().Text("PRZYJMUJĄCY (Adoptujący):").Bold().FontSize(9);
                col.Item().PaddingTop(3).Text(_data.Adopter.FullName).Bold();
                col.Item().Text($"Adres: {_data.Adopter.Address}, {_data.Adopter.PostalCode} {_data.Adopter.City}");
                col.Item().Text($"Data urodzenia: {_data.Adopter.DateOfBirth:dd.MM.yyyy}");
                col.Item().Text($"Nr dokumentu tożsamości: {_data.Adopter.DocumentNumber}");
                col.Item().Text($"Telefon: {_data.Adopter.Phone} | Email: {_data.Adopter.Email}");
            });
        });
    }

    private void ComposeAnimalSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("§ 2. PRZEDMIOT UMOWY")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Text(
                "Przedmiotem niniejszej umowy jest przekazanie do adopcji zwierzęcia o następujących danych:");

            column.Item().PaddingTop(5).Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Nr ewidencyjny:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.RegistrationNumber).Bold();
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Imię:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.Name).Bold();
                    });
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Gatunek:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.Species);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Rasa:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.Breed);
                    });
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Płeć:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.Gender);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Umaszczenie:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.Color);
                    });
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Data urodzenia:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.DateOfBirth?.ToString("dd.MM.yyyy") ?? "Nieznana");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Nr chip:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.ChipNumber ?? "Brak");
                    });
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Sterylizacja/Kastracja:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.IsSterilized ? "Tak" : "Nie");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Szczepienia:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.IsVaccinated ? "Aktualne" : "Do uzupełnienia");
                    });
                });

                if (!string.IsNullOrWhiteSpace(_data.Animal.HealthNotes))
                {
                    col.Item().PaddingTop(5).Column(c =>
                    {
                        c.Item().Text("Uwagi zdrowotne:").FontSize(9).FontColor(SecondaryColor);
                        c.Item().Text(_data.Animal.HealthNotes);
                    });
                }
            });
        });
    }

    private void ComposeTermsSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("§ 3. WARUNKI ADOPCJI")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Column(col =>
            {
                if (_data.Terms.RequiresSterilization)
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(15).Text("•");
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Sterylizacja/Kastracja: ").Bold();
                            text.Span($"Adoptujący zobowiązuje się do przeprowadzenia zabiegu sterylizacji/kastracji " +
                                     $"do dnia {_data.Terms.SterilizationDeadline?.ToString("dd.MM.yyyy") ?? "ustalonego indywidualnie"} " +
                                     "i dostarczenia zaświadczenia weterynaryjnego.");
                        });
                    });
                }

                if (_data.Terms.RequiresVaccination)
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(15).Text("•");
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Szczepienia: ").Bold();
                            text.Span($"Adoptujący zobowiązuje się do przeprowadzenia wymaganych szczepień " +
                                     $"do dnia {_data.Terms.VaccinationDeadline?.ToString("dd.MM.yyyy") ?? "ustalonego indywidualnie"}.");
                        });
                    });
                }

                if (_data.Terms.AllowsHomeVisits)
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(15).Text("•");
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Wizyty kontrolne: ").Bold();
                            text.Span("Adoptujący wyraża zgodę na wizyty kontrolne pracowników schroniska " +
                                     "w miejscu przebywania zwierzęcia.");
                        });
                    });
                }

                if (!string.IsNullOrWhiteSpace(_data.Terms.SpecialConditions))
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(15).Text("•");
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Warunki specjalne: ").Bold();
                            text.Span(_data.Terms.SpecialConditions);
                        });
                    });
                }
            });
        });
    }

    private void ComposeObligationsSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("§ 4. OBOWIĄZKI ADOPTUJĄCEGO")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Text("Adoptujący zobowiązuje się do:");

            var obligations = new[]
            {
                "Zapewnienia zwierzęciu odpowiednich warunków bytowych, w tym właściwego wyżywienia, dostępu do świeżej wody oraz schronienia.",
                "Zapewnienia zwierzęciu regularnej opieki weterynaryjnej, w tym szczepień ochronnych i profilaktyki przeciwpasożytniczej.",
                "Traktowania zwierzęcia w sposób humanitarny, zgodny z obowiązującymi przepisami o ochronie zwierząt.",
                "Niezbywania, niedarowywania i nieodsprzedawania zwierzęcia osobom trzecim bez uprzedniej pisemnej zgody schroniska.",
                "Niezwłocznego powiadomienia schroniska o zaginięciu, ucieczce lub śmierci zwierzęcia.",
                "Umożliwienia pracownikom schroniska przeprowadzenia wizyt kontrolnych.",
                "W przypadku niemożności dalszego sprawowania opieki nad zwierzęciem – zwrócenia go do schroniska."
            };

            foreach (var obligation in obligations)
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(15).Text("•");
                    row.RelativeItem().Text(obligation);
                });
            }
        });
    }

    private void ComposeRodoSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("§ 5. KLAUZULA INFORMACYJNA RODO")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Text(text =>
            {
                text.Span("Zgodnie z art. 13 Rozporządzenia Parlamentu Europejskiego i Rady (UE) 2016/679 " +
                         "z dnia 27 kwietnia 2016 r. (RODO), informujemy że:").FontSize(9);
            });

            column.Item().PaddingTop(3).Column(col =>
            {
                var rodoPoints = new[]
                {
                    $"Administratorem Państwa danych osobowych jest {_data.Shelter.Name} z siedzibą w {_data.Shelter.City}.",
                    "Dane osobowe przetwarzane są w celu realizacji niniejszej umowy adopcji.",
                    "Podstawą prawną przetwarzania jest art. 6 ust. 1 lit. b RODO (wykonanie umowy).",
                    "Dane będą przechowywane przez okres obowiązywania umowy oraz przez okres wymagany przepisami prawa.",
                    "Przysługuje Państwu prawo dostępu do danych, ich sprostowania, usunięcia, ograniczenia przetwarzania, " +
                    "przenoszenia oraz wniesienia sprzeciwu.",
                    "Przysługuje Państwu prawo wniesienia skargi do Prezesa Urzędu Ochrony Danych Osobowych.",
                    "Podanie danych jest dobrowolne, ale niezbędne do zawarcia umowy adopcji."
                };

                var index = 1;
                foreach (var point in rodoPoints)
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(15).Text($"{index}.").FontSize(9);
                        row.RelativeItem().Text(point).FontSize(9);
                    });
                    index++;
                }
            });
        });
    }

    private void ComposeFinalProvisionsSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("§ 6. POSTANOWIENIA KOŃCOWE")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(15).Text("1.");
                    row.RelativeItem().Text(
                        "Umowa wchodzi w życie z dniem jej podpisania przez obie strony.");
                });

                col.Item().Row(row =>
                {
                    row.ConstantItem(15).Text("2.");
                    row.RelativeItem().Text(
                        "Wszelkie zmiany umowy wymagają formy pisemnej pod rygorem nieważności.");
                });

                col.Item().Row(row =>
                {
                    row.ConstantItem(15).Text("3.");
                    row.RelativeItem().Text(
                        "W sprawach nieuregulowanych niniejszą umową zastosowanie mają przepisy Kodeksu Cywilnego " +
                        "oraz Ustawy o ochronie zwierząt.");
                });

                col.Item().Row(row =>
                {
                    row.ConstantItem(15).Text("4.");
                    row.RelativeItem().Text(
                        "Spory wynikłe z niniejszej umowy rozstrzygane będą przez sąd właściwy dla siedziby schroniska.");
                });

                col.Item().Row(row =>
                {
                    row.ConstantItem(15).Text("5.");
                    row.RelativeItem().Text(
                        "Umowę sporządzono w dwóch jednobrzmiących egzemplarzach, po jednym dla każdej ze stron.");
                });
            });
        });
    }

    private void ComposeSignaturesSection(IContainer container)
    {
        container.PaddingTop(30).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(BorderColor).Padding(15).MinHeight(80).Column(col =>
                {
                    col.Item().Text("PRZEKAZUJĄCY").Bold().FontSize(9);
                    col.Item().Text("(pieczęć i podpis)").FontSize(8).FontColor(SecondaryColor);
                    col.Item().PaddingTop(40).Text(_data.Shelter.Name).FontSize(9);
                });

                row.ConstantItem(30);

                row.RelativeItem().Border(1).BorderColor(BorderColor).Padding(15).MinHeight(80).Column(col =>
                {
                    col.Item().Text("ADOPTUJĄCY").Bold().FontSize(9);
                    col.Item().Text("(czytelny podpis)").FontSize(8).FontColor(SecondaryColor);
                    col.Item().PaddingTop(40).Text(_data.Adopter.FullName).FontSize(9);
                });
            });

            column.Item().PaddingTop(10).AlignCenter().Text(
                $"{_data.Shelter.City}, dnia {_data.ContractDate:dd.MM.yyyy} r.")
                .FontSize(9)
                .Italic();
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(BorderColor);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Umowa nr ").FontSize(8).FontColor(SecondaryColor);
                    text.Span(_data.ContractNumber).FontSize(8);
                });

                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("Strona ").FontSize(8).FontColor(SecondaryColor);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" z ").FontSize(8).FontColor(SecondaryColor);
                    text.TotalPages().FontSize(8);
                });

                row.RelativeItem().AlignRight().Text(_data.Shelter.Name)
                    .FontSize(8)
                    .FontColor(SecondaryColor);
            });
        });
    }
}
