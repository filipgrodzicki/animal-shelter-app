using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ShelterApp.Api.Features.Auth.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Users;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Auth.Commands;

/// <summary>
/// Login command.
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null
) : IRequest<Result<AuthResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany")
            .EmailAddress().WithMessage("Nieprawidłowy format adresu email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Hasło jest wymagane");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ShelterDbContext _dbContext;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ShelterDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<AuthResponse>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "Nieprawidłowy email lub hasło"));
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure(
                Error.Unauthorized("Auth.AccountDeactivated", "Konto zostało dezaktywowane"));
        }

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result<AuthResponse>.Failure(
                Error.Unauthorized("Auth.AccountLocked", "Konto zostało tymczasowo zablokowane z powodu zbyt wielu nieudanych prób logowania"));
        }

        // Verify password
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                return Result<AuthResponse>.Failure(
                    Error.Unauthorized("Auth.AccountLocked", "Konto zostało tymczasowo zablokowane z powodu zbyt wielu nieudanych prób logowania"));
            }

            if (signInResult.IsNotAllowed)
            {
                return Result<AuthResponse>.Failure(
                    Error.Unauthorized("Auth.EmailNotConfirmed", "Potwierdź swój adres email przed zalogowaniem"));
            }

            return Result<AuthResponse>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "Nieprawidłowy email lub hasło"));
        }

        // Record login
        user.RecordLogin();

        // Generate tokens
        var tokenResult = await _tokenService.GenerateAccessTokenAsync(user);

        // Revoke old refresh tokens and add new one
        user.RevokeAllRefreshTokens("Replaced by new token on login");
        user.AddRefreshToken(tokenResult.RefreshToken, tokenResult.RefreshTokenExpiresAt, request.IpAddress);

        // Use UserManager to ensure proper context tracking
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            new UserDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.AvatarUrl,
                roles
            ),
            tokenResult.AccessToken,
            tokenResult.AccessTokenExpiresAt,
            tokenResult.RefreshToken,
            tokenResult.RefreshTokenExpiresAt
        ));
    }
}
