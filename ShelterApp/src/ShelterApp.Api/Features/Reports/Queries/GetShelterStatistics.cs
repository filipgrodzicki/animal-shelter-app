using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Reports.Shared;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Reports.Queries;

// ============================================
// Query - Get Shelter Statistics (WF-32)
// ============================================
public record GetShelterStatisticsQuery(
    DateTime FromDate,
    DateTime ToDate
) : IQuery<Result<ShelterStatisticsDto>>;

// ============================================
// Handler
// ============================================
public class GetShelterStatisticsHandler
    : IQueryHandler<GetShelterStatisticsQuery, Result<ShelterStatisticsDto>>
{
    private readonly ShelterDbContext _context;

    private static readonly string[] PolishMonthNames = new[]
    {
        "", "styczeń", "luty", "marzec", "kwiecień", "maj", "czerwiec",
        "lipiec", "sierpień", "wrzesień", "październik", "listopad", "grudzień"
    };

    public GetShelterStatisticsHandler(ShelterDbContext context)
    {
        _context = context;
    }

    private static string GetPolishMonthName(int year, int month) => $"{PolishMonthNames[month]} {year}";

    public async Task<Result<ShelterStatisticsDto>> Handle(
        GetShelterStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = DateTime.SpecifyKind(request.FromDate.Date, DateTimeKind.Utc);
        var toDate = DateTime.SpecifyKind(request.ToDate.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        // Adoption statistics
        var adoptionStats = await GetAdoptionStatisticsAsync(fromDate, toDate, cancellationToken);

        // Volunteer statistics
        var volunteerStats = await GetVolunteerStatisticsAsync(fromDate, toDate, cancellationToken);

        // Animal statistics
        var animalStats = await GetAnimalStatisticsAsync(fromDate, toDate, cancellationToken);

        var result = new ShelterStatisticsDto(
            FromDate: fromDate,
            ToDate: request.ToDate.Date,
            Adoptions: adoptionStats,
            Volunteers: volunteerStats,
            Animals: animalStats
        );

        return Result.Success(result);
    }

    private async Task<AdoptionStatisticsDto> GetAdoptionStatisticsAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var applications = await _context.AdoptionApplications
            .Where(a => a.CreatedAt >= fromDate && a.CreatedAt <= toDate)
            .AsNoTracking()
            .ToListAsync(ct);

        var animalIds = applications.Select(a => a.AnimalId).Distinct().ToList();
        var animals = await _context.Animals
            .Where(a => animalIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Species })
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, ct);

        var completed = applications.Count(a => a.Status == AdoptionApplicationStatus.Completed);
        var rejected = applications.Count(a => a.Status == AdoptionApplicationStatus.Rejected);
        var cancelled = applications.Count(a => a.Status == AdoptionApplicationStatus.Cancelled);
        var pending = applications.Count(a =>
            a.Status != AdoptionApplicationStatus.Completed &&
            a.Status != AdoptionApplicationStatus.Rejected &&
            a.Status != AdoptionApplicationStatus.Cancelled);

        // Calculate average processing time for completed adoptions
        var completedApps = applications.Where(a =>
            a.Status == AdoptionApplicationStatus.Completed &&
            a.UpdatedAt.HasValue).ToList();

        var avgDays = completedApps.Any()
            ? (decimal)completedApps.Average(a => (a.UpdatedAt!.Value - a.CreatedAt).TotalDays)
            : 0;

        // By month
        var byMonth = applications
            .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
            .Select(g => new AdoptionsByMonthDto(
                Year: g.Key.Year,
                Month: g.Key.Month,
                MonthName: GetPolishMonthName(g.Key.Year, g.Key.Month),
                ApplicationsCount: g.Count(),
                CompletedCount: g.Count(a => a.Status == AdoptionApplicationStatus.Completed)
            ))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        // By species
        var bySpecies = applications
            .Where(a => a.Status == AdoptionApplicationStatus.Completed && animals.ContainsKey(a.AnimalId))
            .GroupBy(a => animals[a.AnimalId].Species)
            .Select(g => new AdoptionsBySpeciesDto(
                Species: g.Key.ToString(),
                SpeciesLabel: GetSpeciesLabel(g.Key),
                Count: g.Count()
            ))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new AdoptionStatisticsDto(
            TotalApplications: applications.Count,
            CompletedAdoptions: completed,
            RejectedApplications: rejected,
            CancelledApplications: cancelled,
            PendingApplications: pending,
            AverageProcessingDays: Math.Round(avgDays, 1),
            ByMonth: byMonth,
            BySpecies: bySpecies
        );
    }

    private async Task<VolunteerStatisticsDto> GetVolunteerStatisticsAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var activeVolunteers = await _context.Volunteers
            .Where(v => v.Status == VolunteerStatus.Active)
            .AsNoTracking()
            .ToListAsync(ct);

        var newVolunteers = activeVolunteers
            .Count(v => v.CreatedAt >= fromDate && v.CreatedAt <= toDate);

        var volunteerIds = activeVolunteers.Select(v => v.Id).ToList();

        var attendances = await _context.Attendances
            .Where(a => volunteerIds.Contains(a.VolunteerId))
            .Where(a => a.CheckInTime >= fromDate && a.CheckInTime <= toDate)
            .Where(a => a.CheckOutTime.HasValue)
            .AsNoTracking()
            .ToListAsync(ct);

        var totalHours = attendances.Sum(a => a.HoursWorked ?? 0);
        var avgHoursPerVolunteer = volunteerIds.Count > 0 ? totalHours / volunteerIds.Count : 0;

        // By month
        var byMonth = attendances
            .GroupBy(a => new { a.CheckInTime.Year, a.CheckInTime.Month })
            .Select(g => new VolunteerActivityByMonthDto(
                Year: g.Key.Year,
                Month: g.Key.Month,
                MonthName: GetPolishMonthName(g.Key.Year, g.Key.Month),
                ActiveVolunteers: g.Select(a => a.VolunteerId).Distinct().Count(),
                HoursWorked: g.Sum(a => a.HoursWorked ?? 0),
                AttendanceCount: g.Count()
            ))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        // Top volunteers
        var volunteerDict = activeVolunteers.ToDictionary(v => v.Id);
        var topVolunteers = attendances
            .GroupBy(a => a.VolunteerId)
            .Select(g => new
            {
                VolunteerId = g.Key,
                HoursWorked = g.Sum(a => a.HoursWorked ?? 0),
                DaysWorked = g.Select(a => a.CheckInTime.Date).Distinct().Count()
            })
            .OrderByDescending(x => x.HoursWorked)
            .Take(10)
            .Select(x => new TopVolunteerDto(
                VolunteerId: x.VolunteerId,
                Name: volunteerDict.TryGetValue(x.VolunteerId, out var v) ? v.FullName : "Nieznany",
                HoursWorked: x.HoursWorked,
                DaysWorked: x.DaysWorked
            ))
            .ToList();

        return new VolunteerStatisticsDto(
            TotalActiveVolunteers: activeVolunteers.Count,
            NewVolunteersInPeriod: newVolunteers,
            TotalHoursWorked: totalHours,
            AverageHoursPerVolunteer: Math.Round(avgHoursPerVolunteer, 2),
            TotalAttendances: attendances.Count,
            ByMonth: byMonth,
            TopVolunteers: topVolunteers
        );
    }

    private async Task<AnimalStatisticsDto> GetAnimalStatisticsAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var allAnimals = await _context.Animals
            .AsNoTracking()
            .ToListAsync(ct);

        // Current population (not adopted, not deceased)
        var currentPopulation = allAnimals.Count(a =>
            a.Status != AnimalStatus.Adopted &&
            a.Status != AnimalStatus.Deceased);

        // Admissions in period
        var admissions = allAnimals.Count(a =>
            a.AdmissionDate >= fromDate && a.AdmissionDate <= toDate);

        // Get adoption status changes in period
        var adoptedInPeriod = await _context.Set<Domain.Animals.Entities.AnimalStatusChange>()
            .Where(sc => sc.NewStatus == AnimalStatus.Adopted)
            .Where(sc => sc.ChangedAt >= fromDate && sc.ChangedAt <= toDate)
            .Select(sc => sc.AnimalId)
            .Distinct()
            .CountAsync(ct);

        // Deceased in period
        var deceasedInPeriod = await _context.Set<Domain.Animals.Entities.AnimalStatusChange>()
            .Where(sc => sc.NewStatus == AnimalStatus.Deceased)
            .Where(sc => sc.ChangedAt >= fromDate && sc.ChangedAt <= toDate)
            .Select(sc => sc.AnimalId)
            .Distinct()
            .CountAsync(ct);

        // By species (current population)
        var bySpecies = allAnimals
            .Where(a => a.Status != AnimalStatus.Adopted && a.Status != AnimalStatus.Deceased)
            .GroupBy(a => a.Species)
            .Select(g => new AnimalsBySpeciesDto(
                Species: g.Key.ToString(),
                SpeciesLabel: GetSpeciesLabel(g.Key),
                Count: g.Count()
            ))
            .OrderByDescending(x => x.Count)
            .ToList();

        // By status
        var byStatus = allAnimals
            .Where(a => a.Status != AnimalStatus.Adopted && a.Status != AnimalStatus.Deceased)
            .GroupBy(a => a.Status)
            .Select(g => new AnimalsByStatusDto(
                Status: g.Key.ToString(),
                StatusLabel: GetStatusLabel(g.Key),
                Count: g.Count()
            ))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new AnimalStatisticsDto(
            TotalAnimalsInShelter: currentPopulation,
            AdmissionsInPeriod: admissions,
            AdoptionsInPeriod: adoptedInPeriod,
            DeceasedInPeriod: deceasedInPeriod,
            BySpecies: bySpecies,
            ByStatus: byStatus
        );
    }

    private static string GetSpeciesLabel(Species species) => species switch
    {
        Species.Dog => "Psy",
        Species.Cat => "Koty",
        _ => species.ToString()
    };

    private static string GetStatusLabel(AnimalStatus status) => status switch
    {
        AnimalStatus.Admitted => "Przyjęte",
        AnimalStatus.Quarantine => "Kwarantanna",
        AnimalStatus.Treatment => "Leczenie",
        AnimalStatus.Available => "Dostępne",
        AnimalStatus.Reserved => "Zarezerwowane",
        AnimalStatus.InAdoptionProcess => "W procesie adopcji",
        AnimalStatus.Adopted => "Zaadoptowane",
        AnimalStatus.Deceased => "Zmarłe",
        _ => status.ToString()
    };
}

// ============================================
// Validator
// ============================================
public class GetShelterStatisticsValidator : AbstractValidator<GetShelterStatisticsQuery>
{
    public GetShelterStatisticsValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("Data początkowa jest wymagana");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("Data końcowa jest wymagana")
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("Data końcowa musi być większa lub równa dacie początkowej");

        RuleFor(x => x)
            .Must(x => (x.ToDate - x.FromDate).TotalDays <= 730)
            .WithMessage("Zakres dat nie może przekraczać 2 lat");
    }
}
