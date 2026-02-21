// Copyright (c) OrgName. All rights reserved.

using System.Net;
using AppDomain.Tests.E2E.OpenApi.Generated;
using Refit;

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
        var cashiers = await ApiClient.GetCashiersAsync(100, 0, CancellationToken);

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
        var uniqueId = Guid.NewGuid();
        var createRequest = new CreateCashierRequest
        {
            Name = $"Test Cashier {uniqueId}",
            Email = $"test{uniqueId}@example.com"
        };

        // Act
        var cashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);

        // Assert
        cashier.ShouldNotBeNull();
        cashier.CashierId.ShouldNotBe(Guid.Empty);
        cashier.Name.ShouldBe(createRequest.Name);
        cashier.Email.ShouldBe(createRequest.Email);
    }

    [Fact]
    public async Task CreateAndGetCashier_ReturnsCreatedCashier()
    {
        // Arrange
        var uniqueId = Guid.NewGuid();
        var createRequest = new CreateCashierRequest
        {
            Name = $"Integration Test Cashier {uniqueId}",
            Email = $"integration{uniqueId}@example.com"
        };

        // Act - Create
        var createdCashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);
        createdCashier.ShouldNotBeNull();

        // Act - Get by ID
        var getCashier = await ApiClient.GetCashierAsync(createdCashier.CashierId, CancellationToken);

        // Assert
        getCashier.ShouldNotBeNull();
        getCashier.CashierId.ShouldBe(createdCashier.CashierId);
        getCashier.Name.ShouldBe(createRequest.Name);
        getCashier.Email.ShouldBe(createRequest.Email);
    }

    [Fact]
    public async Task GetCashierById_WhenCashierNotExists_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.GetCashierAsync(nonExistentId, CancellationToken));
        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCashier_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new CreateCashierRequest
        {
            Name = "",
            Email = $"test{Guid.NewGuid()}@example.com"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.CreateCashierAsync(invalidRequest, CancellationToken));
        exception.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCashier_WithValidData_ReturnsUpdatedCashier()
    {
        // Arrange - Create a cashier first
        var uniqueId = Guid.NewGuid();
        var createRequest = new CreateCashierRequest
        {
            Name = $"Original Name {uniqueId}",
            Email = $"original{uniqueId}@example.com"
        };

        var createdCashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);
        createdCashier.ShouldNotBeNull();

        var updatedUniqueId = Guid.NewGuid();
        var updateRequest = new UpdateCashierRequest
        {
            Name = $"Updated Name {updatedUniqueId}",
            Email = $"updated{updatedUniqueId}@example.com",
            Version = createdCashier.Version
        };

        // Act
        var updatedCashier = await ApiClient.UpdateCashierAsync(createdCashier.CashierId, updateRequest, CancellationToken);

        // Assert
        updatedCashier.ShouldNotBeNull();
        updatedCashier.CashierId.ShouldBe(createdCashier.CashierId);
        updatedCashier.Name.ShouldBe(updateRequest.Name);
        updatedCashier.Email.ShouldBe(updateRequest.Email);
    }

    [Fact]
    public async Task DeleteCashier_ExistingCashier_ReturnsSuccess()
    {
        // Arrange - Create a cashier first
        var uniqueId = Guid.NewGuid();
        var createRequest = new CreateCashierRequest
        {
            Name = $"To Be Deleted {uniqueId}",
            Email = $"delete{uniqueId}@example.com"
        };

        var createdCashier = await ApiClient.CreateCashierAsync(createRequest, CancellationToken);
        createdCashier.ShouldNotBeNull();

        // Act
        await ApiClient.DeleteCashierAsync(createdCashier.CashierId, CancellationToken);

        // Assert - Verify cashier was deleted
        var exception = await Should.ThrowAsync<ApiException>(() => ApiClient.GetCashierAsync(createdCashier.CashierId, CancellationToken));
        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
