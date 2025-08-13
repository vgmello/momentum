< !--#if (includeSample)-->
// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Grpc;
using AppDomain.Tests.Integration._Internal;

namespace AppDomain.Tests.Integration.Cashiers;

public class CreateCashierIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly CashiersService.CashiersServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task CreateCashier_ShouldCreateCashierInDatabase()
    {
        // Arrange
        var request = new CreateCashierRequest
        {
            Name = "Integration Test Cashier",
            Email = "integration@test.com"
        };

        // Act
        var response = await _client.CreateCashierAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Name.ShouldBe(request.Name);
        response.Email.ShouldBe(request.Email);
        Guid.Parse(response.CashierId).ShouldNotBe(Guid.Empty);
    }
}
<!--#endif-->
