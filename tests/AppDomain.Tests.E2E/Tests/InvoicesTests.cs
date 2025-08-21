// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Tests.E2E.OpenApi.Generated;

namespace AppDomain.Tests.E2E.Tests;

/// <summary>
///     End-to-end tests for Invoices API endpoints
/// </summary>
public class InvoicesTests(End2EndTestFixture fixture) : End2EndTest(fixture)
{
    [Fact]
    public async Task GetInvoices_ReturnsValidResponse()
    {
        // Act
        var invoices = await ApiClient.GetInvoicesAsync(null, null, null, CancellationToken);

        // Assert
        invoices.ShouldNotBeNull();

        // For E2E tests, we don't assume empty state - just verify API returns valid data
        foreach (var invoice in invoices)
        {
            invoice.InvoiceId.ShouldNotBe(Guid.Empty);
            // Note: CashierId can be empty GUID (API allows non-existent cashier IDs)
            invoice.Amount.ShouldBeGreaterThan(0);
            invoice.Currency.ShouldNotBeNullOrEmpty();
            invoice.Status.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task CreateInvoice_WithValidData_ReturnsCreatedInvoice()
    {
        // Arrange - Create a cashier first
        var cashier = await CreateTestCashier();

        var createRequest = new CreateInvoiceRequest
        {
            Name = "Test Invoice",
            CashierId = cashier.CashierId,
            Amount = 100.50,
            Currency = "USD"
        };

        // Act
        var invoice = await ApiClient.CreateInvoiceAsync(createRequest, CancellationToken);

        // Assert
        invoice.ShouldNotBeNull();
        invoice.InvoiceId.ShouldNotBe(Guid.Empty);
        invoice.CashierId.ShouldBe(cashier.CashierId);
        invoice.Amount.ShouldBe(100.50);
        invoice.Currency.ShouldBe("USD");
        invoice.Status.ShouldNotBeNullOrEmpty();
        invoice.CreatedDateUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task CreateAndGetInvoice_ReturnsCreatedInvoice()
    {
        // Arrange
        var cashier = await CreateTestCashier();
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Integration Test Invoice",
            CashierId = cashier.CashierId,
            Amount = 250.75,
            Currency = "EUR"
        };

        // Act - Create
        var createdInvoice = await ApiClient.CreateInvoiceAsync(createRequest, CancellationToken);
        createdInvoice.ShouldNotBeNull();

        // Act - Get by ID
        var getInvoice = await ApiClient.GetInvoiceAsync(createdInvoice.InvoiceId, CancellationToken);

        // Assert
        getInvoice.ShouldNotBeNull();
        getInvoice.InvoiceId.ShouldBe(createdInvoice.InvoiceId);
        getInvoice.CashierId.ShouldBe(cashier.CashierId);
        getInvoice.Amount.ShouldBe(250.75);
        getInvoice.Currency.ShouldBe("EUR");
    }

    [Fact]
    public async Task GetInvoiceById_WhenInvoiceNotExists_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.GetInvoiceAsync(nonExistentId, CancellationToken));
        exception.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task CreateInvoice_WithNonExistentCashier_CreatesInvoice()
    {
        // Arrange
        var nonExistentCashierId = Guid.NewGuid();
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice with Non-existent Cashier",
            CashierId = nonExistentCashierId,
            Amount = 100.00,
            Currency = "USD"
        };

        // Act
        var invoice = await ApiClient.CreateInvoiceAsync(createRequest, CancellationToken);

        // Assert - API accepts non-existent cashier IDs
        invoice.ShouldNotBeNull();
        invoice.InvoiceId.ShouldNotBe(Guid.Empty);
        invoice.CashierId.ShouldBe(nonExistentCashierId);
        invoice.Amount.ShouldBe(100.00);
        invoice.Currency.ShouldBe("USD");
        invoice.Status.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateInvoice_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var cashier = await CreateTestCashier();
        var invalidRequest = new CreateInvoiceRequest
        {
            Name = "Negative Amount Test",
            CashierId = cashier.CashierId,
            Amount = -50.00,
            Currency = "USD"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.CreateInvoiceAsync(invalidRequest, CancellationToken));
        exception.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task CancelInvoice_ExistingInvoice_ReturnsSuccess()
    {
        // Arrange - Create an invoice first
        var cashier = await CreateTestCashier();
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Cancel Test Invoice",
            CashierId = cashier.CashierId,
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await ApiClient.CreateInvoiceAsync(createRequest, CancellationToken);
        createdInvoice.ShouldNotBeNull();

        // Act
        var cancelRequest = new CancelInvoiceRequest { Version = createdInvoice.Version };
        var cancelledInvoice = await ApiClient.CancelInvoiceAsync(createdInvoice.InvoiceId, cancelRequest, CancellationToken);

        // Assert
        cancelledInvoice.ShouldNotBeNull();
        cancelledInvoice.Status.ShouldBe("Cancelled");

        // Verify invoice status changed
        var updatedInvoice = await ApiClient.GetInvoiceAsync(createdInvoice.InvoiceId, CancellationToken);
        updatedInvoice.ShouldNotBeNull();
        updatedInvoice.Status.ShouldBe("Cancelled");
    }

    private async Task<Cashier> CreateTestCashier()
    {
        var createRequest = new CreateCashierRequest
        {
            Name = $"Test Cashier {Guid.NewGuid()}",
            Email = $"test{Guid.NewGuid()}@example.com"
        };

        var cashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);
        cashier.ShouldNotBeNull();

        return cashier;
    }
}
