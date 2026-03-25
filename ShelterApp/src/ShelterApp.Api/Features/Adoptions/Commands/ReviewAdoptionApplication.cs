using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Commands;

// ============================================
// Command - Take for Review
// ============================================
/// <summary>
/// Pracownik podejmuje zgłoszenie do rozpatrzenia
/// </summary>
public record TakeApplicationForReviewCommand(
    Guid ApplicationId,
    Guid ReviewerUserId,
    string ReviewerName
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Take for Review
// ============================================
public class TakeApplicationForReviewHandler
    : ICommandHandler<TakeApplicationForReviewCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<TakeApplicationForReviewHandler> _logger;

    public TakeApplicationForReviewHandler(
        ShelterDbContext context,
        ILogger<TakeApplicationForReviewHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        TakeApplicationForReviewCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _context.AdoptionApplications
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application is null)
        {
            return Result.Failure<AdoptionApplicationDto>(
                Error.NotFound("AdoptionApplication", request.ApplicationId));
        }

        var result = application.TakeForReview(request.ReviewerUserId, request.ReviewerName);
        if (result.IsFailure)
        {
            return Result.Failure<AdoptionApplicationDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Application {ApplicationId} taken for review by {ReviewerName}",
            application.Id,
            request.ReviewerName);

        var adopter = await _context.Adopters.FindAsync(new object[] { application.AdopterId }, cancellationToken);
        var animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        return Result.Success(application.ToDto(adopter, animal));
    }
}

// ============================================
// Validator - Take for Review
// ============================================
public class TakeApplicationForReviewValidator : AbstractValidator<TakeApplicationForReviewCommand>
{
    public TakeApplicationForReviewValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.ReviewerUserId)
            .NotEmpty().WithMessage("ID pracownika jest wymagane");

        RuleFor(x => x.ReviewerName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");
    }
}

// ============================================
// Command - Approve Application
// ============================================
/// <summary>
/// Zatwierdza zgłoszenie adopcyjne (WB-11)
/// </summary>
public record ApproveApplicationCommand(
    Guid ApplicationId,
    string ReviewerName,
    string? Notes
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Approve Application
// ============================================
public class ApproveApplicationHandler
    : ICommandHandler<ApproveApplicationCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ApproveApplicationHandler> _logger;

    public ApproveApplicationHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<ApproveApplicationHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        ApproveApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _context.AdoptionApplications
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application is null)
        {
            return Result.Failure<AdoptionApplicationDto>(
                Error.NotFound("AdoptionApplication", request.ApplicationId));
        }

        var result = application.ApproveApplication(request.ReviewerName, request.Notes);
        if (result.IsFailure)
        {
            return Result.Failure<AdoptionApplicationDto>(result.Error);
        }

        // Zmień status adoptującego na Verified
        var adopter = await _context.Adopters
            .FirstOrDefaultAsync(a => a.Id == application.AdopterId, cancellationToken);

        if (adopter is not null && adopter.CanChangeStatus(AdopterStatusTrigger.ZatwierdznieZgloszenia))
        {
            adopter.ChangeStatus(AdopterStatusTrigger.ZatwierdznieZgloszenia, request.ReviewerName);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Wyślij email
        var animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);
        if (adopter is not null && animal is not null)
        {
            try
            {
                await _emailService.SendApplicationAcceptedAsync(
                    adopter.Email,
                    adopter.FullName,
                    animal.Name,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval email for application {ApplicationId}", application.Id);
            }
        }

        _logger.LogInformation(
            "Application {ApplicationId} approved by {ReviewerName}",
            application.Id,
            request.ReviewerName);

        return Result.Success(application.ToDto(adopter, animal));
    }
}

// ============================================
// Validator - Approve Application
// ============================================
public class ApproveApplicationValidator : AbstractValidator<ApproveApplicationCommand>
{
    public ApproveApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.ReviewerName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notatki nie mogą przekraczać 2000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

// ============================================
// Command - Reject Application
// ============================================
/// <summary>
/// Odrzuca zgłoszenie adopcyjne (WB-11)
/// </summary>
public record RejectApplicationCommand(
    Guid ApplicationId,
    string ReviewerName,
    string Reason
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Reject Application
// ============================================
public class RejectApplicationHandler
    : ICommandHandler<RejectApplicationCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<RejectApplicationHandler> _logger;

    public RejectApplicationHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<RejectApplicationHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        RejectApplicationCommand request,
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

            var result = application.RejectApplication(request.ReviewerName, request.Reason);
            if (result.IsFailure)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<AdoptionApplicationDto>(result.Error);
            }

            // Przywróć status zwierzęcia do Available
            var animal = await _context.Animals
                .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

            if (animal is not null && animal.CanChangeStatus(Domain.Animals.AnimalStatusTrigger.AnulowanieZgloszenia))
            {
                animal.ChangeStatus(
                    Domain.Animals.AnimalStatusTrigger.AnulowanieZgloszenia,
                    request.ReviewerName,
                    request.Reason);
            }

            // Zmień status adoptującego na Registered (odrzucenie)
            var adopter = await _context.Adopters
                .FirstOrDefaultAsync(a => a.Id == application.AdopterId, cancellationToken);

            if (adopter is not null && adopter.CanChangeStatus(AdopterStatusTrigger.OdrzucenieZgloszenia))
            {
                adopter.ChangeStatus(AdopterStatusTrigger.OdrzucenieZgloszenia, request.ReviewerName, request.Reason);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);

            // Wyślij email
            if (adopter is not null && animal is not null)
            {
                try
                {
                    await _emailService.SendApplicationRejectedAsync(
                        adopter.Email,
                        adopter.FullName,
                        animal.Name,
                        request.Reason,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send rejection email for application {ApplicationId}", application.Id);
                }
            }

            _logger.LogInformation(
                "Application {ApplicationId} rejected by {ReviewerName}. Reason: {Reason}",
                application.Id,
                request.ReviewerName,
                request.Reason);

            return Result.Success(application.ToDto(adopter, animal));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting application {ApplicationId}", request.ApplicationId);

            if (_context.HasActiveTransaction)
            {
                await _context.RollbackTransactionAsync(cancellationToken);
            }

            throw;
        }
    }
}

// ============================================
// Validator - Reject Application
// ============================================
public class RejectApplicationValidator : AbstractValidator<RejectApplicationCommand>
{
    public RejectApplicationValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.ReviewerName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód odrzucenia jest wymagany")
            .MaximumLength(1000).WithMessage("Powód odrzucenia nie może przekraczać 1000 znaków");
    }
}
