using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Queries;

// ============================================
// Query
// ============================================
public record GetAnimalStatusHistoryQuery(
    Guid AnimalId,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 20
) : IQuery<Result<AnimalStatusHistoryResultDto>>;

// ============================================
// Response DTO
// ============================================
public record AnimalStatusHistoryResultDto(
    Guid AnimalId,
    string RegistrationNumber,
    string CurrentStatus,
    IEnumerable<string> PermittedActions,
    PagedResult<AnimalStatusChangeDto> History
);

// ============================================
// Handler
// ============================================
public class GetAnimalStatusHistoryHandler
    : IQueryHandler<GetAnimalStatusHistoryQuery, Result<AnimalStatusHistoryResultDto>>
{
    private readonly ShelterDbContext _context;

    public GetAnimalStatusHistoryHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AnimalStatusHistoryResultDto>> Handle(
        GetAnimalStatusHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // Get animal basic info
        var animal = await _context.Animals
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<AnimalStatusHistoryResultDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        // Build query for status history
        var historyQuery = _context.AnimalStatusChanges
            .AsNoTracking()
            .Where(s => s.AnimalId == request.AnimalId);

        // Date filters
        if (request.FromDate.HasValue)
        {
            historyQuery = historyQuery.Where(s => s.ChangedAt >= request.FromDate.Value);
        }
        if (request.ToDate.HasValue)
        {
            historyQuery = historyQuery.Where(s => s.ChangedAt <= request.ToDate.Value);
        }

        // Order by date descending (newest first)
        historyQuery = historyQuery.OrderByDescending(s => s.ChangedAt);

        // Pagination
        var pagedHistory = await historyQuery.ToPagedResultAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        var mappedHistory = pagedHistory.Map(s => s.ToDto());

        // Get permitted actions for current status
        var permittedActions = animal.GetPermittedTriggers()
            .Select(t => t.ToString())
            .ToList();

        return Result.Success(new AnimalStatusHistoryResultDto(
            AnimalId: animal.Id,
            RegistrationNumber: animal.RegistrationNumber,
            CurrentStatus: animal.Status.ToString(),
            PermittedActions: permittedActions,
            History: mappedHistory
        ));
    }
}

// ============================================
// Validator
// ============================================
public class GetAnimalStatusHistoryValidator : AbstractValidator<GetAnimalStatusHistoryQuery>
{
    public GetAnimalStatusHistoryValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .WithMessage("Data początkowa musi być wcześniejsza lub równa dacie końcowej")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Numer strony musi być większy lub równy 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Rozmiar strony musi być między 1 a 100");
    }
}
