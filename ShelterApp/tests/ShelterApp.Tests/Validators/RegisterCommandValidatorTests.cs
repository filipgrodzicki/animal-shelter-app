using FluentAssertions;
using FluentValidation.TestHelper;
using ShelterApp.Api.Features.Auth.Commands;
using Xunit;

namespace ShelterApp.Tests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    #region Email Validation

    [Fact]
    public void Validate_EmptyEmail_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email jest wymagany");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "invalid-email",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Nieprawidłowy format adresu email");
    }

    [Fact]
    public void Validate_ValidEmail_ShouldNotHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan.kowalski@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Password Validation

    [Fact]
    public void Validate_EmptyPassword_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "",
            ConfirmPassword: "",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Hasło jest wymagane");
    }

    [Fact]
    public void Validate_PasswordTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "Short1",
            ConfirmPassword: "Short1",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Hasło musi mieć minimum 8 znaków");
    }

    [Fact]
    public void Validate_PasswordWithoutUppercase_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "password123",
            ConfirmPassword: "password123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Hasło musi zawierać przynajmniej jedną wielką literę");
    }

    [Fact]
    public void Validate_PasswordWithoutLowercase_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "PASSWORD123",
            ConfirmPassword: "PASSWORD123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Hasło musi zawierać przynajmniej jedną małą literę");
    }

    [Fact]
    public void Validate_PasswordWithoutDigit_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "PasswordOnly",
            ConfirmPassword: "PasswordOnly",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Hasło musi zawierać przynajmniej jedną cyfrę");
    }

    [Fact]
    public void Validate_ValidPassword_ShouldNotHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region ConfirmPassword Validation

    [Fact]
    public void Validate_EmptyConfirmPassword_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Potwierdzenie hasła jest wymagane");
    }

    [Fact]
    public void Validate_PasswordsDoNotMatch_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "DifferentPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Hasła muszą być identyczne");
    }

    [Fact]
    public void Validate_PasswordsMatch_ShouldNotHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    #endregion

    #region FirstName Validation

    [Fact]
    public void Validate_EmptyFirstName_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("Imię jest wymagane");
    }

    [Fact]
    public void Validate_FirstNameTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: new string('a', 101),
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("Imię może mieć maksymalnie 100 znaków");
    }

    #endregion

    #region LastName Validation

    [Fact]
    public void Validate_EmptyLastName_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Nazwisko jest wymagane");
    }

    [Fact]
    public void Validate_LastNameTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: new string('a', 101),
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Nazwisko może mieć maksymalnie 100 znaków");
    }

    #endregion

    #region PhoneNumber Validation

    [Fact]
    public void Validate_InvalidPhoneNumber_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: "invalid");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Nieprawidłowy format numeru telefonu");
    }

    [Fact]
    public void Validate_ValidPhoneNumber_ShouldNotHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: "+48123456789");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_NullPhoneNumber_ShouldNotHaveError()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    #endregion

    #region Full Command Validation

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "jan.kowalski@test.pl",
            Password: "ValidPassword123",
            ConfirmPassword: "ValidPassword123",
            FirstName: "Jan",
            LastName: "Kowalski",
            PhoneNumber: "+48123456789");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "",
            Password: "",
            ConfirmPassword: "",
            FirstName: "",
            LastName: "",
            PhoneNumber: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
    }

    #endregion
}
