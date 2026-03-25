using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Commands;

// ============================================
// Command - Generate Contract
// ============================================
/// <summary>
/// Generuje umowę adopcyjną
/// </summary>
public record GenerateContractCommand(
    Guid ApplicationId,
    string GeneratedByName
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Generate Contract
// ============================================
public class GenerateContractHandler
    : ICommandHandler<GenerateContractCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<GenerateContractHandler> _logger;

    public GenerateContractHandler(
        ShelterDbContext context,
        ILogger<GenerateContractHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        GenerateContractCommand request,
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

        // Generuj numer umowy: UA-RRRR-NNNNNN
        var year = DateTime.UtcNow.Year;
        var contractsThisYear = await _context.AdoptionApplications
            .CountAsync(a =>
                a.ContractNumber != null &&
                a.ContractGeneratedDate != null &&
                a.ContractGeneratedDate.Value.Year == year,
                cancellationToken);

        var contractNumber = $"UA-{year}-{(contractsThisYear + 1):D6}";

        var result = application.GenerateContract(contractNumber, request.GeneratedByName);
        if (result.IsFailure)
        {
            return Result.Failure<AdoptionApplicationDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Contract {ContractNumber} generated for application {ApplicationId} by {GeneratedBy}",
            contractNumber,
            application.Id,
            request.GeneratedByName);

        var adopter = await _context.Adopters.FindAsync(new object[] { application.AdopterId }, cancellationToken);
        var animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        return Result.Success(application.ToDto(adopter, animal));
    }
}

// ============================================
// Validator - Generate Contract
// ============================================
public class GenerateContractValidator : AbstractValidator<GenerateContractCommand>
{
    public GenerateContractValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.GeneratedByName)
            .NotEmpty().WithMessage("Nazwa pracownika jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa pracownika nie może przekraczać 200 znaków");
    }
}

// ============================================
// Command - Finalize Adoption (Sign Contract)
// ============================================
/// <summary>
/// Finalizuje adopcję poprzez podpisanie umowy
/// </summary>
public record FinalizeAdoptionCommand(
    Guid ApplicationId,
    string ContractFilePath,
    string SignedByName
) : ICommand<Result<AdoptionApplicationDto>>;

// ============================================
// Handler - Finalize Adoption
// ============================================
public class FinalizeAdoptionHandler
    : ICommandHandler<FinalizeAdoptionCommand, Result<AdoptionApplicationDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<FinalizeAdoptionHandler> _logger;

    public FinalizeAdoptionHandler(
        ShelterDbContext context,
        IEmailService emailService,
        ILogger<FinalizeAdoptionHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AdoptionApplicationDto>> Handle(
        FinalizeAdoptionCommand request,
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

        var result = application.FinalizeAdoption(request.ContractFilePath, request.SignedByName);
        if (result.IsFailure)
        {
            return Result.Failure<AdoptionApplicationDto>(result.Error);
        }

        // Zmień status zwierzęcia na Adopted
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        if (animal is not null && animal.CanChangeStatus(AnimalStatusTrigger.PodpisanieUmowy))
        {
            animal.ChangeStatus(
                AnimalStatusTrigger.PodpisanieUmowy,
                request.SignedByName);
        }

        // Zmień status adoptującego na Registered (po zakończonej adopcji może adoptować ponownie)
        var adopter = await _context.Adopters
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == application.AdopterId, cancellationToken);

        if (adopter is not null && adopter.CanChangeStatus(AdopterStatusTrigger.PodpisanieUmowy))
        {
            adopter.ChangeStatus(
                AdopterStatusTrigger.PodpisanieUmowy,
                request.SignedByName);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Wyślij email z gratulacjami
        if (adopter is not null && animal is not null)
        {
            try
            {
                await _emailService.SendAdoptionCompletedAsync(
                    adopter.Email,
                    adopter.FullName,
                    animal.Name,
                    application.ContractNumber!,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send adoption completed email for application {ApplicationId}", application.Id);
            }
        }

        _logger.LogInformation(
            "Adoption finalized for application {ApplicationId}. Contract: {ContractNumber}. Animal: {AnimalId} -> {AdopterName}",
            application.Id,
            application.ContractNumber,
            animal?.Id,
            adopter?.FullName);

        // Pobierz świeże dane dla DTO
        animal = await _context.Animals.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        return Result.Success(application.ToDto(adopter, animal));
    }
}

// ============================================
// Validator - Finalize Adoption
// ============================================
public class FinalizeAdoptionValidator : AbstractValidator<FinalizeAdoptionCommand>
{
    public FinalizeAdoptionValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");

        RuleFor(x => x.ContractFilePath)
            .NotEmpty().WithMessage("Ścieżka do pliku umowy jest wymagana")
            .MaximumLength(500).WithMessage("Ścieżka do pliku nie może przekraczać 500 znaków");

        RuleFor(x => x.SignedByName)
            .NotEmpty().WithMessage("Nazwa osoby podpisującej jest wymagana")
            .MaximumLength(200).WithMessage("Nazwa osoby nie może przekraczać 200 znaków");
    }
}
