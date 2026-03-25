using System.Globalization;
using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Queries;

// ============================================
// Query - Get Volunteer Hours Report (WS-19)
// ============================================
public record GetVolunteerHoursReportQuery(
    Guid VolunteerId,
    DateTime FromDate,
    DateTime ToDate,
    string Format = "json" // "json", "csv", "pdf"
) : IQuery<Result<VolunteerHoursReportDto>>;

// ============================================
// Handler
// ============================================
public class GetVolunteerHoursReportHandler
    : IQueryHandler<GetVolunteerHoursReportQuery, Result<VolunteerHoursReportDto>>
{
    private readonly ShelterDbContext _context;

    public GetVolunteerHoursReportHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<VolunteerHoursReportDto>> Handle(
        GetVolunteerHoursReportQuery request,
        CancellationToken cancellationToken)
    {
        // Get volunteer
        var volunteer = await _context.Volunteers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerHoursReportDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        // Get attendances in the date range
        var attendances = await _context.Attendances
            .Where(a => a.VolunteerId == request.VolunteerId)
            .Where(a => a.CheckInTime >= request.FromDate && a.CheckInTime <= request.ToDate)
            .Where(a => a.CheckOutTime.HasValue) // Only completed attendances
            .OrderBy(a => a.CheckInTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get schedule slots for attendances with slot IDs
        var slotIds = attendances
            .Where(a => a.ScheduleSlotId.HasValue)
            .Select(a => a.ScheduleSlotId!.Value)
            .Distinct()
            .ToList();

        var slots = await _context.ScheduleSlots
            .Where(s => slotIds.Contains(s.Id))
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var attendanceItems = attendances.Select(a => new AttendanceReportItemDto(
            Date: a.CheckInTime.Date,
            CheckInTime: TimeOnly.FromDateTime(a.CheckInTime),
            CheckOutTime: a.CheckOutTime.HasValue ? TimeOnly.FromDateTime(a.CheckOutTime.Value) : null,
            HoursWorked: a.HoursWorked,
            SlotDescription: a.ScheduleSlotId.HasValue && slots.TryGetValue(a.ScheduleSlotId.Value, out var slot)
                ? slot.Description
                : null,
            WorkDescription: a.WorkDescription,
            IsApproved: a.IsApproved
        )).ToList();

        var totalHoursWorked = attendances
            .Where(a => a.HoursWorked.HasValue)
            .Sum(a => a.HoursWorked!.Value);

        var totalDaysWorked = attendances
            .Select(a => a.CheckInTime.Date)
            .Distinct()
            .Count();

        var averageHoursPerDay = totalDaysWorked > 0
            ? Math.Round(totalHoursWorked / totalDaysWorked, 2)
            : 0;

        // Generate report content based on format
        string? reportContent = null;
        string? contentType = null;
        string? fileName = null;

        var format = request.Format.ToLowerInvariant();

        switch (format)
        {
            case "csv":
                reportContent = GenerateCsvReport(volunteer.FullName, volunteer.Email,
                    request.FromDate, request.ToDate, attendanceItems, totalHoursWorked);
                contentType = "text/csv";
                fileName = $"volunteer_hours_{volunteer.Id}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.csv";
                break;

            case "pdf":
                // PDF generation would typically use a library like iText, QuestPDF, etc.
                // For now, return HTML that can be converted to PDF
                reportContent = GenerateHtmlReport(volunteer.FullName, volunteer.Email,
                    request.FromDate, request.ToDate, attendanceItems, totalHoursWorked, totalDaysWorked, averageHoursPerDay);
                contentType = "text/html";
                fileName = $"volunteer_hours_{volunteer.Id}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.html";
                break;

            case "json":
            default:
                // JSON is the default - no additional content needed
                break;
        }

        var report = new VolunteerHoursReportDto(
            VolunteerId: volunteer.Id,
            VolunteerName: volunteer.FullName,
            Email: volunteer.Email,
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            Attendances: attendanceItems,
            TotalHoursWorked: totalHoursWorked,
            TotalDaysWorked: totalDaysWorked,
            AverageHoursPerDay: averageHoursPerDay,
            ReportContent: reportContent,
            ContentType: contentType,
            FileName: fileName
        );

        return Result.Success(report);
    }

    private static string GenerateCsvReport(
        string volunteerName,
        string email,
        DateTime fromDate,
        DateTime toDate,
        List<AttendanceReportItemDto> attendances,
        decimal totalHours)
    {
        var sb = new StringBuilder();
        var culture = new CultureInfo("pl-PL");

        // Header info
        sb.AppendLine($"Raport godzin wolontariusza");
        sb.AppendLine($"Wolontariusz:;{volunteerName}");
        sb.AppendLine($"Email:;{email}");
        sb.AppendLine($"Okres:;{fromDate:yyyy-MM-dd} - {toDate:yyyy-MM-dd}");
        sb.AppendLine($"Suma godzin:;{totalHours.ToString("F2", culture)}");
        sb.AppendLine();

        // Column headers
        sb.AppendLine("Data;Godzina wejścia;Godzina wyjścia;Przepracowane godziny;Opis dyżuru;Opis pracy;Zatwierdzone");

        // Data rows
        foreach (var attendance in attendances)
        {
            sb.AppendLine(string.Join(";",
                attendance.Date.ToString("yyyy-MM-dd"),
                attendance.CheckInTime.ToString("HH:mm"),
                attendance.CheckOutTime?.ToString("HH:mm") ?? "",
                attendance.HoursWorked?.ToString("F2", culture) ?? "",
                EscapeCsvField(attendance.SlotDescription ?? ""),
                EscapeCsvField(attendance.WorkDescription ?? ""),
                attendance.IsApproved ? "Tak" : "Nie"
            ));
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static string GenerateHtmlReport(
        string volunteerName,
        string email,
        DateTime fromDate,
        DateTime toDate,
        List<AttendanceReportItemDto> attendances,
        decimal totalHours,
        int totalDays,
        decimal avgHoursPerDay)
    {
        var culture = new CultureInfo("pl-PL");
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"pl\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<title>Raport godzin wolontariusza</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("h1 { color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }");
        sb.AppendLine(".info { margin: 20px 0; }");
        sb.AppendLine(".info-item { margin: 5px 0; }");
        sb.AppendLine(".info-label { font-weight: bold; display: inline-block; width: 150px; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #3498db; color: white; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine(".summary { margin-top: 20px; background-color: #ecf0f1; padding: 15px; border-radius: 5px; }");
        sb.AppendLine(".summary-item { margin: 5px 0; }");
        sb.AppendLine(".approved { color: #27ae60; }");
        sb.AppendLine(".not-approved { color: #e74c3c; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<h1>Raport godzin wolontariusza</h1>");

        sb.AppendLine("<div class=\"info\">");
        sb.AppendLine($"<div class=\"info-item\"><span class=\"info-label\">Wolontariusz:</span> {volunteerName}</div>");
        sb.AppendLine($"<div class=\"info-item\"><span class=\"info-label\">Email:</span> {email}</div>");
        sb.AppendLine($"<div class=\"info-item\"><span class=\"info-label\">Okres:</span> {fromDate:yyyy-MM-dd} - {toDate:yyyy-MM-dd}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<table>");
        sb.AppendLine("<thead>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Data</th>");
        sb.AppendLine("<th>Wejście</th>");
        sb.AppendLine("<th>Wyjście</th>");
        sb.AppendLine("<th>Godziny</th>");
        sb.AppendLine("<th>Dyżur</th>");
        sb.AppendLine("<th>Opis pracy</th>");
        sb.AppendLine("<th>Status</th>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");
        sb.AppendLine("<tbody>");

        foreach (var attendance in attendances)
        {
            var statusClass = attendance.IsApproved ? "approved" : "not-approved";
            var statusText = attendance.IsApproved ? "Zatwierdzone" : "Oczekuje";

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{attendance.Date:yyyy-MM-dd}</td>");
            sb.AppendLine($"<td>{attendance.CheckInTime:HH:mm}</td>");
            sb.AppendLine($"<td>{attendance.CheckOutTime?.ToString("HH:mm") ?? "-"}</td>");
            sb.AppendLine($"<td>{attendance.HoursWorked?.ToString("F2", culture) ?? "-"}</td>");
            sb.AppendLine($"<td>{attendance.SlotDescription ?? "-"}</td>");
            sb.AppendLine($"<td>{attendance.WorkDescription ?? "-"}</td>");
            sb.AppendLine($"<td class=\"{statusClass}\">{statusText}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine("<h3>Podsumowanie</h3>");
        sb.AppendLine($"<div class=\"summary-item\"><strong>Łączna liczba godzin:</strong> {totalHours.ToString("F2", culture)} h</div>");
        sb.AppendLine($"<div class=\"summary-item\"><strong>Liczba dni pracy:</strong> {totalDays}</div>");
        sb.AppendLine($"<div class=\"summary-item\"><strong>Średnia godzin dziennie:</strong> {avgHoursPerDay.ToString("F2", culture)} h</div>");
        sb.AppendLine("</div>");

        sb.AppendLine($"<p style=\"margin-top: 30px; font-size: 12px; color: #7f8c8d;\">Wygenerowano: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}

// ============================================
// Validator
// ============================================
public class GetVolunteerHoursReportValidator : AbstractValidator<GetVolunteerHoursReportQuery>
{
    private static readonly string[] ValidFormats = { "json", "csv", "pdf" };

    public GetVolunteerHoursReportValidator()
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

        RuleFor(x => x.Format)
            .Must(f => ValidFormats.Contains(f.ToLowerInvariant()))
            .WithMessage($"Format musi być jednym z: {string.Join(", ", ValidFormats)}");
    }
}

// ============================================
// Query - Get Volunteer Hours Summary (aggregate)
// ============================================
public record GetVolunteerHoursSummaryQuery(
    DateTime FromDate,
    DateTime ToDate,
    string? Status = null
) : IQuery<Result<List<VolunteerHoursSummaryItemDto>>>;

public record VolunteerHoursSummaryItemDto(
    Guid VolunteerId,
    string VolunteerName,
    string Email,
    string Status,
    decimal TotalHoursWorked,
    int TotalDaysWorked,
    int AttendanceCount,
    int ApprovedCount,
    int PendingApprovalCount
);

// ============================================
// Handler - Get Volunteer Hours Summary
// ============================================
public class GetVolunteerHoursSummaryHandler
    : IQueryHandler<GetVolunteerHoursSummaryQuery, Result<List<VolunteerHoursSummaryItemDto>>>
{
    private readonly ShelterDbContext _context;

    public GetVolunteerHoursSummaryHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<VolunteerHoursSummaryItemDto>>> Handle(
        GetVolunteerHoursSummaryQuery request,
        CancellationToken cancellationToken)
    {
        // Get all volunteers (optionally filtered by status)
        var volunteersQuery = _context.Volunteers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<Domain.Volunteers.VolunteerStatus>(request.Status, true, out var status))
        {
            volunteersQuery = volunteersQuery.Where(v => v.Status == status);
        }

        var volunteers = await volunteersQuery.ToListAsync(cancellationToken);
        var volunteerIds = volunteers.Select(v => v.Id).ToList();

        // Get attendance aggregates
        var attendanceData = await _context.Attendances
            .Where(a => volunteerIds.Contains(a.VolunteerId))
            .Where(a => a.CheckInTime >= request.FromDate && a.CheckInTime <= request.ToDate)
            .Where(a => a.CheckOutTime.HasValue)
            .GroupBy(a => a.VolunteerId)
            .Select(g => new
            {
                VolunteerId = g.Key,
                TotalHours = g.Sum(a => a.HoursWorked ?? 0),
                TotalDays = g.Select(a => a.CheckInTime.Date).Distinct().Count(),
                AttendanceCount = g.Count(),
                ApprovedCount = g.Count(a => a.IsApproved),
                PendingCount = g.Count(a => !a.IsApproved)
            })
            .ToDictionaryAsync(x => x.VolunteerId, cancellationToken);

        var result = volunteers.Select(v =>
        {
            var hasData = attendanceData.TryGetValue(v.Id, out var data);
            return new VolunteerHoursSummaryItemDto(
                VolunteerId: v.Id,
                VolunteerName: v.FullName,
                Email: v.Email,
                Status: v.Status.ToString(),
                TotalHoursWorked: hasData ? data!.TotalHours : 0,
                TotalDaysWorked: hasData ? data!.TotalDays : 0,
                AttendanceCount: hasData ? data!.AttendanceCount : 0,
                ApprovedCount: hasData ? data!.ApprovedCount : 0,
                PendingApprovalCount: hasData ? data!.PendingCount : 0
            );
        })
        .OrderByDescending(x => x.TotalHoursWorked)
        .ToList();

        return Result.Success(result);
    }
}

// ============================================
// Validator - Get Volunteer Hours Summary
// ============================================
public class GetVolunteerHoursSummaryValidator : AbstractValidator<GetVolunteerHoursSummaryQuery>
{
    private static readonly string[] ValidStatuses = Enum.GetNames<Domain.Volunteers.VolunteerStatus>();

    public GetVolunteerHoursSummaryValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("Data początkowa jest wymagana");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("Data końcowa jest wymagana")
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("Data końcowa musi być większa lub równa dacie początkowej");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status musi być jednym z: {string.Join(", ", ValidStatuses)}");
    }
}
