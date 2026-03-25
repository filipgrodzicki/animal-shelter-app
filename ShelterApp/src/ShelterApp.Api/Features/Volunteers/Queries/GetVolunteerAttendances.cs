using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Queries;

// ============================================
// Query
// ============================================
public record GetVolunteerAttendancesQuery(
    Guid VolunteerId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
) : IQuery<Result<PagedResult<AttendanceListItemDto>>>;

// ============================================
// Handler
// ============================================
public class GetVolunteerAttendancesHandler : IQueryHandler<GetVolunteerAttendancesQuery, Result<PagedResult<AttendanceListItemDto>>>
{
    private readonly ShelterDbContext _context;

    public GetVolunteerAttendancesHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AttendanceListItemDto>>> Handle(
        GetVolunteerAttendancesQuery request,
        CancellationToken cancellationToken)
    {
        // Verify volunteer exists
        var volunteer = await _context.Volunteers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<PagedResult<AttendanceListItemDto>>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        var query = _context.Attendances
            .AsNoTracking()
            .Where(a => a.VolunteerId == request.VolunteerId);

        // Apply date filters
        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.CheckInTime >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            var toDateEnd = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CheckInTime < toDateEnd);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated results
        var attendances = await query
            .OrderByDescending(a => a.CheckInTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = attendances.Select(a => a.ToListItemDto(volunteer.FullName)).ToList();

        var result = new PagedResult<AttendanceListItemDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount
        );

        return Result.Success(result);
    }
}

// ============================================
// Validator
// ============================================
public class GetVolunteerAttendancesValidator : AbstractValidator<GetVolunteerAttendancesQuery>
{
    public GetVolunteerAttendancesValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Numer strony musi być większy od 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Rozmiar strony musi być większy od 0")
            .LessThanOrEqualTo(100).WithMessage("Rozmiar strony nie może przekraczać 100");
    }
}
