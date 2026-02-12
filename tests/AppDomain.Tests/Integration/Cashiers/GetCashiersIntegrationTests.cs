// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Grpc;
using AppDomain.Tests.Integration._Internal;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Cashiers;

public class GetCashiersIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly CashiersService.CashiersServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task GetCashiers_ShouldReturnCashiers()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE main.cashiers CASCADE;");

        // Arrange - Create a few cashiers first
        var createRequests = new[]
        {
            new CreateCashierRequest { Name = "Cashier 1", Email = "cashier1@test.com" },
            new CreateCashierRequest { Name = "Cashier 2", Email = "cashier2@test.com" }
        };

        var createdCashiers = new List<AppDomain.Cashiers.Grpc.Models.Cashier>();

        foreach (var createRequest in createRequests)
        {
            var createResponse = await _client.CreateCashierAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);
            createdCashiers.Add(createResponse);
        }

        var request = new GetCashiersRequest
        {
            Limit = 10,
            Offset = 0
        };

        // Act
        var response = await _client.GetCashiersAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Cashiers.Count.ShouldBeGreaterThanOrEqualTo(2);

        response.Cashiers[0].TenantId.ShouldBe("12345678-0000-0000-0000-000000000000");
        response.Cashiers[0].CashierId.ShouldBe(createdCashiers[0].CashierId);
        response.Cashiers[0].Name.ShouldBe(createdCashiers[0].Name);
        response.Cashiers[0].Email.ShouldBe(createdCashiers[0].Email);
        response.Cashiers[0].CreatedDateUtc.ShouldNotBeNull();
        response.Cashiers[0].UpdatedDateUtc.ShouldNotBeNull();

        response.Cashiers[1].TenantId.ShouldBe("12345678-0000-0000-0000-000000000000");
        response.Cashiers[1].CashierId.ShouldBe(createdCashiers[1].CashierId);
        response.Cashiers[1].Name.ShouldBe(createdCashiers[1].Name);
        response.Cashiers[1].Email.ShouldBe(createdCashiers[1].Email);
        response.Cashiers[1].CreatedDateUtc.ShouldNotBeNull();
        response.Cashiers[1].UpdatedDateUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetCashiers_WithLimitAndOffset_ShouldReturnPaginatedResults()
    {
        // Arrange
        var request = new GetCashiersRequest
        {
            Limit = 1,
            Offset = 0
        };

        // Act
        var response = await _client.GetCashiersAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Cashiers.Count.ShouldBeLessThanOrEqualTo(1);
    }
}
