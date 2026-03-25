using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Volunteers.Commands;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Queries;

// ============================================
// Query
// ============================================
public record GetAnimalNotesQuery(
    Guid AnimalId,
    string? NoteType = null,
    bool? IsImportant = null,
    int Page = 1,
    int PageSize = 20
) : IQuery<Result<PagedResult<AnimalNoteDto>>>;

// ============================================
// Handler
// ============================================
public class GetAnimalNotesHandler : IQueryHandler<GetAnimalNotesQuery, Result<PagedResult<AnimalNoteDto>>>
{
    private readonly ShelterDbContext _context;

    public GetAnimalNotesHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AnimalNoteDto>>> Handle(
        GetAnimalNotesQuery request,
        CancellationToken cancellationToken)
    {
        // Verify animal exists
        var animalExists = await _context.Animals
            .AnyAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (!animalExists)
        {
            return Result.Failure<PagedResult<AnimalNoteDto>>(
                Error.NotFound("Animal", request.AnimalId));
        }

        var query = _context.AnimalNotes
            .AsNoTracking()
            .Where(n => n.AnimalId == request.AnimalId);

        // Apply filters
        if (!string.IsNullOrEmpty(request.NoteType) &&
            Enum.TryParse<AnimalNoteType>(request.NoteType, out var noteType))
        {
            query = query.Where(n => n.NoteType == noteType);
        }

        if (request.IsImportant.HasValue)
        {
            query = query.Where(n => n.IsImportant == request.IsImportant.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated results
        var notes = await query
            .OrderByDescending(n => n.ObservationDate)
            .ThenByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = notes.Select(n => n.ToDto()).ToList();

        var result = new PagedResult<AnimalNoteDto>(
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
public class GetAnimalNotesValidator : AbstractValidator<GetAnimalNotesQuery>
{
    public GetAnimalNotesValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Numer strony musi być większy od 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Rozmiar strony musi być większy od 0")
            .LessThanOrEqualTo(100).WithMessage("Rozmiar strony nie może przekraczać 100");
    }
}
