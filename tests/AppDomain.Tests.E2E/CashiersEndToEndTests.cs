// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Tests.E2E.Models;

namespace AppDomain.Tests.E2E;

/// <summary>
/// End-to-end tests for Cashiers API endpoints
/// </summary>
public class CashiersEndToEndTests : ApiEndToEndTestsBase
{
    [Fact]
    public async Task GetCashiers_WhenNoCashiers_ReturnsEmptyArray()
    {
        // Act
        var cashiers = await GetJsonAsync<CashierDto[]>("/cashiers");

        // Assert
        cashiers.ShouldNotBeNull();
        cashiers.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateCashier_WithValidData_ReturnsCreatedCashier()
    {
        // Arrange
        var createRequest = new CreateCashierRequest
        {
            Name = "Test Cashier",
            SupportedCurrencies = ["USD", "EUR"]
        };

        // Act
        var response = await PostJsonAsync("/cashiers", createRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var cashier = await response.Content.ReadFromJsonAsync<CashierDto>();
        cashier.ShouldNotBeNull();
        cashier.Id.ShouldNotBe(Guid.Empty);
        cashier.Name.ShouldBe("Test Cashier");
        cashier.SupportedCurrencies.ShouldBe(["USD", "EUR"]);
        cashier.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAndGetCashier_ReturnsCreatedCashier()
    {
        // Arrange
        var createRequest = new CreateCashierRequest
        {
            Name = "Integration Test Cashier",
            SupportedCurrencies = ["GBP", "JPY"]
        };

        // Act - Create
        var createResponse = await PostJsonAsync("/cashiers", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var createdCashier = await createResponse.Content.ReadFromJsonAsync<CashierDto>();
        createdCashier.ShouldNotBeNull();

        // Act - Get by ID
        var getCashier = await GetJsonAsync<CashierDto>($"/cashiers/{createdCashier.Id}");

        // Assert
        getCashier.ShouldNotBeNull();
        getCashier.Id.ShouldBe(createdCashier.Id);
        getCashier.Name.ShouldBe("Integration Test Cashier");
        getCashier.SupportedCurrencies.ShouldBe(["GBP", "JPY"]);
    }

    [Fact]
    public async Task GetCashierById_WhenCashierNotExists_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/cashiers/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCashier_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new CreateCashierRequest
        {
            Name = "",
            SupportedCurrencies = ["USD"]
        };

        // Act
        var response = await PostJsonAsync("/cashiers", invalidRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCashier_WithValidData_ReturnsUpdatedCashier()
    {
        // Arrange - Create a cashier first
        var createRequest = new CreateCashierRequest
        {
            Name = "Original Name",
            SupportedCurrencies = ["USD"]
        };
        
        var createResponse = await PostJsonAsync("/cashiers", createRequest);
        var createdCashier = await createResponse.Content.ReadFromJsonAsync<CashierDto>();
        createdCashier.ShouldNotBeNull();

        var updateRequest = new UpdateCashierRequest
        {
            Name = "Updated Name",
            SupportedCurrencies = ["USD", "EUR", "GBP"]
        };

        // Act
        var updateResponse = await PutJsonAsync($"/cashiers/{createdCashier.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var updatedCashier = await updateResponse.Content.ReadFromJsonAsync<CashierDto>();
        updatedCashier.ShouldNotBeNull();
        updatedCashier.Id.ShouldBe(createdCashier.Id);
        updatedCashier.Name.ShouldBe("Updated Name");
        updatedCashier.SupportedCurrencies.ShouldBe(["USD", "EUR", "GBP"]);
    }

    [Fact]
    public async Task DeleteCashier_ExistingCashier_ReturnsNoContent()
    {
        // Arrange - Create a cashier first
        var createRequest = new CreateCashierRequest
        {
            Name = "To Be Deleted",
            SupportedCurrencies = ["USD"]
        };
        
        var createResponse = await PostJsonAsync("/cashiers", createRequest);
        var createdCashier = await createResponse.Content.ReadFromJsonAsync<CashierDto>();
        createdCashier.ShouldNotBeNull();

        // Act
        var deleteResponse = await HttpClient.DeleteAsync($"/cashiers/{createdCashier.Id}");

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify cashier was deleted
        var getResponse = await HttpClient.GetAsync($"/cashiers/{createdCashier.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}