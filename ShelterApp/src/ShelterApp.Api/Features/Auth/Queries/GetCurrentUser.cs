using MediatR;
using Microsoft.AspNetCore.Identity;
using ShelterApp.Api.Features.Auth.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Users;

namespace ShelterApp.Api.Features.Auth.Queries;

/// <summary>
/// Get current user query.
/// </summary>
public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserDto>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Result<UserDto>.Failure(
                Error.NotFound("User.NotFound", "Użytkownik nie został znaleziony"));
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            roles
        ));
    }
}
