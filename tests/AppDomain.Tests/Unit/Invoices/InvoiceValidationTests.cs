// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Commands;
using FluentValidation.TestHelper;

namespace AppDomain.Tests.Unit.Invoices;

public class InvoiceValidationTests
{
    #region CreateInvoiceValidator Tests

    [Fact]
    public void CreateInvoiceValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoiceCommand(Guid.NewGuid(), "Test Invoice", 100.50m, "USD", DateTime.Now.AddDays(30), Guid.NewGuid());

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateInvoiceValidator_WithEmptyName_ShouldHaveValidationError(string name)
    {
        // Arrange
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoiceCommand(Guid.NewGuid(), name, 100.50m, "USD");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("'Name' must not be empty.");
    }

    [Fact]
    public void CreateInvoiceValidator_WithNameTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoiceCommand(Guid.NewGuid(), "A", 100.50m, "USD");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("The length of 'Name' must be at least 2 characters. You entered 1 characters.");
    }

    [Fact]
    public void CreateInvoiceValidator_WithNameTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateInvoiceValidator();
        var longName = new string('A', 101); // 101 characters
        var command = new CreateInvoiceCommand(Guid.NewGuid(), longName, 100.50m, "USD");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("The length of 'Name' must be 100 characters or fewer. You entered 101 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void CreateInvoiceValidator_WithNonPositiveAmount_ShouldHaveValidationError(decimal amount)
    {
        // Arrange
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoiceCommand(Guid.NewGuid(), "Test Invoice", amount, "USD");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("'Amount' must be greater than '0'.");
    }

    [Fact]
    public void CreateInvoiceValidator_WithPositiveAmount_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoiceCommand(Guid.NewGuid(), "Test Invoice", 0.01m, "USD");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void CreateInvoiceValidator_WithCurrencyTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoiceCommand(Guid.NewGuid(), "Test Invoice", 100.50m, "USDX"); // 4 characters

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("The length of 'Currency' must be 3 characters or fewer. You entered 4 characters.");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("")]
    public void CreateInvoiceValidator_WithValidCurrency_ShouldNotHaveValidationError(string currency)
    {
        // Arrange
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoiceCommand(Guid.NewGuid(), "Test Invoice", 100.50m, currency);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void CreateInvoiceValidator_WithValidNameBoundaries_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new CreateInvoiceValidator();

        var minLengthCommand = new CreateInvoiceCommand(Guid.NewGuid(), "Ab", 100.50m, "USD"); // 2 characters
        var maxLengthCommand = new CreateInvoiceCommand(Guid.NewGuid(), new string('A', 100), 100.50m, "USD"); // 100 characters

        // Act
        var minResult = validator.TestValidate(minLengthCommand);
        var maxResult = validator.TestValidate(maxLengthCommand);

        // Assert
        minResult.ShouldNotHaveAnyValidationErrors();
        maxResult.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region MarkInvoiceAsPaidValidator Tests

    [Fact]
    public void MarkInvoiceAsPaidValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new MarkInvoiceAsPaidValidator();
        var command = new MarkInvoiceAsPaidCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 150.75m, DateTime.Now);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MarkInvoiceAsPaidValidator_WithEmptyInvoiceId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new MarkInvoiceAsPaidValidator();
        var command = new MarkInvoiceAsPaidCommand(Guid.NewGuid(), Guid.Empty, 1, 150.75m);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage("'Invoice Id' must not be empty.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void MarkInvoiceAsPaidValidator_WithNonPositiveAmountPaid_ShouldHaveValidationError(decimal amountPaid)
    {
        // Arrange
        var validator = new MarkInvoiceAsPaidValidator();
        var command = new MarkInvoiceAsPaidCommand(Guid.NewGuid(), Guid.NewGuid(), 1, amountPaid);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AmountPaid)
            .WithErrorMessage("'Amount Paid' must be greater than '0'.");
    }

    [Fact]
    public void MarkInvoiceAsPaidValidator_WithPositiveAmountPaid_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new MarkInvoiceAsPaidValidator();
        var command = new MarkInvoiceAsPaidCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 0.01m);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AmountPaid);
    }

    [Fact]
    public void MarkInvoiceAsPaidValidator_WithNullPaymentDate_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new MarkInvoiceAsPaidValidator();
        var command = new MarkInvoiceAsPaidCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 100.50m);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PaymentDate);
    }

    #endregion

    #region CancelInvoiceValidator Tests

    [Fact]
    public void CancelInvoiceValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new CancelInvoiceValidator();
        var command = new CancelInvoiceCommand(Guid.NewGuid(), Guid.NewGuid(), 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CancelInvoiceValidator_WithEmptyInvoiceId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CancelInvoiceValidator();
        var command = new CancelInvoiceCommand(Guid.NewGuid(), Guid.Empty, 1);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage("'Invoice Id' must not be empty.");
    }

    #endregion

    #region SimulatePaymentValidator Tests

    [Fact]
    public void SimulatePaymentValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new SimulatePaymentValidator();
        var command = new SimulatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 250.00m, "EUR", "Bank Transfer", "REF-123");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SimulatePaymentValidator_WithEmptyTenantId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new SimulatePaymentValidator();
        var command = new SimulatePaymentCommand(Guid.Empty, Guid.NewGuid(), 1, 250.00m);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
            .WithErrorMessage("'Tenant Id' must not be empty.");
    }

    [Fact]
    public void SimulatePaymentValidator_WithEmptyInvoiceId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new SimulatePaymentValidator();
        var command = new SimulatePaymentCommand(Guid.NewGuid(), Guid.Empty, 1, 250.00m);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage("'Invoice Id' must not be empty.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void SimulatePaymentValidator_WithNonPositiveAmount_ShouldHaveValidationError(decimal amount)
    {
        // Arrange
        var validator = new SimulatePaymentValidator();
        var command = new SimulatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 1, amount);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("'Amount' must be greater than '0'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SimulatePaymentValidator_WithEmptyCurrency_ShouldHaveValidationError(string currency)
    {
        // Arrange
        var validator = new SimulatePaymentValidator();
        var command = new SimulatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 100.00m, currency);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("'Currency' must not be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SimulatePaymentValidator_WithEmptyPaymentMethod_ShouldHaveValidationError(string paymentMethod)
    {
        // Arrange
        var validator = new SimulatePaymentValidator();
        var command = new SimulatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 100.00m, "USD", paymentMethod);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod)
            .WithErrorMessage("'Payment Method' must not be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SimulatePaymentValidator_WithEmptyPaymentReference_ShouldHaveValidationError(string paymentReference)
    {
        // Arrange
        var validator = new SimulatePaymentValidator();
        var command = new SimulatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 100.00m, "USD", "Credit Card", paymentReference);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentReference)
            .WithErrorMessage("'Payment Reference' must not be empty.");
    }

    [Fact]
    public void SimulatePaymentValidator_WithDefaults_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var validator = new SimulatePaymentValidator();
        var command = new SimulatePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 100.00m); // Using defaults

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
