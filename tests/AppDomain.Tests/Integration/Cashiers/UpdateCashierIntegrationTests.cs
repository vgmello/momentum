< !--#if (includeSample)-->
// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Grpc;
using AppDomain.Tests.Integration._Internal;
using Dapper;
using System.Data.Common;
using Grpc.Core;

namespace AppDomain.Tests.Integration.Cashiers;

public class UpdateCashierIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly CashiersService.CashiersServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task UpdateCashier_ShouldUpdateCashierSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.cashiers;");

        // Arrange - Create a cashier first
        var createRequest = new CreateCashierRequest
        {
            Name = "Original Name",
            Email = "original@test.com"
        };

        var createdCashier = await _client.CreateCashierAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Get the current version from database (xmin column)
        var currentVersion = await connection.QuerySingleAsync<int>(
            "SELECT xmin FROM AppDomain.cashiers WHERE cashier_id = @Id",
            new { Id = Guid.Parse(createdCashier.CashierId) });

        var updateRequest = new UpdateCashierRequest
        {
            CashierId = createdCashier.CashierId,
            Name = "Updated Name",
            Email = "updated@test.com",
            Version = currentVersion
        };

        // Act
        var updatedCashier = await _client.UpdateCashierAsync(updateRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        updatedCashier.CashierId.ShouldBe(createdCashier.CashierId);
        updatedCashier.Name.ShouldBe("Updated Name");
        updatedCashier.Email.ShouldBe("updated@test.com");
        updatedCashier.TenantId.ShouldBe("12345678-0000-0000-0000-000000000000");
    }

    [Fact]
    public async Task UpdateCashier_WithInvalidVersion_ShouldThrowInvalidArgumentException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.cashiers;");

        // Arrange - Create a cashier first
        var createRequest = new CreateCashierRequest
        {
            Name = "Original Name",
            Email = "original@test.com"
        };

        var createdCashier = await _client.CreateCashierAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var updateRequest = new UpdateCashierRequest
        {
            CashierId = createdCashier.CashierId,
            Name = "Updated Name",
            Email = "updated@test.com",
            Version = 999 // Invalid version
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.UpdateCashierAsync(updateRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
        exception.Status.Detail.ShouldContain("Cashier not found");
    }

    [Fact]
    public async Task UpdateCashier_WithNonExistentCashierId_ShouldThrowInvalidArgumentException()
    {
        // Arrange
        var updateRequest = new UpdateCashierRequest
        {
            CashierId = Guid.NewGuid().ToString(),
            Name = "Updated Name",
            Email = "updated@test.com",
            Version = 1
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.UpdateCashierAsync(updateRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
        exception.Status.Detail.ShouldContain("Cashier not found");
    }
}
<!--#endif-->
