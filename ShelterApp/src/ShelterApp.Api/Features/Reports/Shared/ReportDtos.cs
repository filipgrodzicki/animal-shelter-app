namespace ShelterApp.Api.Features.Reports.Shared;

// ============================================
// Statistics DTOs (WF-32)
// ============================================

/// <summary>
/// Ogólne statystyki schroniska
/// </summary>
public record ShelterStatisticsDto(
    DateTime FromDate,
    DateTime ToDate,
    AdoptionStatisticsDto Adoptions,
    VolunteerStatisticsDto Volunteers,
    AnimalStatisticsDto Animals
);

/// <summary>
/// Statystyki adopcji
/// </summary>
public record AdoptionStatisticsDto(
    int TotalApplications,
    int CompletedAdoptions,
    int RejectedApplications,
    int CancelledApplications,
    int PendingApplications,
    decimal AverageProcessingDays,
    List<AdoptionsByMonthDto> ByMonth,
    List<AdoptionsBySpeciesDto> BySpecies
);

public record AdoptionsByMonthDto(
    int Year,
    int Month,
    string MonthName,
    int ApplicationsCount,
    int CompletedCount
);

public record AdoptionsBySpeciesDto(
    string Species,
    string SpeciesLabel,
    int Count
);

/// <summary>
/// Statystyki wolontariuszy
/// </summary>
public record VolunteerStatisticsDto(
    int TotalActiveVolunteers,
    int NewVolunteersInPeriod,
    decimal TotalHoursWorked,
    decimal AverageHoursPerVolunteer,
    int TotalAttendances,
    List<VolunteerActivityByMonthDto> ByMonth,
    List<TopVolunteerDto> TopVolunteers
);

public record VolunteerActivityByMonthDto(
    int Year,
    int Month,
    string MonthName,
    int ActiveVolunteers,
    decimal HoursWorked,
    int AttendanceCount
);

public record TopVolunteerDto(
    Guid VolunteerId,
    string Name,
    decimal HoursWorked,
    int DaysWorked
);

/// <summary>
/// Statystyki zwierząt
/// </summary>
public record AnimalStatisticsDto(
    int TotalAnimalsInShelter,
    int AdmissionsInPeriod,
    int AdoptionsInPeriod,
    int DeceasedInPeriod,
    List<AnimalsBySpeciesDto> BySpecies,
    List<AnimalsByStatusDto> ByStatus
);

public record AnimalsBySpeciesDto(
    string Species,
    string SpeciesLabel,
    int Count
);

public record AnimalsByStatusDto(
    string Status,
    string StatusLabel,
    int Count
);

// ============================================
// Government Report DTOs (WF-33)
// ============================================

/// <summary>
/// Raport dla samorządu - zestawienie przyjęć i adopcji
/// </summary>
public record GovernmentReportDto(
    DateTime FromDate,
    DateTime ToDate,
    string ReportPeriod,
    ShelterInfoDto ShelterInfo,
    AdmissionsAndAdoptionsSummaryDto Summary,
    List<AdmissionRecordDto> Admissions,
    List<AdoptionRecordDto> Adoptions,
    VolunteerHoursSummaryDto VolunteerHours,
    string? ReportContent,
    string? ContentType,
    string? FileName
);

public record ShelterInfoDto(
    string Name,
    string Address,
    string City,
    string PostalCode,
    string Phone,
    string Email,
    string? Nip,
    string? Regon
);

public record AdmissionsAndAdoptionsSummaryDto(
    int TotalAdmissions,
    int AdmissionsDogs,
    int AdmissionsCats,
    int AdmissionsOther,
    int TotalAdoptions,
    int AdoptionsDogs,
    int AdoptionsCats,
    int AdoptionsOther,
    int TotalDeceased,
    int CurrentPopulation
);

public record AdmissionRecordDto(
    string RegistrationNumber,
    string Name,
    string Species,
    string Breed,
    string Gender,
    DateTime AdmissionDate,
    string AdmissionCircumstances,
    string CurrentStatus
);

public record AdoptionRecordDto(
    string RegistrationNumber,
    string AnimalName,
    string Species,
    string Breed,
    DateTime AdoptionDate,
    string ContractNumber,
    string AdopterInitials,
    string AdopterCity
);

public record VolunteerHoursSummaryDto(
    int TotalVolunteers,
    decimal TotalHoursWorked,
    int TotalDaysWorked,
    List<VolunteerHoursRecordDto> Records
);

public record VolunteerHoursRecordDto(
    string VolunteerName,
    string Email,
    decimal HoursWorked,
    int DaysWorked,
    int AttendanceCount
);
