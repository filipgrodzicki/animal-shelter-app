using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Adoptions.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Adoptions.Queries;

/// <summary>
/// Gets adoption applications for the logged-in user
/// </summary>
public record GetMyAdoptionApplicationsQuery(
    Guid UserId,
    AdoptionApplicationStatus? Status,
    int Page = 1,
    int PageSize = 20
) : IQuery<Result<PagedResult<AdoptionApplicationListItemDto>>>;

public class GetMyAdoptionApplicationsHandler
    : IQueryHandler<GetMyAdoptionApplicationsQuery, Result<PagedResult<AdoptionApplicationListItemDto>>>
{
    private readonly ShelterDbContext _context;

    public GetMyAdoptionApplicationsHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AdoptionApplicationListItemDto>>> Handle(
        GetMyAdoptionApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        // Find adopter linked to the user or by email
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Success(new PagedResult<AdoptionApplicationListItemDto>(
                new List<AdoptionApplicationListItemDto>(), 0, request.Page, request.PageSize));
        }

        // Find adopter - may be linked by UserId or Email
        var adopter = await _context.Adopters
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == request.UserId || a.Email == user.Email, cancellationToken);

        if (adopter is null)
        {
            return Result.Success(new PagedResult<AdoptionApplicationListItemDto>(
                new List<AdoptionApplicationListItemDto>(), 0, request.Page, request.PageSize));
        }

        // Get adoption applications for this adopter
        var query = _context.AdoptionApplications
            .AsNoTracking()
            .Where(a => a.AdopterId == adopter.Id);

        // Filter by status
        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting and pagination
        var applications = await query
            .OrderByDescending(a => a.ApplicationDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Fetch animal data
        var animalIds = applications.Select(a => a.AnimalId).Distinct().ToList();
        var animals = await _context.Animals
            .Where(a => animalIds.Contains(a.Id))
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        // Map to DTOs
        var items = applications.Select(app =>
        {
            var animal = animals.GetValueOrDefault(app.AnimalId);

            return app.ToListItemDto(
                adopterName: adopter.FullName,
                adopterEmail: adopter.Email,
                animalName: animal?.Name ?? "Nieznane",
                animalRegistrationNumber: animal?.RegistrationNumber ?? "");
        }).ToList();

        var pagedResult = new PagedResult<AdoptionApplicationListItemDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount);

        return Result.Success(pagedResult);
    }
}
