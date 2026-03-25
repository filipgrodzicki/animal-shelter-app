using FluentAssertions;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Adoptions.Events;
using Xunit;

namespace ShelterApp.Tests.Adoptions;

public class AdoptionApplicationTests
{
    private static AdoptionApplication CreateTestApplication()
    {
        var result = AdoptionApplication.Create(
            adopterId: Guid.NewGuid(),
            animalId: Guid.NewGuid(),
            adoptionMotivation: "Chcę mieć psa",
            petExperience: "Miałem psa przez 5 lat",
            livingConditions: "Dom z ogrodem",
            otherPetsInfo: "Brak innych zwierząt"
        );
        return result.Value;
    }

    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var adopterId = Guid.NewGuid();
        var animalId = Guid.NewGuid();

        // Act
        var result = AdoptionApplication.Create(
            adopterId: adopterId,
            animalId: animalId,
            adoptionMotivation: "Motywacja",
            petExperience: "Doświadczenie",
            livingConditions: "Warunki",
            otherPetsInfo: "Inne zwierzęta"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdopterId.Should().Be(adopterId);
        result.Value.AnimalId.Should().Be(animalId);
        result.Value.Status.Should().Be(AdoptionApplicationStatus.New);
        result.Value.AdoptionMotivation.Should().Be("Motywacja");
    }

    [Fact]
    public void Create_ShouldPublishAdoptionApplicationCreatedEvent()
    {
        // Act
        var result = AdoptionApplication.Create(
            adopterId: Guid.NewGuid(),
            animalId: Guid.NewGuid()
        );

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdoptionApplicationCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var app1 = CreateTestApplication();
        var app2 = CreateTestApplication();

        // Assert
        app1.Id.Should().NotBe(Guid.Empty);
        app2.Id.Should().NotBe(Guid.Empty);
        app1.Id.Should().NotBe(app2.Id);
    }

    [Fact]
    public void Create_ShouldSetApplicationDateToNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var result = AdoptionApplication.Create(Guid.NewGuid(), Guid.NewGuid());
        var after = DateTime.UtcNow;

