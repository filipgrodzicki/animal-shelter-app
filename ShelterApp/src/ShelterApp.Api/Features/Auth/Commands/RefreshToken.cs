using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Auth.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Users;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Auth.Commands;

/// <summary>
/// Refresh token command.
/// </summary>
public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken,
    string? IpAddress = null
) : IRequest<Result<TokenResponse>>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Access token jest wymagany");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token jest wymagany");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ShelterDbContext _dbContext;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ShelterDbContext dbContext)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate access token (without checking expiration)
        var userId = await _tokenService.ValidateAccessTokenAsync(request.AccessToken);
        if (!userId.HasValue)
        {
            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Auth.InvalidToken", "Nieprawidłowy access token"));
        }

        // Get user with refresh tokens using UserManager for proper tracking
        var user = await _userManager.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user == null)
        {
            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Auth.UserNotFound", "Użytkownik nie został znaleziony"));
        }

        if (!user.IsActive)
        {
            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Auth.AccountDeactivated", "Konto zostało dezaktywowane"));
        }

        // Find the refresh token
        var refreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);
        if (refreshToken == null)
        {
            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Auth.InvalidRefreshToken", "Nieprawidłowy refresh token"));
        }

        // Check if token is active
        if (!refreshToken.IsActive)
        {
            // If token was revoked, revoke all tokens (potential token theft)
            if (refreshToken.IsRevoked)
            {
                user.RevokeAllRefreshTokens("Attempted reuse of revoked token");
                await _userManager.UpdateAsync(user);
            }

            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Auth.RefreshTokenExpired", "Refresh token wygasł lub został unieważniony"));
        }

        // Generate new tokens
        var tokenResult = await _tokenService.GenerateAccessTokenAsync(user);

        // Revoke old token and add new one
        refreshToken.Revoke("Replaced by new token", request.IpAddress, tokenResult.RefreshToken);
        user.AddRefreshToken(tokenResult.RefreshToken, tokenResult.RefreshTokenExpiresAt, request.IpAddress);

        // Use UserManager to ensure proper context tracking
        await _userManager.UpdateAsync(user);

        return Result<TokenResponse>.Success(new TokenResponse(
            tokenResult.AccessToken,
            tokenResult.AccessTokenExpiresAt,
            tokenResult.RefreshToken,
            tokenResult.RefreshTokenExpiresAt
        ));
    }
}
