using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;

namespace ShelterApp.Api.Features.Adoptions.Queries;

// ============================================
// Query
// ============================================
/// <summary>
/// Pobiera szczegóły zgłoszenia adopcyjnego
/// </summary>
public record GetAdoptionApplicationByIdQuery(
    Guid Id,
    bool IncludeStatusHistory = false
) : IQuery<Result<AdoptionApplicationDetailDto>>;

// ============================================
// Response DTO
// ============================================
public record MatchScoreDto(
    double TotalScore,
    double TotalPercentage,
    double ExperienceScore,
    double SpaceScore,
    double CareTimeScore,
    double ChildrenScore,
    double OtherAnimalsScore,
    double ExperienceWeight,
    double SpaceWeight,
    double CareTimeWeight,
    double ChildrenWeight,
    double OtherAnimalsWeight
);

public record AdoptionApplicationDetailDto(
    Guid Id,
    string ApplicationNumber,
    Guid AdopterId,
    string AdopterName,
    string AdopterEmail,
    string AdopterPhone,
    Guid AnimalId,
    string AnimalName,
    string AnimalSpecies,
    string Status,
    DateTime ApplicationDate,
    // Dane rozpatrzenia
    Guid? ReviewedByUserId,
    DateTime? ReviewDate,
    string? ReviewNotes,
    // Dane wizyty
    DateTime? ScheduledVisitDate,
    DateTime? VisitDate,
    string? VisitNotes,
    int? VisitAssessment,
    Guid? VisitConductedByUserId,
    // Dane umowy
    DateTime? ContractGeneratedDate,
    string? ContractNumber,
    DateTime? ContractSignedDate,
    string? ContractFilePath,
    // Dodatkowe informacje
    string? AdoptionMotivation,
    string? PetExperience,
    string? LivingConditions,
    string? OtherPetsInfo,
    string? RejectionReason,
    string? CancellationReason,
    DateTime? CompletionDate,
    // Historia statusów
    IEnumerable<AdoptionApplicationStatusChangeDto>? StatusHistory,
    IEnumerable<string> PermittedActions,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    // Dopasowanie adopcyjne
    MatchScoreDto? MatchScore
);

// ============================================
// Handler
// ============================================
public class GetAdoptionApplicationByIdHandler
    : IQueryHandler<GetAdoptionApplicationByIdQuery, Result<AdoptionApplicationDetailDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IAdoptionMatchingService _matchingService;

    public GetAdoptionApplicationByIdHandler(
        ShelterDbContext context,
        IAdoptionMatchingService matchingService)
    {
        _context = context;
        _matchingService = matchingService;
    }

    public async Task<Result<AdoptionApplicationDetailDto>> Handle(
        GetAdoptionApplicationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AdoptionApplications
            .AsNoTracking()
            .AsQueryable();

        if (request.IncludeStatusHistory)
        {
            query = query.Include(a => a.StatusHistory);
        }

        var application = await query
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (application is null)
        {
            return Result.Failure<AdoptionApplicationDetailDto>(
                Error.NotFound("AdoptionApplication", request.Id));
        }

        // Pobierz dane adoptującego
        var adopter = await _context.Adopters
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == application.AdopterId, cancellationToken);

        // Pobierz dane zwierzęcia
        var animal = await _context.Animals
            .Include(a => a.Photos)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        // Pobierz dozwolone akcje
        var permittedActions = application.GetPermittedTriggers()
            .Select(t => t.ToString())
            .ToList();

        // Oblicz dopasowanie adopcyjne
        MatchScoreDto? matchScore = null;
        if (animal is not null)
        {
            var scoreResult = _matchingService.CalculateMatchScore(application, animal);
            if (scoreResult is not null)
            {
                matchScore = new MatchScoreDto(
                    TotalScore: scoreResult.TotalScore,
                    TotalPercentage: Math.Round(scoreResult.TotalScore * 100, 0),
                    ExperienceScore: scoreResult.ExperienceScore,
                    SpaceScore: scoreResult.SpaceScore,
                    CareTimeScore: scoreResult.CareTimeScore,
                    ChildrenScore: scoreResult.ChildrenScore,
                    OtherAnimalsScore: scoreResult.OtherAnimalsScore,
                    ExperienceWeight: scoreResult.Weights.Experience,
                    SpaceWeight: scoreResult.Weights.Space,
                    CareTimeWeight: scoreResult.Weights.CareTime,
                    ChildrenWeight: scoreResult.Weights.Children,
                    OtherAnimalsWeight: scoreResult.Weights.OtherAnimals
                );
            }
        }

        var dto = new AdoptionApplicationDetailDto(
            Id: application.Id,
            ApplicationNumber: application.Id.ToString()[..8].ToUpper(),
            AdopterId: application.AdopterId,
            AdopterName: adopter?.FullName ?? "Nieznany",
            AdopterEmail: adopter?.Email ?? "",
            AdopterPhone: adopter?.Phone ?? "",
            AnimalId: application.AnimalId,
            AnimalName: animal?.Name ?? "Nieznane",
            AnimalSpecies: animal?.Species.ToString() ?? "",
            Status: application.Status.ToString(),
            ApplicationDate: application.ApplicationDate,
            ReviewedByUserId: application.ReviewedByUserId,
            ReviewDate: application.ReviewDate,
            ReviewNotes: application.ReviewNotes,
            ScheduledVisitDate: application.ScheduledVisitDate,
            VisitDate: application.VisitDate,
            VisitNotes: application.VisitNotes,
            VisitAssessment: application.VisitAssessment,
            VisitConductedByUserId: application.VisitConductedByUserId,
            ContractGeneratedDate: application.ContractGeneratedDate,
            ContractNumber: application.ContractNumber,
            ContractSignedDate: application.ContractSignedDate,
            ContractFilePath: application.ContractFilePath,
            AdoptionMotivation: application.AdoptionMotivation,
            PetExperience: application.PetExperience,
            LivingConditions: application.LivingConditions,
            OtherPetsInfo: application.OtherPetsInfo,
            RejectionReason: application.RejectionReason,
            CancellationReason: application.Status == Domain.Adoptions.AdoptionApplicationStatus.Cancelled
                ? application.StatusHistory.LastOrDefault(s => s.NewStatus == Domain.Adoptions.AdoptionApplicationStatus.Cancelled)?.Reason
                : null,
            CompletionDate: application.CompletionDate,
            StatusHistory: request.IncludeStatusHistory
                ? application.StatusHistory
                    .OrderByDescending(s => s.ChangedAt)
                    .Select(s => s.ToDto())
                : null,
            PermittedActions: permittedActions,
            CreatedAt: application.CreatedAt,
            UpdatedAt: application.UpdatedAt,
            MatchScore: matchScore
        );

        return Result.Success(dto);
    }
}

// ============================================
// Validator
// ============================================
public class GetAdoptionApplicationByIdValidator : AbstractValidator<GetAdoptionApplicationByIdQuery>
{
    public GetAdoptionApplicationByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID zgłoszenia jest wymagane");
    }
}
