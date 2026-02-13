// Copyright (c) Momentum .NET. All rights reserved.

using FluentValidation.Results;

namespace Momentum.Extensions.Tests;

public class ResultTests
{
    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        var expected = "hello";

        // Act
        Result<string> result = expected;

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBe(expected);
    }

    [Fact]
    public void ImplicitConversion_FromValidationFailures_ShouldCreateFailureResult()
    {
        // Arrange
        var errors = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };

        // Act
        Result<string> result = errors;

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBe(errors);
    }

    [Fact]
    public void Match_WhenSuccess_ShouldInvokeSuccessHandler()
    {
        // Arrange
        Result<int> result = 42;

        // Act
        var output = result.Match(
            value => $"ok:{value}",
            errors => $"fail:{errors.Count}");

        // Assert
        output.ShouldBe("ok:42");
    }

    [Fact]
    public void Match_WhenFailure_ShouldInvokeErrorHandler()
    {
        // Arrange
        Result<int> result = new List<ValidationFailure> { new("Field", "Error") };

        // Act
        var output = result.Match(
            value => $"ok:{value}",
            errors => $"fail:{errors.Count}");

        // Assert
        output.ShouldBe("fail:1");
    }

    [Fact]
    public void Switch_WhenSuccess_ShouldInvokeSuccessAction()
    {
        // Arrange
        Result<string> result = "test";
        string? captured = null;

        // Act
        result.Switch(
            value => captured = value,
            _ => captured = "error");

        // Assert
        captured.ShouldBe("test");
    }

    [Fact]
    public void Switch_WhenFailure_ShouldInvokeErrorAction()
    {
        // Arrange
        Result<string> result = new List<ValidationFailure> { new("F", "E") };
        List<ValidationFailure>? captured = null;

        // Act
        result.Switch(
            _ => { },
            errors => captured = errors);

        // Assert
        captured.ShouldNotBeNull();
        captured.Count.ShouldBe(1);
    }

    [Fact]
    public void ImplicitConversion_FromEmptyValidationFailures_ShouldCreateFailureResult()
    {
        // Arrange & Act
        Result<string> result = new List<ValidationFailure>();

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeEmpty();
    }
}
