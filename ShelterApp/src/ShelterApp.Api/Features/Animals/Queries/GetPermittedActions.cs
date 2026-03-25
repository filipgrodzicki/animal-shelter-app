using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Queries;

// ============================================
// Query
// ============================================
public record GetPermittedActionsQuery(Guid AnimalId) : IQuery<Result<PermittedActionsDto>>;

// ============================================
// Response DTO
// ============================================
public record PermittedActionsDto(
    Guid AnimalId,
    string CurrentStatus,
    IEnumerable<PermittedActionDto> Actions
);

public record PermittedActionDto(
    string Trigger,
    string DisplayName,
    string Description,
    bool RequiresReason
);

// ============================================
// Handler
// ============================================
public class GetPermittedActionsHandler : IQueryHandler<GetPermittedActionsQuery, Result<PermittedActionsDto>>
{
    private readonly ShelterDbContext _context;

    public GetPermittedActionsHandler(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PermittedActionsDto>> Handle(
        GetPermittedActionsQuery request,
        CancellationToken cancellationToken)
    {
        var animal = await _context.Animals
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<PermittedActionsDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        var permittedTriggers = animal.GetPermittedTriggers();

        var actions = permittedTriggers.Select(trigger => new PermittedActionDto(
            Trigger: trigger.ToString(),
            DisplayName: GetTriggerDisplayName(trigger),
            Description: GetTriggerDescription(trigger),
            RequiresReason: TriggerRequiresReason(trigger)
        )).ToList();

        return Result.Success(new PermittedActionsDto(
            AnimalId: animal.Id,
            CurrentStatus: animal.Status.ToString(),
            Actions: actions
        ));
    }

    private static string GetTriggerDisplayName(Domain.Animals.AnimalStatusTrigger trigger) => trigger switch
    {
        Domain.Animals.AnimalStatusTrigger.SkierowanieNaKwarantanne => "Skierowanie na kwarantannę",
        Domain.Animals.AnimalStatusTrigger.DopuszczenieDoAdopcji => "Dopuszczenie do adopcji",
        Domain.Animals.AnimalStatusTrigger.WykrycieChoroby => "Wykrycie choroby",
        Domain.Animals.AnimalStatusTrigger.ZakonczenieKwarantanny => "Zakończenie kwarantanny",
        Domain.Animals.AnimalStatusTrigger.Wyleczenie => "Wyleczenie",
        Domain.Animals.AnimalStatusTrigger.Zachorowanie => "Zachorowanie",
        Domain.Animals.AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego => "Złożenie zgłoszenia adopcyjnego",
        Domain.Animals.AnimalStatusTrigger.AnulowanieZgloszenia => "Anulowanie zgłoszenia",
        Domain.Animals.AnimalStatusTrigger.Rezygnacja => "Rezygnacja",
        Domain.Animals.AnimalStatusTrigger.ZatwierdznieZgloszenia => "Zatwierdzenie zgłoszenia",
        Domain.Animals.AnimalStatusTrigger.NegatywnaOcena => "Negatywna ocena",
        Domain.Animals.AnimalStatusTrigger.PodpisanieUmowy => "Podpisanie umowy adopcyjnej",
        Domain.Animals.AnimalStatusTrigger.Zgon => "Zgon",
        _ => trigger.ToString()
    };

    private static string GetTriggerDescription(Domain.Animals.AnimalStatusTrigger trigger) => trigger switch
    {
        Domain.Animals.AnimalStatusTrigger.SkierowanieNaKwarantanne =>
            "Zwierzę zostaje skierowane na okres kwarantanny zgodnie z procedurami schroniska",
        Domain.Animals.AnimalStatusTrigger.DopuszczenieDoAdopcji =>
            "Zwierzę zostaje dopuszczone do adopcji i będzie widoczne publicznie",
        Domain.Animals.AnimalStatusTrigger.WykrycieChoroby =>
            "Podczas kwarantanny wykryto chorobę wymagającą leczenia",
        Domain.Animals.AnimalStatusTrigger.ZakonczenieKwarantanny =>
            "Kwarantanna zakończona pomyślnie, zwierzę może być dopuszczone do adopcji",
        Domain.Animals.AnimalStatusTrigger.Wyleczenie =>
            "Leczenie zakończone pomyślnie, zwierzę może być dopuszczone do adopcji",
        Domain.Animals.AnimalStatusTrigger.Zachorowanie =>
            "Zwierzę zachorowało i wymaga leczenia",
        Domain.Animals.AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego =>
            "Potencjalny adoptujący złożył zgłoszenie adopcyjne",
        Domain.Animals.AnimalStatusTrigger.AnulowanieZgloszenia =>
            "Zgłoszenie adopcyjne zostało anulowane przez pracownika",
        Domain.Animals.AnimalStatusTrigger.Rezygnacja =>
            "Potencjalny adoptujący zrezygnował z adopcji",
        Domain.Animals.AnimalStatusTrigger.ZatwierdznieZgloszenia =>
            "Zgłoszenie adopcyjne zostało zatwierdzone, rozpoczyna się proces adopcji",
        Domain.Animals.AnimalStatusTrigger.NegatywnaOcena =>
            "Ocena adoptującego lub warunków jest negatywna, adopcja nie może być zrealizowana",
        Domain.Animals.AnimalStatusTrigger.PodpisanieUmowy =>
            "Umowa adopcyjna została podpisana, adopcja zakończona pomyślnie",
        Domain.Animals.AnimalStatusTrigger.Zgon =>
            "Zwierzę zmarło",
        _ => string.Empty
    };

    private static bool TriggerRequiresReason(Domain.Animals.AnimalStatusTrigger trigger) => trigger switch
    {
        Domain.Animals.AnimalStatusTrigger.Zgon => true,
        Domain.Animals.AnimalStatusTrigger.WykrycieChoroby => true,
        Domain.Animals.AnimalStatusTrigger.Zachorowanie => true,
        Domain.Animals.AnimalStatusTrigger.AnulowanieZgloszenia => true,
        Domain.Animals.AnimalStatusTrigger.Rezygnacja => true,
        Domain.Animals.AnimalStatusTrigger.NegatywnaOcena => true,
        _ => false
    };
}

// ============================================
// Validator
// ============================================
public class GetPermittedActionsValidator : AbstractValidator<GetPermittedActionsQuery>
{
    public GetPermittedActionsValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");
    }
}
