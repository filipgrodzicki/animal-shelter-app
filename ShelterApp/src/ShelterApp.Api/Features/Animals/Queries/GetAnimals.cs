using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Queries;

// ============================================
// Query
// ============================================
public record GetAnimalsQuery(
    string? Species,
    int? AgeMin,
    int? AgeMax,
    string? Gender,
    string? Size,
    string? Status,
    string? ExperienceLevel,
    string? ChildrenCompatibility,
    string? AnimalCompatibility,
    string? SpaceRequirement,
    string? CareTime,
    string? SearchTerm,
    bool PublicOnly = false,
    string SortBy = "AdmissionDate",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20
) : IQuery<Result<PagedResult<AnimalListItemDto>>>;

// ============================================
// Handler
// ============================================
public class GetAnimalsHandler : IQueryHandler<GetAnimalsQuery, Result<PagedResult<AnimalListItemDto>>>
{
    private readonly ShelterDbContext _context;

    public GetAnimalsHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AnimalListItemDto>>> Handle(
        GetAnimalsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Animals
            .Include(a => a.Photos.Where(p => p.IsMain))
            .AsNoTracking()
            .AsQueryable();

        // Public visibility filter (WS-07)
        if (request.PublicOnly)
        {
            query = query.Where(a =>
                a.Status == AnimalStatus.Available ||
                a.Status == AnimalStatus.Reserved ||
                a.Status == AnimalStatus.InAdoptionProcess);
        }

        // Species filter
        if (!string.IsNullOrWhiteSpace(request.Species) &&
            Enum.TryParse<Species>(request.Species, true, out var species))
        {
            query = query.Where(a => a.Species == species);
        }

        // Age filter (in months)
        if (request.AgeMin.HasValue)
        {
            query = query.Where(a => a.AgeInMonths >= request.AgeMin.Value);
        }
        if (request.AgeMax.HasValue)
        {
            query = query.Where(a => a.AgeInMonths <= request.AgeMax.Value);
        }

        // Gender filter
        if (!string.IsNullOrWhiteSpace(request.Gender) &&
            Enum.TryParse<Gender>(request.Gender, true, out var gender))
        {
            query = query.Where(a => a.Gender == gender);
        }

        // Size filter
        if (!string.IsNullOrWhiteSpace(request.Size) &&
            Enum.TryParse<Size>(request.Size, true, out var size))
        {
            query = query.Where(a => a.Size == size);
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<AnimalStatus>(request.Status, true, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        // Activity level filter
        if (!string.IsNullOrWhiteSpace(request.ExperienceLevel) &&
            Enum.TryParse<ExperienceLevel>(request.ExperienceLevel, true, out var experienceLevel))
        {
            query = query.Where(a => a.ExperienceLevel == experienceLevel);
        }

        // Children compatibility filter
        if (!string.IsNullOrWhiteSpace(request.ChildrenCompatibility) &&
            Enum.TryParse<ChildrenCompatibility>(request.ChildrenCompatibility, true, out var childrenCompat))
        {
            query = query.Where(a => a.ChildrenCompatibility == childrenCompat);
        }

        // Animal compatibility filter
        if (!string.IsNullOrWhiteSpace(request.AnimalCompatibility) &&
            Enum.TryParse<AnimalCompatibility>(request.AnimalCompatibility, true, out var animalCompat))
        {
            query = query.Where(a => a.AnimalCompatibility == animalCompat);
        }

        // Space requirement filter
        if (!string.IsNullOrWhiteSpace(request.SpaceRequirement) &&
            Enum.TryParse<SpaceRequirement>(request.SpaceRequirement, true, out var spaceReq))
        {
            query = query.Where(a => a.SpaceRequirement == spaceReq);
        }

        // Care time filter
        if (!string.IsNullOrWhiteSpace(request.CareTime) &&
            Enum.TryParse<CareTime>(request.CareTime, true, out var careTime))
        {
            query = query.Where(a => a.CareTime == careTime);
        }

        // Search term filter (name, breed, registration number)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(searchTerm) ||
                a.Breed.ToLower().Contains(searchTerm) ||
                a.RegistrationNumber.ToLower().Contains(searchTerm) ||
                (a.ChipNumber != null && a.ChipNumber.ToLower().Contains(searchTerm)));
        }

        // Sorting
        query = ApplySorting(query, request.SortBy, request.SortDescending);

        // Pagination
        var pagedResult = await query.ToPagedResultAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        var mappedResult = pagedResult.Map(a => a.ToListItemDto());

        return Result.Success(mappedResult);
    }

