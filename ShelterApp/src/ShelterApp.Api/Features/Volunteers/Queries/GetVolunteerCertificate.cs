using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;

namespace ShelterApp.Api.Features.Volunteers.Queries;

// ============================================
// Query - Get Volunteer Certificate
// ============================================
public record GetVolunteerCertificateQuery(
    Guid VolunteerId,
    DateTime FromDate,
    DateTime ToDate
) : IQuery<Result<VolunteerCertificateDto>>;

public record VolunteerCertificateDto(
    string FileName,
    string ContentType,
    byte[] Content
);

// ============================================
// Handler
// ============================================
public class GetVolunteerCertificateHandler
    : IQueryHandler<GetVolunteerCertificateQuery, Result<VolunteerCertificateDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ShelterOptions _shelterOptions;

    public GetVolunteerCertificateHandler(
        ShelterDbContext context,
        IOptions<ShelterOptions> shelterOptions)
    {
        _context = context;
        _shelterOptions = shelterOptions.Value;
    }

    public async Task<Result<VolunteerCertificateDto>> Handle(
        GetVolunteerCertificateQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = DateTime.SpecifyKind(request.FromDate.Date, DateTimeKind.Utc);
        var toDate = DateTime.SpecifyKind(request.ToDate.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        // Get volunteer
        var volunteer = await _context.Volunteers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerCertificateDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        // Get attendances in the date range
        var attendances = await _context.Attendances
            .Where(a => a.VolunteerId == request.VolunteerId)
            .Where(a => a.CheckInTime >= fromDate && a.CheckInTime <= toDate)
            .Where(a => a.CheckOutTime.HasValue)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalHoursWorked = attendances
            .Where(a => a.HoursWorked.HasValue)
            .Sum(a => a.HoursWorked!.Value);

        var totalDaysWorked = attendances
            .Select(a => a.CheckInTime.Date)
            .Distinct()
            .Count();

        // Generate PDF certificate
        var pdfBytes = GenerateCertificatePdf(
            volunteer.FullName,
            volunteer.Email,
            fromDate,
            toDate,
            totalHoursWorked,
            totalDaysWorked);

        var fileName = $"zaswiadczenie_wolontariatu_{volunteer.Id}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf";

        return Result.Success(new VolunteerCertificateDto(
            FileName: fileName,
            ContentType: "application/pdf",
            Content: pdfBytes
        ));
    }

    private byte[] GenerateCertificatePdf(
        string volunteerName,
        string email,
        DateTime fromDate,
        DateTime toDate,
        decimal totalHours,
        int totalDays)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(12).FontFamily("DejaVu Sans"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, volunteerName, email, fromDate, toDate, totalHours, totalDays));
                page.Footer().Element(ComposeFooter);
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(_shelterOptions.Name)
                .FontSize(16).Bold().FontColor(Colors.Green.Darken3);

            column.Item().AlignCenter().Text($"{_shelterOptions.Address}, {_shelterOptions.PostalCode} {_shelterOptions.City}")
                .FontSize(10).FontColor(Colors.Grey.Darken1);

            column.Item().AlignCenter().Text($"Tel: {_shelterOptions.Phone} | Email: {_shelterOptions.Email}")
                .FontSize(10).FontColor(Colors.Grey.Darken1);

            column.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Green.Darken3);
        });
    }

    private static void ComposeContent(
        IContainer container,
        string volunteerName,
        string email,
        DateTime fromDate,
        DateTime toDate,
        decimal totalHours,
        int totalDays)
    {
        container.PaddingVertical(30).Column(column =>
        {
            // Title
            column.Item().AlignCenter().PaddingBottom(30).Text("ZAŚWIADCZENIE")
                .FontSize(28).Bold().FontColor(Colors.Green.Darken2);

            column.Item().AlignCenter().PaddingBottom(20).Text("o wykonaniu pracy wolontariackiej")
                .FontSize(16).FontColor(Colors.Grey.Darken2);

            // Main content
            column.Item().PaddingBottom(20).Text(text =>
            {
                text.Span("Zaświadcza się, że ");
                text.Span(volunteerName).Bold();
                text.Span($" (email: {email}) ");
                text.Span("wykonywał(a) pracę wolontariacką w naszym schronisku ");
                text.Span($"w okresie od {fromDate:dd.MM.yyyy} do {toDate:dd.MM.yyyy}.");
            });

            // Hours summary box
            column.Item().PaddingVertical(20).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(20).Column(summaryColumn =>
            {
                summaryColumn.Item().AlignCenter().Text("Podsumowanie pracy wolontariackiej")
                    .FontSize(14).Bold().FontColor(Colors.Green.Darken2);

                summaryColumn.Item().PaddingTop(15).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().AlignCenter().Text($"{totalHours:F1}").FontSize(32).Bold().FontColor(Colors.Green.Darken3);
                        c.Item().AlignCenter().Text("godzin").FontSize(12).FontColor(Colors.Grey.Darken1);
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().AlignCenter().Text($"{totalDays}").FontSize(32).Bold().FontColor(Colors.Green.Darken3);
                        c.Item().AlignCenter().Text("dni").FontSize(12).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            // Additional text
            column.Item().PaddingTop(20).Text(text =>
            {
                text.Span("Wolontariusz wykazał się zaangażowaniem i odpowiedzialnością w opiece nad zwierzętami. ");
                text.Span("Jego/jej praca przyczyniła się do poprawy warunków życia podopiecznych schroniska.");
            });

            column.Item().PaddingTop(30).Text("Zaświadczenie wydaje się na prośbę zainteresowanego w celu przedłożenia we właściwym urzędzie lub instytucji.")
                .FontSize(10).FontColor(Colors.Grey.Darken1);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"{_shelterOptions.City}, {DateTime.Now:dd.MM.yyyy}")
                        .FontSize(10);
                    c.Item().PaddingTop(30).Text(".....................................................")
                        .FontSize(10);
                    c.Item().Text("podpis i pieczęć")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text($"Nr zaświadczenia: ZW/{DateTime.Now:yyyyMMdd}/{Guid.NewGuid().ToString()[..6].ToUpper()}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }
}

// ============================================
// Validator
// ============================================
public class GetVolunteerCertificateValidator : AbstractValidator<GetVolunteerCertificateQuery>
{
    public GetVolunteerCertificateValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("Data początkowa jest wymagana");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("Data końcowa jest wymagana")
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("Data końcowa musi być większa lub równa dacie początkowej");

        RuleFor(x => x)
            .Must(x => (x.ToDate - x.FromDate).TotalDays <= 365)
            .WithMessage("Zakres dat nie może przekraczać 365 dni");
    }
}
