// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Grpc;
using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.TestDataGenerators;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Cashiers;

public class UpdateCashierIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly CashiersService.CashiersServiceClient _client = new(fixture.GrpcChannel);
    private readonly CashierFaker _cashierFaker = new();

    [Fact]
    public async Task UpdateCashier_ShouldUpdateCashierSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.cashiers;");

        // Arrange - Create a cashier first
        var createRequest = _cashierFaker.Generate();

        var createdCashier = await _client.CreateCashierAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Get the current version from database (xmin column)
        var currentVersion = await connection.QuerySingleAsync<int>(
            "SELECT xmin FROM app_domain.cashiers WHERE cashier_id = @Id",
            new { Id = Guid.Parse(createdCashier.CashierId) });

        var updateFaker = new UpdateCashierFaker(createdCashier.CashierId);
        var updateRequest = updateFaker.Generate();
        updateRequest.Version = currentVersion;

        // Act
        var updatedCashier = await _client.UpdateCashierAsync(updateRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        updatedCashier.CashierId.ShouldBe(createdCashier.CashierId);
        updatedCashier.Name.ShouldBe(updateRequest.Name);
        updatedCashier.Email.ShouldBe(updateRequest.Email);
        updatedCashier.TenantId.ShouldBe("12345678-0000-0000-0000-000000000000");
    }

    [Fact]
    public async Task UpdateCashier_WithInvalidVersion_ShouldThrowInvalidArgumentException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.cashiers;");

        // Arrange - Create a cashier first
        var createRequest = _cashierFaker.Generate();

        var createdCashier = await _client.CreateCashierAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var updateFaker = new UpdateCashierFaker(createdCashier.CashierId);
        var updateRequest = updateFaker.Generate();
        updateRequest.Version = 999; // Invalid version

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
        var updateFaker = new UpdateCashierFaker();
        var updateRequest = updateFaker.Generate();
        updateRequest.Version = 1;

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.UpdateCashierAsync(updateRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
        exception.Status.Detail.ShouldContain("Cashier not found");
    }
}
