using FluentAssertions;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Adoptions.Events;
using ShelterApp.Domain.Common;
using Xunit;

namespace ShelterApp.Tests.Adoptions;

public class AdoptionApplicationStateMachineTests
{
    private static Result<AdoptionApplication> CreateTestApplication()
    {
        return AdoptionApplication.Create(
            adopterId: Guid.NewGuid(),
            animalId: Guid.NewGuid(),
            adoptionMotivation: "Zawsze marzyłem o psie",
            petExperience: "Miałem psa przez 10 lat",
            livingConditions: "Dom z ogrodem",
            otherPetsInfo: "Brak innych zwierząt"
        );
    }

    #region 1. Tworzenie zgłoszenia

    [Fact]
    public void Create_ValidData_ShouldReturnSuccess()
    {
        // Act
        var result = CreateTestApplication();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Create_NewApplication_ShouldHaveNewStatus()
    {
        // Act
        var result = CreateTestApplication();

        // Assert
        result.Value.Status.Should().Be(AdoptionApplicationStatus.New);
    }

    [Fact]
    public void Create_NewApplication_ShouldPublishCreatedEvent()
    {
        // Act
        var result = CreateTestApplication();

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdoptionApplicationCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldSetCorrectProperties()
    {
        // Arrange
        var adopterId = Guid.NewGuid();
        var animalId = Guid.NewGuid();

        // Act
        var result = AdoptionApplication.Create(
            adopterId, animalId,
            "Motywacja", "Doświadczenie", "Warunki", "Inne zwierzęta");

        // Assert
        var app = result.Value;
        app.AdopterId.Should().Be(adopterId);
        app.AnimalId.Should().Be(animalId);
        app.AdoptionMotivation.Should().Be("Motywacja");
        app.ApplicationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region 2. Przejście New -> Submitted

    [Fact]
    public void ChangeStatus_FromNewToSubmitted_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ClearDomainEvents();

        // Act
        var result = app.ChangeStatus(
            AdoptionApplicationTrigger.ZlozenieZgloszenia,
            "jan@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Submitted);
    }

    #endregion

    #region 3. Przejście Submitted -> UnderReview

    [Fact]
    public void TakeForReview_FromSubmitted_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.ClearDomainEvents();

        // Act
        var reviewerId = Guid.NewGuid();
        var result = app.TakeForReview(reviewerId, "Anna Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.UnderReview);
        app.ReviewedByUserId.Should().Be(reviewerId);
        app.ReviewDate.Should().NotBeNull();
    }

    #endregion

    #region 4. Przejście Submitted -> Cancelled

    [Fact]
    public void CancelByUser_FromSubmitted_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.ClearDomainEvents();

        // Act
        var result = app.CancelByUser("Zmiana planów", "jan@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Cancelled);
        app.RejectionReason.Should().Be("Zmiana planów");
        app.CompletionDate.Should().NotBeNull();
    }

    #endregion

    #region 5. Przejście UnderReview -> Accepted

    [Fact]
    public void ApproveApplication_FromUnderReview_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.ApproveApplication("Anna Nowak", "Wszystko w porządku");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Accepted);
        app.ReviewNotes.Should().Be("Wszystko w porządku");
    }

    #endregion

    #region 6. Przejście UnderReview -> Rejected

    [Fact]
    public void RejectApplication_FromUnderReview_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.RejectApplication("Anna Nowak", "Nieodpowiednie warunki");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Rejected);
        app.ReviewNotes.Should().Be("Nieodpowiednie warunki");
        app.CompletionDate.Should().NotBeNull();
    }

    #endregion

    #region 7. Przejście Accepted -> VisitScheduled

    [Fact]
    public void ScheduleVisit_FromAccepted_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var visitDate = DateTime.UtcNow.AddDays(7);
        var result = app.ScheduleVisit(visitDate, "Anna Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.VisitScheduled);
        app.ScheduledVisitDate.Should().Be(visitDate);
    }

    [Fact]
    public void ScheduleVisit_WithPastDate_ShouldFail()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");

        // Act
        var result = app.ScheduleVisit(DateTime.UtcNow.AddDays(-1), "Anna Nowak");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("przyszłości");
    }

    #endregion

    #region 8. Przejście Accepted -> Cancelled (rezygnacja)

    [Fact]
    public void CancelByUser_FromAccepted_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.CancelByUser("Zmieniłem zdanie", "jan@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Cancelled);
    }

    #endregion

    #region 9. Przejście VisitScheduled -> VisitCompleted

    [Fact]
    public void RecordVisitAttendance_FromVisitScheduled_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var conductedById = Guid.NewGuid();
        var result = app.RecordVisitAttendance(conductedById, "Anna Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.VisitCompleted);
        app.VisitDate.Should().NotBeNull();
        app.VisitConductedByUserId.Should().Be(conductedById);
    }

    #endregion

    #region 10. Przejście VisitScheduled -> Cancelled (niestawienie się)

    [Fact]
    public void CancelByUser_FromVisitScheduled_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.CancelByUser("Nie mogę przyjść", "jan@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Cancelled);
    }

    #endregion

    #region 11. Przejście VisitCompleted -> PendingFinalization

    [Fact]
    public void RecordVisitResult_Positive_ShouldMoveToPendingFinalization()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.RecordVisitResult(
            isPositive: true,
            assessment: 5,
            notes: "Świetna wizyta",
            recordedBy: "Anna Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.PendingFinalization);
        app.VisitAssessment.Should().Be(5);
        app.VisitNotes.Should().Be("Świetna wizyta");
    }

    [Fact]
    public void RecordVisitResult_InvalidAssessment_ShouldFail()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");

        // Act
        var result = app.RecordVisitResult(true, 6, "Notatki", "Anna Nowak");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("1-5");
    }

    #endregion

    #region 12. Przejście VisitCompleted -> Rejected

    [Fact]
    public void RecordVisitResult_Negative_ShouldMoveToRejected()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.RecordVisitResult(
            isPositive: false,
            assessment: 2,
            notes: "Nieodpowiednie warunki",
            recordedBy: "Anna Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Rejected);
        app.CompletionDate.Should().NotBeNull();
    }

    #endregion

    #region 13. Generowanie umowy

    [Fact]
    public void GenerateContract_FromPendingFinalization_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.GenerateContract("ADO/2024/001", "Anna Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.ContractNumber.Should().Be("ADO/2024/001");
        app.ContractGeneratedDate.Should().NotBeNull();
    }

    [Fact]
    public void GenerateContract_FromWrongStatus_ShouldFail()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");

        // Act
        var result = app.GenerateContract("ADO/2024/001", "Anna Nowak");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region 14. Przejście PendingFinalization -> Completed

    [Fact]
    public void FinalizeAdoption_FromPendingFinalization_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "Anna Nowak");
        app.GenerateContract("ADO/2024/001", "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.FinalizeAdoption("/contracts/umowa.pdf", "Anna Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Completed);
        app.ContractFilePath.Should().Be("/contracts/umowa.pdf");
        app.ContractSignedDate.Should().NotBeNull();
        app.CompletionDate.Should().NotBeNull();
    }

    [Fact]
    public void FinalizeAdoption_WithoutContract_ShouldFail()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "Anna Nowak");
        // Brak GenerateContract

        // Act
        var result = app.FinalizeAdoption("/contracts/umowa.pdf", "Anna Nowak");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("wygenerować umowę");
    }

    [Fact]
    public void FinalizeAdoption_ShouldPublishCompletedEvent()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "Anna Nowak");
        app.GenerateContract("ADO/2024/001", "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        app.FinalizeAdoption("/contracts/umowa.pdf", "Anna Nowak");

        // Assert
        app.DomainEvents.Should().Contain(e => e is AdoptionCompletedEvent);
    }

    #endregion

    #region 15. Przejście PendingFinalization -> Cancelled

    [Fact]
    public void CancelByUser_FromPendingFinalization_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "Anna Nowak");
        app.ClearDomainEvents();

        // Act
        var result = app.CancelByUser("Rezygnuję przed podpisaniem", "jan@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Cancelled);
    }

    #endregion

    #region 16. Stany końcowe

    [Fact]
    public void ChangeStatus_FromCompleted_ShouldFail()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "Anna Nowak");
        app.GenerateContract("ADO/2024/001", "Anna Nowak");
        app.FinalizeAdoption("/contracts/umowa.pdf", "Anna Nowak");

        // Act
        var result = app.ChangeStatus(
            AdoptionApplicationTrigger.AnulowanePrzezUzytkownika,
            "jan@test.pl");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void GetPermittedTriggers_WhenCompleted_ShouldReturnEmpty()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "Anna Nowak");
        app.GenerateContract("ADO/2024/001", "Anna Nowak");
        app.FinalizeAdoption("/contracts/umowa.pdf", "Anna Nowak");

        // Act
        var triggers = app.GetPermittedTriggers();

        // Assert
        triggers.Should().BeEmpty();
    }

    [Fact]
    public void GetPermittedTriggers_WhenRejected_ShouldReturnEmpty()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.RejectApplication("Anna Nowak", "Powód");

        // Act
        var triggers = app.GetPermittedTriggers();

        // Assert
        triggers.Should().BeEmpty();
    }

    [Fact]
    public void GetPermittedTriggers_WhenCancelled_ShouldReturnEmpty()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.CancelByUser("Powód", "jan@test.pl");

        // Act
        var triggers = app.GetPermittedTriggers();

        // Assert
        triggers.Should().BeEmpty();
    }

    #endregion

    #region 17. Historia zmian statusu

    [Fact]
    public void FullAdoptionFlow_ShouldTrackAllStatusChanges()
    {
        // Arrange
        var app = CreateTestApplication().Value;

        // Act - pełna ścieżka adopcji
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");
        app.ApproveApplication("Anna Nowak");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "Anna Nowak");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "Anna Nowak");
        app.GenerateContract("ADO/2024/001", "Anna Nowak");
        app.FinalizeAdoption("/contracts/umowa.pdf", "Anna Nowak");

        // Assert
        app.StatusHistory.Should().HaveCount(7);
        app.Status.Should().Be(AdoptionApplicationStatus.Completed);
    }

    [Fact]
    public void ChangeStatus_ShouldAddEntryToStatusHistory()
    {
        // Arrange
        var app = CreateTestApplication().Value;

        // Act
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");

        // Assert
        app.StatusHistory.Should().ContainSingle();
        var entry = app.StatusHistory.First();
        entry.PreviousStatus.Should().Be(AdoptionApplicationStatus.New);
        entry.NewStatus.Should().Be(AdoptionApplicationStatus.Submitted);
        entry.Trigger.Should().Be(AdoptionApplicationTrigger.ZlozenieZgloszenia);
    }

    #endregion

    #region 18. GetPermittedTriggers

    [Fact]
    public void GetPermittedTriggers_WhenNew_ShouldReturnSubmitOnly()
    {
        // Arrange
        var app = CreateTestApplication().Value;

        // Act
        var triggers = app.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().ContainSingle();
        triggers.Should().Contain(AdoptionApplicationTrigger.ZlozenieZgloszenia);
    }

    [Fact]
    public void GetPermittedTriggers_WhenSubmitted_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");

        // Act
        var triggers = app.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(AdoptionApplicationTrigger.PodjęciePrzezPracownika);
        triggers.Should().Contain(AdoptionApplicationTrigger.AnulowanePrzezUzytkownika);
    }

    [Fact]
    public void GetPermittedTriggers_WhenUnderReview_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var app = CreateTestApplication().Value;
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "jan@test.pl");
        app.TakeForReview(Guid.NewGuid(), "Anna Nowak");

        // Act
        var triggers = app.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(AdoptionApplicationTrigger.PozytywnaWeryfikacjaDanych);
        triggers.Should().Contain(AdoptionApplicationTrigger.NegatywnaWeryfikacja);
    }

    #endregion
}
