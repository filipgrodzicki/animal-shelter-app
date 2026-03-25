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
/// Register a new user command.
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    string? PhoneNumber
) : IRequest<Result<AuthResponse>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany")
            .EmailAddress().WithMessage("Nieprawidłowy format adresu email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Hasło jest wymagane")
            .MinimumLength(8).WithMessage("Hasło musi mieć minimum 8 znaków")
            .Matches("[A-Z]").WithMessage("Hasło musi zawierać przynajmniej jedną wielką literę")
            .Matches("[a-z]").WithMessage("Hasło musi zawierać przynajmniej jedną małą literę")
            .Matches("[0-9]").WithMessage("Hasło musi zawierać przynajmniej jedną cyfrę");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Potwierdzenie hasła jest wymagane")
            .Equal(x => x.Password).WithMessage("Hasła muszą być identyczne");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Imię jest wymagane")
            .MaximumLength(100).WithMessage("Imię może mieć maksymalnie 100 znaków");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Nazwisko jest wymagane")
            .MaximumLength(100).WithMessage("Nazwisko może mieć maksymalnie 100 znaków");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{3,6}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Nieprawidłowy format numeru telefonu");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ShelterDbContext _dbContext;
    private readonly IEmailService _emailService;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ShelterDbContext dbContext,
        IEmailService emailService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure(
                Error.Conflict("User.AlreadyExists", "Użytkownik o podanym adresie email już istnieje"));
        }

        // Create user
        var user = ApplicationUser.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            request.PhoneNumber
        );

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result<AuthResponse>.Failure(
                Error.Validation("User.Creation", $"Nie udało się utworzyć konta: {errors}"));
        }

        // Assign default role
        await _userManager.AddToRoleAsync(user, AppRoles.User);

        // Generate email confirmation token
        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // TODO: Send confirmation email
        // await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationToken);

        // Generate tokens
        var tokenResult = await _tokenService.GenerateAccessTokenAsync(user);

        // Store refresh token - use UserManager to ensure proper context tracking
        var refreshToken = user.AddRefreshToken(tokenResult.RefreshToken, tokenResult.RefreshTokenExpiresAt);
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
