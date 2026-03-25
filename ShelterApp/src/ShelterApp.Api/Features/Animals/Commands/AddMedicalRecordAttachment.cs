using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Domain.Common;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Animals.Commands;

// ============================================
// Command
// ============================================
public record AddMedicalRecordAttachmentCommand(
    Guid AnimalId,
    Guid MedicalRecordId,
    IFormFile File,
    string? Description
) : ICommand<Result<MedicalRecordAttachmentDto>>;

// ============================================
// Handler
// ============================================
public class AddMedicalRecordAttachmentHandler : ICommandHandler<AddMedicalRecordAttachmentCommand, Result<MedicalRecordAttachmentDto>>
{
    private readonly ShelterDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AddMedicalRecordAttachmentHandler> _logger;

    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public AddMedicalRecordAttachmentHandler(
        ShelterDbContext context,
        IWebHostEnvironment environment,
        ILogger<AddMedicalRecordAttachmentHandler> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<Result<MedicalRecordAttachmentDto>> Handle(
        AddMedicalRecordAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        // Get animal with medical records and attachments
        var animal = await _context.Animals
            .Include(a => a.MedicalRecords)
                .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<MedicalRecordAttachmentDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        // Find medical record
        var medicalRecord = animal.MedicalRecords
            .FirstOrDefault(m => m.Id == request.MedicalRecordId);

        if (medicalRecord is null)
        {
            return Result.Failure<MedicalRecordAttachmentDto>(
                Error.NotFound("MedicalRecord", request.MedicalRecordId));
        }

        // Validate file extension
        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return Result.Failure<MedicalRecordAttachmentDto>(
                Error.Validation($"Niedozwolony typ pliku. Dozwolone: {string.Join(", ", AllowedExtensions)}"));
        }

        // Validate file size
        if (request.File.Length > MaxFileSize)
        {
            return Result.Failure<MedicalRecordAttachmentDto>(
                Error.Validation($"Plik jest zbyt duży. Maksymalny rozmiar: {MaxFileSize / 1024 / 1024} MB"));
        }

        // Generate unique file name and path
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine(
            _environment.WebRootPath ?? "wwwroot",
            "uploads",
            "medical-records",
            request.AnimalId.ToString(),
            request.MedicalRecordId.ToString());

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        var relativePath = $"/uploads/medical-records/{request.AnimalId}/{request.MedicalRecordId}/{uniqueFileName}";

        try
        {
            // Save file to disk
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await request.File.CopyToAsync(fileStream, cancellationToken);

            // Add attachment to medical record
            var attachment = medicalRecord.AddAttachment(
                fileName: request.File.FileName,
                filePath: relativePath,
                contentType: request.File.ContentType,
                fileSize: request.File.Length,
                description: request.Description
            );

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Attachment {AttachmentId} uploaded for medical record {MedicalRecordId} of animal {AnimalId}",
                attachment.Id,
                request.MedicalRecordId,
                request.AnimalId);

            return Result.Success(attachment.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error uploading attachment for medical record {MedicalRecordId} of animal {AnimalId}",
                request.MedicalRecordId,
                request.AnimalId);

            // Clean up file on error
            if (File.Exists(filePath))
                File.Delete(filePath);

            return Result.Failure<MedicalRecordAttachmentDto>(
                Error.Validation("Wystąpił błąd podczas zapisywania pliku"));
        }
    }
}

// ============================================
// Validator
// ============================================
public class AddMedicalRecordAttachmentValidator : AbstractValidator<AddMedicalRecordAttachmentCommand>
{
    public AddMedicalRecordAttachmentValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.MedicalRecordId)
            .NotEmpty().WithMessage("ID rekordu medycznego jest wymagane");

        RuleFor(x => x.File)
            .NotNull().WithMessage("Plik jest wymagany");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Opis nie może przekraczać 500 znaków")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
