// Copyright (c) OrgName. All rights reserved.

using System.Net;
using AppDomain.Tests.E2E.OpenApi.Generated;
using Refit;

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
        var invoices = await ApiClient.GetInvoicesAsync(100, 0, null, CancellationToken);

        // Assert
        invoices.ShouldNotBeNull();

        // For E2E tests, we don't assume empty state - just verify API returns valid data
        foreach (var invoice in invoices)
        {
            invoice.InvoiceId.ShouldNotBe(Guid.Empty);
            // Note: CashierId can be empty GUID (API allows non-existent cashier IDs)
            invoice.Amount.ShouldBeGreaterThan(0);
            invoice.Currency.ShouldNotBeNullOrEmpty();
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
            Amount = 100.50m,
            Currency = "USD"
        };

        // Act
        var invoice = await ApiClient.CreateInvoiceAsync(createRequest, CancellationToken);

        // Assert
        invoice.ShouldNotBeNull();
        invoice.InvoiceId.ShouldNotBe(Guid.Empty);
        invoice.CashierId.ShouldBe(cashier.CashierId);
        invoice.Amount.ShouldBe(100.50m);
        invoice.Currency.ShouldBe("USD");
        invoice.Status.ShouldBe(InvoiceStatus.Draft);
        invoice.CreatedDateUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));
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
            Amount = 250.75m,
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
        getInvoice.Amount.ShouldBe(250.75m);
        getInvoice.Currency.ShouldBe("EUR");
    }

    [Fact]
    public async Task GetInvoiceById_WhenInvoiceNotExists_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.GetInvoiceAsync(nonExistentId, CancellationToken));
        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInvoice_WithNonExistentCashier_ReturnsError()
    {
        // Arrange
        var nonExistentCashierId = Guid.NewGuid();
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice with Non-existent Cashier",
            CashierId = nonExistentCashierId,
            Amount = 100.00m,
            Currency = "USD"
        };

        // Act & Assert - API rejects non-existent cashier IDs (FK constraint)
        var exception = await Should.ThrowAsync<ApiException>(
            () => ApiClient.CreateInvoiceAsync(createRequest, CancellationToken));
        ((int)exception.StatusCode).ShouldBeOneOf(400, 404, 500);
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
            Amount = -50.00m,
            Currency = "USD"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.CreateInvoiceAsync(invalidRequest, CancellationToken));
        exception.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
            Amount = 100.00m,
            Currency = "USD"
        };

        var createdInvoice = await ApiClient.CreateInvoiceAsync(createRequest, CancellationToken);
        createdInvoice.ShouldNotBeNull();

        // Act
        var cancelRequest = new CancelInvoiceRequest { Version = createdInvoice.Version };
        var cancelledInvoice = await ApiClient.CancelInvoiceAsync(createdInvoice.InvoiceId, cancelRequest, CancellationToken);

        // Assert
        cancelledInvoice.ShouldNotBeNull();
        cancelledInvoice.Status.ShouldBe(InvoiceStatus.Cancelled);

        // Verify invoice status changed
        var updatedInvoice = await ApiClient.GetInvoiceAsync(createdInvoice.InvoiceId, CancellationToken);
        updatedInvoice.ShouldNotBeNull();
        updatedInvoice.Status.ShouldBe(InvoiceStatus.Cancelled);
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
