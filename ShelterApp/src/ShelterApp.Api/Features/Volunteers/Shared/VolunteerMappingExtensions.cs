using ShelterApp.Domain.Volunteers;

namespace ShelterApp.Api.Features.Volunteers.Shared;

public static class VolunteerMappingExtensions
{
    public static VolunteerDto ToDto(this Volunteer volunteer)
    {
        return new VolunteerDto(
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
            PermittedActions: volunteer.GetPermittedTriggers().Select(t => t.ToString()),
            CreatedAt: volunteer.CreatedAt,
            UpdatedAt: volunteer.UpdatedAt
        );
    }

    public static VolunteerListItemDto ToListItemDto(this Volunteer volunteer)
    {
        return new VolunteerListItemDto(
            Id: volunteer.Id,
            FullName: volunteer.FullName,
            Email: volunteer.Email,
            Phone: volunteer.Phone,
            Status: volunteer.Status.ToString(),
            ApplicationDate: volunteer.ApplicationDate,
            TotalHoursWorked: volunteer.TotalHoursWorked,
            Skills: volunteer.Skills
        );
    }

    public static VolunteerSummaryDto ToSummaryDto(this Volunteer volunteer)
    {
        return new VolunteerSummaryDto(
            Id: volunteer.Id,
            FullName: volunteer.FullName,
            Email: volunteer.Email,
            Status: volunteer.Status.ToString()
        );
    }

    public static VolunteerStatusChangeDto ToDto(this VolunteerStatusChange statusChange)
    {
        return new VolunteerStatusChangeDto(
            Id: statusChange.Id,
            PreviousStatus: statusChange.PreviousStatus.ToString(),
            NewStatus: statusChange.NewStatus.ToString(),
            Trigger: statusChange.Trigger.ToString(),
            ChangedBy: statusChange.ChangedBy,
            Reason: statusChange.Reason,
            ChangedAt: statusChange.ChangedAt
        );
    }

    public static ScheduleSlotDto ToDto(this ScheduleSlot slot, bool includeAssignments = false)
    {
        return new ScheduleSlotDto(
            Id: slot.Id,
            Date: slot.Date,
            StartTime: slot.StartTime,
            EndTime: slot.EndTime,
            MaxVolunteers: slot.MaxVolunteers,
            CurrentVolunteers: slot.CurrentVolunteers,
            HasAvailableSpots: slot.HasAvailableSpots,
            Description: slot.Description,
            IsActive: slot.IsActive,
            Assignments: includeAssignments
                ? slot.Assignments.Select(a => a.ToDto())
                : null,
            CreatedAt: slot.CreatedAt
        );
    }

    public static ScheduleSlotListItemDto ToListItemDto(this ScheduleSlot slot)
    {
        return new ScheduleSlotListItemDto(
            Id: slot.Id,
            Date: slot.Date,
            StartTime: slot.StartTime,
            EndTime: slot.EndTime,
            MaxVolunteers: slot.MaxVolunteers,
            CurrentVolunteers: slot.CurrentVolunteers,
            Description: slot.Description,
            IsActive: slot.IsActive
        );
    }

    public static VolunteerAssignmentDto ToDto(this VolunteerAssignment assignment, string? volunteerName = null)
    {
        return new VolunteerAssignmentDto(
            Id: assignment.Id,
            ScheduleSlotId: assignment.ScheduleSlotId,
            VolunteerId: assignment.VolunteerId,
            VolunteerName: volunteerName ?? "",
            Status: assignment.Status.ToString(),
            AssignedAt: assignment.AssignedAt,
            ConfirmedAt: assignment.ConfirmedAt,
            CancelledAt: assignment.CancelledAt,
            CancellationReason: assignment.CancellationReason
        );
    }

    public static AttendanceDto ToDto(this Attendance attendance, string? volunteerName = null, string? slotDescription = null)
    {
        return new AttendanceDto(
            Id: attendance.Id,
            VolunteerId: attendance.VolunteerId,
            VolunteerName: volunteerName ?? "",
            ScheduleSlotId: attendance.ScheduleSlotId,
            SlotDescription: slotDescription,
            CheckInTime: attendance.CheckInTime,
            CheckOutTime: attendance.CheckOutTime,
            HoursWorked: attendance.HoursWorked,
            Notes: attendance.Notes,
            WorkDescription: attendance.WorkDescription,
            IsApproved: attendance.IsApproved,
            ApprovedByUserId: attendance.ApprovedByUserId,
            ApprovedAt: attendance.ApprovedAt,
            CreatedAt: attendance.CreatedAt
        );
    }

    public static AttendanceListItemDto ToListItemDto(this Attendance attendance, string volunteerName)
    {
        return new AttendanceListItemDto(
            Id: attendance.Id,
            VolunteerId: attendance.VolunteerId,
            VolunteerName: volunteerName,
            Date: DateOnly.FromDateTime(attendance.CheckInTime),
            CheckInTime: TimeOnly.FromDateTime(attendance.CheckInTime),
            CheckOutTime: attendance.CheckOutTime.HasValue
                ? TimeOnly.FromDateTime(attendance.CheckOutTime.Value)
                : null,
            HoursWorked: attendance.HoursWorked,
            IsApproved: attendance.IsApproved
        );
    }
}
