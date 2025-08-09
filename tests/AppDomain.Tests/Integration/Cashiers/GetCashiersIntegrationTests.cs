<!--#if (includeSample)-->
// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Grpc;
using AppDomain.Tests.Integration._Internal;
using Dapper;
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
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.cashiers;");

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

        response.Cashiers[0].ShouldBe(new AppDomain.Cashiers.Grpc.Models.Cashier
        {
            TenantId = "12345678-0000-0000-0000-000000000000",
            CashierId = createdCashiers[0].CashierId,
            Name = createdCashiers[0].Name,
            Email = createdCashiers[0].Email
        });

        response.Cashiers[1].ShouldBe(new AppDomain.Cashiers.Grpc.Models.Cashier
        {
            TenantId = "12345678-0000-0000-0000-000000000000",
            CashierId = createdCashiers[1].CashierId,
            Name = createdCashiers[1].Name,
            Email = createdCashiers[1].Email
        });
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
<!--#endif-->
