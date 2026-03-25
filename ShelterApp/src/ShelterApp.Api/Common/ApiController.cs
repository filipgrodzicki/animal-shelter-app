using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Domain.Common;

namespace ShelterApp.Api.Common;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiController : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        return result.Match(
            onSuccess: value => Ok(value),
            onFailure: HandleError
        );
    }

    protected IActionResult HandleResult(Result result)
    {
        return result.Match(
            onSuccess: () => Ok(),
            onFailure: HandleError
        );
    }

    protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName, Func<T, object> routeValues)
    {
        return result.Match(
            onSuccess: value => CreatedAtAction(actionName, routeValues(value), value),
            onFailure: HandleError
        );
    }

    protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName, object routeValues)
    {
        return result.Match(
            onSuccess: value => CreatedAtAction(actionName, routeValues, value),
            onFailure: HandleError
        );
    }

    protected IActionResult HandleNoContentResult(Result result)
    {
        return result.Match(
            onSuccess: () => NoContent(),
            onFailure: HandleError
        );
    }

    private IActionResult HandleError(Error error)
    {
        return error.Code switch
        {
            _ when error.Code.Contains("NotFound") => NotFound(CreateProblemDetails(error, StatusCodes.Status404NotFound)),
            _ when error.Code.Contains("Validation") => BadRequest(CreateProblemDetails(error, StatusCodes.Status400BadRequest)),
            _ when error.Code.Contains("Conflict") => Conflict(CreateProblemDetails(error, StatusCodes.Status409Conflict)),
            _ when error.Code.Contains("Unauthorized") => Unauthorized(CreateProblemDetails(error, StatusCodes.Status401Unauthorized)),
            _ when error.Code.Contains("Forbidden") => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(error, StatusCodes.Status403Forbidden)),
            _ => BadRequest(CreateProblemDetails(error, StatusCodes.Status400BadRequest))
        };
    }

    private static ProblemDetails CreateProblemDetails(Error error, int statusCode)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Message,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
    }
}
