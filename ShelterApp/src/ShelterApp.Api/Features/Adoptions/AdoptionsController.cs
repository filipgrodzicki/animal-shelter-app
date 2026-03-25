using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Adoptions.Commands;
using ShelterApp.Api.Features.Adoptions.Queries;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Common;

namespace ShelterApp.Api.Features.Adoptions;

/// <summary>
/// Animal adoption process management
/// </summary>
/// <remarks>
/// Controller handles the full adoption application lifecycle:
/// - Submitting applications (online and walk-in)
/// - Reviewing and approving/rejecting applications
/// - Scheduling and recording visits
/// - Generating and signing contracts
/// - Finalizing adoptions
/// </remarks>
[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
[Authorize]
public class AdoptionsController : ApiController
{
    #region Queries

    /// <summary>
    /// Gets adoption applications for the currently logged-in user
    /// </summary>
    /// <param name="status">Filter by application status</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of user's applications</returns>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<AdoptionApplicationListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyApplications(
        [FromQuery] AdoptionApplicationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var query = new GetMyAdoptionApplicationsQuery(userId, status, page, pageSize);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets a filtered and paginated list of adoption applications
    /// </summary>
    /// <param name="status">Filter by application status</param>
    /// <param name="adopterId">Filter by adopter ID</param>
    /// <param name="animalId">Filter by animal ID</param>
    /// <param name="fromDate">Start date of range</param>
    /// <param name="toDate">End date of range</param>
    /// <param name="searchTerm">Search by adopter name, email, or animal name</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    /// <param name="sortBy">Sort field: applicationDate, status, completionDate, scheduledVisitDate</param>
    /// <param name="sortDescending">Descending sort (default true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of adoption applications</returns>
    /// <remarks>
    /// Example queries:
    ///
    ///     GET /api/adoptions?status=Submitted&amp;page=1&amp;pageSize=10
    ///     GET /api/adoptions?fromDate=2024-01-01&amp;toDate=2024-12-31
    ///     GET /api/adoptions?searchTerm=Kowalski&amp;sortBy=applicationDate
    ///
    /// Available statuses: Submitted, UnderReview, Accepted, Rejected, VisitScheduled,
    /// VisitCompleted, PendingFinalization, Completed, Cancelled
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(PagedResult<AdoptionApplicationListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetApplications(
        [FromQuery] AdoptionApplicationStatus? status,
        [FromQuery] Guid? adopterId,
        [FromQuery] Guid? animalId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "applicationDate",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAdoptionApplicationsQuery(
            status, adopterId, animalId, fromDate, toDate, searchTerm,
            page, pageSize, sortBy, sortDescending);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets adoption application details
    /// </summary>
    /// <param name="id">Application identifier</param>
    /// <param name="includeStatusHistory">Whether to include status change history</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Adoption application details</returns>
    /// <remarks>
    /// Returns full application information including:
    /// - Adopter data
    /// - Animal data
    /// - Status history (optional)
    /// - Permitted actions for current status
    ///
    /// Example:
    ///
    ///     GET /api/adoptions/3fa85f64-5717-4562-b3fc-2c963f66afa6?includeStatusHistory=true
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdoptionApplicationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetApplicationById(
        Guid id,
        [FromQuery] bool includeStatusHistory = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAdoptionApplicationByIdQuery(id, includeStatusHistory);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets the adoption contract as a PDF
    /// </summary>
    /// <param name="id">Adoption application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file with the adoption contract</returns>
    /// <remarks>
    /// The contract can only be downloaded for applications with status 'PendingFinalization' or 'Completed'.
    /// The contract must first be generated via the PUT /{id}/generate-contract endpoint.
    ///
    /// Returned file format: Umowa_Adopcyjna_{AnimalName}_{ContractNumber}.pdf
    /// </remarks>
    [HttpGet("{id:guid}/contract")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetContract(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetAdoptionContractQuery(id);
        var result = await Sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        var contract = result.Value;
        return File(contract.PdfContent, contract.ContentType, contract.FileName);
    }

    #endregion

    #region Submit Application

    /// <summary>
    /// Submits a new online adoption application
    /// </summary>
    /// <param name="command">Adoption application data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submission result with ID and statuses</returns>
    /// <remarks>
    /// Endpoint for users submitting applications via the online form.
    /// Authentication is required.
    ///
    /// After submission:
    /// - Application status: Submitted
    /// - Animal status: Reserved
    /// - A confirmation email is sent
    ///
    /// Example:
    ///
    ///     POST /api/adoptions
    ///     {
    ///       "animalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "adopterId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///       "adoptionMotivation": "Chcę dać psu kochający dom",
    ///       "petExperience": "Miałem psa przez 10 lat",
    ///       "livingConditions": "Dom z ogrodem",
    ///       "otherPetsInfo": "Brak innych zwierząt"
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(SubmitApplicationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitApplication(
        [FromBody] SubmitAdoptionApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Submits a walk-in adoption application (by staff)
    /// </summary>
    /// <param name="command">Application data with client details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submission result</returns>
    /// <remarks>
    /// Endpoint for staff receiving walk-in clients.
    /// Staff enters the adopter's data and the application is automatically
    /// taken for review.
    ///
    /// Differences from online applications:
    /// - Email confirmation can be skipped (skipEmailConfirmation=true)
    /// - Application is automatically assigned to the staff member
    /// - Adopter data is entered by the staff member
    ///
    /// Example:
    ///
    ///     POST /api/adoptions/walk-in
    ///     {
    ///       "animalId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "firstName": "Jan",
    ///       "lastName": "Kowalski",
    ///       "email": "jan.kowalski@example.com",
    ///       "phone": "+48123456789",
    ///       "dateOfBirth": "1990-01-15",
    ///       "address": "ul. Przykładowa 1",
    ///       "city": "Warszawa",
    ///       "postalCode": "00-001",
    ///       "rodoConsent": true,
    ///       "motivation": "Szukam towarzysza",
    ///       "staffUserId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
    ///       "staffName": "Anna Nowak",
    ///       "skipEmailConfirmation": false
    ///     }
    /// </remarks>
    [HttpPost("walk-in")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(SubmitApplicationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitWalkInApplication(
        [FromBody] WalkInApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Review Process

    /// <summary>
    /// Takes an application for review
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Staff member data taking the application</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    /// <remarks>
    /// Changes application status from 'Submitted' to 'UnderReview'.
    /// Assigns the application to the staff member.
    ///
    /// Required permissions: Staff, Admin
    /// </remarks>
    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TakeForReview(
        Guid id,
        [FromBody] TakeForReviewRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TakeApplicationForReviewCommand(id, request.ReviewerUserId, request.ReviewerName);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Approves an adoption application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Approval data with optional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    /// <remarks>
    /// Changes application status from 'UnderReview' to 'Accepted'.
    /// After approval, an adoption visit can be scheduled.
    ///
    /// Required permissions: Staff, Admin
    /// </remarks>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveApplication(
        Guid id,
        [FromBody] ApproveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ApproveApplicationCommand(id, request.ReviewerName, request.Notes);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Rejects an adoption application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Rejection data with required reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    /// <remarks>
    /// Changes application status to 'Rejected'.
    /// A rejection reason is required and will be saved in the application.
    /// The animal is released (status: Available).
    ///
    /// Required permissions: Staff, Admin
    /// </remarks>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectApplication(
        Guid id,
        [FromBody] RejectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RejectApplicationCommand(id, request.ReviewerName, request.Reason);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Visit Management

    /// <summary>
    /// Schedules an adoption visit
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Visit data with date and notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    /// <remarks>
    /// Schedules a visit for an application with status 'Accepted'.
    /// Changes application status to 'VisitScheduled'.
    ///
    /// Available time slots can be checked via GET /api/appointments/available
    ///
    /// Example:
    ///
    ///     PUT /api/adoptions/{id}/schedule-visit
    ///     {
    ///       "visitDate": "2024-02-15T10:00:00",
    ///       "scheduledByName": "Anna Nowak",
    ///       "notes": "Proszę zabrać dokument tożsamości"
    ///     }
    /// </remarks>
    [HttpPut("{id:guid}/schedule-visit")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ScheduleVisit(
        Guid id,
        [FromBody] ScheduleVisitRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ScheduleVisitCommand(id, request.VisitDate, request.ScheduledByName, request.Notes);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Records the adopter's attendance at the visit
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Staff member conducting the visit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    /// <remarks>
    /// Records that the adopter showed up for the scheduled visit.
    /// After this step, the visit result must be recorded.
    ///
    /// Required permissions: Staff, Admin
    /// </remarks>
    [HttpPut("{id:guid}/record-attendance")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordAttendance(
        Guid id,
        [FromBody] RecordAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordVisitAttendanceCommand(id, request.ConductedByUserId, request.ConductedByName);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Records the adoption visit result
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Visit result with assessment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    /// <remarks>
    /// Records the result of the conducted visit:
    /// - isPositive: true = adoption process continues
    /// - isPositive: false = application rejected
    /// - assessment: rating on a 1-5 scale
    /// - notes: detailed visit notes
    ///
    /// On a positive result, status changes to 'VisitCompleted'.
    /// On a negative result, status changes to 'Rejected'.
    ///
    /// Required permissions: Staff, Admin
    ///
    /// Example:
    ///
    ///     PUT /api/adoptions/{id}/record-visit
    ///     {
    ///       "isPositive": true,
    ///       "assessment": 5,
    ///       "notes": "Warunki mieszkaniowe bardzo dobre, ogród ogrodzony",
    ///       "recordedByName": "Anna Nowak"
    ///     }
    /// </remarks>
    [HttpPut("{id:guid}/record-visit")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordVisitResult(
        Guid id,
        [FromBody] RecordVisitResultRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordVisitResultCommand(id, request.IsPositive, request.Assessment, request.Notes, request.RecordedByName);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Contract & Finalization

    /// <summary>
    /// Generates an adoption contract
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Data of the person generating the contract</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application with contract number</returns>
    /// <remarks>
    /// Generates an adoption contract in PDF format.
    /// Application status changes to 'PendingFinalization'.
    ///
    /// Once generated, the contract can be downloaded via GET /{id}/contract
    ///
    /// Required permissions: Staff, Admin
    /// </remarks>
    [HttpPut("{id:guid}/generate-contract")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateContract(
        Guid id,
        [FromBody] GenerateContractRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateContractCommand(id, request.GeneratedByName);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Finalizes the adoption (contract signing)
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Finalization data with signed contract file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    /// <remarks>
    /// Completes the adoption process after the contract is signed by both parties.
    /// Application status changes to 'Completed'.
    /// Animal status changes to 'Adopted'.
    ///
    /// Required permissions: Staff, Admin
    ///
    /// Example:
    ///
    ///     PUT /api/adoptions/{id}/complete
    ///     {
    ///       "contractFilePath": "/contracts/2024/umowa_12345.pdf",
    ///       "signedByName": "Anna Nowak"
    ///     }
    /// </remarks>
    [HttpPut("{id:guid}/complete")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> FinalizeAdoption(
        Guid id,
        [FromBody] FinalizeAdoptionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new FinalizeAdoptionCommand(id, request.ContractFilePath, request.SignedByName);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Cancellation

    /// <summary>
    /// Cancels an adoption application
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Cancellation data with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated application</returns>
    /// <remarks>
    /// Cancels an application at any stage of the process (before finalization).
    /// Can be invoked by the user or staff member.
    ///
    /// After cancellation:
    /// - Application status: Cancelled
    /// - Animal status: Available (released)
    ///
    /// Example:
    ///
    ///     PUT /api/adoptions/{id}/cancel
    ///     {
    ///       "reason": "Zmiana sytuacji życiowej",
    ///       "userName": "Jan Kowalski"
    ///     }
    /// </remarks>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(typeof(AdoptionApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelApplication(
        Guid id,
        [FromBody] CancelApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CancelApplicationByUserCommand(id, request.Reason, request.UserName);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Data for taking an application for review
/// </summary>
public record TakeForReviewRequest(
    /// <summary>ID of the user taking the application</summary>
    Guid ReviewerUserId,
    /// <summary>Staff member's full name</summary>
    string ReviewerName
);

/// <summary>
/// Data for approving an application
/// </summary>
public record ApproveRequest(
    /// <summary>Approver's full name</summary>
    string ReviewerName,
    /// <summary>Optional notes</summary>
    string? Notes
);

/// <summary>
/// Data for rejecting an application
/// </summary>
public record RejectRequest(
    /// <summary>Rejector's full name</summary>
    string ReviewerName,
    /// <summary>Rejection reason (required)</summary>
    string Reason
);

/// <summary>
/// Data for scheduling an adoption visit
/// </summary>
public record ScheduleVisitRequest(
    /// <summary>Visit date and time</summary>
    DateTime VisitDate,
    /// <summary>Scheduler's full name</summary>
    string ScheduledByName,
    /// <summary>Optional notes for the adopter</summary>
    string? Notes
);

/// <summary>
/// Data for recording visit attendance
/// </summary>
public record RecordAttendanceRequest(
    /// <summary>ID of the user conducting the visit</summary>
    Guid ConductedByUserId,
    /// <summary>Staff member's full name</summary>
    string ConductedByName
);

/// <summary>
/// Adoption visit result data
/// </summary>
public record RecordVisitResultRequest(
    /// <summary>Whether the visit was positive</summary>
    bool IsPositive,
    /// <summary>Rating on a 1-5 scale</summary>
    int Assessment,
    /// <summary>Detailed visit notes</summary>
    string Notes,
    /// <summary>Name of the person recording the result</summary>
    string RecordedByName
);

/// <summary>
/// Data for generating a contract
/// </summary>
public record GenerateContractRequest(
    /// <summary>Name of the person generating the contract</summary>
    string GeneratedByName
);

/// <summary>
/// Data for finalizing the adoption
/// </summary>
public record FinalizeAdoptionRequest(
    /// <summary>Path to the signed contract file</summary>
    string ContractFilePath,
    /// <summary>Name of the person finalizing</summary>
    string SignedByName
);

/// <summary>
/// Data for cancelling an application
/// </summary>
public record CancelApplicationRequest(
    /// <summary>Cancellation reason</summary>
    string Reason,
    /// <summary>Name of the person cancelling</summary>
    string UserName
);

#endregion
