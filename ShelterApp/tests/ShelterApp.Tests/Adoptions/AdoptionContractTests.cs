using FluentAssertions;
using ShelterApp.Domain.Adoptions;
using Xunit;

namespace ShelterApp.Tests.Adoptions;

public class AdoptionContractTests
{
    private static AdoptionContract CreateTestContract(
        Guid? applicationId = null,
        string contractNumber = "UMO/2024/001",
        decimal? adoptionFee = 150.00m,
        string? additionalTerms = null,
        string? notes = null)
    {
        return AdoptionContract.Create(
            adoptionApplicationId: applicationId ?? Guid.NewGuid(),
            contractNumber: contractNumber,
            adoptionFee: adoptionFee,
            additionalTerms: additionalTerms,
            notes: notes
        );
    }

    #region Create Tests

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        // Act
        var contract = AdoptionContract.Create(
            adoptionApplicationId: applicationId,
            contractNumber: "UMO/2024/001",
            adoptionFee: 200.00m,
            additionalTerms: "Zwierzę musi być sterylizowane",
            notes: "Notatki"
        );

        // Assert
        contract.AdoptionApplicationId.Should().Be(applicationId);
        contract.ContractNumber.Should().Be("UMO/2024/001");
        contract.AdoptionFee.Should().Be(200.00m);
        contract.AdditionalTerms.Should().Be("Zwierzę musi być sterylizowane");
        contract.Notes.Should().Be("Notatki");
        contract.FeePaid.Should().BeFalse();
        contract.IsSigned.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var contract1 = CreateTestContract();
        var contract2 = CreateTestContract();

