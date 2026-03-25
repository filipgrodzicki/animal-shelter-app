using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShelterApp.Api.Common;
using ShelterApp.Api.Features.Animals.Commands;
using ShelterApp.Api.Features.Animals.Queries;
using ShelterApp.Api.Features.Animals.Shared;
using ShelterApp.Api.Features.Volunteers.Commands;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Common;

namespace ShelterApp.Api.Features.Animals;

/// <summary>
/// Shelter animal management
/// </summary>
[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
public class AnimalsController : ApiController
{
    #region Queries

    /// <summary>
    /// Gets a filtered and paginated list of animals
    /// </summary>
    /// <remarks>
    /// Example query:
    ///
    ///     GET /api/animals?species=Dog&amp;status=Available&amp;publicOnly=true&amp;page=1&amp;pageSize=10
    ///
    /// Filtering:
    /// - **species**: Dog, Cat, Other
    /// - **gender**: Male, Female, Unknown
    /// - **size**: Small, Medium, Large
    /// - **status**: Admitted, Quarantine, Treatment, Available, Reserved, InAdoptionProcess, Adopted, Deceased
    /// - **experienceLevel**: Low, Medium, High
    /// - **requiredExperience**: None, Basic, Advanced
    /// - **spaceRequirement**: Apartment, House, HouseWithGarden
    /// - **publicOnly**: true - shows only animals available for adoption (Available, Reserved, InAdoptionProcess)
    ///
    /// Sorting:
    /// - **sortBy**: name, age, species, status, admissiondate, registrationnumber
    /// - **sortDescending**: true/false
    /// </remarks>
    /// <param name="species">Animal species</param>
    /// <param name="ageMin">Minimum age in months</param>
    /// <param name="ageMax">Maximum age in months</param>
    /// <param name="gender">Gender</param>
    /// <param name="size">Size</param>
    /// <param name="status">Animal status</param>
    /// <param name="experienceLevel">Required experience level</param>
    /// <param name="childrenCompatibility">Children compatibility (Yes/Partially/No)</param>
    /// <param name="animalCompatibility">Other animals compatibility (Yes/Partially/No)</param>
    /// <param name="spaceRequirement">Space requirements</param>
    /// <param name="careTime">Required care time</param>
    /// <param name="searchTerm">Search by name, breed, registration number</param>
    /// <param name="publicOnly">Only publicly visible animals</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDescending">Descending sort</param>
    /// <param name="page">Page number (from 1)</param>
    /// <param name="pageSize">Page size (1-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">List of animals</response>
    /// <response code="400">Invalid query parameters</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<AnimalListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAnimals(
        [FromQuery] string? species = null,
        [FromQuery] int? ageMin = null,
        [FromQuery] int? ageMax = null,
        [FromQuery] string? gender = null,
        [FromQuery] string? size = null,
        [FromQuery] string? status = null,
        [FromQuery] string? experienceLevel = null,
        [FromQuery] string? childrenCompatibility = null,
        [FromQuery] string? animalCompatibility = null,
        [FromQuery] string? spaceRequirement = null,
        [FromQuery] string? careTime = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool publicOnly = false,
        [FromQuery] string sortBy = "AdmissionDate",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Force publicOnly for anonymous users
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            publicOnly = true;
        }

        var query = new GetAnimalsQuery(
            Species: species,
            AgeMin: ageMin,
            AgeMax: ageMax,
            Gender: gender,
            Size: size,
            Status: status,
            ExperienceLevel: experienceLevel,
            ChildrenCompatibility: childrenCompatibility,
            AnimalCompatibility: animalCompatibility,
            SpaceRequirement: spaceRequirement,
            CareTime: careTime,
            SearchTerm: searchTerm,
            PublicOnly: publicOnly,
            SortBy: sortBy,
            SortDescending: sortDescending,
            Page: page,
            PageSize: pageSize
        );

        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets detailed animal data
    /// </summary>
    /// <remarks>
    /// Example query:
    ///
    ///     GET /api/animals/550e8400-e29b-41d4-a716-446655440000
    ///
    /// Returns full animal data including:
    /// - Status change history
    /// - Medical records
    /// - Photos
    /// - Permitted actions
    /// </remarks>
    /// <param name="id">Animal identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Animal details</response>
    /// <response code="404">Animal not found</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AnimalDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetAnimalByIdQuery(id);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets the animal's status change history
    /// </summary>
    /// <remarks>
    /// Example query:
    ///
    ///     GET /api/animals/550e8400-e29b-41d4-a716-446655440000/status-history?fromDate=2024-01-01&amp;page=1&amp;pageSize=20
    ///
    /// History includes:
    /// - Previous and new status
    /// - Action (trigger) that caused the change
    /// - Reason for change (if provided)
    /// - Who and when made the change
    /// </remarks>
    /// <param name="id">Animal identifier</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Status change history</response>
    /// <response code="404">Animal not found</response>
    [HttpGet("{id:guid}/status-history")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AnimalStatusHistoryResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatusHistory(
        Guid id,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAnimalStatusHistoryQuery(
            AnimalId: id,
            FromDate: fromDate,
            ToDate: toDate,
            Page: page,
            PageSize: pageSize
        );

        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets permitted actions for the animal's current status
    /// </summary>
    /// <remarks>
    /// Example query:
    ///
    ///     GET /api/animals/550e8400-e29b-41d4-a716-446655440000/permitted-actions
    ///
    /// Returns a list of actions that can be performed for the animal in its current status.
    /// Each action contains:
    /// - Trigger (action name)
    /// - DisplayName (display name)
    /// - Description (action description)
    /// - RequiresReason (whether a reason is required)
    /// </remarks>
    /// <param name="id">Animal identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">List of permitted actions</response>
    /// <response code="404">Animal not found</response>
    [HttpGet("{id:guid}/permitted-actions")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(PermittedActionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPermittedActions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetPermittedActionsQuery(id);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets a medical record for an animal
    /// </summary>
    /// <param name="id">Animal identifier</param>
    /// <param name="recordId">Medical record identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Medical record</response>
    /// <response code="404">Animal or record not found</response>
    [HttpGet("{id:guid}/medical-records/{recordId:guid}")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(MedicalRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMedicalRecord(
        Guid id,
        Guid recordId,
        CancellationToken cancellationToken)
    {
        var query = new GetMedicalRecordByIdQuery(id, recordId);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets notes for an animal
    /// </summary>
    /// <param name="id">Animal identifier</param>
    /// <param name="noteType">Filter by note type</param>
    /// <param name="isImportant">Filter important notes only</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">List of notes</response>
    /// <response code="404">Animal not found</response>
    [HttpGet("{id:guid}/notes")]
    [Authorize(Roles = "Volunteer,Staff,Admin")]
    [ProducesResponseType(typeof(PagedResult<AnimalNoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAnimalNotes(
        Guid id,
        [FromQuery] string? noteType = null,
        [FromQuery] bool? isImportant = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAnimalNotesQuery(id, noteType, isImportant, page, pageSize);
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Registers a new animal in the shelter
    /// </summary>
    /// <remarks>
    /// Example query:
    ///
    ///     POST /api/animals
    ///     {
    ///         "species": "Dog",
    ///         "breed": "Labrador Retriever",
    ///         "name": "Burek",
    ///         "ageInMonths": 24,
    ///         "gender": "Male",
    ///         "size": "Large",
    ///         "color": "Czarny",
    ///         "chipNumber": "123456789012345",
    ///         "admissionCircumstances": "Znaleziony na ulicy bez opieki",
    ///         "description": "Przyjazny i energiczny pies, uwielbia spacery",
    ///         "experienceLevel": "High",
    ///         "goodWithChildren": true,
    ///         "goodWithAnimals": true,
    ///         "requiredExperience": "Basic",
    ///         "spaceRequirement": "HouseWithGarden",
    ///         "registeredByUserId": "550e8400-e29b-41d4-a716-446655440000"
    ///     }
    ///
    /// Registration number is auto-generated in the format: SCH/{SPECIES}/{YEAR}/{NUMBER}
    /// </remarks>
    /// <param name="command">New animal data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="201">Animal registered successfully</response>
    /// <response code="400">Invalid data</response>
    /// <response code="409">Animal with the given chip number already exists</response>
    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AnimalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterAnimalCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.Match(
            onSuccess: animal => CreatedAtAction(
                nameof(GetById),
                new { id = animal.Id },
                animal),
            onFailure: error => HandleResult(Result.Failure<AnimalDto>(error))
        );
    }

    /// <summary>
    /// Changes the animal's status
    /// </summary>
    /// <remarks>
    /// Example query:
    ///
    ///     PUT /api/animals/550e8400-e29b-41d4-a716-446655440000/status
    ///     {
    ///         "trigger": "SkierowanieNaKwarantanne",
    ///         "reason": "Standardowa procedura dla nowych zwierząt",
    ///         "changedBy": "jan.kowalski@schronisko.pl"
    ///     }
    ///
    /// Available triggers depend on the current status. Use the GET /permitted-actions endpoint to check allowed actions.
    ///
    /// Triggers requiring a reason:
    /// - Zgon
    /// - WykrycieChoroby
    /// - Zachorowanie
    /// - AnulowanieZgloszenia
    /// - Rezygnacja
    /// - NegatywnaOcena
    /// </remarks>
    /// <param name="id">Animal identifier</param>
    /// <param name="request">Status change data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Status changed successfully</response>
    /// <response code="400">Invalid data or disallowed status transition</response>
    /// <response code="404">Animal not found</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(AnimalStatusChangeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeAnimalStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeAnimalStatusCommand(
            AnimalId: id,
            Trigger: request.Trigger,
            Reason: request.Reason,
            ChangedBy: request.ChangedBy
        );

        var result = await Sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Adds a medical record for an animal
    /// </summary>
    /// <remarks>
    /// Example query:
    ///
    ///     POST /api/animals/550e8400-e29b-41d4-a716-446655440000/medical-records
    ///     {
    ///         "type": "Vaccination",
    ///         "title": "Szczepienie przeciw wściekliźnie",
    ///         "description": "Coroczne szczepienie obowiązkowe",
    ///         "recordDate": "2024-01-15T10:00:00Z",
    ///         "veterinarianName": "dr Jan Kowalski",
    ///         "nextVisitDate": "2025-01-15T10:00:00Z",
    ///         "cost": 150.00
    ///     }
    ///
    /// Medical record types:
    /// - Examination
    /// - Vaccination
    /// - Surgery
    /// - Treatment
    /// - Deworming
    /// - Sterilization
    /// - Microchipping
    /// - DentalCare
    /// - Laboratory
    /// - Other
    /// </remarks>
    /// <param name="id">Animal identifier</param>
    /// <param name="request">Medical record data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="201">Medical record added</response>
    /// <response code="400">Invalid data</response>
    /// <response code="404">Animal not found</response>
    [HttpPost("{id:guid}/medical-records")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(MedicalRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddMedicalRecord(
        Guid id,
        [FromBody] AddMedicalRecordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddMedicalRecordCommand(
            AnimalId: id,
            Type: request.Type,
            Title: request.Title,
            Description: request.Description,
            RecordDate: request.RecordDate,
            Diagnosis: request.Diagnosis,
            Treatment: request.Treatment,
            Medications: request.Medications,
            NextVisitDate: request.NextVisitDate,
            VeterinarianName: request.VeterinarianName,
            Notes: request.Notes,
            Cost: request.Cost,
            EnteredBy: request.EnteredBy,
            EnteredByUserId: request.EnteredByUserId
        );

        var result = await Sender.Send(command, cancellationToken);

        return result.Match(
            onSuccess: record => CreatedAtAction(
                nameof(GetMedicalRecord),
                new { id, recordId = record.Id },
                record),
            onFailure: error => HandleResult(Result.Failure<MedicalRecordDto>(error))
        );
    }

    /// <summary>
    /// Adds an attachment to a medical record (WF-06)
    /// </summary>
    /// <remarks>
    /// Supported formats: PDF, JPG, JPEG, PNG, DOC, DOCX
    /// Maximum file size: 10 MB
    ///
    /// Example query (form-data):
    ///
    ///     POST /api/animals/550e8400-e29b-41d4-a716-446655440000/medical-records/660e8400-e29b-41d4-a716-446655440001/attachments
    ///     Content-Type: multipart/form-data
    ///
    ///     file: [veterinary document scan]
    ///     description: "Wyniki badań krwi"
    /// </remarks>
    /// <param name="id">Animal identifier</param>
    /// <param name="recordId">Medical record identifier</param>
    /// <param name="file">Attachment file</param>
    /// <param name="description">Attachment description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="201">Attachment added</response>
    /// <response code="400">Invalid file or format</response>
    /// <response code="404">Animal or medical record not found</response>
    [HttpPost("{id:guid}/medical-records/{recordId:guid}/attachments")]
    [Authorize(Roles = "Staff,Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MedicalRecordAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> AddMedicalRecordAttachment(
        Guid id,
        Guid recordId,
        IFormFile file,
        [FromForm] string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Plik jest wymagany"
            });
        }

        var command = new AddMedicalRecordAttachmentCommand(
            AnimalId: id,
            MedicalRecordId: recordId,
            File: file,
            Description: description
        );

        var result = await Sender.Send(command, cancellationToken);

        return result.Match(
            onSuccess: attachment => CreatedAtAction(
                nameof(GetMedicalRecord),
                new { id, recordId },
                attachment),
            onFailure: error => HandleResult(Result.Failure<MedicalRecordAttachmentDto>(error))
        );
    }

    /// <summary>
    /// Uploads an animal photo
    /// </summary>
    /// <remarks>
    /// Supported formats: JPG, JPEG, PNG, WEBP
    /// Maximum file size: 10 MB
    ///
    /// Example query (form-data):
    ///
    ///     POST /api/animals/550e8400-e29b-41d4-a716-446655440000/photos
    ///     Content-Type: multipart/form-data
    ///
    ///     file: [photo file]
    ///     isMain: true
    ///     description: "Zdjęcie profilowe"
    ///
    /// If isMain=true, the previous main photo will be demoted to regular.
    /// The first photo automatically becomes the main one.
    /// </remarks>
    /// <param name="id">Animal identifier</param>
    /// <param name="file">Photo file</param>
    /// <param name="isMain">Whether to set as main photo</param>
    /// <param name="description">Photo description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="201">Photo added</response>
    /// <response code="400">Invalid file or format</response>
    /// <response code="404">Animal not found</response>
    [HttpPost("{id:guid}/photos")]
    [Authorize(Roles = "Staff,Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AnimalPhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> UploadPhoto(
        Guid id,
        IFormFile file,
        [FromForm] bool isMain = false,
        [FromForm] string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Plik jest wymagany"
            });
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadAnimalPhotoCommand(
            AnimalId: id,
            FileName: file.FileName,
            ContentType: file.ContentType,
            FileSize: file.Length,
            FileStream: stream,
            IsMain: isMain,
            Description: description
        );

        var result = await Sender.Send(command, cancellationToken);

        return result.Match(
            onSuccess: photo => CreatedAtAction(
                nameof(GetById),
                new { id },
                photo),
            onFailure: error => HandleResult(Result.Failure<AnimalPhotoDto>(error))
        );
    }

    /// <summary>
    /// Deletes an animal photo
    /// </summary>
    /// <param name="id">Animal identifier</param>
    /// <param name="photoId">Photo identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="204">Photo deleted</response>
    /// <response code="404">Animal or photo not found</response>
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePhoto(
        Guid id,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteAnimalPhotoCommand(id, photoId);
        var result = await Sender.Send(command, cancellationToken);
        return HandleNoContentResult(result);
    }

    /// <summary>
    /// Adds a note for an animal (volunteer)
    /// </summary>
    /// <remarks>
    /// Allows a volunteer to add a note about an animal.
    /// The volunteer must be active (status Active).
    ///
    /// Note types:
    /// - BehaviorObservation
    /// - HealthObservation
    /// - Feeding
    /// - WalkActivity
    /// - AnimalInteraction
    /// - HumanInteraction
    /// - Grooming
    /// - Training
    /// - General
    /// - Urgent
    /// </remarks>
    /// <param name="id">Animal identifier</param>
    /// <param name="request">Note data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="201">Note added</response>
    /// <response code="400">Invalid data</response>
    /// <response code="404">Animal or volunteer not found</response>
    [HttpPost("{id:guid}/notes")]
    [Authorize(Roles = "Volunteer,Staff,Admin")]
    [ProducesResponseType(typeof(AnimalNoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddAnimalNote(
        Guid id,
        [FromBody] AddAnimalNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AnimalNoteType>(request.NoteType, out var noteType))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = $"Nieprawidłowy typ notatki: {request.NoteType}"
            });
        }

        var command = new AddAnimalNoteCommand(
            AnimalId: id,
            VolunteerId: request.VolunteerId,
            NoteType: noteType,
            Title: request.Title,
            Content: request.Content,
            IsImportant: request.IsImportant,
            ObservationDate: request.ObservationDate
        );

        var result = await Sender.Send(command, cancellationToken);

        return result.Match(
            onSuccess: note => CreatedAtAction(
                nameof(GetAnimalNotes),
                new { id },
                note),
            onFailure: error => HandleResult(Result.Failure<AnimalNoteDto>(error))
        );
    }

    #endregion
}

// ============================================
// Request DTOs
// ============================================

/// <summary>
/// Animal status change request
/// </summary>
public record ChangeAnimalStatusRequest(
    /// <summary>
    /// Action (trigger) to perform
    /// </summary>
    /// <example>SkierowanieNaKwarantanne</example>
    string Trigger,

    /// <summary>
    /// Status change reason (required for some actions)
    /// </summary>
    /// <example>Standardowa procedura dla nowych zwierząt</example>
    string? Reason,

    /// <summary>
    /// Person performing the change
    /// </summary>
    /// <example>jan.kowalski@schronisko.pl</example>
    string ChangedBy
);

/// <summary>
/// Add medical record request
/// </summary>
public record AddMedicalRecordRequest(
    /// <summary>
    /// Medical record type
    /// </summary>
    /// <example>Vaccination</example>
    string Type,

    /// <summary>
    /// Record title
    /// </summary>
    /// <example>Szczepienie przeciw wściekliźnie</example>
    string Title,

    /// <summary>
    /// Detailed description
    /// </summary>
    /// <example>Coroczne szczepienie obowiązkowe</example>
    string Description,

    /// <summary>
    /// Date performed (defaults to now)
    /// </summary>
    DateTime? RecordDate,

    /// <summary>
    /// Diagnosis (for examinations and treatments)
    /// </summary>
    string? Diagnosis,

    /// <summary>
    /// Treatment applied
    /// </summary>
    string? Treatment,

    /// <summary>
    /// Prescribed medications
    /// </summary>
    string? Medications,

    /// <summary>
    /// Next visit date
    /// </summary>
    DateTime? NextVisitDate,

    /// <summary>
    /// Veterinarian name
    /// </summary>
    /// <example>dr Jan Kowalski</example>
    string VeterinarianName,

    /// <summary>
    /// Additional notes
    /// </summary>
    string? Notes,

    /// <summary>
    /// Service cost
    /// </summary>
    /// <example>150.00</example>
    decimal? Cost,

    /// <summary>
    /// Person entering the record (WF-06)
    /// </summary>
    /// <example>Jan Kowalski</example>
    string EnteredBy,

    /// <summary>
    /// ID of the user entering the record (optional)
    /// </summary>
    Guid? EnteredByUserId = null
);

/// <summary>
/// Add animal note request
/// </summary>
public record AddAnimalNoteRequest(
    /// <summary>
    /// Volunteer ID adding the note
    /// </summary>
    Guid VolunteerId,

    /// <summary>
    /// Note type
    /// </summary>
    /// <example>BehaviorObservation</example>
    string NoteType,

    /// <summary>
    /// Note title
    /// </summary>
    /// <example>Obserwacja zachowania podczas spaceru</example>
    string Title,

    /// <summary>
    /// Note content
    /// </summary>
    /// <example>Pies był spokojny i dobrze reagował na inne zwierzęta</example>
    string Content,

    /// <summary>
    /// Whether the note is important
    /// </summary>
    bool IsImportant = false,

    /// <summary>
    /// Observation date (defaults to now)
    /// </summary>
    DateTime? ObservationDate = null
);
