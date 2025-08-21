// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Tests.E2E.OpenApi.Generated;

namespace AppDomain.Tests.E2E.Tests;

/// <summary>
///     End-to-end tests for Cashiers API endpoints
/// </summary>
public class CashiersTests(End2EndTestFixture fixture) : End2EndTest(fixture)
{
    [Fact]
    public async Task GetCashiers_ReturnsValidResponse()
    {
        // Act
        var cashiers = await ApiClient.GetCashiersAsync(null, null, CancellationToken);

        // Assert
        cashiers.ShouldNotBeNull();

        // For E2E tests, we don't assume empty state - just verify API returns valid data
        foreach (var cashier in cashiers)
        {
            cashier.CashierId.ShouldNotBe(Guid.Empty);
            cashier.Name.ShouldNotBeNullOrEmpty();
            cashier.Email.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task CreateCashier_WithValidData_ReturnsCreatedCashier()
    {
        // Arrange
        var createRequest = new CreateCashierRequest
        {
            Name = "Test Cashier",
            Email = "test@example.com"
        };

        // Act
        var cashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);

        // Assert
        cashier.ShouldNotBeNull();
        cashier.CashierId.ShouldNotBe(Guid.Empty);
        cashier.Name.ShouldBe("Test Cashier");
        cashier.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task CreateAndGetCashier_ReturnsCreatedCashier()
    {
        // Arrange
        var createRequest = new CreateCashierRequest
        {
            Name = "Integration Test Cashier",
            Email = "integration@example.com"
        };

        // Act - Create
        var createdCashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);
        createdCashier.ShouldNotBeNull();

        // Act - Get by ID
        var getCashier = await ApiClient.GetCashierAsync(createdCashier.CashierId, CancellationToken);

        // Assert
        getCashier.ShouldNotBeNull();
        getCashier.CashierId.ShouldBe(createdCashier.CashierId);
        getCashier.Name.ShouldBe("Integration Test Cashier");
        getCashier.Email.ShouldBe("integration@example.com");
    }

    [Fact]
    public async Task GetCashierById_WhenCashierNotExists_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.GetCashierAsync(nonExistentId, CancellationToken));
        exception.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task CreateCashier_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new CreateCashierRequest
        {
            Name = "",
            Email = "test@example.com"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.CreateCashierAsync(invalidRequest, CancellationToken));
        exception.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task UpdateCashier_WithValidData_ReturnsUpdatedCashier()
    {
        // Arrange - Create a cashier first
        var createRequest = new CreateCashierRequest
        {
            Name = "Original Name",
            Email = "original@example.com"
        };

        var createdCashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);
        createdCashier.ShouldNotBeNull();

        var updateRequest = new UpdateCashierRequest
        {
            Name = "Updated Name",
            Email = "updated@example.com",
            Version = createdCashier.Version
        };

        // Act
        var updatedCashier = await ApiClient.UpdateCashierAsync(createdCashier.CashierId, updateRequest, CancellationToken);

        // Assert
        updatedCashier.ShouldNotBeNull();
        updatedCashier.CashierId.ShouldBe(createdCashier.CashierId);
        updatedCashier.Name.ShouldBe("Updated Name");
        updatedCashier.Email.ShouldBe("updated@example.com");
    }

    [Fact]
    public async Task DeleteCashier_ExistingCashier_ReturnsSuccess()
    {
        // Arrange - Create a cashier first
        var createRequest = new CreateCashierRequest
        {
            Name = "To Be Deleted",
            Email = "delete@example.com"
        };

        var createdCashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);
        createdCashier.ShouldNotBeNull();

        // Act
        await ApiClient.DeleteCashierAsync(createdCashier.CashierId, CancellationToken);

        // Assert - Verify cashier was deleted
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.GetCashierAsync(createdCashier.CashierId, CancellationToken));
        exception.StatusCode.ShouldBe(404);
    }
}
