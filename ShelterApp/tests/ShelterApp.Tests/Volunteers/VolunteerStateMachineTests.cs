using FluentAssertions;
using ShelterApp.Domain.Common;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Domain.Volunteers.Events;
using Xunit;

namespace ShelterApp.Tests.Volunteers;

public class VolunteerStateMachineTests
{
    private static Result<Volunteer> CreateTestVolunteer(
        string firstName = "Jan",
        string lastName = "Kowalski",
        DateTime? dateOfBirth = null)
    {
        return Volunteer.Create(
            firstName: firstName,
            lastName: lastName,
            email: "jan.kowalski@test.pl",
            phone: "+48123456789",
            dateOfBirth: dateOfBirth ?? DateTime.Today.AddYears(-25),
            address: "ul. Testowa 1",
            city: "Warszawa",
            postalCode: "00-001",
            emergencyContactName: "Anna Kowalska",
            emergencyContactPhone: "+48987654321",
            skills: new[] { "Opieka nad psami", "Pierwsza pomoc" },
            availability: new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }
        );
    }

    #region 1. Tworzenie wolontariusza

    [Fact]
    public void Create_ValidData_ShouldReturnSuccess()
    {
        // Act
        var result = CreateTestVolunteer();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Create_NewVolunteer_ShouldHaveCandidateStatus()
    {
        // Act
        var result = CreateTestVolunteer();

        // Assert
        result.Value.Status.Should().Be(VolunteerStatus.Candidate);
    }

    [Fact]
    public void Create_NewVolunteer_ShouldPublishApplicationSubmittedEvent()
    {
        // Act
        var result = CreateTestVolunteer();

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<VolunteerApplicationSubmittedEvent>();
    }

    [Fact]
    public void Create_VolunteerUnder16_ShouldReturnFailure()
    {
        // Arrange
        var dateOfBirth = DateTime.Today.AddYears(-15);

        // Act
        var result = CreateTestVolunteer(dateOfBirth: dateOfBirth);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("16 lat");
    }

    [Fact]
    public void Create_Volunteer16YearsOld_ShouldReturnSuccess()
    {
        // Arrange
        var dateOfBirth = DateTime.Today.AddYears(-16);

        // Act
        var result = CreateTestVolunteer(dateOfBirth: dateOfBirth);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetCorrectProperties()
    {
        // Act
        var result = CreateTestVolunteer("Anna", "Nowak");

        // Assert
        var volunteer = result.Value;
        volunteer.FirstName.Should().Be("Anna");
        volunteer.LastName.Should().Be("Nowak");
        volunteer.FullName.Should().Be("Anna Nowak");
        volunteer.Email.Should().Be("jan.kowalski@test.pl");
        volunteer.Skills.Should().Contain("Opieka nad psami");
        volunteer.Availability.Should().Contain(DayOfWeek.Monday);
    }

    #endregion

    #region 2. Przejście Candidate -> InTraining

    [Fact]
    public void AcceptAndStartTraining_FromCandidate_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.AcceptAndStartTraining("admin@shelter.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.InTraining);
        volunteer.TrainingStartDate.Should().NotBeNull();
    }

    [Fact]
    public void CanChangeStatus_FromCandidateToInTraining_ShouldReturnTrue()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        var canChange = volunteer.CanChangeStatus(
            VolunteerStatusTrigger.AkceptacjaZgloszenia);

        // Assert
        canChange.Should().BeTrue();
    }

    [Fact]
    public void AcceptAndStartTraining_ShouldPublishStatusChangedEvent()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.ClearDomainEvents();

        // Act
        volunteer.AcceptAndStartTraining("admin@shelter.pl");

        // Assert
        volunteer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<VolunteerStatusChangedEvent>();
    }

    #endregion

    #region 3. Przejście Candidate -> Inactive (odrzucenie)

    [Fact]
    public void RejectApplication_FromCandidate_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.RejectApplication(
            "admin@shelter.pl",
            "Brak wymaganego doświadczenia");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Inactive);
    }

    [Fact]
    public void RejectApplication_WithoutReason_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        var result = volunteer.RejectApplication("admin@shelter.pl", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Powód odrzucenia");
    }

    #endregion

    #region 4. Przejście InTraining -> Active

    [Fact]
    public void CompleteTraining_FromInTraining_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.CompleteTraining(
            "admin@shelter.pl",
            "VOL/2024/001");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Active);
        volunteer.ContractNumber.Should().Be("VOL/2024/001");
        volunteer.ContractSignedDate.Should().NotBeNull();
    }

    [Fact]
    public void CompleteTraining_WithoutContractNumber_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");

        // Act
        var result = volunteer.CompleteTraining("admin@shelter.pl", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Numer umowy");
    }

    [Fact]
    public void CompleteTraining_ShouldPublishActivatedEvent()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.ClearDomainEvents();

        // Act
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");

        // Assert
        volunteer.DomainEvents.Should().HaveCount(2);
        volunteer.DomainEvents.Should().ContainSingle(e => e is VolunteerActivatedEvent);
    }

    #endregion

    #region 5. Przejście InTraining -> Inactive (rezygnacja)

    [Fact]
    public void ResignFromTraining_FromInTraining_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.ResignFromTraining(
            "jan.kowalski@test.pl",
            "Brak czasu");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Inactive);
    }

    #endregion

    #region 6. Przejście Active -> Suspended

    [Fact]
    public void Suspend_FromActive_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.Suspend(
            "admin@shelter.pl",
            "Naruszenie regulaminu");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Suspended);
    }

    [Fact]
    public void Suspend_WithoutReason_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");

        // Act
        var result = volunteer.Suspend("admin@shelter.pl", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Powód zawieszenia");
    }

    #endregion

    #region 7. Przejście Suspended -> Active (wznowienie)

    [Fact]
    public void Resume_FromSuspended_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");
        volunteer.Suspend("admin@shelter.pl", "Naruszenie regulaminu");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.Resume("admin@shelter.pl", "Po rozmowie dyscyplinarnej");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Active);
    }

    #endregion

    #region 8. Przejście Active/Suspended -> Inactive (rezygnacja)

    [Fact]
    public void Resign_FromActive_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.Resign("jan.kowalski@test.pl", "Przeprowadzka");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Inactive);
    }

    [Fact]
    public void Resign_FromSuspended_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");
        volunteer.Suspend("admin@shelter.pl", "Powód");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.Resign("jan.kowalski@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Inactive);
    }

    #endregion

    #region 9. Przejście Inactive -> Candidate (ponowne zgłoszenie)

    [Fact]
    public void Reapply_FromInactive_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.RejectApplication("admin@shelter.pl", "Powód");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.Reapply("jan.kowalski@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Candidate);
        volunteer.TrainingStartDate.Should().BeNull();
        volunteer.TrainingEndDate.Should().BeNull();
    }

    #endregion

    #region 10. Niedozwolone przejścia

    [Fact]
    public void ChangeStatus_InvalidTransition_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act - próba ukończenia szkolenia bez jego rozpoczęcia
        var result = volunteer.ChangeStatus(
            VolunteerStatusTrigger.UkonczenieSzkolenia,
            "admin@shelter.pl");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Nie można wykonać akcji");
    }

    [Fact]
    public void Suspend_FromCandidate_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        var result = volunteer.Suspend("admin@shelter.pl", "Powód");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Resume_FromActive_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");

        // Act
        var result = volunteer.Resume("admin@shelter.pl");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region 11. Godziny pracy

    [Fact]
    public void AddWorkHours_ForActiveVolunteer_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.AddWorkHours(4.5m, "admin@shelter.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.TotalHoursWorked.Should().Be(4.5m);
    }

    [Fact]
    public void AddWorkHours_ForInactiveVolunteer_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        var result = volunteer.AddWorkHours(4.5m, "admin@shelter.pl");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("aktywnych wolontariuszy");
    }

    [Fact]
    public void AddWorkHours_NegativeValue_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");

        // Act
        var result = volunteer.AddWorkHours(-2m, "admin@shelter.pl");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("większa od zera");
    }

    [Fact]
    public void AddWorkHours_ShouldAccumulate()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");

        // Act
        volunteer.AddWorkHours(4m, "admin");
        volunteer.AddWorkHours(3m, "admin");
        volunteer.AddWorkHours(5m, "admin");

        // Assert
        volunteer.TotalHoursWorked.Should().Be(12m);
    }

    #endregion

    #region 12. Aktualizacja danych

    [Fact]
    public void UpdatePersonalInfo_ValidData_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        var result = volunteer.UpdatePersonalInfo(
            firstName: "Janusz",
            lastName: "Nowak",
            email: "janusz.nowak@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.FirstName.Should().Be("Janusz");
        volunteer.LastName.Should().Be("Nowak");
        volunteer.Email.Should().Be("janusz.nowak@test.pl");
    }

    [Fact]
    public void UpdatePersonalInfo_Under16_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        var result = volunteer.UpdatePersonalInfo(
            dateOfBirth: DateTime.Today.AddYears(-15));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateSkills_ShouldReplaceAllSkills()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        volunteer.UpdateSkills(new[] { "Nowa umiejętność 1", "Nowa umiejętność 2" });

        // Assert
        volunteer.Skills.Should().HaveCount(2);
        volunteer.Skills.Should().Contain("Nowa umiejętność 1");
        volunteer.Skills.Should().NotContain("Opieka nad psami");
    }

    [Fact]
    public void AddSkill_ShouldAddNewSkill()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        var initialCount = volunteer.Skills.Count;

        // Act
        volunteer.AddSkill("Nowa umiejętność");

        // Assert
        volunteer.Skills.Should().HaveCount(initialCount + 1);
        volunteer.Skills.Should().Contain("Nowa umiejętność");
    }

    [Fact]
    public void AddSkill_Duplicate_ShouldNotAdd()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        var initialCount = volunteer.Skills.Count;

        // Act
        volunteer.AddSkill("Opieka nad psami"); // już istnieje

        // Assert
        volunteer.Skills.Should().HaveCount(initialCount);
    }

    [Fact]
    public void RemoveSkill_ExistingSkill_ShouldRemove()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        volunteer.RemoveSkill("Opieka nad psami");

        // Assert
        volunteer.Skills.Should().NotContain("Opieka nad psami");
    }

    #endregion

    #region 13. Historia zmian statusu

    [Fact]
    public void ChangeStatus_ShouldAddEntryToStatusHistory()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        volunteer.AcceptAndStartTraining("admin@shelter.pl");

        // Assert
        volunteer.StatusHistory.Should().ContainSingle();
        var entry = volunteer.StatusHistory.First();
        entry.PreviousStatus.Should().Be(VolunteerStatus.Candidate);
        entry.NewStatus.Should().Be(VolunteerStatus.InTraining);
        entry.Trigger.Should().Be(VolunteerStatusTrigger.AkceptacjaZgloszenia);
        entry.ChangedBy.Should().Be("admin@shelter.pl");
    }

    [Fact]
    public void MultipleStatusChanges_ShouldTrackAllInHistory()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");
        volunteer.Suspend("admin@shelter.pl", "Powód");
        volunteer.Resume("admin@shelter.pl");

        // Assert
        volunteer.StatusHistory.Should().HaveCount(4);
    }

    #endregion

    #region 14. GetPermittedTriggers

    [Fact]
    public void GetPermittedTriggers_WhenCandidate_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;

        // Act
        var triggers = volunteer.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(VolunteerStatusTrigger.AkceptacjaZgloszenia);
        triggers.Should().Contain(VolunteerStatusTrigger.OdrzucenieZgloszenia);
        triggers.Should().NotContain(VolunteerStatusTrigger.UkonczenieSzkolenia);
    }

    [Fact]
    public void GetPermittedTriggers_WhenActive_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var volunteer = CreateTestVolunteer().Value;
        volunteer.AcceptAndStartTraining("admin@shelter.pl");
        volunteer.CompleteTraining("admin@shelter.pl", "VOL/2024/001");

        // Act
        var triggers = volunteer.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(VolunteerStatusTrigger.ZawieszenieAktywnosci);
        triggers.Should().Contain(VolunteerStatusTrigger.Rezygnacja);
        triggers.Should().NotContain(VolunteerStatusTrigger.Wznowienie);
    }

    #endregion
}