        // Assert
        result.Value.ApplicationDate.Should().BeOnOrAfter(before);
        result.Value.ApplicationDate.Should().BeOnOrBefore(after);
    }

    #endregion

    #region Business Operations Tests

    [Fact]
    public void TakeForReview_FromSubmitted_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.ClearDomainEvents();
        var reviewerId = Guid.NewGuid();

        // Act
        var result = app.TakeForReview(reviewerId, "Jan Kowalski");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.UnderReview);
        app.ReviewedByUserId.Should().Be(reviewerId);
        app.ReviewDate.Should().NotBeNull();
    }

    [Fact]
    public void ApproveApplication_FromUnderReview_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ClearDomainEvents();

        // Act
        var result = app.ApproveApplication("reviewer", "Pozytywna weryfikacja");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Accepted);
        app.ReviewNotes.Should().Be("Pozytywna weryfikacja");
    }

    [Fact]
    public void RejectApplication_FromUnderReview_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ClearDomainEvents();

        // Act
        var result = app.RejectApplication("reviewer", "Nieodpowiednie warunki");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Rejected);
        app.RejectionReason.Should().Be("Nieodpowiednie warunki");
    }

    [Fact]
    public void ScheduleVisit_WithFutureDate_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");
        var visitDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = app.ScheduleVisit(visitDate, "admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.VisitScheduled);
        app.ScheduledVisitDate.Should().Be(visitDate);
    }

    [Fact]
    public void ScheduleVisit_WithPastDate_ShouldFail()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");

        // Act
        var result = app.ScheduleVisit(DateTime.UtcNow.AddHours(-1), "admin");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("przyszłości");
    }

    [Fact]
    public void RecordVisitAttendance_ShouldSetVisitDateAndConductor()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "admin");
        var conductorId = Guid.NewGuid();

        // Act
        var result = app.RecordVisitAttendance(conductorId, "Anna Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.VisitCompleted);
        app.VisitConductedByUserId.Should().Be(conductorId);
        app.VisitDate.Should().NotBeNull();
    }

    [Fact]
    public void RecordVisitResult_Positive_ShouldMoveToFending()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "admin");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");

        // Act
        var result = app.RecordVisitResult(
            isPositive: true,
            assessment: 5,
            notes: "Świetne warunki",
            recordedBy: "reviewer"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.PendingFinalization);
        app.VisitAssessment.Should().Be(5);
        app.VisitNotes.Should().Be("Świetne warunki");
    }

    [Fact]
    public void RecordVisitResult_Negative_ShouldReject()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "admin");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");

        // Act
        var result = app.RecordVisitResult(
            isPositive: false,
            assessment: 2,
            notes: "Nieodpowiednie warunki",
            recordedBy: "reviewer"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Rejected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public void RecordVisitResult_InvalidAssessment_ShouldFail(int assessment)
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "admin");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");

        // Act
        var result = app.RecordVisitResult(true, assessment, "Notatka", "reviewer");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("1-5");
    }

    [Fact]
    public void GenerateContract_FromPendingFinalization_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "admin");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "reviewer");

        // Act
        var result = app.GenerateContract("UMO/2024/001", "admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.ContractNumber.Should().Be("UMO/2024/001");
        app.ContractGeneratedDate.Should().NotBeNull();
    }

    [Fact]
    public void GenerateContract_NotFromPendingFinalization_ShouldFail()
    {
        // Arrange
        var app = CreateTestApplication();

        // Act
        var result = app.GenerateContract("UMO/2024/001", "admin");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("oczekujących na finalizację");
    }

    [Fact]
    public void FinalizeAdoption_WithContract_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "admin");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "reviewer");
        app.GenerateContract("UMO/2024/001", "admin");
        app.ClearDomainEvents();

        // Act
        var result = app.FinalizeAdoption("/contracts/UMO-2024-001.pdf", "admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Completed);
        app.ContractFilePath.Should().Be("/contracts/UMO-2024-001.pdf");
        app.ContractSignedDate.Should().NotBeNull();
        app.CompletionDate.Should().NotBeNull();
    }

    [Fact]
    public void FinalizeAdoption_WithoutContract_ShouldFail()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");
        app.ScheduleVisit(DateTime.UtcNow.AddDays(7), "admin");
        app.RecordVisitAttendance(Guid.NewGuid(), "Anna Nowak");
        app.RecordVisitResult(true, 5, "OK", "reviewer");

        // Act
        var result = app.FinalizeAdoption("/contracts/test.pdf", "admin");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("wygenerować umowę");
    }

    #endregion

    #region CancelByUser Tests

    [Fact]
    public void CancelByUser_FromSubmitted_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");

        // Act
        var result = app.CancelByUser("Zmiana zdania", "user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Cancelled);
    }

    [Fact]
    public void CancelByUser_FromAccepted_ShouldSucceed()
    {
        // Arrange
        var app = CreateTestApplication();
        app.ChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia, "user");
        app.TakeForReview(Guid.NewGuid(), "reviewer");
        app.ApproveApplication("reviewer", "OK");

        // Act
        var result = app.CancelByUser("Zmiana zdania", "user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(AdoptionApplicationStatus.Cancelled);
    }

    #endregion

    #region State Machine Tests

    [Fact]
    public void GetPermittedTriggers_FromNew_ShouldContainZlozenieZgloszenia()
    {
        // Arrange
        var app = CreateTestApplication();

        // Act
        var triggers = app.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(AdoptionApplicationTrigger.ZlozenieZgloszenia);
    }

    [Fact]
    public void CanChangeStatus_ValidTransition_ShouldReturnTrue()
    {
        // Arrange
        var app = CreateTestApplication();

        // Act
        var canChange = app.CanChangeStatus(AdoptionApplicationTrigger.ZlozenieZgloszenia);

        // Assert
        canChange.Should().BeTrue();
    }

    [Fact]
    public void CanChangeStatus_InvalidTransition_ShouldReturnFalse()
    {
        // Arrange
        var app = CreateTestApplication();

        // Act
        var canChange = app.CanChangeStatus(AdoptionApplicationTrigger.PodpisanieUmowy);

        // Assert
        canChange.Should().BeFalse();
    }

    #endregion
}
