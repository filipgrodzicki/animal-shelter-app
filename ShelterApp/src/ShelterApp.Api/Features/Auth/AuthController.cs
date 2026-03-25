using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Auth.Commands;
using ShelterApp.Api.Features.Auth.Queries;
using ShelterApp.Api.Features.Auth.Shared;
using ShelterApp.Domain.Users;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Auth;

[Route("api/auth")]
public class AuthController : ApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ShelterDbContext _context;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ShelterDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.FirstName,
            request.LastName,
            request.PhoneNumber
        );

        var result = await Sender.Send(command);
        return result.IsSuccess
            ? Ok(result.Value)
            : HandleResult(result);
    }

    /// <summary>
    /// Login user and get tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new LoginCommand(request.Email, request.Password, ipAddress);

        var result = await Sender.Send(command);
        return result.IsSuccess
            ? Ok(result.Value)
            : HandleResult(result);
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken, ipAddress);

        var result = await Sender.Send(command);
        return result.IsSuccess
            ? Ok(result.Value)
            : HandleResult(result);
    }

    /// <summary>
    /// Logout user and revoke tokens.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var command = new LogoutCommand(userId.Value, request?.RefreshToken);
        var result = await Sender.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleResult(result);
    }

    /// <summary>
    /// Get current user information.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetCurrentUserQuery(userId.Value);
        var result = await Sender.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleResult(result);
    }

    /// <summary>
    /// Request password reset email.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var result = await Sender.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleResult(result);
    }

    /// <summary>
    /// Reset password with token.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var command = new ResetPasswordCommand(
            request.Email,
            request.Token,
            request.NewPassword,
            request.ConfirmPassword
        );

        var result = await Sender.Send(command);
        return result.IsSuccess
            ? Ok(result.Value)
            : HandleResult(result);
    }

    #region RODO / GDPR

    /// <summary>
    /// Eksportuje wszystkie dane użytkownika (RODO Art. 20 - prawo do przenoszenia danych).
    /// </summary>
    [HttpGet("me/export")]
    [Authorize]
    [ProducesResponseType(typeof(UserDataExportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportMyData(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        // Pobierz Adopter powiązanego z użytkownikiem
        var adopter = await _context.Adopters
            .FirstOrDefaultAsync(a => a.UserId == userId.Value, cancellationToken);

        // Pobierz wnioski adopcyjne użytkownika (przez Adopter)
        var adoptionApplications = new List<AdoptionApplicationExportDto>();
        if (adopter != null)
        {
            adoptionApplications = await _context.AdoptionApplications
                .Where(a => a.AdopterId == adopter.Id)
                .Select(a => new AdoptionApplicationExportDto(
                    a.Id,
                    a.AnimalId,
                    a.Status.ToString(),
                    a.ApplicationDate,
                    a.UpdatedAt
                ))
                .ToListAsync(cancellationToken);
        }

        // Pobierz dane wolontariusza jeśli istnieją
        var volunteer = await _context.Volunteers
            .Where(v => v.UserId == userId.Value)
            .Select(v => new VolunteerExportDto(
                v.Id,
                v.Status.ToString(),
                v.ApplicationDate,
                (double)v.TotalHoursWorked
            ))
            .FirstOrDefaultAsync(cancellationToken);

        var exportData = new UserDataExportDto(
            ExportDate: DateTime.UtcNow,
            PersonalData: new PersonalDataDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.DateOfBirth,
                user.Address,
                user.City,
                user.PostalCode,
                user.CreatedAt,
                user.LastLoginAt
            ),
            Roles: roles.ToList(),
            AdoptionApplications: adoptionApplications,
            VolunteerData: volunteer
        );

        return Ok(exportData);
    }

    /// <summary>
    /// Usuwa (anonimizuje) konto użytkownika (RODO Art. 17 - prawo do bycia zapomnianym).
    /// </summary>
    /// <remarks>
    /// Dane są anonimizowane, nie usuwane fizycznie, aby zachować integralność
    /// historii adopcji i innych powiązanych rekordów.
    /// </remarks>
    [HttpDelete("me")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMyAccount(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
            return NotFound();

        // Pobierz Adopter powiązanego z użytkownikiem
        var adopter = await _context.Adopters
            .FirstOrDefaultAsync(a => a.UserId == userId.Value, cancellationToken);

        // Sprawdź czy użytkownik nie ma aktywnych procesów adopcyjnych
        if (adopter != null)
        {
            var hasActiveAdoptions = await _context.AdoptionApplications
                .AnyAsync(a => a.AdopterId == adopter.Id &&
                    (a.Status == Domain.Adoptions.AdoptionApplicationStatus.Submitted ||
                     a.Status == Domain.Adoptions.AdoptionApplicationStatus.UnderReview ||
                     a.Status == Domain.Adoptions.AdoptionApplicationStatus.Accepted ||
                     a.Status == Domain.Adoptions.AdoptionApplicationStatus.VisitScheduled),
                    cancellationToken);

            if (hasActiveAdoptions)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Account.HasActiveAdoptions",
                    Detail = "Nie można usunąć konta z aktywnymi procesami adopcyjnymi. Najpierw anuluj wnioski.",
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        // Anonimizuj dane użytkownika
        user.Anonymize();
        await _userManager.UpdateAsync(user);

        return Ok(new SuccessResponse("Konto zostało usunięte. Dane osobowe zostały zanonimizowane."));
    }

    #endregion

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

// Request DTOs
public record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    string? PhoneNumber
);

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record LogoutRequest(string? RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword
);

// RODO Export DTOs
public record UserDataExportDto(
    DateTime ExportDate,
    PersonalDataDto PersonalData,
    List<string> Roles,
    List<AdoptionApplicationExportDto> AdoptionApplications,
    VolunteerExportDto? VolunteerData
);

public record PersonalDataDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? Address,
    string? City,
    string? PostalCode,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record AdoptionApplicationExportDto(
    Guid Id,
    Guid AnimalId,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record VolunteerExportDto(
    Guid Id,
    string Status,
    DateTime StartDate,
    double TotalHours
);
