// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.TestDataGenerators;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Invoices;

public class CancelInvoiceIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);
    private readonly InvoiceFaker _invoiceFaker = new();

    [Fact]
    public async Task CancelInvoice_ShouldCancelInvoiceSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE main.invoices;");

        // Arrange - Create an invoice first
        var createRequest = _invoiceFaker
            .WithAmount(100.00m)
            .WithCurrency("USD")
            .Generate();

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var cancelFaker = new CancelInvoiceFaker(createdInvoice.InvoiceId);
        var cancelRequest = cancelFaker.Generate();
        cancelRequest.Version = createdInvoice.Version;

        // Act
        var cancelledInvoice = await _client.CancelInvoiceAsync(cancelRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        cancelledInvoice.ShouldNotBeNull();
        cancelledInvoice.InvoiceId.ShouldBe(createdInvoice.InvoiceId);
        cancelledInvoice.Status.ShouldBe("Cancelled");
        cancelledInvoice.Name.ShouldBe(createRequest.Name);
        ((decimal)cancelledInvoice.Amount).ShouldBe(100.00m);

        // Verify in database
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT status FROM main.invoices WHERE invoice_id = @Id",
            new { Id = Guid.Parse(createdInvoice.InvoiceId) });

        dbInvoice!.ShouldNotBeNull();
        var status = (string)dbInvoice.status;
        status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task CancelInvoice_WithNonExistentInvoice_ShouldThrowFailedPreconditionException()
    {
        // Arrange
        var cancelFaker = new CancelInvoiceFaker(Guid.NewGuid().ToString());
        var cancelRequest = cancelFaker.Generate();
        cancelRequest.Version = 1;

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.CancelInvoiceAsync(cancelRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.FailedPrecondition);
        exception.Status.Detail.ShouldContain("Invoice not found, cannot be cancelled, or was modified by another user");
    }

    [Fact]
    public async Task CancelInvoice_WithInvalidGuid_ShouldThrowInvalidArgumentException()
    {
        // Arrange
        var cancelFaker = new CancelInvoiceFaker("invalid-guid");
        var cancelRequest = cancelFaker.Generate();
        cancelRequest.Version = 1;

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.CancelInvoiceAsync(cancelRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task CancelInvoice_AlreadyCancelledInvoice_ShouldThrowInvalidArgumentException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE main.invoices;");

        // Arrange - Create and cancel an invoice
        var createRequest = _invoiceFaker
            .WithAmount(100.00m)
            .WithCurrency("USD")
            .Generate();

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Cancel it first time
        var cancelFaker = new CancelInvoiceFaker(createdInvoice.InvoiceId);
        var cancelRequest = cancelFaker.Generate();
        cancelRequest.Version = createdInvoice.Version;

        await _client.CancelInvoiceAsync(cancelRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert - Try to cancel again with old version
        var secondCancelFaker = new CancelInvoiceFaker(createdInvoice.InvoiceId);
        var secondCancelRequest = secondCancelFaker.Generate();
        secondCancelRequest.Version = createdInvoice.Version; // Using old version should fail
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.CancelInvoiceAsync(secondCancelRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.FailedPrecondition);
        exception.Status.Detail.ShouldContain("Invoice not found, cannot be cancelled, or was modified by another user");
    }
}
