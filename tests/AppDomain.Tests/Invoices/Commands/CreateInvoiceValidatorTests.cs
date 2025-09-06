// Copyright (c) OrgName. All rights reserved.

using AppDomain.Core.Enums;
using AppDomain.Invoices.Commands;
using FluentValidation.TestHelper;

namespace AppDomain.Tests.Invoices.Commands;

/// <summary>
///     Tests for CreateInvoiceValidator to ensure proper validation rules.
/// </summary>
public class CreateInvoiceValidatorTests
{
    private readonly CreateInvoiceValidator _validator;

    public CreateInvoiceValidatorTests()
    {
        _validator = new CreateInvoiceValidator();
    }

    [Fact]
    public void Should_Have_Error_When_TenantId_Is_Empty()
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.Empty,
            "Test Invoice",
            100.00m,
            "USD",
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
            .WithErrorMessage("Tenant ID is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Have_Error_When_Name_Is_Empty_Or_Whitespace(string? name)
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            name!,
            100.00m,
            "USD",
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Invoice name is required");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Too_Short()
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            "A",
            100.00m,
            "USD",
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Invoice name must be at least 2 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Too_Long()
    {
        // Arrange
        var longName = new string('A', 101);
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            longName,
            100.00m,
            "USD",
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Invoice name cannot exceed 100 characters");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Should_Have_Error_When_Amount_Is_Zero_Or_Negative(decimal amount)
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            "Test Invoice",
            amount,
            "USD",
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than zero");
    }

    [Fact]
    public void Should_Have_Error_When_Amount_Exceeds_Maximum()
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            "Test Invoice",
            1_000_001m,
            "USD",
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount cannot exceed 1,000,000");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("")]
    public void Should_Have_Error_When_Currency_Is_Invalid_Length(string currency)
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            "Test Invoice",
            100.00m,
            currency,
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be a 3-character ISO code");
    }

    [Fact]
    public void Should_Have_Error_When_DueDate_Is_In_Past()
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            "Test Invoice",
            100.00m,
            "USD",
            DateTime.Today.AddDays(-1),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DueDate)
            .WithErrorMessage("Due date cannot be in the past");
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            "Valid Invoice Name",
            100.00m,
            "USD",
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Optional_Fields_Are_Null()
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            "Valid Invoice Name",
            100.00m,
            null, // Currency is optional
            null, // DueDate is optional
            null  // CashierId is optional
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Currency_Is_Null()
    {
        // Arrange
        var command = new CreateInvoiceCommand(
            Guid.NewGuid(),
            "Valid Invoice Name",
            100.00m,
            null,
            DateTime.Today.AddDays(30),
            Guid.NewGuid()
        );

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }
}
