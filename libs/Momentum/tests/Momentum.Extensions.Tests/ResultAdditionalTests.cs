// Copyright (c) Momentum .NET. All rights reserved.

using FluentValidation.Results;

namespace Momentum.Extensions.Tests;

public class ResultAdditionalTests
{
    [Fact]
    public void Match_WithSuccess_ShouldReturnTransformedValue()
    {
        // Arrange
        Result<int> result = 10;

        // Act
        var output = result.Match(
            value => value * 2,
            _ => -1);

        // Assert
        output.ShouldBe(20);
    }

    [Fact]
    public void Match_WithMultipleFailures_ShouldProvideAllErrors()
    {
        // Arrange
        var errors = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Email", "Email is invalid"),
            new("Age", "Age must be positive")
        };
        Result<string> result = errors;

        // Act
        var output = result.Match(
            _ => 0,
            failures => failures.Count);

        // Assert
        output.ShouldBe(3);
    }

    [Fact]
    public void Switch_WithSuccess_ShouldNotInvokeErrorAction()
    {
        // Arrange
        Result<int> result = 42;
        var errorActionCalled = false;

        // Act
        result.Switch(
            _ => { },
            _ => errorActionCalled = true);

        // Assert
        errorActionCalled.ShouldBeFalse();
    }

    [Fact]
    public void Switch_WithFailure_ShouldNotInvokeSuccessAction()
    {
        // Arrange
        Result<int> result = new List<ValidationFailure> { new("Field", "Error") };
        var successActionCalled = false;

        // Act
        result.Switch(
            _ => successActionCalled = true,
            _ => { });

        // Assert
        successActionCalled.ShouldBeFalse();
    }

    [Fact]
    public void Result_WithNullValue_ShouldBeSuccess()
    {
        // Arrange & Act
        Result<string?> result = (string?)null!;

        // Assert
        result.IsT0.ShouldBeTrue();
    }

    [Fact]
    public void Result_WithComplexType_ShouldPreserveValue()
    {
        // Arrange
        var customer = new TestCustomer(Guid.NewGuid(), "John Doe");

        // Act
        Result<TestCustomer> result = customer;

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.Id.ShouldBe(customer.Id);
        result.AsT0.Name.ShouldBe(customer.Name);
    }

    [Fact]
    public void Result_IsT0_WhenSuccess_ShouldBeTrue()
    {
        // Arrange & Act
        Result<int> result = 100;

        // Assert
        result.IsT0.ShouldBeTrue();
        result.IsT1.ShouldBeFalse();
    }

    [Fact]
    public void Result_IsT1_WhenFailure_ShouldBeTrue()
    {
        // Arrange & Act
        Result<int> result = new List<ValidationFailure> { new("Field", "Error") };

        // Assert
        result.IsT1.ShouldBeTrue();
        result.IsT0.ShouldBeFalse();
    }

    private record TestCustomer(Guid Id, string Name);
}
