using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Queries;

// ============================================
// Query
// ============================================
public record GetVolunteersQuery(
    string? Status,
    string? SearchTerm,
    IEnumerable<string>? Skills,
    DayOfWeek? AvailableOn,
    string SortBy = "ApplicationDate",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20
) : IQuery<Result<PagedResult<VolunteerListItemDto>>>;

// ============================================
// Handler
// ============================================
public class GetVolunteersHandler : IQueryHandler<GetVolunteersQuery, Result<PagedResult<VolunteerListItemDto>>>
{
    private readonly ShelterDbContext _context;

    public GetVolunteersHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<VolunteerListItemDto>>> Handle(
        GetVolunteersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Volunteers
            .AsNoTracking()
            .AsQueryable();

        // Status filter
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<VolunteerStatus>(request.Status, true, out var status))
        {
            query = query.Where(v => v.Status == status);
        }

        // Search term filter (name, email, phone)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(v =>
                v.FirstName.ToLower().Contains(searchTerm) ||
                v.LastName.ToLower().Contains(searchTerm) ||
                v.Email.ToLower().Contains(searchTerm) ||
                v.Phone.Contains(searchTerm));
        }

        // Skills filter - match any of the provided skills
        if (request.Skills != null && request.Skills.Any())
        {
            var skillsList = request.Skills.ToList();
            query = query.Where(v => v.Skills.Any(s => skillsList.Contains(s)));
        }

        // Availability filter
        if (request.AvailableOn.HasValue)
        {
            query = query.Where(v => v.Availability.Contains(request.AvailableOn.Value));
        }

        // Sorting
        query = ApplySorting(query, request.SortBy, request.SortDescending);

        // Pagination
        var pagedResult = await query.ToPagedResultAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        var mappedResult = pagedResult.Map(v => v.ToListItemDto());

        return Result.Success(mappedResult);
    }

    private static IQueryable<Volunteer> ApplySorting(
        IQueryable<Volunteer> query,
        string sortBy,
        bool descending)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "name" or "fullname" => descending
                ? query.OrderByDescending(v => v.LastName).ThenByDescending(v => v.FirstName)
                : query.OrderBy(v => v.LastName).ThenBy(v => v.FirstName),
            "email" => descending
                ? query.OrderByDescending(v => v.Email)
                : query.OrderBy(v => v.Email),
            "status" => descending
                ? query.OrderByDescending(v => v.Status)
                : query.OrderBy(v => v.Status),
            "totalhours" or "hoursworked" => descending
                ? query.OrderByDescending(v => v.TotalHoursWorked)
                : query.OrderBy(v => v.TotalHoursWorked),
            _ => descending
                ? query.OrderByDescending(v => v.ApplicationDate)
                : query.OrderBy(v => v.ApplicationDate)
        };
    }
}

// ============================================
// Validator
// ============================================
public class GetVolunteersValidator : AbstractValidator<GetVolunteersQuery>
{
    private static readonly string[] ValidStatuses = Enum.GetNames<VolunteerStatus>();
    private static readonly string[] ValidSortFields =
        { "name", "fullname", "email", "status", "applicationdate", "totalhours", "hoursworked" };

    public GetVolunteersValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status musi być jednym z: {string.Join(", ", ValidStatuses)}");

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
