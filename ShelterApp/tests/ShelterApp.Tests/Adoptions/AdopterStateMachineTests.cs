using FluentAssertions;
using ShelterApp.Domain.Adoptions;
using ShelterApp.Domain.Adoptions.Events;
using ShelterApp.Domain.Common;
using Xunit;

namespace ShelterApp.Tests.Adoptions;

public class AdopterStateMachineTests
{
    private static Result<Adopter> CreateTestAdopter(
        string firstName = "Jan",
        string lastName = "Kowalski",
        DateTime? dateOfBirth = null)
    {
        return Adopter.Create(
            userId: Guid.NewGuid(),
            firstName: firstName,
            lastName: lastName,
            email: "jan.kowalski@test.pl",
            phone: "+48123456789",
            dateOfBirth: dateOfBirth ?? DateTime.Today.AddYears(-30),
            address: "ul. Testowa 1",
            city: "Warszawa",
            postalCode: "00-001"
        );
    }

    #region 1. Tworzenie osoby adoptującej

    [Fact]
    public void Create_ValidData_ShouldReturnSuccess()
    {
        // Act
        var result = CreateTestAdopter();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Create_NewAdopter_ShouldHaveRegisteredStatus()
    {
        // Act
        var result = CreateTestAdopter();

        // Assert
        result.Value.Status.Should().Be(AdopterStatus.Registered);
    }

    [Fact]
    public void Create_NewAdopter_ShouldPublishRegisteredEvent()
    {
        // Act
        var result = CreateTestAdopter();

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdopterRegisteredEvent>();
    }

    [Fact]
    public void Create_AdopterUnder18_ShouldReturnFailure()
    {
        // Arrange
        var dateOfBirth = DateTime.Today.AddYears(-17);

        // Act
        var result = CreateTestAdopter(dateOfBirth: dateOfBirth);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("18 lat");
    }

    [Fact]
    public void Create_Adopter18YearsOld_ShouldReturnSuccess()
    {
        // Arrange
        var dateOfBirth = DateTime.Today.AddYears(-18);

        // Act
        var result = CreateTestAdopter(dateOfBirth: dateOfBirth);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetCorrectProperties()
    {
        // Act
        var result = CreateTestAdopter("Anna", "Nowak");

        // Assert
        var adopter = result.Value;
        adopter.FirstName.Should().Be("Anna");
        adopter.LastName.Should().Be("Nowak");
        adopter.FullName.Should().Be("Anna Nowak");
        adopter.Email.Should().Be("jan.kowalski@test.pl");
        adopter.RodoConsentDate.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldCalculateAgeCorrectly()
    {
        // Arrange
        var dateOfBirth = DateTime.Today.AddYears(-30);

        // Act
        var result = CreateTestAdopter(dateOfBirth: dateOfBirth);

        // Assert
        result.Value.Age.Should().Be(30);
    }

    #endregion

    #region 2. Anonimowy użytkownik

    [Fact]
    public void CreateAnonymous_ShouldHaveAnonymousStatus()
    {
        // Act
        var adopter = Adopter.CreateAnonymous();

        // Assert
        adopter.Status.Should().Be(AdopterStatus.Anonymous);
    }

    [Fact]
    public void CreateAnonymous_ShouldHaveEmptyPersonalData()
    {
        // Act
        var adopter = Adopter.CreateAnonymous();

        // Assert
        adopter.FirstName.Should().BeEmpty();
        adopter.LastName.Should().BeEmpty();
        adopter.Email.Should().BeEmpty();
    }

    #endregion

    #region 3. Rejestracja anonimowego użytkownika

    [Fact]
    public void Register_FromAnonymous_ShouldSucceed()
    {
        // Arrange
        var adopter = Adopter.CreateAnonymous();

        // Act
        var result = adopter.Register(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@test.pl",
            phone: "+48123456789",
            dateOfBirth: DateTime.Today.AddYears(-25));

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Registered);
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
            email: "jan@test.pl",
            phone: "+48123456789",
            dateOfBirth: DateTime.Today.AddYears(-17));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_FromRegistered_ShouldFail()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act
        var result = adopter.Register(
            userId: Guid.NewGuid(),
            firstName: "Jan",
            lastName: "Kowalski",
            email: "jan@test.pl",
            phone: "+48123456789",
            dateOfBirth: DateTime.Today.AddYears(-25));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("anonimowy");
    }

    #endregion

    #region 4. Przejście Registered -> Applying

    [Fact]
    public void ChangeStatus_FromRegisteredToApplying_ShouldSucceed()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ClearDomainEvents();

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
            "jan@test.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Applying);
    }

