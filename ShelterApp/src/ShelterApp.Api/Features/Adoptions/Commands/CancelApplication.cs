using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Commands;

// ============================================
// Command - Cancel Application By User
// ============================================
/// <summary>
/// Anuluje zgłoszenie adopcyjne przez użytkownika
/// </summary>
public record CancelApplicationByUserCommand(
    Guid ApplicationId,
    string Reason,
    string UserName
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Cancel Application By User
// ============================================
public class CancelApplicationByUserHandler
    : ICommandHandler<CancelApplicationByUserCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<CancelApplicationByUserHandler> _logger;

    public CancelApplicationByUserHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<CancelApplicationByUserHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        CancelApplicationByUserCommand request,
        CancellationToken cancellationToken)
    {
        await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            var application = await _context.AdoptionApplications
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

            if (application is null)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<AdoptionApplicationDto>(
                    Error.NotFound("AdoptionApplication", request.ApplicationId));
            }

            var result = application.CancelByUser(request.Reason, request.UserName);
            if (result.IsFailure)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<AdoptionApplicationDto>(result.Error);
            }

            // Przywróć status zwierzęcia do Available
            var animal = await _context.Animals
                .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

            if (animal is not null && animal.CanChangeStatus(AnimalStatusTrigger.AnulowanieZgloszenia))
            {
                animal.ChangeStatus(
                    AnimalStatusTrigger.AnulowanieZgloszenia,
                    request.UserName,
                    request.Reason);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);

            // Pobierz dane do emaila
            var adopter = await _context.Adopters.FindAsync(new object[] { application.AdopterId }, cancellationToken);
            animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

            // Wyślij email z potwierdzeniem anulowania
            if (adopter is not null && animal is not null)
            {
                try
                {
                    await _emailService.SendApplicationCancelledAsync(
                        adopter.Email,
                        adopter.FullName,
                        animal.Name,
                        request.Reason,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send cancellation email for application {ApplicationId}", application.Id);
                }
            }

            _logger.LogInformation(
                "Application {ApplicationId} cancelled by user {UserName}. Reason: {Reason}",
                application.Id,
                request.UserName,
                request.Reason);

            return Result.Success(application.ToDto(adopter, animal));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling application {ApplicationId}", request.ApplicationId);

            if (_context.HasActiveTransaction)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
            }

            throw;
        }
    }
}

// ============================================
// Validator - Cancel Application By User
// ============================================
public class CancelApplicationByUserValidator : AbstractValidator<CancelApplicationByUserCommand>
{
    public CancelApplicationByUserValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód anulowania jest wymagany")
            .MaximumLength(1000).WithMessage("Powód anulowania nie może przekraczać 1000 znaków");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Nazwa użytkownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa użytkownika nie może przekraczać 200 znaków");
    }
}

// ============================================
// Command - Cancel Application By Staff
// ============================================
/// <summary>
/// Anuluje zgłoszenie adopcyjne przez pracownika (np. z powodów administracyjnych)
/// </summary>
public record CancelApplicationByStaffCommand(
    Guid ApplicationId,
    string Reason,
    string StaffName,
    bool NotifyAdopter = true
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Cancel Application By Staff
// ============================================
public class CancelApplicationByStaffHandler
    : ICommandHandler<CancelApplicationByStaffCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<CancelApplicationByStaffHandler> _logger;

    public CancelApplicationByStaffHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<CancelApplicationByStaffHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        CancelApplicationByStaffCommand request,
        CancellationToken cancellationToken)
    {
        await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            var application = await _context.AdoptionApplications
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

            if (application is null)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<AdoptionApplicationDto>(
                    Error.NotFound("AdoptionApplication", request.ApplicationId));
            }

            // Pracownik może anulować z dowolnego statusu oprócz stanów końcowych
            var result = application.CancelByUser(request.Reason, request.StaffName);
            if (result.IsFailure)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<AdoptionApplicationDto>(result.Error);
            }

            // Przywróć status zwierzęcia do Available
            var animal = await _context.Animals
                .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

            if (animal is not null && animal.CanChangeStatus(AnimalStatusTrigger.AnulowanieZgloszenia))
            {
                animal.ChangeStatus(
                    AnimalStatusTrigger.AnulowanieZgloszenia,
                    request.StaffName,
                    request.Reason);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);

            // Pobierz dane do emaila
            var adopter = await _context.Adopters.FindAsync(new object[] { application.AdopterId }, cancellationToken);
            animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

            // Wyślij email z powiadomieniem o anulowaniu (jeśli włączone)
            if (request.NotifyAdopter && adopter is not null && animal is not null)
            {
                try
                {
                    await _emailService.SendApplicationCancelledAsync(
                        adopter.Email,
                        adopter.FullName,
                        animal.Name,
                        request.Reason,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send cancellation email for application {ApplicationId}", application.Id);
                }
            }

            _logger.LogInformation(
                "Application {ApplicationId} cancelled by staff {StaffName}. Reason: {Reason}",
                application.Id,
                request.StaffName,
                request.Reason);

            return Result.Success(application.ToDto(adopter, animal));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling application {ApplicationId} by staff", request.ApplicationId);

            if (_context.HasActiveTransaction)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
            }

            throw;
        }
    }
}

// ============================================
// Validator - Cancel Application By Staff
// ============================================
public class CancelApplicationByStaffValidator : AbstractValidator<CancelApplicationByStaffCommand>
{
    public CancelApplicationByStaffValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód anulowania jest wymagany")
            .MaximumLength(1000).WithMessage("Powód anulowania nie może przekraczać 1000 znaków");

        RuleFor(x => x.StaffName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");
    }
}
