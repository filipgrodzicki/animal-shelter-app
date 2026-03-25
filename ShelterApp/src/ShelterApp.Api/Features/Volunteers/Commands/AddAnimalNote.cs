using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Infrastructure.Persistence;

namespace ShelterApp.Api.Features.Volunteers.Commands;

// ============================================
// DTO
// ============================================
public record AnimalNoteDto(
    Guid Id,
    Guid AnimalId,
    Guid? VolunteerId,
    string AuthorName,
    string NoteType,
    string Title,
    string Content,
    bool IsImportant,
    DateTime ObservationDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ============================================
// Command - Add Animal Note (WB-19)
// ============================================
/// <summary>
/// Dodaje notatkę o zwierzęciu przez wolontariusza
/// </summary>
public record AddAnimalNoteCommand(
    Guid AnimalId,
    Guid VolunteerId,
    AnimalNoteType NoteType,
    string Title,
    string Content,
    bool IsImportant = false,
    DateTime? ObservationDate = null
) : ICommand<Result<AnimalNoteDto>>;

// ============================================
// Handler - Add Animal Note
// ============================================
public class AddAnimalNoteHandler
    : ICommandHandler<AddAnimalNoteCommand, Result<AnimalNoteDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<AddAnimalNoteHandler> _logger;

    public AddAnimalNoteHandler(
        ShelterDbContext context,
        ILogger<AddAnimalNoteHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AnimalNoteDto>> Handle(
        AddAnimalNoteCommand request,
        CancellationToken cancellationToken)
    {
        // Sprawdź czy zwierzę istnieje
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<AnimalNoteDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        // Sprawdź czy wolontariusz istnieje i jest aktywny
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<AnimalNoteDto>(
                Error.NotFound("Volunteer", request.VolunteerId));
        }

        if (volunteer.Status != VolunteerStatus.Active)
        {
            return Result.Failure<AnimalNoteDto>(
                Error.Validation($"Wolontariusz musi być aktywny. Aktualny status: {volunteer.Status}"));
        }

        // Utwórz notatkę
        var noteResult = AnimalNote.Create(
            animalId: request.AnimalId,
            noteType: request.NoteType,
            title: request.Title,
            content: request.Content,
            authorName: volunteer.FullName,
            volunteerId: request.VolunteerId,
            isImportant: request.IsImportant,
            observationDate: request.ObservationDate);

        if (noteResult.IsFailure)
        {
            return Result.Failure<AnimalNoteDto>(noteResult.Error);
        }

        var note = noteResult.Value;
        _context.AnimalNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Animal note added by volunteer {VolunteerId} for animal {AnimalId}. Type: {NoteType}, Important: {IsImportant}",
            request.VolunteerId, request.AnimalId, request.NoteType, request.IsImportant);

        return Result.Success(note.ToDto());
    }
}

// ============================================
// Validator - Add Animal Note
// ============================================
public class AddAnimalNoteValidator : AbstractValidator<AddAnimalNoteCommand>
{
    public AddAnimalNoteValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.VolunteerId)
            .NotEmpty().WithMessage("ID wolontariusza jest wymagane");

        RuleFor(x => x.NoteType)
            .IsInEnum().WithMessage("Nieprawidłowy typ notatki");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tytuł jest wymagany")
            .MaximumLength(200).WithMessage("Tytuł nie może przekraczać 200 znaków");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Treść notatki jest wymagana")
            .MaximumLength(4000).WithMessage("Treść nie może przekraczać 4000 znaków");

        RuleFor(x => x.ObservationDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Data obserwacji nie może być w przyszłości")
            .When(x => x.ObservationDate.HasValue);
    }
}

// ============================================
// Command - Add Animal Note By Staff
// ============================================
/// <summary>
/// Dodaje notatkę o zwierzęciu przez pracownika
/// </summary>
public record AddAnimalNoteByStaffCommand(
    Guid AnimalId,
    Guid UserId,
    string AuthorName,
    AnimalNoteType NoteType,
    string Title,
    string Content,
    bool IsImportant = false,
    DateTime? ObservationDate = null
) : ICommand<Result<AnimalNoteDto>>;

// ============================================
// Handler - Add Animal Note By Staff
// ============================================
public class AddAnimalNoteByStaffHandler
    : ICommandHandler<AddAnimalNoteByStaffCommand, Result<AnimalNoteDto>>
{
    private readonly ShelterDbContext _context;
    private readonly ILogger<AddAnimalNoteByStaffHandler> _logger;

    public AddAnimalNoteByStaffHandler(
        ShelterDbContext context,
        ILogger<AddAnimalNoteByStaffHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AnimalNoteDto>> Handle(
        AddAnimalNoteByStaffCommand request,
        CancellationToken cancellationToken)
    {
        // Sprawdź czy zwierzę istnieje
        var animal = await _context.Animals
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId, cancellationToken);

        if (animal is null)
        {
            return Result.Failure<AnimalNoteDto>(
                Error.NotFound("Animal", request.AnimalId));
        }

        // Utwórz notatkę
        var noteResult = AnimalNote.Create(
            animalId: request.AnimalId,
            noteType: request.NoteType,
            title: request.Title,
            content: request.Content,
            authorName: request.AuthorName,
            userId: request.UserId,
            isImportant: request.IsImportant,
            observationDate: request.ObservationDate);

        if (noteResult.IsFailure)
        {
            return Result.Failure<AnimalNoteDto>(noteResult.Error);
        }

        var note = noteResult.Value;
        _context.AnimalNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Animal note added by staff {AuthorName} for animal {AnimalId}. Type: {NoteType}, Important: {IsImportant}",
            request.AuthorName, request.AnimalId, request.NoteType, request.IsImportant);

        return Result.Success(note.ToDto());
    }
}

// ============================================
// Validator - Add Animal Note By Staff
// ============================================
public class AddAnimalNoteByStaffValidator : AbstractValidator<AddAnimalNoteByStaffCommand>
{
    public AddAnimalNoteByStaffValidator()
    {
        RuleFor(x => x.AnimalId)
            .NotEmpty().WithMessage("ID zwierzęcia jest wymagane");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID użytkownika jest wymagane");

        RuleFor(x => x.AuthorName)
            .NotEmpty().WithMessage("Imię autora jest wymagane")
            .MaximumLength(200).WithMessage("Imię autora nie może przekraczać 200 znaków");

        RuleFor(x => x.NoteType)
            .IsInEnum().WithMessage("Nieprawidłowy typ notatki");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tytuł jest wymagany")
            .MaximumLength(200).WithMessage("Tytuł nie może przekraczać 200 znaków");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Treść notatki jest wymagana")
            .MaximumLength(4000).WithMessage("Treść nie może przekraczać 4000 znaków");

        RuleFor(x => x.ObservationDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Data obserwacji nie może być w przyszłości")
            .When(x => x.ObservationDate.HasValue);
    }
}

// ============================================
// Mapping Extension
// ============================================
public static class AnimalNoteMappingExtensions
{
    public static AnimalNoteDto ToDto(this AnimalNote note)
    {
        return new AnimalNoteDto(
            Id: note.Id,
            AnimalId: note.AnimalId,
            VolunteerId: note.VolunteerId,
            AuthorName: note.AuthorName,
            NoteType: note.NoteType.ToString(),
            Title: note.Title,
            Content: note.Content,
            IsImportant: note.IsImportant,
            ObservationDate: note.ObservationDate,
            CreatedAt: note.CreatedAt,
            UpdatedAt: note.UpdatedAt
        );
    }
}
