using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Commands;

// ============================================
// Command
// ============================================
public record UploadAnimalPhotoCommand(
    Guid AnimalId,
    string FileName,
    string ContentType,
    long FileSize,
    Stream FileStream,
    bool IsMain,
    string? Description
) : ICommand<Result<AnimalPhotoDto>>;

// ============================================
// Handler
// ============================================
public class UploadAnimalPhotoHandler : ICommandHandler<UploadAnimalPhotoCommand, Result<AnimalPhotoDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadAnimalPhotoHandler> _logger;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public UploadAnimalPhotoHandler(
        ShelterDbContext context,
        IWebHostEnvironment environment,
        ILogger<UploadAnimalPhotoHandler> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<Result<AnimalPhotoDto>> Handle(
        UploadAnimalPhotoCommand request,
        CancellationToken cancellationToken)
    {
        // Validate file
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return Result.Failure<AnimalPhotoDto>(
                Error.Validation($"Niedozwolone rozszerzenie pliku. Dozwolone: {string.Join(", ", AllowedExtensions)}"));
        }

        if (!AllowedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
        {
            return Result.Failure<AnimalPhotoDto>(
                Error.Validation($"Niedozwolony typ pliku. Dozwolone: {string.Join(", ", AllowedContentTypes)}"));
        }

        if (request.FileSize > MaxFileSize)
        {
            return Result.Failure<AnimalPhotoDto>(
                Error.Validation($"Plik jest zbyt duży. Maksymalny rozmiar: {MaxFileSize / 1024 / 1024} MB"));
        }

        // Get animal
        var animal = await _context.Animals
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<AnimalPhotoDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        // Generate unique file name and path
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "animals", animal.Id.ToString());

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        var relativePath = $"/uploads/animals/{animal.Id}/{uniqueFileName}";

        try
        {
            // Save file to disk
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await request.FileStream.CopyToAsync(fileStream, cancellationToken);

            // Add photo to animal
            var result = animal.AddPhoto(
                fileName: request.FileName,
                filePath: relativePath,
                contentType: request.ContentType,
                fileSize: request.FileSize,
                isMain: request.IsMain,
                description: request.Description
            );

            if (result.IsFailure)
            {
                // Clean up file if domain operation failed
                if (File.Exists(filePath))
                    File.Delete(filePath);

                return Result.Failure<AnimalPhotoDto>(result.Error);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Photo {PhotoId} uploaded for animal {AnimalId}",
                result.Value.Id,
                animal.Id);

            return Result.Success(result.Value.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for animal {AnimalId}", animal.Id);

            // Clean up file on error
            if (File.Exists(filePath))
                File.Delete(filePath);

            return Result.Failure<AnimalPhotoDto>(
                Error.Validation("Wystąpił błąd podczas zapisywania pliku"));
        }
    }
}

// ============================================
// Validator
// ============================================
public class UploadAnimalPhotoValidator : AbstractValidator<UploadAnimalPhotoCommand>
{
    public UploadAnimalPhotoValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Nazwa pliku jest wymagana")
            .MaximumLength(255).WithMessage("Nazwa pliku nie może przekraczać 255 znaków");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Typ pliku jest wymagany");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("Plik nie może być pusty");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("Strumień pliku jest wymagany");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Opis nie może przekraczać 500 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
