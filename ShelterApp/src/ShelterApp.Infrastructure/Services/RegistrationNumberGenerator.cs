using Microsoft.EntityFrameworkCore;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Infrastructure.Services;

public interface IRegistrationNumberGenerator
{
    Task<string> GenerateAsync(Species species, CancellationToken cancellationToken = default);
}

public class RegistrationNumberGenerator : IRegistrationNumberGenerator
{
    private readonly ShelterDbContext _context;

    public RegistrationNumberGenerator(ShelterDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync(Species species, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = GetSpeciesPrefix(species);

        // Format: SCH/{PREFIX}/{YEAR}/{NUMBER}
        // np. SCH/DOG/2024/00001

        var pattern = $"SCH/{prefix}/{year}/%";

        var lastNumber = await _context.Animals
            .Where(a => EF.Functions.Like(a.RegistrationNumber, pattern))
            .OrderByDescending(a => a.RegistrationNumber)
            .Select(a => a.RegistrationNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int nextNumber = 1;

        if (lastNumber is not null)
        {
            var parts = lastNumber.Split('/');
            if (parts.Length == 4 && int.TryParse(parts[3], out var currentNumber))
            {
                nextNumber = currentNumber + 1;
            }
        }

        return $"SCH/{prefix}/{year}/{nextNumber:D5}";
    }

    private static string GetSpeciesPrefix(Species species) => species switch
    {
        Species.Dog => "DOG",
        Species.Cat => "CAT",
        _ => "UNK"
    };
}
