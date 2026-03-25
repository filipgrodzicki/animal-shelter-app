using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Queries;

// ============================================
// Query
// ============================================
/// <summary>
/// Pobiera listę zgłoszeń adopcyjnych z filtrowaniem i paginacją
/// </summary>
public record GetAdoptionApplicationsQuery(
    // Filtry
    AdoptionApplicationStatus? Status,
    Guid? AdopterId,
    Guid? AnimalId,
    DateTime? FromDate,
    DateTime? ToDate,
    string? SearchTerm,
    // Paginacja
    int Page = 1,
    int PageSize = 20,
    // Sortowanie
    string SortBy = "applicationDate",
    bool SortDescending = true
) : IQuery<Result<PagedResult<AdoptionApplicationListItemDto>>>;

// ============================================
// Handler
// ============================================
public class GetAdoptionApplicationsHandler
    : IQueryHandler<GetAdoptionApplicationsQuery, Result<PagedResult<AdoptionApplicationListItemDto>>>
{
    private readonly ShelterDbContext _context;

    public GetAdoptionApplicationsHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AdoptionApplicationListItemDto>>> Handle(
        GetAdoptionApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AdoptionApplications
            .AsNoTracking()
            .AsQueryable();

        // Filtry
        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        if (request.AdopterId.HasValue)
        {
            query = query.Where(a => a.AdopterId == request.AdopterId.Value);
        }

        if (request.AnimalId.HasValue)
        {
            query = query.Where(a => a.AnimalId == request.AnimalId.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.ApplicationDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.ApplicationDate <= request.ToDate.Value);
        }

        // Pobierz całkowitą liczbę
        var totalCount = await query.CountAsync(cancellationToken);

        // Sortowanie
        query = request.SortBy.ToLowerInvariant() switch
        {
            "status" => request.SortDescending
                ? query.OrderByDescending(a => a.Status)
                : query.OrderBy(a => a.Status),
            "completiondate" => request.SortDescending
                ? query.OrderByDescending(a => a.CompletionDate)
                : query.OrderBy(a => a.CompletionDate),
            "scheduledvisitdate" => request.SortDescending
                ? query.OrderByDescending(a => a.ScheduledVisitDate)
                : query.OrderBy(a => a.ScheduledVisitDate),
            _ => request.SortDescending
                ? query.OrderByDescending(a => a.ApplicationDate)
                : query.OrderBy(a => a.ApplicationDate)
        };

        // Paginacja
        var applications = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Pobierz dane adoptujących i zwierząt
        var adopterIds = applications.Select(a => a.AdopterId).Distinct().ToList();
        var animalIds = applications.Select(a => a.AnimalId).Distinct().ToList();

        var adopters = await _context.Adopters
            .Where(a => adopterIds.Contains(a.Id))
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var animals = await _context.Animals
            .Where(a => animalIds.Contains(a.Id))
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        // Filtrowanie po wyszukiwaniu (po pobraniu danych)
        var items = applications.Select(app =>
        {
            var adopter = adopters.GetValueOrDefault(app.AdopterId);
            var animal = animals.GetValueOrDefault(app.AnimalId);

            return app.ToListItemDto(
                adopterName: adopter?.FullName ?? "Nieznany",
                adopterEmail: adopter?.Email ?? "",
                animalName: animal?.Name ?? "Nieznane",
                animalRegistrationNumber: animal?.RegistrationNumber ?? "");
        }).ToList();

        // Filtrowanie po SearchTerm
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLowerInvariant();
            items = items.Where(i =>
                i.AdopterName.ToLowerInvariant().Contains(term) ||
                i.AdopterEmail.ToLowerInvariant().Contains(term) ||
                i.AnimalName.ToLowerInvariant().Contains(term) ||
                i.AnimalRegistrationNumber.ToLowerInvariant().Contains(term)
            ).ToList();
        }

        var pagedResult = new PagedResult<AdoptionApplicationListItemDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount);

        return Result.Success(pagedResult);
    }
}

// ============================================
// Validator
// ============================================
public class GetAdoptionApplicationsValidator : AbstractValidator<GetAdoptionApplicationsQuery>
{
    public GetAdoptionApplicationsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Numer strony musi być większy lub równy 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Rozmiar strony musi być między 1 a 100");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("Data końcowa musi być równa lub późniejsza niż data początkowa");
    }
}
