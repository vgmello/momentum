// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Grpc;
using AppDomain.Cashiers.Grpc.Models;
using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.TestDataGenerators;

namespace AppDomain.Tests.Integration.Cashiers;

public class CreateCashierIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly CashiersService.CashiersServiceClient _client = new(fixture.GrpcChannel);
    private readonly CashierFaker _cashierFaker = new();

    [Fact]
    public async Task CreateCashier_ShouldCreateCashierInDatabase()
    {
        // Arrange
        var request = _cashierFaker.Generate();

        // Act
        var response = await _client.CreateCashierAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Name.ShouldBe(request.Name);
        response.Email.ShouldBe(request.Email);
        Guid.Parse(response.CashierId).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateMultipleCashiers_ShouldCreateUniqueEntries()
    {
        // Arrange
        var requests = _cashierFaker.Generate(3);

        // Act
        var responses = new List<Cashier>();

        foreach (var request in requests)
        {
            var response = await _client.CreateCashierAsync(request, cancellationToken: TestContext.Current.CancellationToken);
            responses.Add(response);
        }

        // Assert
        responses.ShouldAllBe(r => Guid.Parse(r.CashierId) != Guid.Empty);
        responses.Select(r => r.CashierId).ShouldBeUnique();
        responses.Select(r => r.Email).ShouldBeUnique();
    }
}
