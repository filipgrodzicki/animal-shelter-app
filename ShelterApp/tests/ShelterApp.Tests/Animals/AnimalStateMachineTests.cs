using FluentAssertions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Animals.Events;
using Xunit;

namespace ShelterApp.Tests.Animals;

public class AnimalStateMachineTests
{
    private static Animal CreateTestAnimal()
    {
        return Animal.Create(
            registrationNumber: "SCH/DOG/2024/001",
            species: Species.Dog,
            breed: "Labrador",
            name: "Burek",
            gender: Gender.Male,
            size: Size.Large,
            color: "Czarny",
            admissionCircumstances: "Znaleziony na ulicy",
            ageInMonths: 24,
            chipNumber: "123456789012345",
            description: "Przyjazny pies"
        );
    }

    #region 1. Nowe zwierzę ma status Admitted

    [Fact]
    public void Create_NewAnimal_ShouldHaveAdmittedStatus()
    {
        // Act
        var animal = CreateTestAnimal();

        // Assert
        animal.Status.Should().Be(AnimalStatus.Admitted);
    }

    [Fact]
    public void Create_NewAnimal_ShouldHaveEmptyStatusHistory()
    {
        // Act
        var animal = CreateTestAnimal();

        // Assert
        animal.StatusHistory.Should().BeEmpty();
    }

    [Fact]
    public void Create_NewAnimal_ShouldPublishAnimalAdmittedEvent()
    {
        // Act
        var animal = CreateTestAnimal();

        // Assert
        animal.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AnimalAdmittedEvent>();
    }

    #endregion

    #region 2. Przejście Admitted -> Quarantine jest dozwolone

    [Fact]
    public void ChangeStatus_FromAdmittedToQuarantine_ShouldBeAllowed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ClearDomainEvents();

        // Act
        var result = animal.ChangeStatus(
            AnimalStatusTrigger.SkierowanieNaKwarantanne,
            changedBy: "jan.kowalski@schronisko.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Status.Should().Be(AnimalStatus.Quarantine);
    }

    [Fact]
    public void CanChangeStatus_FromAdmittedToQuarantine_ShouldReturnTrue()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        var canChange = animal.CanChangeStatus(AnimalStatusTrigger.SkierowanieNaKwarantanne);

