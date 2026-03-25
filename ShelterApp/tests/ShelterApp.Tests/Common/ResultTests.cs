using FluentAssertions;
using ShelterApp.Domain.Common;
using Xunit;

namespace ShelterApp.Tests.Common;

public class ResultTests
{
    #region Result (non-generic)

    [Fact]
    public void Success_ShouldReturnSuccessResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldReturnFailureResult()
    {
        // Arrange
        var error = Error.Validation("Test error");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    #endregion

    #region Result<T>

    [Fact]
    public void Success_WithValue_ShouldReturnSuccessResult()
    {
        // Act
        var result = Result.Success("test value");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test value");
    }

    [Fact]
    public void Failure_Generic_ShouldReturnFailureResult()
    {
        // Arrange
        var error = Error.NotFound("Entity", 123);

        // Act
        var result = Result.Failure<string>(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_OnFailure_ShouldThrowException()
    {
        // Arrange
        var result = Result.Failure<string>(Error.Validation("Error"));

        // Act & Assert
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void Create_WithNonNullValue_ShouldReturnSuccess()
    {
        // Act
        var result = Result.Create("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void Create_WithNullValue_ShouldReturnFailure()
    {
        // Act
        var result = Result.Create<string>(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    #endregion

    #region Map

    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        // Arrange
        var result = Result.Success(5);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [Fact]
    public void Map_OnFailure_ShouldPropagateError()
    {
        // Arrange
        var error = Error.Validation("Error");
        var result = Result.Failure<int>(error);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(error);
    }

    #endregion

    #region Tap

    [Fact]
    public void Tap_OnSuccess_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Success("test");
        var executed = false;

        // Act
        result.Tap(v => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void Tap_OnFailure_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result.Failure<string>(Error.Validation("Error"));
        var executed = false;

        // Act
        result.Tap(v => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessResult()
    {
        // Act
        Result<string> result = "test";

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void ImplicitConversion_FromNull_ShouldCreateFailureResult()
    {
        // Act
        Result<string> result = (string?)null;

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion
}

public class ErrorTests
{
    [Fact]
    public void None_ShouldHaveEmptyCodeAndMessage()
    {
        // Assert
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void NullValue_ShouldHaveCorrectCodeAndMessage()
    {
        // Assert
        Error.NullValue.Code.Should().Be("Error.NullValue");
        Error.NullValue.Message.Should().Contain("null");
    }

    [Fact]
    public void NotFound_ShouldCreateCorrectError()
    {
        // Act
        var error = Error.NotFound("User", 123);

        // Assert
        error.Code.Should().Be("User.NotFound");
        error.Message.Should().Contain("User");
        error.Message.Should().Contain("123");
    }

    [Fact]
    public void Validation_ShouldCreateCorrectError()
    {
        // Act
        var error = Error.Validation("Invalid data");

        // Assert
        error.Code.Should().Be("Validation.Error");
        error.Message.Should().Be("Invalid data");
    }

    [Fact]
    public void Validation_WithCode_ShouldCreateCorrectError()
    {
        // Act
        var error = Error.Validation("Custom.Code", "Custom message");

        // Assert
        error.Code.Should().Be("Custom.Code");
        error.Message.Should().Be("Custom message");
    }

    [Fact]
    public void Conflict_ShouldCreateCorrectError()
    {
        // Act
        var error = Error.Conflict("Resource already exists");

        // Assert
        error.Code.Should().Be("Conflict.Error");
        error.Message.Should().Be("Resource already exists");
    }

    [Fact]
    public void Unauthorized_ShouldCreateCorrectError()
    {
        // Act
        var error = Error.Unauthorized();

        // Assert
        error.Code.Should().Be("Unauthorized.Error");
        error.Message.Should().Be("Unauthorized access");
    }

    [Fact]
    public void Forbidden_ShouldCreateCorrectError()
    {
        // Act
        var error = Error.Forbidden();

        // Assert
        error.Code.Should().Be("Forbidden.Error");
        error.Message.Should().Be("Access forbidden");
    }
}

public class ResultExtensionsTests
{
    [Fact]
    public void ToResult_WithNonNullValue_ShouldReturnSuccess()
    {
        // Arrange
        string? value = "test";

        // Act
        var result = value.ToResult(Error.NullValue);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void ToResult_WithNullValue_ShouldReturnFailure()
    {
        // Arrange
        string? value = null;
        var error = Error.Validation("Value is required");

        // Act
        var result = value.ToResult(error);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Ensure_WithPassingPredicate_ShouldReturnOriginalResult()
    {
        // Arrange
        var result = Result.Success(10);

        // Act
        var ensured = result.Ensure(x => x > 5, Error.Validation("Value must be > 5"));

        // Assert
        ensured.IsSuccess.Should().BeTrue();
        ensured.Value.Should().Be(10);
    }

    [Fact]
    public void Ensure_WithFailingPredicate_ShouldReturnFailure()
    {
        // Arrange
        var result = Result.Success(3);
        var error = Error.Validation("Value must be > 5");

        // Act
        var ensured = result.Ensure(x => x > 5, error);

        // Assert
        ensured.IsFailure.Should().BeTrue();
        ensured.Error.Should().Be(error);
    }

    [Fact]
    public void Ensure_OnFailure_ShouldReturnOriginalFailure()
    {
        // Arrange
        var originalError = Error.NotFound("Entity", 1);
        var result = Result.Failure<int>(originalError);

        // Act
        var ensured = result.Ensure(x => x > 5, Error.Validation("Different error"));

        // Assert
        ensured.IsFailure.Should().BeTrue();
        ensured.Error.Should().Be(originalError);
    }

    [Fact]
    public void Bind_OnSuccess_ShouldChainOperations()
    {
        // Arrange
        var result = Result.Success(5);

        // Act
        var bound = result.Bind(x => Result.Success(x.ToString()));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("5");
    }

    [Fact]
    public void Bind_OnFailure_ShouldPropagateError()
    {
        // Arrange
        var error = Error.Validation("Error");
        var result = Result.Failure<int>(error);

        // Act
        var bound = result.Bind(x => Result.Success(x.ToString()));

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_ChainedFailure_ShouldPropagateError()
    {
        // Arrange
        var result = Result.Success(5);
        var error = Error.Validation("Chain error");

        // Act
        var bound = result.Bind<int, string>(x => Result.Failure<string>(error));

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public void Match_OnSuccess_ShouldCallSuccessHandler()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var matched = result.Match(
            onSuccess: () => "success",
            onFailure: e => "failure");

        // Assert
        matched.Should().Be("success");
    }

    [Fact]
    public void Match_OnFailure_ShouldCallFailureHandler()
    {
        // Arrange
        var error = Error.Validation("Error");
        var result = Result.Failure(error);

        // Act
        var matched = result.Match(
            onSuccess: () => "success",
            onFailure: e => e.Message);

        // Assert
        matched.Should().Be("Error");
    }

    [Fact]
    public void Match_Generic_OnSuccess_ShouldCallSuccessHandlerWithValue()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var matched = result.Match(
            onSuccess: v => v * 2,
            onFailure: e => 0);

        // Assert
        matched.Should().Be(84);
    }

    [Fact]
    public void Match_Generic_OnFailure_ShouldCallFailureHandler()
    {
        // Arrange
        var result = Result.Failure<int>(Error.Validation("Error"));

        // Act
        var matched = result.Match(
            onSuccess: v => v * 2,
            onFailure: e => -1);

        // Assert
        matched.Should().Be(-1);
    }
}
