using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Volunteers.Commands;
using ShelterApp.Api.Features.Volunteers.Queries;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;

namespace ShelterApp.Api.Features.Volunteers;

/// <summary>
/// Volunteer management
/// </summary>
/// <remarks>
/// Controller handles the full volunteer lifecycle:
/// - Registering new volunteers
/// - Recruitment and training process
/// - Status management
/// - Viewing volunteer data
/// </remarks>
[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
[Authorize]
public class VolunteersController : ApiController
{
    #region Queries

    /// <summary>
    /// Gets volunteer data for the logged-in user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Volunteer details</returns>
    /// <remarks>
    /// Endpoint for volunteers to retrieve their own data.
    /// Required permissions: Volunteer
    /// </remarks>
    [HttpGet("me")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(VolunteerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyVolunteer(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetMyVolunteerQuery(userId.Value);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets a filtered and paginated list of volunteers
    /// </summary>
    /// <param name="status">Filter by volunteer status</param>
    /// <param name="searchTerm">Search by first name, last name, email, or phone</param>
    /// <param name="skills">Filter by skills</param>
    /// <param name="availableOn">Filter by availability on a given day of week</param>
    /// <param name="sortBy">Sort field: name, email, status, applicationdate, totalhours</param>
    /// <param name="sortDescending">Descending sort (default true)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of volunteers</returns>
    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(PagedResult<VolunteerListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVolunteers(
        [FromQuery] string? status,
        [FromQuery] string? searchTerm,
        [FromQuery] IEnumerable<string>? skills,
        [FromQuery] DayOfWeek? availableOn,
        [FromQuery] string sortBy = "ApplicationDate",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVolunteersQuery(status, searchTerm, skills, availableOn, sortBy, sortDescending, page, pageSize);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets volunteer details
    /// </summary>
    /// <param name="id">Volunteer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Volunteer details with status history and recent activity</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VolunteerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVolunteerById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVolunteerByIdQuery(id);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets a volunteer hours report
    /// </summary>
    /// <param name="id">Volunteer ID</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="format">Report format: json, csv, pdf (html)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Volunteer hours report</returns>
    [HttpGet("{id:guid}/hours-report")]
    [Authorize(Roles = "Staff,Admin,Volunteer")]
    [ProducesResponseType(typeof(VolunteerHoursReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVolunteerHoursReport(
        Guid id,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        var query = new GetVolunteerHoursReportQuery(id, fromDate, toDate, format);
        var result = await Sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var report = result.Value;

        // Return file for CSV/PDF formats
        if (!string.IsNullOrEmpty(report.ReportContent) && !string.IsNullOrEmpty(report.ContentType))
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(report.ReportContent);
            return File(bytes, report.ContentType, report.FileName);
        }

        return Ok(report);
    }

    /// <summary>
    /// Gets hours summary for all volunteers
    /// </summary>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of volunteer hours summaries</returns>
    [HttpGet("hours-summary")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(List<VolunteerHoursSummaryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVolunteerHoursSummary(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVolunteerHoursSummaryQuery(fromDate, toDate, status);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Generates a volunteer work certificate (PDF)
    /// </summary>
    /// <param name="id">Volunteer ID</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF certificate file</returns>
    [HttpGet("{id:guid}/certificate")]
    [Authorize(Roles = "Staff,Admin")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVolunteerCertificate(
        Guid id,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVolunteerCertificateQuery(id, fromDate, toDate);
        var result = await Sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var certificate = result.Value;
        return File(certificate.Content, certificate.ContentType, certificate.FileName);
    }

    #endregion

    #region Helpers

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    #endregion

    #region Registration

    /// <summary>
    /// Registers a new volunteer
    /// </summary>
    /// <param name="command">Volunteer registration data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registered volunteer</returns>
    /// <remarks>
    /// Creates a new volunteer with status 'Candidate'.
    /// Publicly accessible endpoint - no authorization required.
    ///
    /// Example:
    ///
    ///     POST /api/volunteers
    ///     {
    ///       "firstName": "Jan",
    ///       "lastName": "Kowalski",
    ///       "email": "jan.kowalski@example.com",
    ///       "phone": "+48123456789",
    ///       "dateOfBirth": "1990-01-15",
    ///       "address": "ul. Przykładowa 1",
    ///       "city": "Warszawa",
    ///       "postalCode": "00-001",
    ///       "emergencyContactName": "Anna Kowalska",
    ///       "emergencyContactPhone": "+48987654321",
    ///       "skills": ["opieka nad psami", "spacery"],
    ///       "availability": [1, 2, 5],
    ///       "notes": "Chcę pomagać zwierzętom"
    ///     }
    /// </remarks>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VolunteerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterVolunteer(
        [FromBody] RegisterVolunteerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Approves a volunteer application and starts training
    /// </summary>
    /// <param name="id">Volunteer ID</param>
    /// <param name="request">Approval data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated volunteer</returns>
    /// <remarks>
    /// Changes volunteer status from 'Candidate' to 'InTraining'.
    /// Sets the training start date.
    ///
    /// Required permissions: Admin
    /// </remarks>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VolunteerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveVolunteerApplication(
        Guid id,
        [FromBody] ApproveVolunteerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ApproveVolunteerApplicationCommand(
            id, request.ApprovedByUserId, request.ApprovedByName, request.TrainingStartDate, request.Notes);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Rejects a volunteer application
    /// </summary>
    /// <param name="id">Volunteer ID</param>
    /// <param name="request">Rejection data with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated volunteer</returns>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VolunteerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectVolunteerApplication(
        Guid id,
        [FromBody] RejectVolunteerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RejectVolunteerApplicationCommand(id, request.RejectedByName, request.Reason);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Completes volunteer training and activates them
    /// </summary>
    /// <param name="id">Volunteer ID</param>
    /// <param name="request">Training completion data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated volunteer</returns>
    /// <remarks>
    /// Changes volunteer status from 'InTraining' to 'Active'.
    /// Generates contract number and sets signing date.
    ///
    /// Required permissions: Admin
    /// </remarks>
    [HttpPut("{id:guid}/complete-training")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VolunteerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CompleteTraining(
        Guid id,
        [FromBody] CompleteTrainingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteTrainingCommand(
            id, request.CompletedByName, request.ContractNumber, request.TrainingEndDate);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Suspends a volunteer
    /// </summary>
    /// <param name="id">Volunteer ID</param>
    /// <param name="request">Suspension data with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated volunteer</returns>
    [HttpPut("{id:guid}/suspend")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VolunteerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SuspendVolunteer(
        Guid id,
        [FromBody] SuspendVolunteerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SuspendVolunteerCommand(id, request.SuspendedByName, request.Reason);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Resumes a suspended volunteer
    /// </summary>
    /// <param name="id">Volunteer ID</param>
    /// <param name="request">Resumption data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated volunteer</returns>
    [HttpPut("{id:guid}/resume")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VolunteerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResumeVolunteer(
        Guid id,
        [FromBody] ResumeVolunteerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResumeVolunteerCommand(id, request.ResumedByName, request.Notes);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Data for approving a volunteer
/// </summary>
public record ApproveVolunteerRequest(
    /// <summary>Approving user ID</summary>
    Guid ApprovedByUserId,
    /// <summary>Approver's full name</summary>
    string ApprovedByName,
    /// <summary>Training start date (optional)</summary>
    DateTime? TrainingStartDate = null,
    /// <summary>Notes</summary>
    string? Notes = null
);

/// <summary>
/// Data for rejecting a volunteer
/// </summary>
public record RejectVolunteerRequest(
    /// <summary>Rejector's full name</summary>
    string RejectedByName,
    /// <summary>Rejection reason</summary>
    string Reason
);

/// <summary>
/// Data for completing training
/// </summary>
public record CompleteTrainingRequest(
    /// <summary>Name of the person completing the training</summary>
    string CompletedByName,
    /// <summary>Volunteer contract number</summary>
    string ContractNumber,
    /// <summary>Training end date (optional)</summary>
    DateTime? TrainingEndDate = null
);

/// <summary>
/// Data for suspending a volunteer
/// </summary>
public record SuspendVolunteerRequest(
    /// <summary>Name of the person suspending</summary>
    string SuspendedByName,
    /// <summary>Suspension reason</summary>
    string Reason
);

/// <summary>
/// Data for resuming a volunteer
/// </summary>
public record ResumeVolunteerRequest(
    /// <summary>Name of the person resuming</summary>
    string ResumedByName,
    /// <summary>Notes</summary>
    string? Notes = null
);

#endregion
