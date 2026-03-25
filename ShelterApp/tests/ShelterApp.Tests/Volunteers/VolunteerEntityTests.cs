using FluentAssertions;
using ShelterApp.Domain.Volunteers;
using ShelterApp.Domain.Volunteers.Events;
using Xunit;

namespace ShelterApp.Tests.Volunteers;

public class VolunteerEntityTests
{
    private static DateTime GetAdultDateOfBirth(int age = 20)
    {
        return DateTime.Today.AddYears(-age);
    }

    private static Volunteer CreateTestVolunteer()
    {
        var result = Volunteer.Create(
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan.kowalski@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth(25),
            address: "ul. Testowa 1",
            city: "Warszawa",
            postalCode: "00-001",
            emergencyContactName: "Anna Kowalska",
            emergencyContactPhone: "987654321",
            skills: new[] { "Opieka nad psami", "Spacery" },
            availability: new[] { DayOfWeek.Monday, DayOfWeek.Wednesday },
            notes: "Notatki testowe"
        );
        return result.Value;
    }

    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Act
        var result = Volunteer.Create(
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan.kowalski@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth(20)
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Jan");
        result.Value.LastName.Should().Be("Kowalski");
        result.Value.Email.Should().Be("jan.kowalski@email.com");
        result.Value.Phone.Should().Be("123456789");
        result.Value.Status.Should().Be(VolunteerStatus.Candidate);
    }