    [Fact]
    public void CanChangeStatus_FromRegisteredToApplying_ShouldReturnTrue()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act
        var canChange = adopter.CanChangeStatus(
            AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego);

        // Assert
        canChange.Should().BeTrue();
    }

    #endregion

    #region 5. Przejście Applying -> Registered (anulowanie)

    [Fact]
    public void ChangeStatus_FromApplyingToRegistered_WhenCancelled_ShouldSucceed()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");
        adopter.ClearDomainEvents();

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.AnulowanieZgloszenia,
            "jan@test.pl",
            "Zmiana planów");

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Registered);
    }

    [Fact]
    public void ChangeStatus_FromApplyingToRegistered_WhenRejected_ShouldSucceed()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");
        adopter.ClearDomainEvents();

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.OdrzucenieZgloszenia,
            "admin@shelter.pl",
            "Nieodpowiednie warunki");

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Registered);
    }

    #endregion

    #region 6. Przejście Applying -> Verified

    [Fact]
    public void ChangeStatus_FromApplyingToVerified_ShouldSucceed()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");
        adopter.ClearDomainEvents();

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.ZatwierdznieZgloszenia,
            "admin@shelter.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Verified);
    }

    #endregion

    #region 7. Przejście Verified -> Adopter

    [Fact]
    public void ChangeStatus_FromVerifiedToAdopter_ShouldSucceed()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");
        adopter.ChangeStatus(AdopterStatusTrigger.ZatwierdznieZgloszenia, "admin");
        adopter.ClearDomainEvents();

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.PozytywnaWeryfikacja,
            "admin@shelter.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Adopter);
    }

    #endregion

    #region 8. Przejście Verified -> Registered (negatywna weryfikacja)

    [Fact]
    public void ChangeStatus_FromVerifiedToRegistered_WhenNegative_ShouldSucceed()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");
        adopter.ChangeStatus(AdopterStatusTrigger.ZatwierdznieZgloszenia, "admin");
        adopter.ClearDomainEvents();

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.NegatywnaWeryfikacja,
            "admin@shelter.pl",
            "Nieodpowiednie warunki mieszkaniowe");

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Registered);
    }

    #endregion

    #region 9. Przejście Adopter -> Registered (po adopcji)

    [Fact]
    public void ChangeStatus_FromAdopterToRegistered_AfterContract_ShouldSucceed()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");
        adopter.ChangeStatus(AdopterStatusTrigger.ZatwierdznieZgloszenia, "admin");
        adopter.ChangeStatus(AdopterStatusTrigger.PozytywnaWeryfikacja, "admin");
        adopter.ClearDomainEvents();

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.PodpisanieUmowy,
            "admin@shelter.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.Status.Should().Be(AdopterStatus.Registered);
    }

    #endregion

    #region 10. Niedozwolone przejścia

    [Fact]
    public void ChangeStatus_InvalidTransition_ShouldFail()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act - próba zatwierdzenia bez złożenia zgłoszenia
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.ZatwierdznieZgloszenia,
            "admin@shelter.pl");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Nie można wykonać akcji");
    }

    [Fact]
    public void ChangeStatus_DirectToAdopter_ShouldFail()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act
        var result = adopter.ChangeStatus(
            AdopterStatusTrigger.PozytywnaWeryfikacja,
            "admin@shelter.pl");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region 11. Aktualizacja danych

    [Fact]
    public void UpdateContactInfo_ShouldUpdateProperties()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act
        adopter.UpdateContactInfo(
            phone: "+48999888777",
            address: "ul. Nowa 2",
            city: "Kraków",
            postalCode: "30-001");

        // Assert
        adopter.Phone.Should().Be("+48999888777");
        adopter.Address.Should().Be("ul. Nowa 2");
        adopter.City.Should().Be("Kraków");
        adopter.PostalCode.Should().Be("30-001");
    }

    [Fact]
    public void UpdatePersonalInfo_ValidData_ShouldSucceed()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act
        var result = adopter.UpdatePersonalInfo(
            firstName: "Janusz",
            lastName: "Nowak");

        // Assert
        result.IsSuccess.Should().BeTrue();
        adopter.FirstName.Should().Be("Janusz");
        adopter.LastName.Should().Be("Nowak");
    }

    [Fact]
    public void UpdatePersonalInfo_Under18_ShouldFail()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act
        var result = adopter.UpdatePersonalInfo(
            dateOfBirth: DateTime.Today.AddYears(-17));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void GiveRodoConsent_ShouldSetConsentDate()
    {
        // Arrange
        var adopter = Adopter.CreateAnonymous();
        adopter.Register(
            Guid.NewGuid(), "Jan", "Kowalski", "jan@test.pl",
            "+48123", DateTime.Today.AddYears(-25));

        // Act
        adopter.GiveRodoConsent();

        // Assert
        adopter.RodoConsentDate.Should().NotBeNull();
        adopter.RodoConsentDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region 12. Historia zmian statusu

    [Fact]
    public void ChangeStatus_ShouldAddEntryToStatusHistory()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");

        // Assert
        adopter.StatusHistory.Should().ContainSingle();
        var entry = adopter.StatusHistory.First();
        entry.PreviousStatus.Should().Be(AdopterStatus.Registered);
        entry.NewStatus.Should().Be(AdopterStatus.Applying);
        entry.Trigger.Should().Be(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego);
    }

    [Fact]
    public void ChangeStatus_ShouldPublishStatusChangedEvent()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ClearDomainEvents();

        // Act
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");

        // Assert
        adopter.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AdopterStatusChangedEvent>();
    }

    [Fact]
    public void FullAdopterFlow_ShouldTrackAllStatusChanges()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act - pełna ścieżka adopcji
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");
        adopter.ChangeStatus(AdopterStatusTrigger.ZatwierdznieZgloszenia, "admin");
        adopter.ChangeStatus(AdopterStatusTrigger.PozytywnaWeryfikacja, "admin");
        adopter.ChangeStatus(AdopterStatusTrigger.PodpisanieUmowy, "admin");

        // Assert
        adopter.StatusHistory.Should().HaveCount(4);
        adopter.Status.Should().Be(AdopterStatus.Registered); // Po adopcji wraca do Registered
    }

    #endregion

    #region 13. GetPermittedTriggers

    [Fact]
    public void GetPermittedTriggers_WhenRegistered_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;

        // Act
        var triggers = adopter.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego);
        triggers.Should().HaveCount(1);
    }

    [Fact]
    public void GetPermittedTriggers_WhenApplying_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var adopter = CreateTestAdopter().Value;
        adopter.ChangeStatus(AdopterStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "jan@test.pl");

        // Act
        var triggers = adopter.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(AdopterStatusTrigger.AnulowanieZgloszenia);
        triggers.Should().Contain(AdopterStatusTrigger.OdrzucenieZgloszenia);
        triggers.Should().Contain(AdopterStatusTrigger.ZatwierdznieZgloszenia);
    }

    [Fact]
    public void GetPermittedTriggers_WhenAnonymous_ShouldReturnRegisterTrigger()
    {
        // Arrange
        var adopter = Adopter.CreateAnonymous();

        // Act
        var triggers = adopter.GetPermittedTriggers().ToList();

        // Assert
        triggers.Should().Contain(AdopterStatusTrigger.RejestracjaKonta);
    }

    #endregion
}