        // Assert
        contract1.Id.Should().NotBe(Guid.Empty);
        contract2.Id.Should().NotBe(Guid.Empty);
        contract1.Id.Should().NotBe(contract2.Id);
    }

    [Fact]
    public void Create_ShouldSetGeneratedDateToNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var contract = CreateTestContract();
        var after = DateTime.UtcNow;

        // Assert
        contract.GeneratedDate.Should().BeOnOrAfter(before);
        contract.GeneratedDate.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithNoFee_ShouldHaveNullFee()
    {
        // Act
        var contract = AdoptionContract.Create(
            adoptionApplicationId: Guid.NewGuid(),
            contractNumber: "UMO/2024/001"
        );

        // Assert
        contract.AdoptionFee.Should().BeNull();
    }

    #endregion

    #region SetFilePath Tests

    [Fact]
    public void SetFilePath_ShouldUpdateFilePath()
    {
        // Arrange
        var contract = CreateTestContract();

        // Act
        contract.SetFilePath("/contracts/UMO-2024-001.pdf");

        // Assert
        contract.FilePath.Should().Be("/contracts/UMO-2024-001.pdf");
    }

    [Fact]
    public void SetFilePath_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var contract = CreateTestContract();
        contract.SetFilePath("/initial/path.pdf"); // Ensure UpdatedAt is set
        var initialUpdatedAt = contract.UpdatedAt!.Value;

        // Act
        Thread.Sleep(10); // small delay to ensure time difference
        contract.SetFilePath("/contracts/UMO-2024-001.pdf");

        // Assert
        contract.UpdatedAt.Should().NotBeNull();
        contract.UpdatedAt!.Value.Should().BeOnOrAfter(initialUpdatedAt);
    }

    #endregion

    #region Sign Tests

    [Fact]
    public void Sign_UnsignedContract_ShouldSucceed()
    {
        // Arrange
        var contract = CreateTestContract();

        // Act
        var result = contract.Sign(
            shelterSignatory: "Jan Kowalski",
            adopterSignatory: "Anna Nowak"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        contract.IsSigned.Should().BeTrue();
        contract.ShelterSignatory.Should().Be("Jan Kowalski");
        contract.AdopterSignatory.Should().Be("Anna Nowak");
        contract.SignedDate.Should().NotBeNull();
    }

    [Fact]
    public void Sign_AlreadySignedContract_ShouldFail()
    {
        // Arrange
        var contract = CreateTestContract();
        contract.Sign("Jan Kowalski", "Anna Nowak");

        // Act
        var result = contract.Sign("Piotr Zielinski", "Maria Kwiatkowska");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("już podpisana");
    }

    [Fact]
    public void Sign_ShouldSetSignedDateToNow()
    {
        // Arrange
        var contract = CreateTestContract();
        var before = DateTime.UtcNow;

        // Act
        contract.Sign("Jan Kowalski", "Anna Nowak");
        var after = DateTime.UtcNow;

        // Assert
        contract.SignedDate.Should().NotBeNull();
        contract.SignedDate.Should().BeOnOrAfter(before);
        contract.SignedDate.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void IsSigned_UnsignedContract_ShouldReturnFalse()
    {
        // Arrange
        var contract = CreateTestContract();

        // Act & Assert
        contract.IsSigned.Should().BeFalse();
    }

    [Fact]
    public void IsSigned_SignedContract_ShouldReturnTrue()
    {
        // Arrange
        var contract = CreateTestContract();
        contract.Sign("Jan Kowalski", "Anna Nowak");

        // Act & Assert
        contract.IsSigned.Should().BeTrue();
    }

    #endregion

    #region RecordFeePayment Tests

    [Fact]
    public void RecordFeePayment_WithFee_ShouldSucceed()
    {
        // Arrange
        var contract = CreateTestContract(adoptionFee: 150.00m);

        // Act
        var result = contract.RecordFeePayment();

        // Assert
        result.IsSuccess.Should().BeTrue();
        contract.FeePaid.Should().BeTrue();
        contract.FeePaidDate.Should().NotBeNull();
    }

    [Fact]
    public void RecordFeePayment_WithNoFee_ShouldFail()
    {
        // Arrange
        var contract = CreateTestContract(adoptionFee: null);

        // Act
        var result = contract.RecordFeePayment();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Brak określonej opłaty");
    }

    [Fact]
    public void RecordFeePayment_WithZeroFee_ShouldFail()
    {
        // Arrange
        var contract = CreateTestContract(adoptionFee: 0m);

        // Act
        var result = contract.RecordFeePayment();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Brak określonej opłaty");
    }

    [Fact]
    public void RecordFeePayment_AlreadyPaid_ShouldFail()
    {
        // Arrange
        var contract = CreateTestContract(adoptionFee: 150.00m);
        contract.RecordFeePayment();

        // Act
        var result = contract.RecordFeePayment();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("już uiszczona");
    }

    [Fact]
    public void RecordFeePayment_ShouldSetFeePaidDateToNow()
    {
        // Arrange
        var contract = CreateTestContract(adoptionFee: 150.00m);
        var before = DateTime.UtcNow;

        // Act
        contract.RecordFeePayment();
        var after = DateTime.UtcNow;

        // Assert
        contract.FeePaidDate.Should().NotBeNull();
        contract.FeePaidDate.Should().BeOnOrAfter(before);
        contract.FeePaidDate.Should().BeOnOrBefore(after);
    }

    #endregion

    #region UpdateTerms Tests

    [Fact]
    public void UpdateTerms_ShouldUpdateAdditionalTerms()
    {
        // Arrange
        var contract = CreateTestContract();

        // Act
        contract.UpdateTerms(
            additionalTerms: "Nowe warunki umowy",
            notes: "Nowe notatki"
        );

        // Assert
        contract.AdditionalTerms.Should().Be("Nowe warunki umowy");
        contract.Notes.Should().Be("Nowe notatki");
    }

    [Fact]
    public void UpdateTerms_WithNullNotes_ShouldNotChangeExistingNotes()
    {
        // Arrange
        var contract = CreateTestContract(notes: "Oryginalne notatki");

        // Act
        contract.UpdateTerms(additionalTerms: "Nowe warunki");

        // Assert
        contract.AdditionalTerms.Should().Be("Nowe warunki");
        contract.Notes.Should().Be("Oryginalne notatki");
    }

    [Fact]
    public void UpdateTerms_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var contract = CreateTestContract();
        contract.UpdateTerms("Początkowe warunki"); // Ensure UpdatedAt is set
        var initialUpdatedAt = contract.UpdatedAt!.Value;

        // Act
        Thread.Sleep(10);
        contract.UpdateTerms("Nowe warunki");

        // Assert
        contract.UpdatedAt.Should().NotBeNull();
        contract.UpdatedAt!.Value.Should().BeOnOrAfter(initialUpdatedAt);
    }

    #endregion
}
