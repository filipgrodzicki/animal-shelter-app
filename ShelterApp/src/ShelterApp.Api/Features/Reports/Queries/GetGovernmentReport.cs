using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShelterApp.Api.Features.Reports.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;

namespace ShelterApp.Api.Features.Reports.Queries;

// ============================================
// Query - Get Government Report (WF-33)
// ============================================
public record GetGovernmentReportQuery(
    DateTime FromDate,
    DateTime ToDate,
    string Format = "json" // "json", "csv", "pdf"
) : IQuery<Result<GovernmentReportDto>>;

// ============================================
// Handler
// ============================================
public class GetGovernmentReportHandler
    : IQueryHandler<GetGovernmentReportQuery, Result<GovernmentReportDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ShelterOptions _shelterOptions;

    public GetGovernmentReportHandler(
        ShelterDbContext context,
        IOptions<ShelterOptions> shelterOptions)
    {
        _context = context;
        _shelterOptions = shelterOptions.Value;
    }

    public async Task<Result<GovernmentReportDto>> Handle(
        GetGovernmentReportQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = DateTime.SpecifyKind(request.FromDate.Date, DateTimeKind.Utc);
        var toDate = DateTime.SpecifyKind(request.ToDate.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        // Shelter info
        var shelterInfo = new ShelterInfoDto(
            Name: _shelterOptions.Name,
            Address: _shelterOptions.Address,
            City: _shelterOptions.City,
            PostalCode: _shelterOptions.PostalCode,
            Phone: _shelterOptions.Phone,
            Email: _shelterOptions.Email,
            Nip: _shelterOptions.Nip,
            Regon: _shelterOptions.Regon
        );

        // Get admissions
        var admissions = await GetAdmissionsAsync(fromDate, toDate, cancellationToken);

        // Get adoptions
        var adoptions = await GetAdoptionsAsync(fromDate, toDate, cancellationToken);

        // Get volunteer hours
        var volunteerHours = await GetVolunteerHoursAsync(fromDate, toDate, cancellationToken);

        // Calculate summary
        var currentPopulation = await _context.Animals
            .CountAsync(a =>
                a.Status != AnimalStatus.Adopted &&
                a.Status != AnimalStatus.Deceased, cancellationToken);

        var deceasedCount = await _context.Set<Domain.Animals.Entities.AnimalStatusChange>()
            .Where(sc => sc.NewStatus == AnimalStatus.Deceased)
            .Where(sc => sc.ChangedAt >= fromDate && sc.ChangedAt <= toDate)
            .Select(sc => sc.AnimalId)
            .Distinct()
            .CountAsync(cancellationToken);

        var summary = new AdmissionsAndAdoptionsSummaryDto(
            TotalAdmissions: admissions.Count,
            AdmissionsDogs: admissions.Count(a => a.Species == "Dog" || a.Species == "Psy"),
            AdmissionsCats: admissions.Count(a => a.Species == "Cat" || a.Species == "Koty"),
            AdmissionsOther: admissions.Count(a =>
                a.Species != "Dog" && a.Species != "Cat" &&
                a.Species != "Psy" && a.Species != "Koty"),
            TotalAdoptions: adoptions.Count,
            AdoptionsDogs: adoptions.Count(a => a.Species == "Dog" || a.Species == "Psy"),
            AdoptionsCats: adoptions.Count(a => a.Species == "Cat" || a.Species == "Koty"),
            AdoptionsOther: adoptions.Count(a =>
                a.Species != "Dog" && a.Species != "Cat" &&
                a.Species != "Psy" && a.Species != "Koty"),
            TotalDeceased: deceasedCount,
            CurrentPopulation: currentPopulation
        );

        var reportPeriod = $"{fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy}";

        // Generate report content based on format
        string? reportContent = null;
        string? contentType = null;
        string? fileName = null;
        var format = request.Format.ToLowerInvariant();

        switch (format)
        {
            case "csv":
                reportContent = GenerateCsvReport(shelterInfo, reportPeriod, summary, admissions, adoptions, volunteerHours);
                contentType = "text/csv";
                fileName = $"raport_samorzadowy_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv";
                break;

            case "pdf":
                var pdfBytes = GeneratePdfReport(shelterInfo, reportPeriod, fromDate, toDate, summary, admissions, adoptions, volunteerHours);
                reportContent = Convert.ToBase64String(pdfBytes);
                contentType = "application/pdf";
                fileName = $"raport_samorzadowy_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf";
                break;
        }

        var result = new GovernmentReportDto(
            FromDate: fromDate,
            ToDate: request.ToDate.Date,
            ReportPeriod: reportPeriod,
            ShelterInfo: shelterInfo,
            Summary: summary,
            Admissions: admissions,
            Adoptions: adoptions,
            VolunteerHours: volunteerHours,
            ReportContent: reportContent,
            ContentType: contentType,
            FileName: fileName
        );

        return Result.Success(result);
    }

    private async Task<List<AdmissionRecordDto>> GetAdmissionsAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var animals = await _context.Animals
            .Where(a => a.AdmissionDate >= fromDate && a.AdmissionDate <= toDate)
            .OrderBy(a => a.AdmissionDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return animals.Select(a => new AdmissionRecordDto(
            RegistrationNumber: a.RegistrationNumber,
            Name: a.Name,
            Species: GetSpeciesLabel(a.Species),
            Breed: a.Breed,
            Gender: GetGenderLabel(a.Gender),
            AdmissionDate: a.AdmissionDate,
            AdmissionCircumstances: a.AdmissionCircumstances,
            CurrentStatus: GetStatusLabel(a.Status)
        )).ToList();
    }

    private async Task<List<AdoptionRecordDto>> GetAdoptionsAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        // Get completed applications in period
        var applications = await _context.AdoptionApplications
            .Where(a => a.Status == AdoptionApplicationStatus.Completed)
            .Where(a => a.UpdatedAt.HasValue && a.UpdatedAt >= fromDate && a.UpdatedAt <= toDate)
            .AsNoTracking()
            .ToListAsync(ct);

        var animalIds = applications.Select(a => a.AnimalId).Distinct().ToList();
        var adopterIds = applications.Select(a => a.AdopterId).Distinct().ToList();

        var animals = await _context.Animals
            .Where(a => animalIds.Contains(a.Id))
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, ct);

        var adopters = await _context.Adopters
            .Where(a => adopterIds.Contains(a.Id))
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, ct);

        return applications.Select(app =>
        {
            var animal = animals.GetValueOrDefault(app.AnimalId);
            var adopter = adopters.GetValueOrDefault(app.AdopterId);

            return new AdoptionRecordDto(
                RegistrationNumber: animal?.RegistrationNumber ?? "N/A",
                AnimalName: animal?.Name ?? "N/A",
                Species: animal != null ? GetSpeciesLabel(animal.Species) : "N/A",
                Breed: animal?.Breed ?? "N/A",
                AdoptionDate: app.UpdatedAt ?? app.CreatedAt,
                ContractNumber: app.ContractNumber ?? "N/A",
                AdopterInitials: adopter != null
                    ? $"{adopter.FirstName[0]}.{adopter.LastName[0]}."
                    : "N/A",
                AdopterCity: adopter?.City ?? "N/A"
            );
        })
        .OrderBy(a => a.AdoptionDate)
        .ToList();
    }

    private async Task<VolunteerHoursSummaryDto> GetVolunteerHoursAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var volunteers = await _context.Volunteers
            .Where(v => v.Status == VolunteerStatus.Active)
            .AsNoTracking()
            .ToListAsync(ct);

        var volunteerIds = volunteers.Select(v => v.Id).ToList();

        var attendances = await _context.Attendances
            .Where(a => volunteerIds.Contains(a.VolunteerId))
            .Where(a => a.CheckInTime >= fromDate && a.CheckInTime <= toDate)
            .Where(a => a.CheckOutTime.HasValue)
            .AsNoTracking()
            .ToListAsync(ct);

        var volunteerDict = volunteers.ToDictionary(v => v.Id);

        var records = attendances
            .GroupBy(a => a.VolunteerId)
            .Select(g =>
            {
                var volunteer = volunteerDict.GetValueOrDefault(g.Key);
                return new VolunteerHoursRecordDto(
                    VolunteerName: volunteer?.FullName ?? "Nieznany",
                    Email: volunteer?.Email ?? "",
                    HoursWorked: g.Sum(a => a.HoursWorked ?? 0),
                    DaysWorked: g.Select(a => a.CheckInTime.Date).Distinct().Count(),
                    AttendanceCount: g.Count()
                );
            })
            .OrderByDescending(r => r.HoursWorked)
            .ToList();

        return new VolunteerHoursSummaryDto(
            TotalVolunteers: records.Count,
            TotalHoursWorked: records.Sum(r => r.HoursWorked),
            TotalDaysWorked: attendances.Select(a => a.CheckInTime.Date).Distinct().Count(),
            Records: records
        );
    }

    private static string GenerateCsvReport(
        ShelterInfoDto shelter,
        string reportPeriod,
        AdmissionsAndAdoptionsSummaryDto summary,
        List<AdmissionRecordDto> admissions,
        List<AdoptionRecordDto> adoptions,
        VolunteerHoursSummaryDto volunteerHours)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("RAPORT DLA SAMORZĄDU");
        sb.AppendLine($"Schronisko:;{shelter.Name}");
        sb.AppendLine($"Adres:;{shelter.Address}, {shelter.PostalCode} {shelter.City}");
        sb.AppendLine($"Okres:;{reportPeriod}");
        sb.AppendLine();

        // Summary
        sb.AppendLine("PODSUMOWANIE");
        sb.AppendLine($"Przyjęcia ogółem:;{summary.TotalAdmissions}");
        sb.AppendLine($"  - Psy:;{summary.AdmissionsDogs}");
        sb.AppendLine($"  - Koty:;{summary.AdmissionsCats}");
        sb.AppendLine($"  - Inne:;{summary.AdmissionsOther}");
        sb.AppendLine($"Adopcje ogółem:;{summary.TotalAdoptions}");
        sb.AppendLine($"  - Psy:;{summary.AdoptionsDogs}");
        sb.AppendLine($"  - Koty:;{summary.AdoptionsCats}");
        sb.AppendLine($"  - Inne:;{summary.AdoptionsOther}");
        sb.AppendLine($"Zgony:;{summary.TotalDeceased}");
        sb.AppendLine($"Aktualna populacja:;{summary.CurrentPopulation}");
        sb.AppendLine();

        // Admissions
        sb.AppendLine("PRZYJĘCIA");
        sb.AppendLine("Nr ewidencyjny;Imię;Gatunek;Rasa;Płeć;Data przyjęcia;Okoliczności;Status");
        foreach (var admission in admissions)
        {
            sb.AppendLine(string.Join(";",
                admission.RegistrationNumber,
                EscapeCsv(admission.Name),
                admission.Species,
                EscapeCsv(admission.Breed),
                admission.Gender,
                admission.AdmissionDate.ToString("yyyy-MM-dd"),
                EscapeCsv(admission.AdmissionCircumstances),
                admission.CurrentStatus
            ));
        }
        sb.AppendLine();

        // Adoptions
        sb.AppendLine("ADOPCJE");
        sb.AppendLine("Nr ewidencyjny;Imię zwierzęcia;Gatunek;Rasa;Data adopcji;Nr umowy;Adoptujący (inicjały);Miasto");
        foreach (var adoption in adoptions)
        {
            sb.AppendLine(string.Join(";",
                adoption.RegistrationNumber,
                EscapeCsv(adoption.AnimalName),
                adoption.Species,
                EscapeCsv(adoption.Breed),
                adoption.AdoptionDate.ToString("yyyy-MM-dd"),
                adoption.ContractNumber,
                adoption.AdopterInitials,
                adoption.AdopterCity
            ));
        }
        sb.AppendLine();

        // Volunteer hours
        sb.AppendLine("EWIDENCJA GODZIN WOLONTARIUSZY");
        sb.AppendLine($"Łączna liczba wolontariuszy:;{volunteerHours.TotalVolunteers}");
        sb.AppendLine($"Łączna liczba godzin:;{volunteerHours.TotalHoursWorked:F2}");
        sb.AppendLine($"Łączna liczba dni pracy:;{volunteerHours.TotalDaysWorked}");
        sb.AppendLine();
        sb.AppendLine("Wolontariusz;Email;Godziny;Dni;Obecności");
        foreach (var record in volunteerHours.Records)
        {
            sb.AppendLine(string.Join(";",
                EscapeCsv(record.VolunteerName),
                record.Email,
                record.HoursWorked.ToString("F2").Replace('.', ','),
                record.DaysWorked,
                record.AttendanceCount
            ));
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static byte[] GeneratePdfReport(
        ShelterInfoDto shelter,
        string reportPeriod,
        DateTime fromDate,
        DateTime toDate,
        AdmissionsAndAdoptionsSummaryDto summary,
        List<AdmissionRecordDto> admissions,
        List<AdoptionRecordDto> adoptions,
        VolunteerHoursSummaryDto volunteerHours)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = new GovernmentReportDocument(
            shelter, reportPeriod, fromDate, toDate, summary, admissions, adoptions, volunteerHours);

        return document.GeneratePdf();
    }

    private static string GetSpeciesLabel(Species species) => species switch
    {
        Species.Dog => "Psy",
        Species.Cat => "Koty",
        _ => species.ToString()
    };

    private static string GetGenderLabel(Gender gender) => gender switch
    {
        Gender.Male => "Samiec",
        Gender.Female => "Samica",
        Gender.Unknown => "Nieznana",
        _ => gender.ToString()
    };

    private static string GetStatusLabel(AnimalStatus status) => status switch
    {
        AnimalStatus.Admitted => "Przyjęte",
        AnimalStatus.Quarantine => "Kwarantanna",
        AnimalStatus.Treatment => "Leczenie",
        AnimalStatus.Available => "Dostępne",
        AnimalStatus.Reserved => "Zarezerwowane",
        AnimalStatus.InAdoptionProcess => "W procesie adopcji",
        AnimalStatus.Adopted => "Zaadoptowane",
        AnimalStatus.Deceased => "Zmarłe",
        _ => status.ToString()
    };
}