    private static IQueryable<Animal> ApplySorting(
        IQueryable<Animal> query,
        string sortBy,
        bool descending)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "name" => descending
                ? query.OrderByDescending(a => a.Name)
                : query.OrderBy(a => a.Name),
            "age" => descending
                ? query.OrderByDescending(a => a.AgeInMonths)
                : query.OrderBy(a => a.AgeInMonths),
            "species" => descending
                ? query.OrderByDescending(a => a.Species)
                : query.OrderBy(a => a.Species),
            "status" => descending
                ? query.OrderByDescending(a => a.Status)
                : query.OrderBy(a => a.Status),
            "registrationnumber" => descending
                ? query.OrderByDescending(a => a.RegistrationNumber)
                : query.OrderBy(a => a.RegistrationNumber),
            _ => descending
                ? query.OrderByDescending(a => a.AdmissionDate)
                : query.OrderBy(a => a.AdmissionDate)
        };
    }
}

// ============================================
// Validator
// ============================================
public class GetAnimalsValidator : AbstractValidator<GetAnimalsQuery>
{
    private static readonly string[] ValidSpecies = Enum.GetNames<Species>();
    private static readonly string[] ValidGenders = Enum.GetNames<Gender>();
    private static readonly string[] ValidSizes = Enum.GetNames<Size>();
    private static readonly string[] ValidStatuses = Enum.GetNames<AnimalStatus>();
    private static readonly string[] ValidExperienceLevels = Enum.GetNames<ExperienceLevel>();
    private static readonly string[] ValidChildrenCompatibility = Enum.GetNames<ChildrenCompatibility>();
    private static readonly string[] ValidAnimalCompatibility = Enum.GetNames<AnimalCompatibility>();
    private static readonly string[] ValidSpaceRequirements = Enum.GetNames<SpaceRequirement>();
    private static readonly string[] ValidCareTime = Enum.GetNames<CareTime>();
    private static readonly string[] ValidSortFields = { "name", "age", "species", "status", "admissiondate", "registrationnumber" };

    public GetAnimalsValidator()
    {
        RuleFor(x => x.Species)
            .Must(s => string.IsNullOrWhiteSpace(s) || ValidSpecies.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Gatunek musi być jednym z: {string.Join(", ", ValidSpecies)}");

        RuleFor(x => x.Gender)
            .Must(g => string.IsNullOrWhiteSpace(g) || ValidGenders.Contains(g, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Płeć musi być jedną z: {string.Join(", ", ValidGenders)}");

        RuleFor(x => x.Size)
            .Must(s => string.IsNullOrWhiteSpace(s) || ValidSizes.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Rozmiar musi być jednym z: {string.Join(", ", ValidSizes)}");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status musi być jednym z: {string.Join(", ", ValidStatuses)}");

        RuleFor(x => x.ExperienceLevel)
            .Must(a => string.IsNullOrWhiteSpace(a) || ValidExperienceLevels.Contains(a, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Wymagane doświadczenie musi być jednym z: {string.Join(", ", ValidExperienceLevels)}");

        RuleFor(x => x.ChildrenCompatibility)
            .Must(c => string.IsNullOrWhiteSpace(c) || ValidChildrenCompatibility.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Zgodność z dziećmi musi być jedną z: {string.Join(", ", ValidChildrenCompatibility)}");

        RuleFor(x => x.AnimalCompatibility)
            .Must(a => string.IsNullOrWhiteSpace(a) || ValidAnimalCompatibility.Contains(a, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Zgodność z innymi zwierzętami musi być jedną z: {string.Join(", ", ValidAnimalCompatibility)}");

        RuleFor(x => x.SpaceRequirement)
            .Must(s => string.IsNullOrWhiteSpace(s) || ValidSpaceRequirements.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Wymagania przestrzeni muszą być jednym z: {string.Join(", ", ValidSpaceRequirements)}");

        RuleFor(x => x.CareTime)
            .Must(c => string.IsNullOrWhiteSpace(c) || ValidCareTime.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Czas opieki musi być jednym z: {string.Join(", ", ValidCareTime)}");

        RuleFor(x => x.AgeMin)
            .GreaterThanOrEqualTo(0).WithMessage("Minimalny wiek musi być liczbą nieujemną")
            .When(x => x.AgeMin.HasValue);

        RuleFor(x => x.AgeMax)
            .GreaterThanOrEqualTo(0).WithMessage("Maksymalny wiek musi być liczbą nieujemną")
            .GreaterThanOrEqualTo(x => x.AgeMin ?? 0).WithMessage("Maksymalny wiek musi być większy lub równy minimalnemu")
            .When(x => x.AgeMax.HasValue);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Numer strony musi być większy lub równy 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Rozmiar strony musi być między 1 a 100");

        RuleFor(x => x.SortBy)
            .Must(s => ValidSortFields.Contains(s.ToLowerInvariant()))
            .WithMessage($"Pole sortowania musi być jednym z: {string.Join(", ", ValidSortFields)}");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("Wyszukiwana fraza nie może przekraczać 100 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));
    }
}
