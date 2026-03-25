using MediatR;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Auth.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Auth.Commands;

/// <summary>
/// Logout command - revokes refresh token.
/// </summary>
public record LogoutCommand(
    Guid UserId,
    string? RefreshToken = null
) : IRequest<Result<SuccessResponse>>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<SuccessResponse>>
{
    private readonly ShelterDbContext _dbContext;

    public LogoutCommandHandler(ShelterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SuccessResponse>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return Result<SuccessResponse>.Success(new SuccessResponse("Wylogowano pomyślnie"));
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            // Revoke specific token
            var token = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);
            token?.Revoke("User logged out");
        }
        else
        {
            // Revoke all tokens
            user.RevokeAllRefreshTokens("User logged out");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SuccessResponse>.Success(new SuccessResponse("Wylogowano pomyślnie"));
    }
}
