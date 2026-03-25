using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;
using ShelterApp.Infrastructure.Services;

namespace ShelterApp.Api.Features.Adoptions.Queries;

// ============================================
// Query
// ============================================
/// <summary>
/// Pobiera umowę adopcyjną w formacie PDF
/// </summary>
public record GetAdoptionContractQuery(Guid ApplicationId) : IQuery<Result<AdoptionContractResult>>;

// ============================================
// Result
// ============================================
public record AdoptionContractResult(
    byte[] PdfContent,
    string FileName,
    string ContentType
);

// ============================================
// Handler
// ============================================
public class GetAdoptionContractHandler : IQueryHandler<GetAdoptionContractQuery, Result<AdoptionContractResult>>
{
    private readonly ShelterDbContext _context;
    private readonly IContractGeneratorService _contractGenerator;
    private readonly ShelterOptions _shelterOptions;

    public GetAdoptionContractHandler(
        ShelterDbContext context,
        IContractGeneratorService contractGenerator,
        IOptions<ShelterOptions> shelterOptions)
    {
        _context = context;
        _contractGenerator = contractGenerator;
        _shelterOptions = shelterOptions.Value;
    }

    public async Task<Result<AdoptionContractResult>> Handle(
        GetAdoptionContractQuery request,
        CancellationToken cancellationToken)
    {
        // Pobierz zgłoszenie adopcyjne
        var application = await _context.AdoptionApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (application is null)
        {
            return Result.Failure<AdoptionContractResult>(
                Error.NotFound("AdoptionApplication", request.ApplicationId));
        }

        // Sprawdź czy umowa może być wygenerowana (musi być w odpowiednim statusie)
        if (application.Status != AdoptionApplicationStatus.PendingFinalization &&
            application.Status != AdoptionApplicationStatus.Completed)
        {
            return Result.Failure<AdoptionContractResult>(
                Error.Validation("Umowa może być wygenerowana tylko dla zgłoszeń w statusie 'Oczekujące na finalizację' lub 'Zakończone'"));
        }

        // Sprawdź czy numer umowy został wygenerowany
        if (string.IsNullOrWhiteSpace(application.ContractNumber))
        {
            return Result.Failure<AdoptionContractResult>(
                Error.Validation("Przed pobraniem umowy należy ją wygenerować (użyj endpointu GenerateContract)"));
        }

        // Pobierz dane adoptującego
        var adopter = await _context.Adopters
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == application.AdopterId, cancellationToken);

        if (adopter is null)
        {
            return Result.Failure<AdoptionContractResult>(
                Error.NotFound("Adopter", application.AdopterId));
        }

        // Pobierz dane zwierzęcia
        var animal = await _context.Animals
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == application.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<AdoptionContractResult>(
                Error.NotFound("Animal", application.AnimalId));
        }

        // Przygotuj dane do umowy
        var contractData = new AdoptionContractData
        {
            ContractNumber = application.ContractNumber,
            ContractDate = application.ContractGeneratedDate ?? DateTime.UtcNow,

            Shelter = new ShelterInfo
            {
                Name = _shelterOptions.Name,
                Address = _shelterOptions.Address,
                City = _shelterOptions.City,
                PostalCode = _shelterOptions.PostalCode,
                Phone = _shelterOptions.Phone,
                Email = _shelterOptions.Email,
                Nip = _shelterOptions.Nip,
                Regon = _shelterOptions.Regon
            },

            Adopter = new AdopterInfo
            {
                FirstName = adopter.FirstName,
                LastName = adopter.LastName,
                Address = adopter.Address ?? "Nie podano",
                City = adopter.City ?? "Nie podano",
                PostalCode = adopter.PostalCode ?? "00-000",
                Phone = adopter.Phone,
                Email = adopter.Email,
                DocumentNumber = "***********", // Ze względów bezpieczeństwa nie przechowujemy
                DateOfBirth = adopter.DateOfBirth
            },

            Animal = new AnimalInfo
            {
                RegistrationNumber = animal.RegistrationNumber,
                Name = animal.Name,
                Species = animal.Species switch
                {
                    Domain.Animals.Enums.Species.Dog => "Pies",
                    Domain.Animals.Enums.Species.Cat => "Kot",
                    _ => "Inne"
                },
                Breed = animal.Breed,
                Gender = animal.Gender switch
                {
                    Domain.Animals.Enums.Gender.Male => "Samiec",
                    Domain.Animals.Enums.Gender.Female => "Samica",
                    _ => "Nieznana"
                },
                Color = animal.Color,
                DateOfBirth = animal.AgeInMonths.HasValue
                    ? DateTime.UtcNow.AddMonths(-animal.AgeInMonths.Value)
                    : null,
                ChipNumber = animal.ChipNumber,
                IsSterilized = false, // TODO: Dodać pole do encji Animal
                IsVaccinated = true,  // TODO: Sprawdzić na podstawie MedicalRecords
                HealthNotes = animal.Description
            },

            Terms = new AdoptionTerms
            {
                RequiresSterilization = !false, // TODO: Sprawdzić na podstawie danych zwierzęcia
                SterilizationDeadline = DateTime.UtcNow.AddMonths(3),
                RequiresVaccination = false,
                VaccinationDeadline = null,
                AllowsHomeVisits = true,
                SpecialConditions = null
            }
        };

        // Wygeneruj PDF
        var pdfContent = await _contractGenerator.GenerateAdoptionContractAsync(contractData);

        var fileName = $"Umowa_adopcyjna_{application.ContractNumber}_{animal.Name}.pdf";

        return Result.Success(new AdoptionContractResult(
            PdfContent: pdfContent,
            FileName: fileName,
            ContentType: "application/pdf"
        ));
    }
}

// ============================================
// Validator
// ============================================
public class GetAdoptionContractValidator : AbstractValidator<GetAdoptionContractQuery>
{
    public GetAdoptionContractValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("ID zgłoszenia adopcyjnego jest wymagane");
    }
}