        // Assert
        canChange.Should().BeTrue();
    }

    #endregion

    #region 3. Przejście Admitted -> Available jest dozwolone

    [Fact]
    public void ChangeStatus_FromAdmittedToAvailable_ShouldBeAllowed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ClearDomainEvents();

        // Act
        var result = animal.ChangeStatus(
            AnimalStatusTrigger.DopuszczenieDoAdopcji,
            changedBy: "jan.kowalski@schronisko.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Status.Should().Be(AnimalStatus.Available);
    }

    [Fact]
    public void CanChangeStatus_FromAdmittedToAvailable_ShouldReturnTrue()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        var canChange = animal.CanChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji);

        // Assert
        canChange.Should().BeTrue();
    }

    #endregion

    #region 4. Przejście Available -> Reserved jest dozwolone

    [Fact]
    public void ChangeStatus_FromAvailableToReserved_ShouldBeAllowed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ClearDomainEvents();

        // Act
        var result = animal.ChangeStatus(
            AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego,
            changedBy: "jan.kowalski@schronisko.pl");

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Status.Should().Be(AnimalStatus.Reserved);
    }

    [Fact]
    public void CanChangeStatus_FromAvailableToReserved_ShouldReturnTrue()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");

        // Act
        var canChange = animal.CanChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego);

        // Assert
        canChange.Should().BeTrue();
    }

    #endregion

    #region 5. Przejście Reserved -> Available (anulowanie) jest dozwolone

    [Fact]
    public void ChangeStatus_FromReservedToAvailable_WhenCancelled_ShouldBeAllowed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");
        animal.ClearDomainEvents();

        // Act
        var result = animal.ChangeStatus(
            AnimalStatusTrigger.AnulowanieZgloszenia,
            changedBy: "jan.kowalski@schronisko.pl",
            reason: "Adoptujący zrezygnował");

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Status.Should().Be(AnimalStatus.Available);
    }

    [Fact]
    public void ChangeStatus_FromReservedToAvailable_WhenResigned_ShouldBeAllowed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");
        animal.ClearDomainEvents();

        // Act
        var result = animal.ChangeStatus(
            AnimalStatusTrigger.Rezygnacja,
            changedBy: "jan.kowalski@schronisko.pl",
            reason: "Rezygnacja adoptującego");

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Status.Should().Be(AnimalStatus.Available);
    }

    [Fact]
    public void CanChangeStatus_FromReservedToAvailable_ShouldReturnTrue()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");

        // Act
        var canCancelResult = animal.CanChangeStatus(AnimalStatusTrigger.AnulowanieZgloszenia);
        var canResignResult = animal.CanChangeStatus(AnimalStatusTrigger.Rezygnacja);

        // Assert
        canCancelResult.Should().BeTrue();
        canResignResult.Should().BeTrue();
    }

    #endregion

    #region 6. Przejście Available -> Adopted jest NIEdozwolone

    [Fact]
    public void ChangeStatus_FromAvailableToAdopted_ShouldNotBeAllowed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ClearDomainEvents();

        // Act
        var result = animal.ChangeStatus(
            AnimalStatusTrigger.PodpisanieUmowy,
            changedBy: "jan.kowalski@schronisko.pl");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Nie można wykonać akcji");
        animal.Status.Should().Be(AnimalStatus.Available);
    }

    [Fact]
    public void CanChangeStatus_FromAvailableToAdopted_ShouldReturnFalse()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");

        // Act
        var canChange = animal.CanChangeStatus(AnimalStatusTrigger.PodpisanieUmowy);

        // Assert
        canChange.Should().BeFalse();
    }

    [Fact]
    public void ChangeStatus_ToAdopted_RequiresCorrectPath_ThroughReservedAndInAdoptionProcess()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act - przejście przez poprawną ścieżkę
        var step1 = animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        var step2 = animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");
        var step3 = animal.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "admin");
        var step4 = animal.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "admin");

        // Assert - wszystkie kroki zakończone sukcesem
        step1.IsSuccess.Should().BeTrue();
        step2.IsSuccess.Should().BeTrue();
        step3.IsSuccess.Should().BeTrue();
        step4.IsSuccess.Should().BeTrue();
        animal.Status.Should().Be(AnimalStatus.Adopted);
    }

    #endregion

    #region 7. Przejście Adopted -> Deceased jest NIEdozwolone

    [Fact]
    public void ChangeStatus_FromAdoptedToDeceased_ShouldNotBeAllowed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "admin");
        animal.ClearDomainEvents();

        // Act
        var result = animal.ChangeStatus(
            AnimalStatusTrigger.Zgon,
            changedBy: "jan.kowalski@schronisko.pl",
            reason: "Przyczyna zgonu");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Nie można wykonać akcji");
        animal.Status.Should().Be(AnimalStatus.Adopted);
    }

    [Fact]
    public void CanChangeStatus_FromAdoptedToDeceased_ShouldReturnFalse()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "admin");

        // Act
        var canChange = animal.CanChangeStatus(AnimalStatusTrigger.Zgon);

        // Assert
        canChange.Should().BeFalse();
    }

    [Fact]
    public void GetPermittedTriggers_WhenAdopted_ShouldReturnEmpty()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "admin");

        // Act
        var permittedTriggers = animal.GetPermittedTriggers();

        // Assert
        permittedTriggers.Should().BeEmpty();
    }

    #endregion

    #region 8. Każda zmiana statusu jest zapisywana w historii

    [Fact]
    public void ChangeStatus_ShouldAddEntryToStatusHistory()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        animal.ChangeStatus(
            AnimalStatusTrigger.SkierowanieNaKwarantanne,
            changedBy: "jan.kowalski@schronisko.pl",
            reason: "Standardowa procedura");

        // Assert
        animal.StatusHistory.Should().ContainSingle();
        var historyEntry = animal.StatusHistory.First();
        historyEntry.PreviousStatus.Should().Be(AnimalStatus.Admitted);
        historyEntry.NewStatus.Should().Be(AnimalStatus.Quarantine);
        historyEntry.Trigger.Should().Be(AnimalStatusTrigger.SkierowanieNaKwarantanne);
        historyEntry.ChangedBy.Should().Be("jan.kowalski@schronisko.pl");
        historyEntry.Reason.Should().Be("Standardowa procedura");
    }

    [Fact]
    public void ChangeStatus_MultipleChanges_ShouldTrackAllInHistory()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "user1");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "user2");
        animal.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "user3");
        animal.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "user4");

        // Assert
        animal.StatusHistory.Should().HaveCount(4);

        var historyList = animal.StatusHistory.ToList();

        historyList[0].PreviousStatus.Should().Be(AnimalStatus.Admitted);
        historyList[0].NewStatus.Should().Be(AnimalStatus.Available);

        historyList[1].PreviousStatus.Should().Be(AnimalStatus.Available);
        historyList[1].NewStatus.Should().Be(AnimalStatus.Reserved);

        historyList[2].PreviousStatus.Should().Be(AnimalStatus.Reserved);
        historyList[2].NewStatus.Should().Be(AnimalStatus.InAdoptionProcess);

        historyList[3].PreviousStatus.Should().Be(AnimalStatus.InAdoptionProcess);
        historyList[3].NewStatus.Should().Be(AnimalStatus.Adopted);
    }

    [Fact]
    public void ChangeStatus_FailedChange_ShouldNotAddEntryToHistory()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        var historyCountBeforeFailedChange = animal.StatusHistory.Count;

        // Act - próba niedozwolonej zmiany
        animal.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "admin");

        // Assert
        animal.StatusHistory.Should().HaveCount(historyCountBeforeFailedChange);
    }

    [Fact]
    public void ChangeStatus_HistoryEntry_ShouldHaveCorrectTimestamp()
    {
        // Arrange
        var animal = CreateTestAnimal();
        var beforeChange = DateTime.UtcNow;

        // Act
        animal.ChangeStatus(AnimalStatusTrigger.SkierowanieNaKwarantanne, "admin");
        var afterChange = DateTime.UtcNow;

        // Assert
        var historyEntry = animal.StatusHistory.First();
        historyEntry.ChangedAt.Should().BeOnOrAfter(beforeChange);
        historyEntry.ChangedAt.Should().BeOnOrBefore(afterChange);
    }

    #endregion

    #region 9. Event AnimalStatusChanged jest publikowany przy każdej zmianie

    [Fact]
    public void ChangeStatus_ShouldPublishAnimalStatusChangedEvent()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ClearDomainEvents();

        // Act
        animal.ChangeStatus(
            AnimalStatusTrigger.SkierowanieNaKwarantanne,
            changedBy: "jan.kowalski@schronisko.pl",
            reason: "Standardowa procedura");

        // Assert
        animal.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AnimalStatusChangedEvent>();

        var statusChangedEvent = (AnimalStatusChangedEvent)animal.DomainEvents.First();
        statusChangedEvent.AnimalId.Should().Be(animal.Id);
        statusChangedEvent.PreviousStatus.Should().Be(AnimalStatus.Admitted);
        statusChangedEvent.NewStatus.Should().Be(AnimalStatus.Quarantine);
        statusChangedEvent.Trigger.Should().Be(AnimalStatusTrigger.SkierowanieNaKwarantanne);
        statusChangedEvent.ChangedBy.Should().Be("jan.kowalski@schronisko.pl");
        statusChangedEvent.Reason.Should().Be("Standardowa procedura");
    }

    [Fact]
    public void ChangeStatus_MultipleChanges_ShouldPublishEventForEach()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ClearDomainEvents();

        // Act
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "user1");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "user2");

        // Assert
        animal.DomainEvents.Should().HaveCount(2);
        animal.DomainEvents.Should().AllBeOfType<AnimalStatusChangedEvent>();
    }

    [Fact]
    public void ChangeStatus_FailedChange_ShouldNotPublishEvent()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ClearDomainEvents();

        // Act - próba niedozwolonej zmiany
        animal.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "admin");

        // Assert
        animal.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeStatus_ToAdopted_ShouldPublishBothStatusChangedAndAdoptedEvents()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZatwierdznieZgloszenia, "admin");
        animal.ClearDomainEvents();

        // Act
        animal.ChangeStatus(AnimalStatusTrigger.PodpisanieUmowy, "adoptujacy@email.com");

        // Assert
        animal.DomainEvents.Should().HaveCount(2);
        animal.DomainEvents.Should().ContainSingle(e => e is AnimalStatusChangedEvent);
        animal.DomainEvents.Should().ContainSingle(e => e is AnimalAdoptedEvent);
    }

    [Fact]
    public void ChangeStatus_ToDeceased_ShouldPublishBothStatusChangedAndDeceasedEvents()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ClearDomainEvents();

        // Act
        animal.ChangeStatus(
            AnimalStatusTrigger.Zgon,
            changedBy: "weterynarz@schronisko.pl",
            reason: "Choroba nieuleczalna");

        // Assert
        animal.DomainEvents.Should().HaveCount(2);
        animal.DomainEvents.Should().ContainSingle(e => e is AnimalStatusChangedEvent);
        animal.DomainEvents.Should().ContainSingle(e => e is AnimalDeceasedEvent);

        var deceasedEvent = animal.DomainEvents.OfType<AnimalDeceasedEvent>().First();
        deceasedEvent.Reason.Should().Be("Choroba nieuleczalna");
    }

    #endregion

    #region Additional Tests - GetPermittedTriggers

    [Fact]
    public void GetPermittedTriggers_WhenAdmitted_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        var permittedTriggers = animal.GetPermittedTriggers().ToList();

        // Assert
        permittedTriggers.Should().Contain(AnimalStatusTrigger.SkierowanieNaKwarantanne);
        permittedTriggers.Should().Contain(AnimalStatusTrigger.DopuszczenieDoAdopcji);
        permittedTriggers.Should().Contain(AnimalStatusTrigger.Zgon);
    }

    [Fact]
    public void GetPermittedTriggers_WhenAvailable_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");

        // Act
        var permittedTriggers = animal.GetPermittedTriggers().ToList();

        // Assert
        permittedTriggers.Should().Contain(AnimalStatusTrigger.Zachorowanie);
        permittedTriggers.Should().Contain(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego);
        permittedTriggers.Should().Contain(AnimalStatusTrigger.Zgon);
        permittedTriggers.Should().NotContain(AnimalStatusTrigger.PodpisanieUmowy);
    }

    [Fact]
    public void GetPermittedTriggers_WhenDeceased_ShouldReturnEmpty()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.Zgon, "admin", "Przyczyna");

        // Act
        var permittedTriggers = animal.GetPermittedTriggers();

        // Assert
        permittedTriggers.Should().BeEmpty();
    }

    [Fact]
    public void GetPermittedTriggers_WhenReserved_ShouldReturnCorrectTriggers()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ChangeStatus(AnimalStatusTrigger.DopuszczenieDoAdopcji, "admin");
        animal.ChangeStatus(AnimalStatusTrigger.ZlozenieZgloszeniaAdopcyjnego, "admin");

        // Act
        var permittedTriggers = animal.GetPermittedTriggers().ToList();

        // Assert
        permittedTriggers.Should().Contain(AnimalStatusTrigger.AnulowanieZgloszenia);
        permittedTriggers.Should().Contain(AnimalStatusTrigger.Rezygnacja);
        permittedTriggers.Should().Contain(AnimalStatusTrigger.ZatwierdznieZgloszenia);
        permittedTriggers.Should().Contain(AnimalStatusTrigger.Zgon);
    }

    #endregion
}
