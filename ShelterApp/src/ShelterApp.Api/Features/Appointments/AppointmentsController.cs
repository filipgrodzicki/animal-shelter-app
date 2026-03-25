using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Appointments.Commands;
using ShelterApp.Api.Features.Appointments.Queries;
using ShelterApp.Api.Features.Appointments.Shared;

namespace ShelterApp.Api.Features.Appointments;

/// <summary>
/// Zarządzanie kalendarzem wizyt adopcyjnych
/// </summary>
[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
public class AppointmentsController : ApiController
{
    #region Queries

    /// <summary>
    /// Pobiera dostępne sloty wizyt w podanym zakresie dat (WS-15)
    /// </summary>
    /// <param name="from">Data początkowa (yyyy-MM-dd)</param>
    /// <param name="to">Data końcowa (yyyy-MM-dd)</param>
    /// <param name="includeFullSlots">Czy uwzględnić pełne sloty</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Lista dostępności pogrupowana po dniach</returns>
    /// <remarks>
    /// Przykładowe zapytanie:
    ///
    ///     GET /api/appointments/available?from=2024-01-01&amp;to=2024-01-31
    ///
    /// Zwraca listę dni z dostępnymi slotami wizyt.
    /// Maksymalny zakres dat to 90 dni.
    /// </remarks>
    [HttpGet("available")]
    [ProducesResponseType(typeof(List<DailyAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool includeFullSlots = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAvailableVisitSlotsQuery(from, to, includeFullSlots);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Slot Management

    /// <summary>
    /// Tworzy nowy slot czasowy na wizytę
    /// </summary>
    /// <param name="request">Dane slotu</param>
    /// <param name="cancellationToken">Token anulowania</param>
    [HttpPost("slots")]
    [ProducesResponseType(typeof(VisitSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSlot(
        [FromBody] CreateSlotRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateVisitSlotCommand(
            request.Date,
            request.StartTime,
            request.EndTime,
            request.MaxCapacity,
            request.Notes);

        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Tworzy wiele slotów naraz (np. na cały tydzień)
    /// </summary>
    /// <param name="request">Dane slotów</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <remarks>
    /// Przykład tworzenia slotów na tydzień roboczy:
    ///
    ///     POST /api/appointments/slots/bulk
    ///     {
    ///       "fromDate": "2024-01-01",
    ///       "toDate": "2024-01-07",
    ///       "templates": [
    ///         { "startTime": "09:00", "endTime": "10:00", "maxCapacity": 2, "daysOfWeek": [1,2,3,4,5] },
    ///         { "startTime": "10:00", "endTime": "11:00", "maxCapacity": 2, "daysOfWeek": [1,2,3,4,5] },
    ///         { "startTime": "14:00", "endTime": "15:00", "maxCapacity": 2, "daysOfWeek": [1,2,3,4,5] }
    ///       ]
    ///     }
    ///
    /// DaysOfWeek: 0=Sunday, 1=Monday, ..., 6=Saturday
    /// </remarks>
    [HttpPost("slots/bulk")]
    [ProducesResponseType(typeof(List<VisitSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSlotsBulk(
        [FromBody] CreateSlotsBulkRequest request,
        CancellationToken cancellationToken)
    {
        var templates = request.Templates.Select(t => new SlotTemplate(
            t.StartTime,
            t.EndTime,
            t.MaxCapacity,
            t.DaysOfWeek
        ));

        var command = new CreateVisitSlotsBulkCommand(
            request.FromDate,
            request.ToDate,
            templates);

        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Dezaktywuje slot (anuluje wszystkie rezerwacje)
    /// </summary>
    /// <param name="id">ID slotu</param>
    /// <param name="request">Dane dezaktywacji</param>
    /// <param name="cancellationToken">Token anulowania</param>
    [HttpPost("slots/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateSlot(
        Guid id,
        [FromBody] DeactivateSlotRequest request,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateSlotCommand(id, request.DeactivatedBy, request.Reason);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Bookings

    /// <summary>
    /// Rezerwuje slot na wizytę adopcyjną
    /// </summary>
    /// <param name="request">Dane rezerwacji</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <remarks>
    /// Rezerwacja możliwa tylko dla zgłoszeń w statusie Accepted.
    /// Jedno zgłoszenie może mieć tylko jedną aktywną rezerwację.
    /// </remarks>
    [HttpPost("bookings")]
    [ProducesResponseType(typeof(BookingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BookSlot(
        [FromBody] BookSlotRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BookVisitSlotCommand(request.SlotId, request.ApplicationId);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Anuluje rezerwację wizyty
    /// </summary>
    /// <param name="id">ID rezerwacji</param>
    /// <param name="request">Dane anulowania</param>
    /// <param name="cancellationToken">Token anulowania</param>
    [HttpPost("bookings/{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(
        Guid id,
        [FromBody] CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CancelBookingCommand(id, request.CancelledBy, request.Reason);
        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion
}

#region Request DTOs

public record CreateSlotRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    string? Notes
);

public record CreateSlotsBulkRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    IEnumerable<SlotTemplateRequest> Templates
);

public record SlotTemplateRequest(
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    DayOfWeek[]? DaysOfWeek
);

public record DeactivateSlotRequest(
    string DeactivatedBy,
    string Reason
);

public record BookSlotRequest(
    Guid SlotId,
    Guid ApplicationId
);

public record CancelBookingRequest(
    string CancelledBy,
    string Reason
);

#endregion
