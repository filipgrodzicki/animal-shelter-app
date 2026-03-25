using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Queries;

// ============================================
// Query
// ============================================
public record GetCurrentAttendanceQuery(Guid VolunteerId) : IQuery<Result<AttendanceDto?>>;

// ============================================
// Handler
// ============================================
public class GetCurrentAttendanceHandler : IQueryHandler<GetCurrentAttendanceQuery, Result<AttendanceDto?>>
{
    private readonly ShelterDbContext _context;

    public GetCurrentAttendanceHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AttendanceDto?>> Handle(
        GetCurrentAttendanceQuery request,
        CancellationToken cancellationToken)
    {
        // Verify volunteer exists
        var volunteer = await _context.Volunteers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<AttendanceDto?>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        // Get active attendance (not checked out yet)
        var activeAttendance = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.VolunteerId == request.VolunteerId && a.CheckOutTime == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeAttendance is null)
        {
            return Result.Success<AttendanceDto?>(null);
        }

        var dto = activeAttendance.ToDto(volunteer.FullName);
        return Result.Success<AttendanceDto?>(dto);
    }
}

// ============================================
// Validator
// ============================================
public class GetCurrentAttendanceValidator : AbstractValidator<GetCurrentAttendanceQuery>
{
    public GetCurrentAttendanceValidator()
    {
        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");
    }
}
