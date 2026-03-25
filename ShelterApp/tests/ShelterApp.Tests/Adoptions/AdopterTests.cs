using FluentAssertions;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Adoptions.Events;
using Xunit;

namespace ShelterApp.Tests.Adoptions;

public class AdopterTests
{
    private static DateTime GetAdultDateOfBirth(int age = 25)
    {
        return DateTime.Today.AddYears(-age);
    }

    #region CreateAnonymous Tests

    [Fact]
    public void CreateAnonymous_ShouldCreateWithAnonymousStatus()
    {
        // Act
        var adopter = Adopter.CreateAnonymous();

        // Assert
        adopter.Status.Should().Be(AdopterStatus.Anonymous);
        adopter.UserId.Should().BeNull();
        adopter.FirstName.Should().BeEmpty();
        adopter.LastName.Should().BeEmpty();
    }

    [Fact]
    public void CreateAnonymous_ShouldGenerateUniqueId()
    {
        // Act
        var adopter1 = Adopter.CreateAnonymous();
        var adopter2 = Adopter.CreateAnonymous();

        // Assert
        adopter1.Id.Should().NotBe(Guid.Empty);
        adopter2.Id.Should().NotBe(Guid.Empty);
        adopter1.Id.Should().NotBe(adopter2.Id);
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = Adopter.Create(
            userId: userId,
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan.kowalski@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth(25),
            address: "ul. Testowa 1",
            city: "Warszawa",
            postalCode: "00-001"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.FirstName.Should().Be("Jan");
        result.Value.LastName.Should().Be("Kowalski");
        result.Value.Email.Should().Be("jan.kowalski@email.com");
        result.Value.Phone.Should().Be("123456789");
        result.Value.Status.Should().Be(AdopterStatus.Registered);
        result.Value.RodoConsentDate.Should().NotBeNull();
    }

    [Fact]
    public void Create_Under18YearsOld_ShouldFail()
    {
        // Act
        var result = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: DateTime.Today.AddYears(-17)
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("18 lat");
    }

    [Fact]
    public void Create_Exactly18YearsOld_ShouldSucceed()
    {
        // Act
        var result = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: DateTime.Today.AddYears(-18)
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldPublishAdopterRegisteredEvent()
    {
        // Act
        var result = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        );

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdopterRegisteredEvent>();
    }

    [Fact]
    public void FullName_ShouldReturnCombinedName()
    {
        // Arrange
        var result = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        );

        // Assert
        result.Value.FullName.Should().Be("Jan Kowalski");
    }

    [Fact]
    public void Age_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: DateTime.Today.AddYears(-30)
        );

        // Assert
        result.Value.Age.Should().Be(30);
    }

    #endregion

    #region Register Tests

    [Fact]
    public void Register_FromAnonymous_ShouldSucceed()
    {
        // Arrange
        var adopter = Adopter.CreateAnonymous();
        var userId = Guid.NewGuid();

        // Act
        var result = adopter.Register(
            userId: userId,
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Registered);
        adopter.UserId.Should().Be(userId);
        adopter.FirstName.Should().Be("Jan");
    }

    [Fact]
    public void Register_FromAnonymous_Under18_ShouldFail()
    {
        // Arrange
        var adopter = Adopter.CreateAnonymous();

        // Act
        var result = adopter.Register(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: DateTime.Today.AddYears(-17)
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("18 lat");
    }

    [Fact]
    public void Register_FromRegistered_ShouldFail()
    {
        // Arrange
        var result = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        );
        var adopter = result.Value;

        // Act
        var registerResult = adopter.Register(
            userId: Guid.NewGuid(),
            firstName: "Piotr",
            lastName: "Nowak",
            email: "piotr@email.com",
            phone: "987654321",
            dateOfBirth: GetAdultDateOfBirth()
        );

        // Assert
        registerResult.IsFailure.Should().BeTrue();
        registerResult.Error.Message.Should().Contain("anonimowy");
    }

    #endregion

    #region Update Methods Tests

    [Fact]
    public void UpdateContactInfo_ShouldUpdateSpecifiedFields()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;

        // Act
        adopter.UpdateContactInfo(
            phone: "111222333",
            address: "ul. Nowa 10",
            city: "Kraków",
            postalCode: "30-001"
        );

        // Assert
        adopter.Phone.Should().Be("111222333");
        adopter.Address.Should().Be("ul. Nowa 10");
        adopter.City.Should().Be("Kraków");
        adopter.PostalCode.Should().Be("30-001");
    }

    [Fact]
    public void UpdatePersonalInfo_ShouldUpdateSpecifiedFields()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;

        // Act
        var result = adopter.UpdatePersonalInfo(
            firstName: "Piotr",
            lastName: "Nowak",
            email: "piotr.nowak@email.com"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.FirstName.Should().Be("Piotr");
        adopter.LastName.Should().Be("Nowak");
        adopter.Email.Should().Be("piotr.nowak@email.com");
    }

    [Fact]
    public void UpdatePersonalInfo_WithUnder18DateOfBirth_ShouldFail()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;

        // Act
        var result = adopter.UpdatePersonalInfo(dateOfBirth: DateTime.Today.AddYears(-17));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("18 lat");
    }

    [Fact]
    public void GiveRodoConsent_ShouldSetRodoConsentDate()
    {
        // Arrange
        var adopter = Adopter.CreateAnonymous();
        var before = DateTime.UtcNow;

        // Act
        adopter.GiveRodoConsent();
        var after = DateTime.UtcNow;

        // Assert
        adopter.RodoConsentDate.Should().NotBeNull();
        adopter.RodoConsentDate.Should().BeOnOrAfter(before);
        adopter.RodoConsentDate.Should().BeOnOrBefore(after);
    }

    #endregion

    #region State Machine Tests

    [Fact]
    public void ChangeStatus_ValidTransition_ShouldSucceed()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;
        adopter.ClearDomainEvents();

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
            changedBy: "admin"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Applying);
    }

    [Fact]
    public void ChangeStatus_InvalidTransition_ShouldFail()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.PodpisanieUmowy,
            changedBy: "admin"
        );

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ChangeStatus_ShouldAddToStatusHistory()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;

        // Act
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");

        // Assert
        adopter.StatusHistory.Should().HaveCount(1);
        var history = adopter.StatusHistory.First();
        history.PreviousStatus.Should().Be(AdopterStatus.Registered);
        history.NewStatus.Should().Be(AdopterStatus.Applying);
        history.Trigger.Should().Be(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego);
    }

    [Fact]
    public void ChangeStatus_ShouldPublishStatusChangedEvent()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;
        adopter.ClearDomainEvents();

        // Act
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");

        // Assert
        adopter.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdopterStatusChangedEvent>();
    }

    [Fact]
    public void CanChangeStatus_ValidTransition_ShouldReturnTrue()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;

        // Act
        var canChange = adopter.CanChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego);

        // Assert
        canChange.Should().BeTrue();
    }

    [Fact]
    public void CanChangeStatus_InvalidTransition_ShouldReturnFalse()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;

        // Act
        var canChange = adopter.CanChangeStatus(AdopterStatusTrigger.PodpisanieUmowy);

        // Assert
        canChange.Should().BeFalse();
    }

    [Fact]
    public void GetPermittedTriggers_FromRegistered_ShouldContainZlozenieZgloszenia()
    {
        // Arrange
        var adopter = Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@email.com",
            phone: "123456789",
            dateOfBirth: GetAdultDateOfBirth()
        ).Value;

        // Act
        var triggers = adopter.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego);
    }

    #endregion
}
