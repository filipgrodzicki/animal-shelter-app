using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Queries;

// ============================================
// Query
// ============================================
public record GetMedicalRecordByIdQuery(
    Guid AnimalId,
    Guid RecordId
) : IQuery<Result<MedicalRecordDto>>;

// ============================================
// Handler
// ============================================
public class GetMedicalRecordByIdHandler : IQueryHandler<GetMedicalRecordByIdQuery, Result<MedicalRecordDto>>
{
    private readonly ShelterDbContext _context;

    public GetMedicalRecordByIdHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MedicalRecordDto>> Handle(
        GetMedicalRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Check if animal exists
        var animalExists = await _context.Animals
            .AnyAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (!animalExists)
        {
            return Result.Failure<MedicalRecordDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        // Get medical record
        var record = await _context.MedicalRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(m =>
                m.Id == request.RecordId &&
                m.AnimalId == request.AnimalId,
                cancellationToken);

        if (record is null)
        {
            return Result.Failure<MedicalRecordDto>(
                Error.NotFound("MedicalRecord", request.RecordId));
        }

        return Result.Success(record.ToDto());
    }
}

// ============================================
// Validator
// ============================================
public class GetMedicalRecordByIdValidator : AbstractValidator<GetMedicalRecordByIdQuery>
{
    public GetMedicalRecordByIdValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.RecordId)
            .NotEmpty().WithMessage("ID rekordu medycznego jest wymagane");
    }
}
