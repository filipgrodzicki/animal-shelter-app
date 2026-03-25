using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ShelterApp.Api.Features.Auth.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Users;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Auth.Commands;

/// <summary>
/// Reset password command.
/// </summary>
public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword
) : IRequest<Result<SuccessResponse>>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany")
            .EmailAddress().WithMessage("Nieprawidłowy format adresu email");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token resetowania jest wymagany");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nowe hasło jest wymagane")
            .MinimumLength(8).WithMessage("Hasło musi mieć minimum 8 znaków")
            .Matches("[A-Z]").WithMessage("Hasło musi zawierać przynajmniej jedną wielką literę")
            .Matches("[a-z]").WithMessage("Hasło musi zawierać przynajmniej jedną małą literę")
            .Matches("[0-9]").WithMessage("Hasło musi zawierać przynajmniej jedną cyfrę");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Potwierdzenie hasła jest wymagane")
            .Equal(x => x.NewPassword).WithMessage("Hasła muszą być identyczne");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<SuccessResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ShelterDbContext _dbContext;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        ShelterDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<Result<SuccessResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<SuccessResponse>.Failure(
                Error.Validation("Auth.InvalidToken", "Nieprawidłowy token resetowania hasła"));
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<SuccessResponse>.Failure(
                Error.Validation("Auth.ResetFailed", $"Nie udało się zresetować hasła: {errors}"));
        }

        // Revoke all refresh tokens after password reset
        user.RevokeAllRefreshTokens("Password was reset");
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SuccessResponse>.Success(
            new SuccessResponse("Hasło zostało pomyślnie zmienione. Możesz się teraz zalogować."));
    }
}
