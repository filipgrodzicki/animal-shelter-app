using FluentAssertions;
using ShelterApp.Domain.Animals;
using ShelterApp.Domain.Animals.Entities;
using ShelterApp.Domain.Animals.Enums;
using ShelterApp.Domain.Animals.Events;
using Xunit;

namespace ShelterApp.Tests.Animals;

public class AnimalEntityTests
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
            description: "Przyjazny pies",
            experienceLevel: ExperienceLevel.Advanced,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.Yes,
            spaceRequirement: SpaceRequirement.House,
            careTime: CareTime.OneToThreeHours
        );
    }

    #region Create Tests

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllPropertiesCorrectly()
    {
        // Act
        var animal = Animal.Create(
            registrationNumber: "SCH/DOG/2024/001",
            species: Species.Dog,
            breed: "Golden Retriever",
            name: "Max",
            gender: Gender.Male,
            size: Size.Large,
            color: "Złoty",
            admissionCircumstances: "Oddany przez właściciela",
            ageInMonths: 36,
            chipNumber: "123456789012345",
            description: "Spokojny pies",
            experienceLevel: ExperienceLevel.Basic,
            childrenCompatibility: ChildrenCompatibility.Yes,
            animalCompatibility: AnimalCompatibility.No,
            spaceRequirement: SpaceRequirement.HouseWithGarden,
            careTime: CareTime.MoreThan3Hours,
            surrenderedByFirstName: "Jan",
            surrenderedByLastName: "Kowalski",
            surrenderedByPhone: "123456789",
            distinguishingMarks: "Biała łata na piersi",
            specialNeeds: "Wymaga specjalnej diety"
        );

        // Assert
        animal.RegistrationNumber.Should().Be("SCH/DOG/2024/001");
        animal.Species.Should().Be(Species.Dog);
        animal.Breed.Should().Be("Golden Retriever");
        animal.Name.Should().Be("Max");
        animal.Gender.Should().Be(Gender.Male);
        animal.Size.Should().Be(Size.Large);
        animal.Color.Should().Be("Złoty");
        animal.AdmissionCircumstances.Should().Be("Oddany przez właściciela");
        animal.AgeInMonths.Should().Be(36);
        animal.ChipNumber.Should().Be("123456789012345");
        animal.Description.Should().Be("Spokojny pies");
        animal.ExperienceLevel.Should().Be(ExperienceLevel.Basic);
        animal.ChildrenCompatibility.Should().Be(ChildrenCompatibility.Yes);
        animal.AnimalCompatibility.Should().Be(AnimalCompatibility.No);
        animal.SpaceRequirement.Should().Be(SpaceRequirement.HouseWithGarden);
        animal.CareTime.Should().Be(CareTime.MoreThan3Hours);
        animal.SpecialNeeds.Should().Be("Wymaga specjalnej diety");
    }

    [Fact]
    public void Create_WithMinimalParameters_ShouldUseDefaults()
    {
        // Act
        var animal = Animal.Create(
            registrationNumber: "SCH/CAT/2024/001",
            species: Species.Cat,
            breed: "Dachowiec",
            name: "Mruczek",
            gender: Gender.Male,
            size: Size.Small,
            color: "Bury",
            admissionCircumstances: "Znaleziony"
        );

        // Assert
        animal.AgeInMonths.Should().BeNull();
        animal.ChipNumber.Should().BeNull();
        animal.Description.Should().BeNull();
        animal.ExperienceLevel.Should().Be(ExperienceLevel.None);
        animal.ChildrenCompatibility.Should().Be(ChildrenCompatibility.Yes);
        animal.AnimalCompatibility.Should().Be(AnimalCompatibility.Yes);
        animal.SpaceRequirement.Should().Be(SpaceRequirement.Apartment);
        animal.CareTime.Should().Be(CareTime.OneToThreeHours);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var animal1 = CreateTestAnimal();
        var animal2 = CreateTestAnimal();

        // Assert
        animal1.Id.Should().NotBe(Guid.Empty);
        animal2.Id.Should().NotBe(Guid.Empty);
        animal1.Id.Should().NotBe(animal2.Id);
    }

    #endregion

    #region Photo Tests

    [Fact]
    public void AddPhoto_FirstPhoto_ShouldSetAsMain()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        var result = animal.AddPhoto(
            fileName: "photo1.jpg",
            filePath: "/photos/photo1.jpg",
            contentType: "image/jpeg",
            fileSize: 1024,
            isMain: false,
            description: "Zdjęcie główne"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Photos.Should().HaveCount(1);
        animal.Photos.First().IsMain.Should().BeTrue();
    }

    [Fact]
    public void AddPhoto_WithIsMainTrue_ShouldUnsetPreviousMainPhoto()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.AddPhoto("photo1.jpg", "/photos/photo1.jpg", "image/jpeg", 1024, true);

        // Act
        var result = animal.AddPhoto("photo2.jpg", "/photos/photo2.jpg", "image/jpeg", 2048, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Photos.Should().HaveCount(2);
        animal.Photos.Count(p => p.IsMain).Should().Be(1);
        animal.Photos.Last().IsMain.Should().BeTrue();
    }

    [Fact]
    public void AddPhoto_ShouldPublishEvent()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ClearDomainEvents();

        // Act
        animal.AddPhoto("photo1.jpg", "/photos/photo1.jpg", "image/jpeg", 1024);

        // Assert
        animal.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AnimalPhotoAddedEvent>();
    }

    [Fact]
    public void SetMainPhoto_ExistingPhoto_ShouldSucceed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        var photo1Result = animal.AddPhoto("photo1.jpg", "/photos/photo1.jpg", "image/jpeg", 1024);
        var photo2Result = animal.AddPhoto("photo2.jpg", "/photos/photo2.jpg", "image/jpeg", 2048);
        var photo1 = photo1Result.Value;
        var photo2 = photo2Result.Value;

        // Act
        var result = animal.SetMainPhoto(photo2.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Photos.First(p => p.Id == photo1.Id).IsMain.Should().BeFalse();
        animal.Photos.First(p => p.Id == photo2.Id).IsMain.Should().BeTrue();
    }

    [Fact]
    public void SetMainPhoto_NonExistingPhoto_ShouldFail()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.AddPhoto("photo1.jpg", "/photos/photo1.jpg", "image/jpeg", 1024);

        // Act
        var result = animal.SetMainPhoto(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void RemovePhoto_ExistingPhoto_ShouldSucceed()
    {
        // Arrange
        var animal = CreateTestAnimal();
        var photoResult = animal.AddPhoto("photo1.jpg", "/photos/photo1.jpg", "image/jpeg", 1024);

        // Act
        var result = animal.RemovePhoto(photoResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Photos.Should().BeEmpty();
    }

    [Fact]
    public void RemovePhoto_MainPhoto_ShouldSetNextPhotoAsMain()
    {
        // Arrange
        var animal = CreateTestAnimal();
        var photo1Result = animal.AddPhoto("photo1.jpg", "/photos/photo1.jpg", "image/jpeg", 1024, true);
        animal.AddPhoto("photo2.jpg", "/photos/photo2.jpg", "image/jpeg", 2048, false);

        // Act
        var result = animal.RemovePhoto(photo1Result.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.Photos.Should().HaveCount(1);
        animal.Photos.First().IsMain.Should().BeTrue();
    }

    [Fact]
    public void RemovePhoto_NonExistingPhoto_ShouldFail()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        var result = animal.RemovePhoto(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Medical Record Tests

    [Fact]
    public void AddMedicalRecord_ShouldAddRecordSuccessfully()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        var result = animal.AddMedicalRecord(
            type: MedicalRecordType.Vaccination,
            title: "Szczepienie przeciwko wściekliźnie",
            description: "Roczne szczepienie",
            veterinarianName: "dr Jan Nowak",
            diagnosis: null,
            treatment: "Szczepionka Rabisin",
            medications: null,
            nextVisitDate: DateTime.UtcNow.AddYears(1),
            notes: "Pies dobrze zniósł szczepienie",
            cost: 120.00m,
            enteredBy: "admin@schronisko.pl"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        animal.MedicalRecords.Should().HaveCount(1);
        var record = result.Value;
        record.Type.Should().Be(MedicalRecordType.Vaccination);
        record.Title.Should().Be("Szczepienie przeciwko wściekliźnie");
        record.VeterinarianName.Should().Be("dr Jan Nowak");
    }

    [Fact]
    public void AddMedicalRecord_ShouldPublishEvent()
    {
        // Arrange
        var animal = CreateTestAnimal();
        animal.ClearDomainEvents();

        // Act
        animal.AddMedicalRecord(
            type: MedicalRecordType.Examination,
            title: "Badanie kontrolne",
            description: "Rutynowa kontrola",
            veterinarianName: "dr Ewa Kowalska"
        );

        // Assert
        animal.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MedicalRecordAddedEvent>();
    }

    [Fact]
    public void AddMedicalRecord_MultipleTimes_ShouldAddAllRecords()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        animal.AddMedicalRecord(MedicalRecordType.Vaccination, "Szczepienie 1", "Opis", "Weterynarz");
        animal.AddMedicalRecord(MedicalRecordType.Treatment, "Leczenie", "Opis", "Weterynarz");
        animal.AddMedicalRecord(MedicalRecordType.Surgery, "Operacja", "Opis", "Weterynarz");

        // Assert
        animal.MedicalRecords.Should().HaveCount(3);
    }

    #endregion

    #region Update Methods Tests

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateSpecifiedFields()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        animal.UpdateBasicInfo(
            name: "Rex",
            breed: "Owczarek niemiecki",
            ageInMonths: 48,
            color: "Czarno-podpalany",
            description: "Nowy opis"
        );

        // Assert
        animal.Name.Should().Be("Rex");
        animal.Breed.Should().Be("Owczarek niemiecki");
        animal.AgeInMonths.Should().Be(48);
        animal.Color.Should().Be("Czarno-podpalany");
        animal.Description.Should().Be("Nowy opis");
    }

    [Fact]
    public void UpdateBasicInfo_WithNullValues_ShouldNotChangeExistingValues()
    {
        // Arrange
        var animal = CreateTestAnimal();
        var originalName = animal.Name;
        var originalBreed = animal.Breed;

        // Act
        animal.UpdateBasicInfo(color: "Biały");

        // Assert
        animal.Name.Should().Be(originalName);
        animal.Breed.Should().Be(originalBreed);
        animal.Color.Should().Be("Biały");
    }

    [Fact]
    public void UpdateAdoptionInfo_ShouldUpdateSpecifiedFields()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        animal.UpdateAdoptionInfo(
            experienceLevel: ExperienceLevel.None,
            childrenCompatibility: ChildrenCompatibility.No,
            animalCompatibility: AnimalCompatibility.Yes,
            spaceRequirement: SpaceRequirement.Apartment,
            careTime: CareTime.LessThan1Hour
        );

        // Assert
        animal.ExperienceLevel.Should().Be(ExperienceLevel.None);
        animal.ChildrenCompatibility.Should().Be(ChildrenCompatibility.No);
        animal.AnimalCompatibility.Should().Be(AnimalCompatibility.Yes);
        animal.SpaceRequirement.Should().Be(SpaceRequirement.Apartment);
        animal.CareTime.Should().Be(CareTime.LessThan1Hour);
    }

    [Fact]
    public void UpdateAdoptionInfo_WithNullValues_ShouldNotChangeExistingValues()
    {
        // Arrange
        var animal = CreateTestAnimal();
        var originalExperienceLevel = animal.ExperienceLevel;

        // Act
        animal.UpdateAdoptionInfo(childrenCompatibility: ChildrenCompatibility.No);

        // Assert
        animal.ExperienceLevel.Should().Be(originalExperienceLevel);
        animal.ChildrenCompatibility.Should().Be(ChildrenCompatibility.No);
    }

    [Fact]
    public void UpdateChipNumber_ShouldUpdateChipNumber()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        animal.UpdateChipNumber("999888777666555");

        // Assert
        animal.ChipNumber.Should().Be("999888777666555");
    }

    [Fact]
    public void UpdateSpecialNeeds_ShouldUpdateSpecialNeeds()
    {
        // Arrange
        var animal = CreateTestAnimal();

        // Act
        animal.UpdateSpecialNeeds("Wymaga codziennych leków");

        // Assert
        animal.SpecialNeeds.Should().Be("Wymaga codziennych leków");
    }

    [Fact]
    public void UpdateSpecialNeeds_WithNull_ShouldClearSpecialNeeds()
    {
        // Arrange
        var animal = Animal.Create(
            registrationNumber: "SCH/DOG/2024/001",
            species: Species.Dog,
            breed: "Labrador",
            name: "Burek",
            gender: Gender.Male,
            size: Size.Large,
            color: "Czarny",
            admissionCircumstances: "Znaleziony",
            specialNeeds: "Alergia pokarmowa"
        );

        // Act
        animal.UpdateSpecialNeeds(null);

        // Assert
        animal.SpecialNeeds.Should().BeNull();
    }

    #endregion
}
