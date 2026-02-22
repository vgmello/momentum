// Copyright (c) Momentum .NET. All rights reserved.

using FluentValidation.Results;

namespace Momentum.Extensions.Tests;

public class ResultOneOfMemberTests
{
    [Fact]
    public void TryPickT0_WhenSuccess_ShouldReturnTrueAndValue()
    {
        // Arrange
        Result<int> result = 42;

        // Act
        var picked = result.TryPickT0(out var value, out _);

        // Assert
        picked.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void TryPickT0_WhenFailure_ShouldReturnFalseAndRemainder()
    {
        // Arrange
        var errors = new List<ValidationFailure> { new("Name", "Required") };
        Result<int> result = errors;

        // Act
        var picked = result.TryPickT0(out var value, out var remainder);

        // Assert
        picked.ShouldBeFalse();
        value.ShouldBe(default);
        remainder.ShouldBe(errors);
    }

    [Fact]
    public void TryPickT1_WhenFailure_ShouldReturnTrueAndErrors()
    {
        // Arrange
        var errors = new List<ValidationFailure> { new("Email", "Invalid") };
        Result<string> result = errors;

        // Act
        var picked = result.TryPickT1(out var value, out _);

        // Assert
        picked.ShouldBeTrue();
        value.ShouldBe(errors);
    }

    [Fact]
    public void TryPickT1_WhenSuccess_ShouldReturnFalseAndRemainder()
    {
        // Arrange
        Result<string> result = "hello";

        // Act
        var picked = result.TryPickT1(out var value, out var remainder);

        // Assert
        picked.ShouldBeFalse();
        value.ShouldBe(default);
        remainder.ShouldBe("hello");
    }

    [Fact]
    public void Value_WhenSuccess_ShouldReturnSuccessValue()
    {
        // Arrange
        Result<int> result = 99;

        // Act
        var value = result.Value;

        // Assert
        value.ShouldBe(99);
    }

    [Fact]
    public void Value_WhenFailure_ShouldReturnErrorList()
    {
        // Arrange
        var errors = new List<ValidationFailure> { new("Field", "Error") };
        Result<int> result = errors;

        // Act
        var value = result.Value;

        // Assert
        value.ShouldBeOfType<List<ValidationFailure>>();
        ((List<ValidationFailure>)value).ShouldBe(errors);
    }

    [Fact]
    public void Index_WhenSuccess_ShouldBeZero()
    {
        // Arrange
        Result<string> result = "test";

        // Act & Assert
        result.Index.ShouldBe(0);
    }

    [Fact]
    public void Index_WhenFailure_ShouldBeOne()
    {
        // Arrange
        Result<string> result = new List<ValidationFailure> { new("X", "Y") };

        // Act & Assert
        result.Index.ShouldBe(1);
    }

    [Fact]
    public void TryPickT0_WithComplexType_WhenSuccess_ShouldReturnValue()
    {
        // Arrange
        var data = new TestRecord(Guid.NewGuid(), "Alice");
        Result<TestRecord> result = data;

        // Act
        var picked = result.TryPickT0(out var value, out _);

        // Assert
        picked.ShouldBeTrue();
        value.ShouldBe(data);
    }

    [Fact]
    public void TryPickT1_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var errors = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Email", "Email is invalid"),
            new("Age", "Must be positive")
        };
        Result<string> result = errors;

        // Act
        var picked = result.TryPickT1(out var value, out _);

        // Assert
        picked.ShouldBeTrue();
        value.Count.ShouldBe(3);
    }

    [Fact]
    public void AsT0_WhenSuccess_ShouldReturnValue()
    {
        // Arrange
        Result<double> result = 3.14;

        // Act
        var value = result.AsT0;

        // Assert
        value.ShouldBe(3.14);
    }

    [Fact]
    public void AsT1_WhenFailure_ShouldReturnErrors()
    {
        // Arrange
        var errors = new List<ValidationFailure> { new("Prop", "Msg") };
        Result<double> result = errors;

        // Act
        var value = result.AsT1;

        // Assert
        value.ShouldBe(errors);
    }

    private record TestRecord(Guid Id, string Name);
}
