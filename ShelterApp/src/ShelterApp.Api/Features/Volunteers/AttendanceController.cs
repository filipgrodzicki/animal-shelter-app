using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Volunteers.Commands;
using ShelterApp.Api.Features.Volunteers.Queries;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;

namespace ShelterApp.Api.Features.Volunteers;

/// <summary>
/// Volunteer attendance tracking (WB-18)
/// </summary>
/// <remarks>
/// Controller handles:
/// - Volunteer check-in and check-out
/// - Attendance approval
/// - Work time corrections
/// </remarks>
[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
[Authorize]
public class AttendanceController : ApiController
{
    #region Queries

    /// <summary>
    /// Gets the current (active) attendance for a volunteer
    /// </summary>
    /// <param name="volunteerId">Volunteer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active attendance or null if none</returns>
    /// <remarks>
    /// Returns an attendance record that has no CheckOutTime set yet.
    /// Used to check if a volunteer is currently checked in.
    ///
    /// Required permissions: Volunteer
    /// </remarks>
    [HttpGet("current/{volunteerId:guid}")]
    [Authorize(Roles = "Volunteer,Staff,Admin")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentAttendance(
        Guid volunteerId,
        CancellationToken cancellationToken)
    {
        var query = new GetCurrentAttendanceQuery(volunteerId);
        var result = await Sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        if (result.Value is null)
        {
            return NoContent();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets volunteer attendance history
    /// </summary>
    /// <param name="volunteerId">Volunteer ID</param>
    /// <param name="fromDate">Start date (optional)</param>
    /// <param name="toDate">End date (optional)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of attendance records</returns>
    /// <remarks>
    /// Required permissions: Volunteer (own data), Staff/Admin (all)
    /// </remarks>
    [HttpGet("volunteer/{volunteerId:guid}")]
    [Authorize(Roles = "Volunteer,Staff,Admin")]
    [ProducesResponseType(typeof(PagedResult<AttendanceListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVolunteerAttendances(
        Guid volunteerId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVolunteerAttendancesQuery(volunteerId, fromDate, toDate, page, pageSize);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Check-in/Check-out

    /// <summary>
    /// Volunteer check-in (start of shift)
    /// </summary>
    /// <param name="request">Check-in data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Check-in result</returns>
    /// <remarks>
    /// Records the start of a volunteer's shift.
    /// Optionally, a schedule slot ID can be provided.
    ///
    /// Required permissions: Volunteer
    ///
    /// Example:
    ///
    ///     POST /api/attendance/check-in
    ///     {
    ///       "volunteerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "scheduleSlotId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///       "notes": "Rozpoczynam poranną zmianę"
    ///     }
    /// </remarks>
    [HttpPost("check-in")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(CheckInResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CheckInCommand(request.VolunteerId, request.ScheduleSlotId, request.Notes);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Volunteer check-out (end of shift)
    /// </summary>
    /// <param name="request">Check-out data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Check-out result with hours worked</returns>
    /// <remarks>
    /// Records the end of a volunteer's shift.
    /// Automatically calculates hours worked.
    ///
    /// Required permissions: Volunteer
    ///
    /// Example:
    ///
    ///     POST /api/attendance/check-out
    ///     {
    ///       "attendanceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "workDescription": "Spacery z 5 psami, karmienie kotów"
    ///     }
    /// </remarks>
    [HttpPost("check-out")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(CheckOutResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckOut(
        [FromBody] CheckOutRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CheckOutCommand(request.AttendanceId, request.WorkDescription);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Management

    /// <summary>
    /// Approves an attendance record
    /// </summary>
    /// <param name="id">Attendance record ID</param>
    /// <param name="request">Approval data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated record</returns>
    /// <remarks>
    /// Approves an attendance record after the shift ends.
    /// Only approved records count towards statistics.
    ///
    /// Required permissions: Staff, Admin
    /// </remarks>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveAttendance(
        Guid id,
        [FromBody] ApproveAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ApproveAttendanceCommand(id, request.ApprovedByUserId);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Corrects attendance times
    /// </summary>
    /// <param name="id">Attendance record ID</param>
    /// <param name="request">Correction data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated record</returns>
    /// <remarks>
    /// Allows correcting check-in/check-out times.
    /// Approved records cannot be corrected.
    ///
    /// Required permissions: Staff, Admin
    ///
    /// Example:
    ///
    ///     PUT /api/attendance/{id}/correct
    ///     {
    ///       "checkInTime": "2024-02-15T08:00:00",
    ///       "checkOutTime": "2024-02-15T12:00:00",
    ///       "correctionNotes": "Korekta - wolontariusz zapomniał się zameldować"
    ///     }
    /// </remarks>
    [HttpPut("{id:guid}/correct")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CorrectAttendance(
        Guid id,
        [FromBody] CorrectAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CorrectAttendanceCommand(id, request.CheckInTime, request.CheckOutTime, request.CorrectionNotes);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Manual attendance entry
    /// </summary>
    /// <param name="request">Entry data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created record</returns>
    /// <remarks>
    /// Allows manually entering an attendance record for a volunteer.
    /// Used when a volunteer forgot to check in.
    ///
    /// Required permissions: Staff, Admin
    ///
    /// Example:
    ///
    ///     POST /api/attendance/manual
    ///     {
    ///       "volunteerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "scheduleSlotId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///       "checkInTime": "2024-02-15T08:00:00",
    ///       "checkOutTime": "2024-02-15T12:00:00",
    ///       "workDescription": "Spacery z psami",
    ///       "notes": "Wpis ręczny - wolontariusz zapomniał się zameldować",
    ///       "enteredByUserId": "5fa85f64-5717-4562-b3fc-2c963f66afa8"
    ///     }
    /// </remarks>
    [HttpPost("manual")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ManualEntry(
        [FromBody] ManualAttendanceEntryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Check-in data
/// </summary>
public record CheckInRequest(
    /// <summary>Volunteer ID</summary>
    Guid VolunteerId,
    /// <summary>Schedule slot ID (optional)</summary>
    Guid? ScheduleSlotId = null,
    /// <summary>Notes</summary>
    string? Notes = null
);

/// <summary>
/// Check-out data
/// </summary>
public record CheckOutRequest(
    /// <summary>Attendance record ID</summary>
    Guid AttendanceId,
    /// <summary>Description of work performed</summary>
    string? WorkDescription = null
);

/// <summary>
/// Attendance approval data
/// </summary>
public record ApproveAttendanceRequest(
    /// <summary>Approving user ID</summary>
    Guid ApprovedByUserId
);

/// <summary>
/// Attendance correction data
/// </summary>
public record CorrectAttendanceRequest(
    /// <summary>Corrected check-in time</summary>
    DateTime? CheckInTime = null,
    /// <summary>Corrected check-out time</summary>
    DateTime? CheckOutTime = null,
    /// <summary>Correction notes</summary>
    string? CorrectionNotes = null
);

#endregion