// ============================================
// PDF Document
// ============================================
internal class GovernmentReportDocument : IDocument
{
    private readonly ShelterInfoDto _shelter;
    private readonly string _reportPeriod;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly AdmissionsAndAdoptionsSummaryDto _summary;
    private readonly List<AdmissionRecordDto> _admissions;
    private readonly List<AdoptionRecordDto> _adoptions;
    private readonly VolunteerHoursSummaryDto _volunteerHours;

    private static readonly string PrimaryColor = "#2E7D32";
    private static readonly string SecondaryColor = "#666666";
    private static readonly string BorderColor = "#CCCCCC";
    private static readonly string HeaderBgColor = "#E8F5E9";

    public GovernmentReportDocument(
        ShelterInfoDto shelter,
        string reportPeriod,
        DateTime fromDate,
        DateTime toDate,
        AdmissionsAndAdoptionsSummaryDto summary,
        List<AdmissionRecordDto> admissions,
        List<AdoptionRecordDto> adoptions,
        VolunteerHoursSummaryDto volunteerHours)
    {
        _shelter = shelter;
        _reportPeriod = reportPeriod;
        _fromDate = fromDate;
        _toDate = toDate;
        _summary = summary;
        _admissions = admissions;
        _adoptions = adoptions;
        _volunteerHours = volunteerHours;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("DejaVu Sans"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(_shelter.Name)
                        .FontSize(14)
                        .Bold()
                        .FontColor(PrimaryColor);

                    col.Item().Text($"{_shelter.Address}, {_shelter.PostalCode} {_shelter.City}")
                        .FontSize(8)
                        .FontColor(SecondaryColor);

                    col.Item().Text($"Tel: {_shelter.Phone} | Email: {_shelter.Email}")
                        .FontSize(8)
                        .FontColor(SecondaryColor);
                });

