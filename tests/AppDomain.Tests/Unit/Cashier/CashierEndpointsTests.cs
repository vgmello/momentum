// Copyright (c) OrgName. All rights reserved.

using System.Net;
using AppDomain.Api.Cashiers;
using AppDomain.Api.Cashiers.Models;
using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Queries;
using AppDomain.Tests.Unit.Common;
using FluentValidation.Results;
using Momentum.Extensions;
namespace AppDomain.Tests.Unit.Cashier;

public class CashierEndpointsTests : EndpointTest
{
    public CashierEndpointsTests()
    {
        ConfigureApp(app => app.MapCashierEndpoints());
    }

    [Fact]
    public async Task GetCashier_WhenCashierExists_ShouldReturnOkWithCashier()
    {
        // Arrange
        var cashierId = Guid.CreateVersion7();
        var cashier = CreateTestCashier(cashierId);

        MockBus.InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Any<GetCashierQuery>(), Arg.Any<CancellationToken>())
            .Returns(cashier);

        // Act
        var response = await Client.GetAsync($"/cashiers/{cashierId}", TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Cashiers.Contracts.Models.Cashier>(TestCancellationToken);
        result.ShouldNotBeNull();
        result.CashierId.ShouldBe(cashierId);
        result.Name.ShouldBe("Test Cashier");
        result.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task GetCashier_WhenCashierNotFound_ShouldReturn404()
    {
        // Arrange
        var cashierId = Guid.CreateVersion7();
        Result<Cashiers.Contracts.Models.Cashier> notFoundResult =
            new List<ValidationFailure> { new("Id", "Cashier not found") };

        MockBus.InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Any<GetCashierQuery>(), Arg.Any<CancellationToken>())
            .Returns(notFoundResult);

        // Act
        var response = await Client.GetAsync($"/cashiers/{cashierId}", TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCashiers_ShouldReturnOkWithList()
    {
        // Arrange
        var cashierResults = new List<GetCashiersQuery.Result>
        {
            new(FakeTenantId, Guid.CreateVersion7(), "Cashier One", "one@example.com", DateTime.UtcNow, DateTime.UtcNow, 1),
            new(FakeTenantId, Guid.CreateVersion7(), "Cashier Two", "two@example.com", DateTime.UtcNow, DateTime.UtcNow, 1)
        };

        MockBus.InvokeAsync<IEnumerable<GetCashiersQuery.Result>>(
            Arg.Any<GetCashiersQuery>(), Arg.Any<CancellationToken>())
            .Returns(cashierResults);

        // Act
        var response = await Client.GetAsync("/cashiers/?limit=100&offset=0", TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GetCashiersQuery.Result>>(TestCancellationToken);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Cashier One");
        result[1].Name.ShouldBe("Cashier Two");
    }

    [Fact]
    public async Task CreateCashier_WithValidRequest_ShouldReturn201WithCashier()
    {
        // Arrange
        var cashierId = Guid.CreateVersion7();
        var cashier = CreateTestCashier(cashierId);

        MockBus.InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Any<CreateCashierCommand>(), Arg.Any<CancellationToken>())
            .Returns(cashier);

        var request = new CreateCashierRequest { Name = "Test Cashier", Email = "test@example.com" };

        // Act
        var response = await Client.PostAsJsonAsync("/cashiers/", request, TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location?.ToString().ShouldBe($"/cashiers/{cashierId}");

        var result = await response.Content.ReadFromJsonAsync<Cashiers.Contracts.Models.Cashier>(TestCancellationToken);
        result.ShouldNotBeNull();
        result.CashierId.ShouldBe(cashierId);
        result.Name.ShouldBe("Test Cashier");
    }

    [Fact]
    public async Task CreateCashier_WithValidationErrors_ShouldReturn400()
    {
        // Arrange
        Result<Cashiers.Contracts.Models.Cashier> validationResult =
            new List<ValidationFailure> { new("Name", "Name is required") };

        MockBus.InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Any<CreateCashierCommand>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        var request = new CreateCashierRequest { Name = "", Email = "test@example.com" };

        // Act
        var response = await Client.PostAsJsonAsync("/cashiers/", request, TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCashier_WithValidRequest_ShouldReturnOkWithCashier()
    {
        // Arrange
        var cashierId = Guid.CreateVersion7();
        var updatedCashier = CreateTestCashier(cashierId, name: "Updated Cashier", version: 2);

        MockBus.InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Any<UpdateCashierCommand>(), Arg.Any<CancellationToken>())
            .Returns(updatedCashier);

        var request = new UpdateCashierRequest { Name = "Updated Cashier", Email = "updated@example.com", Version = 1 };

        // Act
        var response = await Client.PutAsJsonAsync($"/cashiers/{cashierId}", request, TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Cashiers.Contracts.Models.Cashier>(TestCancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Cashier");
        result.Version.ShouldBe(2);
    }

    [Fact]
    public async Task UpdateCashier_WithConcurrencyConflict_ShouldReturn409()
    {
        // Arrange
        var cashierId = Guid.CreateVersion7();
        Result<Cashiers.Contracts.Models.Cashier> conflictResult =
            new List<ValidationFailure> { new("Version", "The entity was modified by another user") };

        MockBus.InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Any<UpdateCashierCommand>(), Arg.Any<CancellationToken>())
            .Returns(conflictResult);

        var request = new UpdateCashierRequest { Name = "Updated Cashier", Version = 1 };

        // Act
        var response = await Client.PutAsJsonAsync($"/cashiers/{cashierId}", request, TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCashier_WhenCashierNotFound_ShouldReturn404()
    {
        // Arrange
        var cashierId = Guid.CreateVersion7();
        Result<Cashiers.Contracts.Models.Cashier> notFoundResult =
            new List<ValidationFailure> { new("CashierId", "Cashier not found") };

        MockBus.InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Any<UpdateCashierCommand>(), Arg.Any<CancellationToken>())
            .Returns(notFoundResult);

        var request = new UpdateCashierRequest { Name = "Updated Cashier", Version = 1 };

        // Act
        var response = await Client.PutAsJsonAsync($"/cashiers/{cashierId}", request, TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCashier_WhenCashierExists_ShouldReturn204()
    {
        // Arrange
        var cashierId = Guid.CreateVersion7();
        Result<bool> successResult = true;

        MockBus.InvokeAsync<Result<bool>>(
            Arg.Any<DeleteCashierCommand>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        var response = await Client.DeleteAsync($"/cashiers/{cashierId}", TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCashier_WhenCashierNotFound_ShouldReturn404()
    {
        // Arrange
        var cashierId = Guid.CreateVersion7();
        Result<bool> notFoundResult =
            new List<ValidationFailure> { new("CashierId", "Cashier not found") };

        MockBus.InvokeAsync<Result<bool>>(
            Arg.Any<DeleteCashierCommand>(), Arg.Any<CancellationToken>())
            .Returns(notFoundResult);

        // Act
        var response = await Client.DeleteAsync($"/cashiers/{cashierId}", TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCashiers_WithPagination_ShouldPassParametersToQuery()
    {
        // Arrange
        MockBus.InvokeAsync<IEnumerable<GetCashiersQuery.Result>>(
            Arg.Any<GetCashiersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<GetCashiersQuery.Result>());

        // Act
        var response = await Client.GetAsync("/cashiers/?limit=10&offset=20", TestCancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await MockBus.Received(1).InvokeAsync<IEnumerable<GetCashiersQuery.Result>>(
            Arg.Is<GetCashiersQuery>(q => q.TenantId == FakeTenantId && q.Limit == 10 && q.Offset == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCashier_ShouldPassTenantIdToCommand()
    {
        // Arrange
        var cashier = CreateTestCashier(Guid.CreateVersion7());

        MockBus.InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Any<CreateCashierCommand>(), Arg.Any<CancellationToken>())
            .Returns(cashier);

        var request = new CreateCashierRequest { Name = "New Cashier", Email = "new@example.com" };

        // Act
        await Client.PostAsJsonAsync("/cashiers/", request, TestCancellationToken);

        // Assert
        await MockBus.Received(1).InvokeAsync<Result<Cashiers.Contracts.Models.Cashier>>(
            Arg.Is<CreateCashierCommand>(cmd =>
                cmd.TenantId == FakeTenantId &&
                cmd.Name == "New Cashier" &&
                cmd.Email == "new@example.com"),
            Arg.Any<CancellationToken>());
    }

    private static Cashiers.Contracts.Models.Cashier CreateTestCashier(
        Guid cashierId, string name = "Test Cashier", string email = "test@example.com", int version = 1)
    {
        return new Cashiers.Contracts.Models.Cashier
        {
            TenantId = FakeTenantId,
            CashierId = cashierId,
            Name = name,
            Email = email,
            CashierPayments = [],
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow,
            Version = version
        };
    }
}
