using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Volunteers.Commands;
using ShelterApp.Api.Features.Volunteers.Queries;
using ShelterApp.Api.Features.Volunteers.Shared;

namespace ShelterApp.Api.Features.Volunteers;

/// <summary>
/// Volunteer schedule management (WB-17)
/// </summary>
/// <remarks>
/// Controller handles:
/// - Creating and managing time slots
/// - Assigning volunteers to slots
/// - Viewing the schedule
/// </remarks>
[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
[Authorize]
public class ScheduleController : ApiController
{
    #region Queries

    /// <summary>
    /// Gets the schedule for a date range
    /// </summary>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="activeOnly">Active slots only (default true)</param>
    /// <param name="includeAssignments">Include volunteer assignments (default true)</param>
    /// <param name="volunteerId">Filter by volunteer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of time slots</returns>
    /// <remarks>
    /// Returns all slots in the given date range (max 90 days).
    ///
    /// Example:
    ///
    ///     GET /api/schedule?fromDate=2024-01-01&amp;toDate=2024-01-31
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSchedule(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] bool? activeOnly = true,
        [FromQuery] bool includeAssignments = true,
        [FromQuery] Guid? volunteerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetScheduleQuery(fromDate, toDate, activeOnly, includeAssignments, volunteerId);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets my schedule as a volunteer
    /// </summary>
    /// <param name="volunteerId">Volunteer ID</param>
    /// <param name="fromDate">Start date (default 7 days back)</param>
    /// <param name="toDate">End date (default 30 days ahead)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of volunteer assignments</returns>
    /// <remarks>
    /// Returns the schedule of a specific volunteer with attendance information.
    ///
    /// Required permissions: Volunteer
    /// </remarks>
    [HttpGet("my")]
    [Authorize(Roles = "Volunteer")]
    [ProducesResponseType(typeof(List<VolunteerScheduleItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMySchedule(
        [FromQuery] Guid volunteerId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyScheduleQuery(volunteerId, fromDate, toDate);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Slot Management

    /// <summary>
    /// Creates a new time slot
    /// </summary>
    /// <param name="command">Slot data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created slot</returns>
    /// <remarks>
    /// Creates a single time slot in the schedule.
    ///
    /// Required permissions: Admin
    ///
    /// Example:
    ///
    ///     POST /api/schedule/slots
    ///     {
    ///       "date": "2024-02-15",
    ///       "startTime": "08:00",
    ///       "endTime": "12:00",
    ///       "maxVolunteers": 3,
    ///       "description": "Poranne spacery z psami",
    ///       "createdByUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    ///     }
    /// </remarks>
    [HttpPost("slots")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSlot(
        [FromBody] CreateScheduleSlotCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates multiple time slots (bulk)
    /// </summary>
    /// <param name="command">Slots data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Slot creation result</returns>
    /// <remarks>
    /// Creates multiple slots based on a date range and pattern.
    ///
    /// Required permissions: Admin
    ///
    /// Example:
    ///
    ///     POST /api/schedule/slots/bulk
    ///     {
    ///       "startDate": "2024-02-01",
    ///       "endDate": "2024-02-28",
    ///       "daysOfWeek": [1, 2, 3, 4, 5],
    ///       "startTime": "08:00",
    ///       "endTime": "12:00",
    ///       "maxVolunteers": 3,
    ///       "description": "Poranne spacery z psami",
    ///       "createdByUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    ///     }
    /// </remarks>
    [HttpPost("slots/bulk")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateScheduleSlotsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSlotsBulk(
        [FromBody] CreateScheduleSlotsBulkCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Assignments

    /// <summary>
    /// Assigns a volunteer to a slot
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="request">Assignment data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created assignment</returns>
    /// <remarks>
    /// Assigns a volunteer to a slot with status 'Pending'.
    /// The volunteer must be active and the slot must have available places.
    ///
    /// Required permissions: Admin
    /// </remarks>
    [HttpPost("slots/{slotId:guid}/assign")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VolunteerAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignVolunteerToSlot(
        Guid slotId,
        [FromBody] AssignVolunteerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignVolunteerToSlotCommand(slotId, request.VolunteerId, request.AssignedByUserId);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Confirms a volunteer assignment
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="assignmentId">Assignment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated assignment</returns>
    [HttpPut("slots/{slotId:guid}/assignments/{assignmentId:guid}/confirm")]
    [Authorize(Roles = "Admin,Volunteer")]
    [ProducesResponseType(typeof(VolunteerAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmAssignment(
        Guid slotId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmAssignmentCommand(assignmentId);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Cancels a volunteer assignment
    /// </summary>
    /// <param name="slotId">Slot ID</param>
    /// <param name="assignmentId">Assignment ID</param>
    /// <param name="request">Cancellation data with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated assignment</returns>
    [HttpPut("slots/{slotId:guid}/assignments/{assignmentId:guid}/cancel")]
    [Authorize(Roles = "Admin,Volunteer")]
    [ProducesResponseType(typeof(VolunteerAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelAssignment(
        Guid slotId,
        Guid assignmentId,
        [FromBody] CancelAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CancelAssignmentCommand(assignmentId, request.Reason);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Data for assigning a volunteer
/// </summary>
public record AssignVolunteerRequest(
    /// <summary>Volunteer ID</summary>
    Guid VolunteerId,
    /// <summary>Assigning user ID</summary>
    Guid AssignedByUserId
);

/// <summary>
/// Data for cancelling an assignment
/// </summary>
public record CancelAssignmentRequest(
    /// <summary>Cancellation reason</summary>
    string Reason
);

#endregion
