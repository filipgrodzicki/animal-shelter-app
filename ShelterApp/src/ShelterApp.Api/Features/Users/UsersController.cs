using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Common;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Users;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Users;

/// <summary>
/// Zarządzanie użytkownikami systemu (wyłączne uprawnienie Administratora)
/// </summary>
/// <remarks>
/// Kontroler obsługuje pełny cykl życia użytkowników:
/// - Tworzenie, edycja i dezaktywacja kont
/// - Zarządzanie rolami (User, Volunteer, Staff, Admin)
/// - Przeglądanie listy użytkowników
/// </remarks>
[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : ApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ShelterDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ShelterDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    #region Queries

    /// <summary>
    /// Pobiera listę wszystkich użytkowników z filtrowaniem i paginacją
    /// </summary>
    /// <param name="role">Filtr po roli</param>
    /// <param name="searchTerm">Szukaj po imieniu, nazwisku lub emailu</param>
    /// <param name="isActive">Filtr po statusie aktywności</param>
    /// <param name="sortBy">Pole sortowania: name, email, createdAt, lastLoginAt</param>
    /// <param name="sortDescending">Sortowanie malejące</param>
    /// <param name="page">Numer strony</param>
    /// <param name="pageSize">Rozmiar strony</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Paginowana lista użytkowników</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? role = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsQueryable();

        // Filtrowanie po statusie aktywności
        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        // Szukanie po imieniu, nazwisku lub emailu
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email!.ToLower().Contains(term));
        }

        // Pobierz użytkowników z rolami
        var usersWithRoles = new List<UserListItemDto>();
        var users = await query.ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            // Filtrowanie po roli
            if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role))
                continue;

            usersWithRoles.Add(new UserListItemDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.PhoneNumber,
                roles.ToList(),
                user.IsActive,
                user.EmailConfirmed,
                user.CreatedAt,
                user.LastLoginAt
            ));
        }

        // Sortowanie
        usersWithRoles = sortBy.ToLower() switch
        {
            "name" => sortDescending
                ? usersWithRoles.OrderByDescending(u => u.FullName).ToList()
                : usersWithRoles.OrderBy(u => u.FullName).ToList(),
            "email" => sortDescending
                ? usersWithRoles.OrderByDescending(u => u.Email).ToList()
                : usersWithRoles.OrderBy(u => u.Email).ToList(),
            "lastloginat" => sortDescending
                ? usersWithRoles.OrderByDescending(u => u.LastLoginAt).ToList()
                : usersWithRoles.OrderBy(u => u.LastLoginAt).ToList(),
            _ => sortDescending
                ? usersWithRoles.OrderByDescending(u => u.CreatedAt).ToList()
                : usersWithRoles.OrderBy(u => u.CreatedAt).ToList()
        };

        // Paginacja
        var totalCount = usersWithRoles.Count;
        var pagedUsers = usersWithRoles
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PagedResult<UserListItemDto>(pagedUsers, totalCount, page, pageSize));
    }

    /// <summary>
    /// Pobiera szczegóły użytkownika
    /// </summary>
    /// <param name="id">Identyfikator użytkownika</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Szczegóły użytkownika</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return NotFound(new ProblemDetails
            {
                Title = "User.NotFound",
                Detail = "Użytkownik nie został znaleziony",
                Status = StatusCodes.Status404NotFound
            });

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDetailDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Address,
            user.City,
            user.PostalCode,
            user.AvatarUrl,
            roles.ToList(),
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt
        ));
    }

    /// <summary>
    /// Pobiera dostępne role w systemie
    /// </summary>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Lista dostępnych ról</returns>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .Select(r => new RoleDto(r.Name!, r.Description))
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Tworzy nowego użytkownika
    /// </summary>
    /// <param name="request">Dane nowego użytkownika</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Utworzony użytkownik</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Sprawdź czy email już istnieje
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "User.EmailExists",
                Detail = "Użytkownik z podanym adresem email już istnieje",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Walidacja roli
        if (!AppRoles.All.Contains(request.Role))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "User.InvalidRole",
                Detail = $"Nieprawidłowa rola. Dostępne role: {string.Join(", ", AppRoles.All)}",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Utwórz użytkownika
        var user = ApplicationUser.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber
        );

        // Ustaw dodatkowe dane profilu
        if (request.DateOfBirth.HasValue || !string.IsNullOrEmpty(request.Address))
        {
            user.UpdateProfile(
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.DateOfBirth,
                request.Address,
                request.City,
                request.PostalCode
            );
        }

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "User.CreateFailed",
                Detail = string.Join(", ", result.Errors.Select(e => e.Description)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Potwierdź email automatycznie (Admin tworzy użytkownika)
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.ConfirmEmailAsync(user, token);

        // Przypisz rolę
        await _userManager.AddToRoleAsync(user, request.Role);

        var roles = await _userManager.GetRolesAsync(user);

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id },
            new UserDetailDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.PhoneNumber,
                user.DateOfBirth,
                user.Address,
                user.City,
                user.PostalCode,
                user.AvatarUrl,
                roles.ToList(),
                user.IsActive,
                user.EmailConfirmed,
                user.CreatedAt,
                user.UpdatedAt,
                user.LastLoginAt
            ));
    }

    /// <summary>
    /// Aktualizuje dane użytkownika
    /// </summary>
    /// <param name="id">Identyfikator użytkownika</param>
    /// <param name="request">Zaktualizowane dane</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Zaktualizowany użytkownik</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return NotFound(new ProblemDetails
            {
                Title = "User.NotFound",
                Detail = "Użytkownik nie został znaleziony",
                Status = StatusCodes.Status404NotFound
            });

        // Aktualizuj profil
        user.UpdateProfile(
            request.FirstName ?? user.FirstName,
            request.LastName ?? user.LastName,
            request.PhoneNumber ?? user.PhoneNumber,
            request.DateOfBirth ?? user.DateOfBirth,
            request.Address ?? user.Address,
            request.City ?? user.City,
            request.PostalCode ?? user.PostalCode
        );

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "User.UpdateFailed",
                Detail = string.Join(", ", result.Errors.Select(e => e.Description)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDetailDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Address,
            user.City,
            user.PostalCode,
            user.AvatarUrl,
            roles.ToList(),
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt
        ));
    }

    /// <summary>
    /// Zmienia rolę użytkownika
    /// </summary>
    /// <param name="id">Identyfikator użytkownika</param>
    /// <param name="request">Nowa rola</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Zaktualizowany użytkownik</returns>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeUserRole(
        Guid id,
        [FromBody] ChangeRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return NotFound(new ProblemDetails
            {
                Title = "User.NotFound",
                Detail = "Użytkownik nie został znaleziony",
                Status = StatusCodes.Status404NotFound
            });

        // Walidacja nowej roli
        if (!AppRoles.All.Contains(request.NewRole))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "User.InvalidRole",
                Detail = $"Nieprawidłowa rola. Dostępne role: {string.Join(", ", AppRoles.All)}",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Usuń wszystkie obecne role
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Przypisz nową rolę
        await _userManager.AddToRoleAsync(user, request.NewRole);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDetailDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Address,
            user.City,
            user.PostalCode,
            user.AvatarUrl,
            roles.ToList(),
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt
        ));
    }

    /// <summary>
    /// Dezaktywuje użytkownika
    /// </summary>
    /// <param name="id">Identyfikator użytkownika</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Zaktualizowany użytkownik</returns>
    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return NotFound(new ProblemDetails
            {
                Title = "User.NotFound",
                Detail = "Użytkownik nie został znaleziony",
                Status = StatusCodes.Status404NotFound
            });

        user.Deactivate();
        user.RevokeAllRefreshTokens("Konto zostało dezaktywowane przez administratora");

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDetailDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Address,
            user.City,
            user.PostalCode,
            user.AvatarUrl,
            roles.ToList(),
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt
        ));
    }

    /// <summary>
    /// Aktywuje użytkownika
    /// </summary>
    /// <param name="id">Identyfikator użytkownika</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Zaktualizowany użytkownik</returns>
    [HttpPut("{id:guid}/activate")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateUser(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return NotFound(new ProblemDetails
            {
                Title = "User.NotFound",
                Detail = "Użytkownik nie został znaleziony",
                Status = StatusCodes.Status404NotFound
            });

        user.Activate();
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDetailDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Address,
            user.City,
            user.PostalCode,
            user.AvatarUrl,
            roles.ToList(),
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt
        ));
    }

    /// <summary>
    /// Resetuje hasło użytkownika
    /// </summary>
    /// <param name="id">Identyfikator użytkownika</param>
    /// <param name="request">Nowe hasło</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Potwierdzenie operacji</returns>
    [HttpPut("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetUserPassword(
        Guid id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
            return NotFound(new ProblemDetails
            {
                Title = "User.NotFound",
                Detail = "Użytkownik nie został znaleziony",
                Status = StatusCodes.Status404NotFound
            });

        // Usuń obecne hasło i ustaw nowe
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "User.ResetPasswordFailed",
                Detail = string.Join(", ", result.Errors.Select(e => e.Description)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Unieważnij wszystkie tokeny odświeżania
        user.RevokeAllRefreshTokens("Hasło zostało zresetowane przez administratora");
        await _userManager.UpdateAsync(user);

        return Ok(new { Message = "Hasło zostało zresetowane pomyślnie" });
    }

    #endregion
}

#region DTOs

/// <summary>
/// Użytkownik na liście
/// </summary>
public record UserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? PhoneNumber,
    List<string> Roles,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

/// <summary>
/// Szczegóły użytkownika
/// </summary>
public record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? Address,
    string? City,
    string? PostalCode,
    string? AvatarUrl,
    List<string> Roles,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastLoginAt
);

/// <summary>
/// Rola systemowa
/// </summary>
public record RoleDto(
    string Name,
    string? Description
);

/// <summary>
/// Tworzenie użytkownika
/// </summary>
public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    string? PhoneNumber = null,
    DateTime? DateOfBirth = null,
    string? Address = null,
    string? City = null,
    string? PostalCode = null
);

/// <summary>
/// Aktualizacja użytkownika
/// </summary>
public record UpdateUserRequest(
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    DateTime? DateOfBirth = null,
    string? Address = null,
    string? City = null,
    string? PostalCode = null
);

/// <summary>
/// Zmiana roli
/// </summary>
public record ChangeRoleRequest(
    string NewRole
);

/// <summary>
/// Reset hasła
/// </summary>
public record ResetPasswordRequest(
    string NewPassword
);

#endregion
