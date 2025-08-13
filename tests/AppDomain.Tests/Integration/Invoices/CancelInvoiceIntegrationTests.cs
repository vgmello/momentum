< !--#if (includeSample)-->
// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using Dapper;
using System.Data.Common;
using Grpc.Core;

namespace AppDomain.Tests.Integration.Invoices;

public class CancelInvoiceIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task CancelInvoice_ShouldCancelInvoiceSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice to Cancel",
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var cancelRequest = new CancelInvoiceRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version
        };

        // Act
        var cancelledInvoice = await _client.CancelInvoiceAsync(cancelRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        cancelledInvoice.ShouldNotBeNull();
        cancelledInvoice.InvoiceId.ShouldBe(createdInvoice.InvoiceId);
        cancelledInvoice.Status.ShouldBe("Cancelled");
        cancelledInvoice.Name.ShouldBe("Invoice to Cancel");
        cancelledInvoice.Amount.ShouldBe(100.00);

        // Verify in database
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT status FROM AppDomain.invoices WHERE invoice_id = @Id",
            new { Id = Guid.Parse(createdInvoice.InvoiceId) });

        dbInvoice!.ShouldNotBeNull();
        var status = (string)dbInvoice.status;
        status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task CancelInvoice_WithNonExistentInvoice_ShouldThrowFailedPreconditionException()
    {
        // Arrange
        var cancelRequest = new CancelInvoiceRequest
        {
            InvoiceId = Guid.NewGuid().ToString(),
            Version = 1
        };

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
        var cancelRequest = new CancelInvoiceRequest
        {
            InvoiceId = "invalid-guid",
            Version = 1
        };

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
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create and cancel an invoice
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice to Cancel Twice",
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Cancel it first time
        var cancelRequest = new CancelInvoiceRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version
        };

        await _client.CancelInvoiceAsync(cancelRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert - Try to cancel again with old version
        var secondCancelRequest = new CancelInvoiceRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version // Using old version should fail
        };
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.CancelInvoiceAsync(secondCancelRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.FailedPrecondition);
        exception.Status.Detail.ShouldContain("Invoice not found, cannot be cancelled, or was modified by another user");
    }
}
<!--#endif-->