    [Fact]
    public void Create_Under16YearsOld_ShouldFail()
    {
        // Act
        var result = Volunteer.Create(
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: DateTime.Today.AddYears(-15)
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("16 lat");
    }

    [Fact]
    public void Create_Exactly16YearsOld_ShouldSucceed()
    {
        // Act
        var result = Volunteer.Create(
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: DateTime.Today.AddYears(-16)
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var volunteer1 = CreateTestVolunteer();
        var volunteer2 = CreateTestVolunteer();

        // Assert
        volunteer1.Id.Should().NotBe(Guid.Empty);
        volunteer2.Id.Should().NotBe(Guid.Empty);
        volunteer1.Id.Should().NotBe(volunteer2.Id);
    }

    [Fact]
    public void Create_ShouldPublishVolunteerApplicationSubmittedEvent()
    {
        // Act
        var result = Volunteer.Create(
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        );

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<VolunteerApplicationSubmittedEvent>();
    }

    [Fact]
    public void Create_ShouldSetSkillsAndAvailability()
    {
        // Act
        var result = Volunteer.Create(
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth(),
            skills: new[] { "Skill1", "Skill2" },
            availability: new[] { DayOfWeek.Monday, DayOfWeek.Friday }
        );

        // Assert
        result.Value.Skills.Should().HaveCount(2);
        result.Value.Skills.Should().Contain("Skill1");
        result.Value.Availability.Should().HaveCount(2);
        result.Value.Availability.Should().Contain(DayOfWeek.Monday);
    }

    [Fact]
    public void CreateWithUser_ShouldSetUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = Volunteer.CreateWithUser(
            userId: userId,
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
    }

    [Fact]
    public void FullName_ShouldReturnCombinedFirstAndLastName()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Assert
        volunteer.FullName.Should().Be("Jan Kowalski");
    }

    #endregion

    #region Training Operations Tests

    [Fact]
    public void AcceptAndStartTraining_FromCandidate_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.AcceptAndStartTraining("admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.InTraining);
        volunteer.TrainingStartDate.Should().NotBeNull();
    }

    [Fact]
    public void CompleteTraining_FromInTraining_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.CompleteTraining("admin", "VOL/2024/001");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Active);
        volunteer.ContractNumber.Should().Be("VOL/2024/001");
        volunteer.TrainingEndDate.Should().NotBeNull();
        volunteer.ContractSignedDate.Should().NotBeNull();
    }

    [Fact]
    public void CompleteTraining_WithoutContractNumber_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");

        // Act
        var result = volunteer.CompleteTraining("admin", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Numer umowy");
    }

    [Fact]
    public void RejectApplication_FromCandidate_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.RejectApplication("admin", "Brak miejsc");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Inactive);
    }

    [Fact]
    public void RejectApplication_WithoutReason_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        var result = volunteer.RejectApplication("admin", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Powód");
    }

    [Fact]
    public void ResignFromTraining_FromInTraining_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");

        // Act
        var result = volunteer.ResignFromTraining("admin", "Osobiste powody");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Inactive);
    }

    #endregion

    #region Activity Operations Tests

    [Fact]
    public void Suspend_FromActive_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.CompleteTraining("admin", "VOL/001");

        // Act
        var result = volunteer.Suspend("admin", "Tymczasowa przerwa");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Suspended);
    }

    [Fact]
    public void Suspend_WithoutReason_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.CompleteTraining("admin", "VOL/001");

        // Act
        var result = volunteer.Suspend("admin", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Powód");
    }

    [Fact]
    public void Resume_FromSuspended_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.CompleteTraining("admin", "VOL/001");
        volunteer.Suspend("admin", "Tymczasowa przerwa");

        // Act
        var result = volunteer.Resume("admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Active);
    }

    [Fact]
    public void Resign_FromActive_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.CompleteTraining("admin", "VOL/001");

        // Act
        var result = volunteer.Resign("admin", "Osobiste powody");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Inactive);
    }

    [Fact]
    public void Reapply_FromInactive_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.RejectApplication("admin", "Powód");

        // Act
        var result = volunteer.Reapply("admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Status.Should().Be(VolunteerStatus.Candidate);
    }

    #endregion

    #region Work Hours Tests

    [Fact]
    public void AddWorkHours_ActiveVolunteer_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.CompleteTraining("admin", "VOL/001");
        volunteer.ClearDomainEvents();

        // Act
        var result = volunteer.AddWorkHours(4.5m, "admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.TotalHoursWorked.Should().Be(4.5m);
    }

    [Fact]
    public void AddWorkHours_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.CompleteTraining("admin", "VOL/001");

        // Act
        volunteer.AddWorkHours(4m, "admin");
        volunteer.AddWorkHours(3m, "admin");
        volunteer.AddWorkHours(2.5m, "admin");

        // Assert
        volunteer.TotalHoursWorked.Should().Be(9.5m);
    }

    [Fact]
    public void AddWorkHours_InactiveVolunteer_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        var result = volunteer.AddWorkHours(4m, "admin");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("aktywnych wolontariuszy");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void AddWorkHours_ZeroOrNegativeHours_ShouldFail(decimal hours)
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.CompleteTraining("admin", "VOL/001");

        // Act
        var result = volunteer.AddWorkHours(hours, "admin");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("większa od zera");
    }

    [Fact]
    public void AddWorkHours_ShouldPublishEvent()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AcceptAndStartTraining("admin");
        volunteer.CompleteTraining("admin", "VOL/001");
        volunteer.ClearDomainEvents();

        // Act
        volunteer.AddWorkHours(4m, "admin");

        // Assert
        volunteer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<VolunteerHoursRecordedEvent>();
    }

    #endregion

    #region Update Methods Tests

    [Fact]
    public void UpdateContactInfo_ShouldUpdateSpecifiedFields()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        volunteer.UpdateContactInfo(
            phone: "111222333",
            address: "ul. Nowa 10",
            city: "Kraków",
            postalCode: "30-001",
            emergencyContactName: "Piotr Kowalski",
            emergencyContactPhone: "444555666"
        );

        // Assert
        volunteer.Phone.Should().Be("111222333");
        volunteer.Address.Should().Be("ul. Nowa 10");
        volunteer.City.Should().Be("Kraków");
        volunteer.PostalCode.Should().Be("30-001");
        volunteer.EmergencyContactName.Should().Be("Piotr Kowalski");
        volunteer.EmergencyContactPhone.Should().Be("444555666");
    }

    [Fact]
    public void UpdatePersonalInfo_ShouldUpdateSpecifiedFields()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        var result = volunteer.UpdatePersonalInfo(
            firstName: "Piotr",
            lastName: "Nowak",
            email: "piotr.nowak@email.com"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.FirstName.Should().Be("Piotr");
        volunteer.LastName.Should().Be("Nowak");
        volunteer.Email.Should().Be("piotr.nowak@email.com");
    }

    [Fact]
    public void UpdatePersonalInfo_WithUnder16DateOfBirth_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        var result = volunteer.UpdatePersonalInfo(dateOfBirth: DateTime.Today.AddYears(-15));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("16 lat");
    }

    [Fact]
    public void UpdateSkills_ShouldReplaceAllSkills()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        volunteer.UpdateSkills(new[] { "Nowa umiejętność 1", "Nowa umiejętność 2" });

        // Assert
        volunteer.Skills.Should().HaveCount(2);
        volunteer.Skills.Should().Contain("Nowa umiejętność 1");
        volunteer.Skills.Should().Contain("Nowa umiejętność 2");
    }

    [Fact]
    public void AddSkill_ShouldAddNewSkill()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        var initialCount = volunteer.Skills.Count;

        // Act
        volunteer.AddSkill("Nowa umiejętność");

        // Assert
        volunteer.Skills.Should().HaveCount(initialCount + 1);
        volunteer.Skills.Should().Contain("Nowa umiejętność");
    }

    [Fact]
    public void AddSkill_DuplicateSkill_ShouldNotAddAgain()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AddSkill("Unikalna umiejętność");
        var countAfterFirstAdd = volunteer.Skills.Count;

        // Act
        volunteer.AddSkill("Unikalna umiejętność");

        // Assert
        volunteer.Skills.Should().HaveCount(countAfterFirstAdd);
    }

    [Fact]
    public void RemoveSkill_ExistingSkill_ShouldRemove()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AddSkill("Do usunięcia");

        // Act
        volunteer.RemoveSkill("Do usunięcia");

        // Assert
        volunteer.Skills.Should().NotContain("Do usunięcia");
    }

    [Fact]
    public void UpdateAvailability_ShouldReplaceAllDays()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        volunteer.UpdateAvailability(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday });

        // Assert
        volunteer.Availability.Should().HaveCount(3);
        volunteer.Availability.Should().Contain(DayOfWeek.Tuesday);
        volunteer.Availability.Should().NotContain(DayOfWeek.Monday);
    }

    [Fact]
    public void UpdateNotes_ShouldUpdateNotes()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        volunteer.UpdateNotes("Nowe notatki");

        // Assert
        volunteer.Notes.Should().Be("Nowe notatki");
    }

    [Fact]
    public void AssignUser_ShouldSetUserId()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        var userId = Guid.NewGuid();

        // Act
        volunteer.AssignUser(userId);

        // Assert
        volunteer.UserId.Should().Be(userId);
    }

    #endregion

    #region Certificate Operations Tests

    [Fact]
    public void AddCertificate_ShouldAddCertificateSuccessfully()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        var result = volunteer.AddCertificate(
            type: CertificateType.BasicTraining,
            name: "Szkolenie podstawowe",
            issuingOrganization: "Schronisko",
            issueDate: DateTime.UtcNow,
            certificateNumber: "CERT/001",
            expiryDate: DateTime.UtcNow.AddYears(2),
            notes: "Notatki"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Certificates.Should().HaveCount(1);
        result.Value.Name.Should().Be("Szkolenie podstawowe");
        result.Value.Type.Should().Be(CertificateType.BasicTraining);
    }

    [Fact]
    public void RemoveCertificate_ExistingCertificate_ShouldSucceed()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        var certResult = volunteer.AddCertificate(
            CertificateType.BasicTraining,
            "Szkolenie",
            "Organizacja",
            DateTime.UtcNow
        );
        var certificateId = certResult.Value.Id;

        // Act
        var result = volunteer.RemoveCertificate(certificateId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        volunteer.Certificates.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCertificate_NonExistingCertificate_ShouldFail()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();

        // Act
        var result = volunteer.RemoveCertificate(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void GetActiveCertificates_ShouldReturnOnlyNonExpired()
    {
        // Arrange
        var volunteer = CreateTestVolunteer();
        volunteer.AddCertificate(
            CertificateType.BasicTraining,
            "Aktywny cert",
            "Org",
            DateTime.UtcNow,
            expiryDate: DateTime.UtcNow.AddYears(1)
        );
        volunteer.AddCertificate(
            CertificateType.AnimalFirstAid,
            "Wygasły cert",
            "Org",
            DateTime.UtcNow.AddYears(-2),
            expiryDate: DateTime.UtcNow.AddDays(-1)
        );
        volunteer.AddCertificate(
            CertificateType.BehavioralTraining,
            "Bezterminowy cert",
            "Org",
            DateTime.UtcNow
        );

        // Act
        var activeCerts = volunteer.GetActiveCertificates().ToList();

        // Assert
        activeCerts.Should().HaveCount(2);
        activeCerts.Should().Contain(c => c.Name == "Aktywny cert");
        activeCerts.Should().Contain(c => c.Name == "Bezterminowy cert");
        activeCerts.Should().NotContain(c => c.Name == "Wygasły cert");
    }

    #endregion
}
