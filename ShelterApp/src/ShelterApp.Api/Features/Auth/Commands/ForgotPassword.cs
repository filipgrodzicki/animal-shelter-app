using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ShelterApp.Api.Features.Auth.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Users;

namespace ShelterApp.Api.Features.Auth.Commands;

/// <summary>
/// Forgot password command - sends reset email.
/// </summary>
public record ForgotPasswordCommand(string Email) : IRequest<Result<SuccessResponse>>;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany")
            .EmailAddress().WithMessage("Nieprawidłowy format adresu email");
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<SuccessResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService)
    {
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<Result<SuccessResponse>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always return success to prevent email enumeration
        if (user == null || !user.IsActive)
        {
            return Result<SuccessResponse>.Success(
                new SuccessResponse("Jeśli podany adres email istnieje w systemie, wysłaliśmy instrukcje resetowania hasła"));
        }

        // Generate password reset token
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        // TODO: Send password reset email
        // await _emailService.SendPasswordResetAsync(user.Email!, resetToken);

        return Result<SuccessResponse>.Success(
            new SuccessResponse("Jeśli podany adres email istnieje w systemie, wysłaliśmy instrukcje resetowania hasła"));
    }
}
