using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Queries;

// ============================================
// Query
// ============================================
public record GetMyVolunteerQuery(Guid UserId) : IQuery<Result<VolunteerDetailDto>>;

// ============================================
// Handler
// ============================================
public class GetMyVolunteerHandler : IQueryHandler<GetMyVolunteerQuery, Result<VolunteerDetailDto>>
{
    private readonly ShelterDbContext _context;

    public GetMyVolunteerHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<VolunteerDetailDto>> Handle(
        GetMyVolunteerQuery request,
        CancellationToken cancellationToken)
    {
        var volunteer = await _context.Volunteers
            .Include(v => v.StatusHistory)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == request.UserId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<VolunteerDetailDto>(
                Error.NotFound("Volunteer", $"UserId: {request.UserId}"));
        }

        // Get recent attendances (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentAttendances = await _context.Attendances
            .Where(a => a.VolunteerId == volunteer.Id && a.CheckInTime >= thirtyDaysAgo)
            .OrderByDescending(a => a.CheckInTime)
            .Take(20)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var permittedActions = volunteer.GetPermittedTriggers()
            .Select(t => t.ToString())
            .ToList();

        var dto = new VolunteerDetailDto(
            Id: volunteer.Id,
            UserId: volunteer.UserId,
            FirstName: volunteer.FirstName,
            LastName: volunteer.LastName,
            FullName: volunteer.FullName,
            Email: volunteer.Email,
            Phone: volunteer.Phone,
            DateOfBirth: volunteer.DateOfBirth,
            Age: volunteer.Age,
            Address: volunteer.Address,
            City: volunteer.City,
            PostalCode: volunteer.PostalCode,
            Status: volunteer.Status.ToString(),
            ApplicationDate: volunteer.ApplicationDate,
            TrainingStartDate: volunteer.TrainingStartDate,
            TrainingEndDate: volunteer.TrainingEndDate,
            ContractSignedDate: volunteer.ContractSignedDate,
            ContractNumber: volunteer.ContractNumber,
            EmergencyContactName: volunteer.EmergencyContactName,
            EmergencyContactPhone: volunteer.EmergencyContactPhone,
            Skills: volunteer.Skills,
            Availability: volunteer.Availability,
            TotalHoursWorked: volunteer.TotalHoursWorked,
            Notes: volunteer.Notes,
            PermittedActions: permittedActions,
            StatusHistory: volunteer.StatusHistory
                .OrderByDescending(s => s.ChangedAt)
                .Select(s => s.ToDto()),
            RecentAttendances: recentAttendances
                .Select(a => a.ToListItemDto(volunteer.FullName)),
            CreatedAt: volunteer.CreatedAt,
            UpdatedAt: volunteer.UpdatedAt
        );

        return Result.Success(dto);
    }
}

// ============================================
// Validator
// ============================================
public class GetMyVolunteerValidator : AbstractValidator<GetMyVolunteerQuery>
{
    public GetMyVolunteerValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID użytkownika jest wymagane");
    }
}