                row.ConstantItem(180).AlignRight().Column(col =>
                {
                    col.Item().Text("RAPORT DLA SAMORZĄDU")
                        .FontSize(12)
                        .Bold()
                        .FontColor(PrimaryColor);

                    col.Item().Text($"Okres: {_reportPeriod}")
                        .FontSize(9);

                    col.Item().Text($"Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(8)
                        .FontColor(SecondaryColor);
                });
            });

            column.Item().PaddingVertical(8).LineHorizontal(1).LineColor(BorderColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            // Summary section
            column.Item().Element(ComposeSummarySection);

            // Admissions section
            if (_admissions.Any())
            {
                column.Item().Element(ComposeAdmissionsSection);
            }

            // Adoptions section
            if (_adoptions.Any())
            {
                column.Item().Element(ComposeAdoptionsSection);
            }

            // Volunteer hours section
            if (_volunteerHours.Records.Any())
            {
                column.Item().Element(ComposeVolunteerSection);
            }
        });
    }

    private void ComposeSummarySection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("PODSUMOWANIE OKRESU")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Border(1).BorderColor(BorderColor).Column(col =>
            {
                // Header row
                col.Item().Background(HeaderBgColor).Padding(8).Row(row =>
                {
                    row.RelativeItem().Text("Kategoria").Bold();
                    row.ConstantItem(60).AlignRight().Text("Ogółem").Bold();
                    row.ConstantItem(60).AlignRight().Text("Psy").Bold();
                    row.ConstantItem(60).AlignRight().Text("Koty").Bold();
                    row.ConstantItem(60).AlignRight().Text("Inne").Bold();
                });

                // Admissions row
                col.Item().Padding(8).Row(row =>
                {
                    row.RelativeItem().Text("Przyjęcia");
                    row.ConstantItem(60).AlignRight().Text(_summary.TotalAdmissions.ToString());
                    row.ConstantItem(60).AlignRight().Text(_summary.AdmissionsDogs.ToString());
                    row.ConstantItem(60).AlignRight().Text(_summary.AdmissionsCats.ToString());
                    row.ConstantItem(60).AlignRight().Text(_summary.AdmissionsOther.ToString());
                });

                col.Item().LineHorizontal(0.5f).LineColor(BorderColor);

                // Adoptions row
                col.Item().Padding(8).Row(row =>
                {
                    row.RelativeItem().Text("Adopcje");
                    row.ConstantItem(60).AlignRight().Text(_summary.TotalAdoptions.ToString());
                    row.ConstantItem(60).AlignRight().Text(_summary.AdoptionsDogs.ToString());
                    row.ConstantItem(60).AlignRight().Text(_summary.AdoptionsCats.ToString());
                    row.ConstantItem(60).AlignRight().Text(_summary.AdoptionsOther.ToString());
                });

                col.Item().LineHorizontal(0.5f).LineColor(BorderColor);

                // Other stats
                col.Item().Padding(8).Row(row =>
                {
                    row.RelativeItem().Text("Zgony");
                    row.ConstantItem(240).AlignRight().Text(_summary.TotalDeceased.ToString());
                });

                col.Item().Background(HeaderBgColor).Padding(8).Row(row =>
                {
                    row.RelativeItem().Text("Aktualna populacja").Bold();
                    row.ConstantItem(240).AlignRight().Text(_summary.CurrentPopulation.ToString()).Bold();
                });
            });
        });
    }

    private void ComposeAdmissionsSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text($"PRZYJĘCIA ({_admissions.Count})")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);  // Nr ewidencyjny
                    columns.RelativeColumn(1);   // Imię
                    columns.ConstantColumn(50);  // Gatunek
                    columns.RelativeColumn(1);   // Rasa
                    columns.ConstantColumn(45);  // Płeć
                    columns.ConstantColumn(70);  // Data
                    columns.ConstantColumn(60);  // Status
                });

                table.Header(header =>
                {
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Nr ewid.").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Imię").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Gatunek").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Rasa").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Płeć").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Data").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Status").FontSize(8).Bold();
                });

                foreach (var admission in _admissions)
                {
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(admission.RegistrationNumber).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(admission.Name).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(admission.Species).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(admission.Breed).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(admission.Gender).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(admission.AdmissionDate.ToString("dd.MM.yyyy")).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(admission.CurrentStatus).FontSize(7);
                }
            });
        });
    }

    private void ComposeAdoptionsSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text($"ADOPCJE ({_adoptions.Count})")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);  // Nr ewidencyjny
                    columns.RelativeColumn(1);   // Imię
                    columns.ConstantColumn(50);  // Gatunek
                    columns.RelativeColumn(1);   // Rasa
                    columns.ConstantColumn(70);  // Data adopcji
                    columns.ConstantColumn(80);  // Nr umowy
                    columns.ConstantColumn(60);  // Miasto
                });

                table.Header(header =>
                {
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Nr ewid.").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Imię").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Gatunek").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Rasa").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Data").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Nr umowy").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Miasto").FontSize(8).Bold();
                });

                foreach (var adoption in _adoptions)
                {
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(adoption.RegistrationNumber).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(adoption.AnimalName).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(adoption.Species).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(adoption.Breed).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(adoption.AdoptionDate.ToString("dd.MM.yyyy")).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(adoption.ContractNumber).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(adoption.AdopterCity).FontSize(7);
                }
            });
        });
    }

    private void ComposeVolunteerSection(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("EWIDENCJA GODZIN WOLONTARIUSZY")
                .FontSize(11)
                .Bold()
                .FontColor(PrimaryColor);

            // Summary
            column.Item().PaddingTop(5).PaddingBottom(5).Row(row =>
            {
                row.RelativeItem().Text($"Łączna liczba wolontariuszy: {_volunteerHours.TotalVolunteers}").FontSize(9);
                row.RelativeItem().Text($"Łączna liczba godzin: {_volunteerHours.TotalHoursWorked:F2}").FontSize(9);
                row.RelativeItem().Text($"Łączna liczba dni pracy: {_volunteerHours.TotalDaysWorked}").FontSize(9);
            });

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);   // Wolontariusz
                    columns.RelativeColumn(2);   // Email
                    columns.ConstantColumn(60);  // Godziny
                    columns.ConstantColumn(50);  // Dni
                    columns.ConstantColumn(60);  // Obecności
                });

                table.Header(header =>
                {
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Wolontariusz").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Email").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Godziny").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Dni").FontSize(8).Bold();
                    header.Cell().Background(HeaderBgColor).Border(0.5f).BorderColor(BorderColor).Padding(4).Text("Obecności").FontSize(8).Bold();
                });

                foreach (var record in _volunteerHours.Records)
                {
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(record.VolunteerName).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(record.Email).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).AlignRight().Text(record.HoursWorked.ToString("F2")).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).AlignRight().Text(record.DaysWorked.ToString()).FontSize(7);
                    table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).AlignRight().Text(record.AttendanceCount.ToString()).FontSize(7);
                }
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(BorderColor);

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"{_shelter.Name} - Raport samorządowy").FontSize(7).FontColor(SecondaryColor);

                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("Strona ").FontSize(7).FontColor(SecondaryColor);
                    text.CurrentPageNumber().FontSize(7);
                    text.Span(" z ").FontSize(7).FontColor(SecondaryColor);
                    text.TotalPages().FontSize(7);
                });

                row.RelativeItem().AlignRight().Text($"Okres: {_reportPeriod}").FontSize(7).FontColor(SecondaryColor);
            });
        });
    }
}

// ============================================
// Validator
// ============================================
public class GetGovernmentReportValidator : AbstractValidator<GetGovernmentReportQuery>
{
    private static readonly string[] ValidFormats = { "json", "csv", "pdf" };

    public GetGovernmentReportValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("Data początkowa jest wymagana");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("Data końcowa jest wymagana")
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("Data końcowa musi być większa lub równa dacie początkowej");

        RuleFor(x => x)
            .Must(x => (x.ToDate - x.FromDate).TotalDays <= 365)
            .WithMessage("Zakres dat nie może przekraczać 365 dni");

        RuleFor(x => x.Format)
            .Must(f => ValidFormats.Contains(f.ToLowerInvariant()))
            .WithMessage($"Format musi być jednym z: {string.Join(", ", ValidFormats)}");
    }
}
