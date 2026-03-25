using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Commands;

// ============================================
// Command
// ============================================
public record DeleteAnimalPhotoCommand(
    Guid AnimalId,
    Guid PhotoId
) : ICommand<Result>;

// ============================================
// Handler
// ============================================
public class DeleteAnimalPhotoHandler : ICommandHandler<DeleteAnimalPhotoCommand, Result>
{
    private readonly ShelterDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DeleteAnimalPhotoHandler> _logger;

    public DeleteAnimalPhotoHandler(
        ShelterDbContext context,
        IWebHostEnvironment environment,
        ILogger<DeleteAnimalPhotoHandler> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteAnimalPhotoCommand request,
        CancellationToken cancellationToken)
    {
        // Get animal with photos
        var animal = await _context.Animals
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure(
                Error.NotFound("Animal", request.AnimalId));
        }

        // Get the photo to find its file path before removing
        var photo = animal.Photos.FirstOrDefault(p => p.Id == request.PhotoId);
        if (photo is null)
        {
            return Result.Failure(
                Error.NotFound("AnimalPhoto", request.PhotoId));
        }

        var filePath = photo.FilePath;

        // Remove photo from domain
        var result = animal.RemovePhoto(request.PhotoId);
        if (result.IsFailure)
        {
            return result;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Delete physical file
        try
        {
            var fullPath = Path.Combine(
                _environment.WebRootPath ?? "wwwroot",
                filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation(
                    "Deleted photo file {FilePath} for animal {AnimalId}",
                    fullPath,
                    animal.Id);
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the operation since the database record is already deleted
            _logger.LogWarning(
                ex,
                "Failed to delete photo file for animal {AnimalId}. File may need manual cleanup.",
                animal.Id);
        }

        _logger.LogInformation(
            "Photo {PhotoId} deleted for animal {AnimalId}",
            request.PhotoId,
            animal.Id);

        return Result.Success();
    }
}

// ============================================
// Validator
// ============================================
public class DeleteAnimalPhotoValidator : AbstractValidator<DeleteAnimalPhotoCommand>
{
    public DeleteAnimalPhotoValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.PhotoId)
            .NotEmpty().WithMessage("ID zdjęcia jest wymagane");
    }
}
