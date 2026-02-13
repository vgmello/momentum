// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Grpc;
using AppDomain.Tests.Integration._Internal;

namespace AppDomain.Tests.Integration.Cashiers;

public class GetCashierIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly CashiersService.CashiersServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task GetCashier_WithValidId_ShouldReturnCashier()
    {
        // Arrange - First create a cashier
        var createRequest = new CreateCashierRequest
        {
            Name = "Test Cashier for Get",
            Email = "get@test.com"
        };

        var createResponse = await _client.CreateCashierAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var getRequest = new GetCashierRequest { Id = createResponse.CashierId };

        // Act
        var response = await _client.GetCashierAsync(getRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.TenantId.ShouldBe("12345678-0000-0000-0000-000000000000");
        response.CashierId.ShouldBe(createResponse.CashierId);
        response.Name.ShouldBe(createRequest.Name);
        response.Email.ShouldBe(createRequest.Email);
        response.CreatedDateUtc.ShouldNotBeNull();
        response.UpdatedDateUtc.ShouldNotBeNull();
        response.Version.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetCashier_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var request = new GetCashierRequest { Id = Guid.NewGuid().ToString() };

        // Act & Assert
        await Should.ThrowAsync<RpcException>(async () =>
            await _client.GetCashierAsync(request, cancellationToken: TestContext.Current.CancellationToken));
    }
}
