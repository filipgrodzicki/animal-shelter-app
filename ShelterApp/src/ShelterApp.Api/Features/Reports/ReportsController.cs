using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Reports.Queries;
using ShelterApp.Api.Features.Reports.Shared;

namespace ShelterApp.Api.Features.Reports;

/// <summary>
/// Raporty i statystyki schroniska (WF-32, WF-33)
/// </summary>
[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
[Authorize(Roles = "Staff,Admin")]
public class ReportsController : ApiController
{
    #region Statistics (WF-32)

    /// <summary>
    /// Pobiera ogólne statystyki schroniska za dany okres
    /// </summary>
    /// <param name="fromDate">Data początkowa okresu</param>
    /// <param name="toDate">Data końcowa okresu</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Statystyki adopcji, wolontariuszy i zwierząt</returns>
    /// <remarks>
    /// Zwraca kompleksowe statystyki obejmujące:
    /// - Liczbę adopcji i ich rozkład czasowy
    /// - Aktywność wolontariuszy i przepracowane godziny
    /// - Stan populacji zwierząt w schronisku
    ///
    /// Przykład:
    ///
    ///     GET /api/reports/statistics?fromDate=2024-01-01&amp;toDate=2024-12-31
    ///
    /// Maksymalny zakres dat: 2 lata
    /// </remarks>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ShelterStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var query = new GetShelterStatisticsQuery(fromDate, toDate);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Government Reports (WF-33)

    /// <summary>
    /// Pobiera raport dla samorządu w formacie JSON, CSV lub PDF
    /// </summary>
    /// <param name="fromDate">Data początkowa okresu</param>
    /// <param name="toDate">Data końcowa okresu</param>
    /// <param name="format">Format raportu: json, csv, pdf (domyślnie: json)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Raport z zestawieniem przyjęć, adopcji i godzin wolontariuszy</returns>
    /// <remarks>
    /// Generuje raport wymagany przez jednostki samorządowe obejmujący:
    /// - Zestawienie przyjęć zwierząt
    /// - Zestawienie adopcji z danymi umów
    /// - Ewidencję godzin pracy wolontariuszy
    ///
    /// Dostępne formaty:
    /// - json: Dane strukturalne (domyślnie)
    /// - csv: Plik CSV do importu do arkusza kalkulacyjnego
    /// - pdf: Sformatowany dokument PDF do druku
    ///
    /// Przykłady:
    ///
    ///     GET /api/reports/government?fromDate=2024-01-01&amp;toDate=2024-03-31&amp;format=json
    ///     GET /api/reports/government?fromDate=2024-01-01&amp;toDate=2024-03-31&amp;format=csv
    ///     GET /api/reports/government?fromDate=2024-01-01&amp;toDate=2024-03-31&amp;format=pdf
    ///
    /// Maksymalny zakres dat: 1 rok
    /// </remarks>
    [HttpGet("government")]
    [ProducesResponseType(typeof(GovernmentReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGovernmentReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        var query = new GetGovernmentReportQuery(fromDate, toDate, format);
        var result = await Sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var report = result.Value;

        // Return file for CSV and PDF formats
        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase) && report.ReportContent != null)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(report.ReportContent);
            return File(bytes, report.ContentType!, report.FileName);
        }

        if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase) && report.ReportContent != null)
        {
            var bytes = Convert.FromBase64String(report.ReportContent);
            return File(bytes, report.ContentType!, report.FileName);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Pobiera raport przyjęć do schroniska w formacie CSV
    /// </summary>
    /// <param name="fromDate">Data początkowa okresu</param>
    /// <param name="toDate">Data końcowa okresu</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Plik CSV z listą przyjęć</returns>
    [HttpGet("admissions/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdmissionsCsv(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var query = new GetGovernmentReportQuery(fromDate, toDate, "csv");
        var result = await Sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var report = result.Value;
        var csvContent = GenerateAdmissionsCsv(report.Admissions);
        var bytes = GetCsvBytesWithBom(csvContent);
        var fileName = $"przyjecia_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// Pobiera raport adopcji w formacie CSV
    /// </summary>
    /// <param name="fromDate">Data początkowa okresu</param>
    /// <param name="toDate">Data końcowa okresu</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Plik CSV z listą adopcji</returns>
    [HttpGet("adoptions/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdoptionsCsv(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var query = new GetGovernmentReportQuery(fromDate, toDate, "csv");
        var result = await Sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var report = result.Value;
        var csvContent = GenerateAdoptionsCsv(report.Adoptions);
        var bytes = GetCsvBytesWithBom(csvContent);
        var fileName = $"adopcje_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// Pobiera ewidencję godzin wolontariuszy w formacie CSV
    /// </summary>
    /// <param name="fromDate">Data początkowa okresu</param>
    /// <param name="toDate">Data końcowa okresu</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Plik CSV z godzinami wolontariuszy</returns>
    [HttpGet("volunteer-hours/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVolunteerHoursCsv(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var query = new GetGovernmentReportQuery(fromDate, toDate, "csv");
        var result = await Sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var report = result.Value;
        var csvContent = GenerateVolunteerHoursCsv(report.VolunteerHours, report.ReportPeriod);
        var bytes = GetCsvBytesWithBom(csvContent);
        var fileName = $"godziny_wolontariuszy_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    #endregion

    #region Private helpers

    private static string GenerateAdmissionsCsv(List<AdmissionRecordDto> admissions)
    {
        var sb = new System.Text.StringBuilder();
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

        return sb.ToString();
    }

    private static string GenerateAdoptionsCsv(List<AdoptionRecordDto> adoptions)
    {
        var sb = new System.Text.StringBuilder();
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

        return sb.ToString();
    }

    private static string GenerateVolunteerHoursCsv(VolunteerHoursSummaryDto volunteerHours, string reportPeriod)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("EWIDENCJA GODZIN WOLONTARIUSZY");
        sb.AppendLine($"Okres:;{reportPeriod}");
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
                record.HoursWorked.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("pl-PL")),
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

    private static byte[] GetCsvBytesWithBom(string csvContent)
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF }; // UTF-8 BOM for Excel
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var result = new byte[bom.Length + contentBytes.Length];
        bom.CopyTo(result, 0);
        contentBytes.CopyTo(result, bom.Length);
        return result;
    }

    #endregion
}
