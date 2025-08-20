// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Tests.E2E.Models;

namespace AppDomain.Tests.E2E;

/// <summary>
/// End-to-end tests for Invoices API endpoints
/// </summary>
public class InvoicesEndToEndTests : ApiEndToEndTestsBase
{
    [Fact]
    public async Task GetInvoices_WhenNoInvoices_ReturnsEmptyArray()
    {
        // Act
        var invoices = await GetJsonAsync<InvoiceDto[]>("/invoices");

        // Assert
        invoices.ShouldNotBeNull();
        invoices.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateInvoice_WithValidData_ReturnsCreatedInvoice()
    {
        // Arrange - Create a cashier first
        var cashier = await CreateTestCashier();
        
        var createRequest = new CreateInvoiceRequest
        {
            CashierId = cashier.Id,
            Amount = 100.50m,
            Currency = "USD"
        };

        // Act
        var response = await PostJsonAsync("/invoices", createRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        invoice.ShouldNotBeNull();
        invoice.Id.ShouldNotBe(Guid.Empty);
        invoice.CashierId.ShouldBe(cashier.Id);
        invoice.Amount.ShouldBe(100.50m);
        invoice.Currency.ShouldBe("USD");
        invoice.Status.ShouldNotBeNullOrEmpty();
        invoice.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task CreateAndGetInvoice_ReturnsCreatedInvoice()
    {
        // Arrange
        var cashier = await CreateTestCashier();
        var createRequest = new CreateInvoiceRequest
        {
            CashierId = cashier.Id,
            Amount = 250.75m,
            Currency = "EUR"
        };

        // Act - Create
        var createResponse = await PostJsonAsync("/invoices", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        createdInvoice.ShouldNotBeNull();

        // Act - Get by ID
        var getInvoice = await GetJsonAsync<InvoiceDto>($"/invoices/{createdInvoice.Id}");

        // Assert
        getInvoice.ShouldNotBeNull();
        getInvoice.Id.ShouldBe(createdInvoice.Id);
        getInvoice.CashierId.ShouldBe(cashier.Id);
        getInvoice.Amount.ShouldBe(250.75m);
        getInvoice.Currency.ShouldBe("EUR");
    }

    [Fact]
    public async Task GetInvoiceById_WhenInvoiceNotExists_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/invoices/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateInvoice_WithNonExistentCashier_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new CreateInvoiceRequest
        {
            CashierId = Guid.NewGuid(),
            Amount = 100.00m,
            Currency = "USD"
        };

        // Act
        var response = await PostJsonAsync("/invoices", invalidRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateInvoice_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var cashier = await CreateTestCashier();
        var invalidRequest = new CreateInvoiceRequest
        {
            CashierId = cashier.Id,
            Amount = -50.00m,
            Currency = "USD"
        };

        // Act
        var response = await PostJsonAsync("/invoices", invalidRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelInvoice_ExistingInvoice_ReturnsSuccess()
    {
        // Arrange - Create an invoice first
        var cashier = await CreateTestCashier();
        var createRequest = new CreateInvoiceRequest
        {
            CashierId = cashier.Id,
            Amount = 100.00m,
            Currency = "USD"
        };
        
        var createResponse = await PostJsonAsync("/invoices", createRequest);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        createdInvoice.ShouldNotBeNull();

        // Act
        var cancelResponse = await HttpClient.PostAsync($"/invoices/{createdInvoice.Id}/cancel", null);

        // Assert
        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify invoice status changed
        var updatedInvoice = await GetJsonAsync<InvoiceDto>($"/invoices/{createdInvoice.Id}");
        updatedInvoice.ShouldNotBeNull();
        updatedInvoice.Status.ShouldBe("Cancelled");
    }

    private async Task<CashierDto> CreateTestCashier()
    {
        var createRequest = new CreateCashierRequest
        {
            Name = $"Test Cashier {Guid.NewGuid()}",
            SupportedCurrencies = ["USD", "EUR", "GBP"]
        };

        var response = await PostJsonAsync("/cashiers", createRequest);
        response.EnsureSuccessStatusCode();
        
        var cashier = await response.Content.ReadFromJsonAsync<CashierDto>();
        cashier.ShouldNotBeNull();
        return cashier;
    }
}