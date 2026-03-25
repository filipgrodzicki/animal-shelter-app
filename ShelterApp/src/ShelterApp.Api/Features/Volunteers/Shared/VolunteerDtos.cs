using ShelterApp.Domain.Volunteers;

namespace ShelterApp.Api.Features.Volunteers.Shared;

// ============================================
// Volunteer DTOs
// ============================================

public record VolunteerDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Phone,
    DateTime DateOfBirth,
    int Age,
    string? Address,
    string? City,
    string? PostalCode,
    string Status,
    DateTime ApplicationDate,
    DateTime? TrainingStartDate,
    DateTime? TrainingEndDate,
    DateTime? ContractSignedDate,
    string? ContractNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    IEnumerable<string> Skills,
    IEnumerable<DayOfWeek> Availability,
    decimal TotalHoursWorked,
    string? Notes,
    IEnumerable<string> PermittedActions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record VolunteerListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string Status,
    DateTime ApplicationDate,
    decimal TotalHoursWorked,
    IEnumerable<string> Skills
);

public record VolunteerSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    string Status
);

public record VolunteerStatusChangeDto(
    Guid Id,
    string PreviousStatus,
    string NewStatus,
    string Trigger,
    string ChangedBy,
    string? Reason,
    DateTime ChangedAt
);

// ============================================
// Schedule DTOs
// ============================================

public record ScheduleSlotDto(
    Guid Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxVolunteers,
    int CurrentVolunteers,
    bool HasAvailableSpots,
    string Description,
    bool IsActive,
    IEnumerable<VolunteerAssignmentDto>? Assignments,
    DateTime CreatedAt
);

public record ScheduleSlotListItemDto(
    Guid Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxVolunteers,
    int CurrentVolunteers,
    string Description,
    bool IsActive
);

public record VolunteerAssignmentDto(
    Guid Id,
    Guid ScheduleSlotId,
    Guid VolunteerId,
    string VolunteerName,
    string Status,
    DateTime AssignedAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    string? CancellationReason
);

public record CreateScheduleSlotsResultDto(
    int CreatedCount,
    IEnumerable<ScheduleSlotDto> Slots
);

// ============================================
// Attendance DTOs (WB-18)
// ============================================

public record AttendanceDto(
    Guid Id,
    Guid VolunteerId,
    string VolunteerName,
    Guid? ScheduleSlotId,
    string? SlotDescription,
    DateTime CheckInTime,
    DateTime? CheckOutTime,
    decimal? HoursWorked,
    string? Notes,
    string? WorkDescription,
    bool IsApproved,
    Guid? ApprovedByUserId,
    DateTime? ApprovedAt,
    DateTime CreatedAt
);

public record AttendanceListItemDto(
    Guid Id,
    Guid VolunteerId,
    string VolunteerName,
    DateOnly Date,
    TimeOnly CheckInTime,
    TimeOnly? CheckOutTime,
    decimal? HoursWorked,
    bool IsApproved
);

public record CheckInResultDto(
    Guid AttendanceId,
    Guid VolunteerId,
    DateTime CheckInTime,
    string Message
);

public record CheckOutResultDto(
    Guid AttendanceId,
    Guid VolunteerId,
    DateTime CheckInTime,
    DateTime CheckOutTime,
    decimal HoursWorked,
    string Message
);

// ============================================
// Report DTOs (WS-19)
// ============================================

public record VolunteerHoursReportDto(
    Guid VolunteerId,
    string VolunteerName,
    string Email,
    DateTime FromDate,
    DateTime ToDate,
    IEnumerable<AttendanceReportItemDto> Attendances,
    decimal TotalHoursWorked,
    int TotalDaysWorked,
    decimal AverageHoursPerDay,
    string? ReportContent,
    string? ContentType,
    string? FileName
);

public record AttendanceReportItemDto(
    DateTime Date,
    TimeOnly CheckInTime,
    TimeOnly? CheckOutTime,
    decimal? HoursWorked,
    string? SlotDescription,
    string? WorkDescription,
    bool IsApproved
);

public record VolunteerDetailDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Phone,
    DateTime DateOfBirth,
    int Age,
    string? Address,
    string? City,
    string? PostalCode,
    string Status,
    DateTime ApplicationDate,
    DateTime? TrainingStartDate,
    DateTime? TrainingEndDate,
    DateTime? ContractSignedDate,
    string? ContractNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    IEnumerable<string> Skills,
    IEnumerable<DayOfWeek> Availability,
    decimal TotalHoursWorked,
    string? Notes,
    IEnumerable<string> PermittedActions,
    IEnumerable<VolunteerStatusChangeDto> StatusHistory,
    IEnumerable<AttendanceListItemDto> RecentAttendances,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
