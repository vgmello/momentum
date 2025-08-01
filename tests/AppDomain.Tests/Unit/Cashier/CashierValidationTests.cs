// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Commands;
using FluentValidation.TestHelper;

namespace AppDomain.Tests.Unit.Cashier;

public class CashierValidationTests
{
    #region CreateCashierValidator Tests

    [Fact]
    public void CreateCashierValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateCashierValidator();
        var command = new CreateCashierCommand(Guid.NewGuid(), "John Doe", "john.doe@example.com");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCashierValidator_WithEmptyName_ShouldHaveValidationError(string name)
    {
        // Arrange
        var validator = new CreateCashierValidator();
        var command = new CreateCashierCommand(Guid.NewGuid(), name, "john.doe@example.com");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("'Name' must not be empty.");
    }

    [Fact]
    public void CreateCashierValidator_WithNameTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateCashierValidator();
        var command = new CreateCashierCommand(Guid.NewGuid(), "A", "john.doe@example.com");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("The length of 'Name' must be at least 2 characters. You entered 1 characters.");
    }

    [Fact]
    public void CreateCashierValidator_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateCashierValidator();
        var longName = new string('A', 101); // 101 characters
        var command = new CreateCashierCommand(Guid.NewGuid(), longName, "john.doe@example.com");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("The length of 'Name' must be 100 characters or fewer. You entered 101 characters.");
    }

    [Fact]
    public void CreateCashierValidator_WithValidNameBoundaries_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateCashierValidator();

        var minLengthCommand = new CreateCashierCommand(Guid.NewGuid(), "Jo", "test@example.com"); // 2 characters
        var maxLengthCommand = new CreateCashierCommand(Guid.NewGuid(), new string('A', 100), "test@example.com"); // 100 characters

        // Act
        var minResult = validator.TestValidate(minLengthCommand);
        var maxResult = validator.TestValidate(maxLengthCommand);

        // Assert
        minResult.ShouldNotHaveAnyValidationErrors();
        maxResult.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region UpdateCashierValidator Tests

    [Fact]
    public void UpdateCashierValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new UpdateCashierValidator();
        var command = new UpdateCashierCommand(Guid.NewGuid(), Guid.NewGuid(), "John Doe Updated", "john.updated@example.com", Version: 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateCashierValidator_WithEmptyCashierId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new UpdateCashierValidator();
        var command = new UpdateCashierCommand(Guid.NewGuid(), Guid.Empty, "John Doe", "john.doe@example.com", Version: 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CashierId)
            .WithErrorMessage("'Cashier Id' must not be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateCashierValidator_WithEmptyName_ShouldHaveValidationError(string name)
    {
        // Arrange
        var validator = new UpdateCashierValidator();
        var command = new UpdateCashierCommand(Guid.NewGuid(), Guid.NewGuid(), name, "john.doe@example.com", Version: 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("'Name' must not be empty.");
    }

    [Fact]
    public void UpdateCashierValidator_WithNameTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new UpdateCashierValidator();
        var command = new UpdateCashierCommand(Guid.NewGuid(), Guid.NewGuid(), "A", "john.doe@example.com", Version: 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("The length of 'Name' must be at least 2 characters. You entered 1 characters.");
    }

    [Fact]
    public void UpdateCashierValidator_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new UpdateCashierValidator();
        var longName = new string('A', 101); // 101 characters
        var command = new UpdateCashierCommand(Guid.NewGuid(), Guid.NewGuid(), longName, "john.doe@example.com", Version: 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("The length of 'Name' must be 100 characters or fewer. You entered 101 characters.");
    }

    [Fact]
    public void UpdateCashierValidator_WithNullEmail_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new UpdateCashierValidator();
        var command = new UpdateCashierCommand(Guid.NewGuid(), Guid.NewGuid(), "John Doe", null, Version: 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region DeleteCashierValidator Tests

    [Fact]
    public void DeleteCashierValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new DeleteCashierValidator();
        var command = new DeleteCashierCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DeleteCashierValidator_WithEmptyCashierId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new DeleteCashierValidator();
        var command = new DeleteCashierCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CashierId)
            .WithErrorMessage("'Cashier Id' must not be empty.");
    }

    #endregion
}
