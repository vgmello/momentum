// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Invoices;

public class GetInvoiceIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task GetInvoice_WithExistingInvoice_ShouldReturnInvoice()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Test Invoice",
            Amount = 150.75,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var getRequest = new GetInvoiceRequest
        {
            Id = createdInvoice.InvoiceId
        };

        // Act
        var retrievedInvoice = await _client.GetInvoiceAsync(getRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        retrievedInvoice.ShouldNotBeNull();
        retrievedInvoice.InvoiceId.ShouldBe(createdInvoice.InvoiceId);
        retrievedInvoice.Name.ShouldBe("Test Invoice");
        retrievedInvoice.Amount.ShouldBe(150.75);
        retrievedInvoice.Currency.ShouldBe("USD");
        retrievedInvoice.Status.ShouldBe("Draft");
    }

    [Fact]
    public async Task GetInvoice_WithNonExistentInvoice_ShouldThrowNotFoundException()
    {
        // Arrange
        var getRequest = new GetInvoiceRequest
        {
            Id = Guid.NewGuid().ToString()
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.GetInvoiceAsync(getRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.NotFound);
    }
}
