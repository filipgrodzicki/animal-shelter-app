using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Commands;

// ============================================
// Command
// ============================================
public record ChangeAnimalStatusCommand(
    Guid AnimalId,
    string Trigger,
    string? Reason,
    string ChangedBy
) : ICommand<Result<AnimalStatusChangeResultDto>>;

// ============================================
// Response DTO
// ============================================
public record AnimalStatusChangeResultDto(
    Guid AnimalId,
    string RegistrationNumber,
    string PreviousStatus,
    string NewStatus,
    string Trigger,
    string? Reason,
    string ChangedBy,
    DateTime ChangedAt,
    IEnumerable<string> PermittedTriggers
);

// ============================================
// Handler
// ============================================
public class ChangeAnimalStatusHandler : ICommandHandler<ChangeAnimalStatusCommand, Result<AnimalStatusChangeResultDto>>
{
    private readonly ShelterDbContext _context;

    public ChangeAnimalStatusHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AnimalStatusChangeResultDto>> Handle(
        ChangeAnimalStatusCommand request,
        CancellationToken cancellationToken)
    {
        // Parse trigger
        if (!Enum.TryParse<AnimalStatusTrigger>(request.Trigger, true, out var trigger))
        {
            return Result.Failure<AnimalStatusChangeResultDto>(
                Error.Validation($"Nieprawidłowa akcja: {request.Trigger}"));
        }

        // Get animal with status history
        var animal = await _context.Animals
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<AnimalStatusChangeResultDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        var previousStatus = animal.Status;

        // Try to change status
        var result = animal.ChangeStatus(trigger, request.ChangedBy, request.Reason);

        if (result.IsFailure)
        {
            return Result.Failure<AnimalStatusChangeResultDto>(result.Error);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Get permitted triggers for the new status
        var permittedTriggers = animal.GetPermittedTriggers()
            .Select(t => t.ToString())
            .ToList();

        return Result.Success(new AnimalStatusChangeResultDto(
            AnimalId: animal.Id,
            RegistrationNumber: animal.RegistrationNumber,
            PreviousStatus: previousStatus.ToString(),
            NewStatus: animal.Status.ToString(),
            Trigger: trigger.ToString(),
            Reason: request.Reason,
            ChangedBy: request.ChangedBy,
            ChangedAt: animal.StatusHistory.Last().ChangedAt,
            PermittedTriggers: permittedTriggers
        ));
    }
}

// ============================================
// Validator
// ============================================
public class ChangeAnimalStatusValidator : AbstractValidator<ChangeAnimalStatusCommand>
{
    private static readonly string[] ValidTriggers = Enum.GetNames<AnimalStatusTrigger>();

    public ChangeAnimalStatusValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.Trigger)
            .NotEmpty().WithMessage("Akcja jest wymagana")
            .Must(t => ValidTriggers.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Akcja musi być jedną z: {string.Join(", ", ValidTriggers)}");

        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Powód nie może przekraczać 1000 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));

        RuleFor(x => x.ChangedBy)
            .NotEmpty().WithMessage("Osoba zmieniająca status jest wymagana")
            .MaximumLength(200).WithMessage("Osoba zmieniająca nie może przekraczać 200 znaków");

        // Custom rule for specific triggers that require a reason
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Powód jest wymagany dla tej akcji")
            .When(x => RequiresReason(x.Trigger));
    }

    private static bool RequiresReason(string trigger)
    {
        var triggersRequiringReason = new[]
        {
            nameof(AnimalStatusTrigger.Zgon),
            nameof(AnimalStatusTrigger.WykrycieChoroby),
            nameof(AnimalStatusTrigger.Zachorowanie),
            nameof(AnimalStatusTrigger.AnulowanieZgloszenia),
            nameof(AnimalStatusTrigger.Rezygnacja),
            nameof(AnimalStatusTrigger.NegatywnaOcena)
        };

        return triggersRequiringReason.Contains(trigger, StringComparer.OrdinalIgnoreCase);
    }
}
