// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Tests.Integration._Internal;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Invoices;

public class ConcurrencyConflictIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task CancelInvoice_WithOutdatedVersion_ShouldThrowFailedPreconditionException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice for Concurrency Test",
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Simulate two users trying to modify the same invoice
        var firstUserCancelRequest = new CancelInvoiceRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version
        };

        var secondUserCancelRequest = new CancelInvoiceRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version // Same version - should cause conflict
        };

        // Act - First user cancels successfully
        var cancelledInvoice =
            await _client.CancelInvoiceAsync(firstUserCancelRequest, cancellationToken: TestContext.Current.CancellationToken);
        cancelledInvoice.Status.ShouldBe("Cancelled");

        // Act & Assert - Second user tries to cancel with outdated version
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.CancelInvoiceAsync(secondUserCancelRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.FailedPrecondition);
        exception.Status.Detail.ShouldContain("Invoice not found, cannot be cancelled, or was modified by another user");
    }

    [Fact]
    public async Task MarkInvoiceAsPaid_WithOutdatedVersion_ShouldThrowFailedPreconditionException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice for Payment Concurrency Test",
            Amount = 200.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Simulate two users trying to pay the same invoice
        var firstUserPayRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            AmountPaid = 200.00
        };

        var secondUserPayRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version, // Same version - should cause conflict
            AmountPaid = 200.00
        };

        // Act - First user pays successfully
        var paidInvoice =
            await _client.MarkInvoiceAsPaidAsync(firstUserPayRequest, cancellationToken: TestContext.Current.CancellationToken);
        paidInvoice.Status.ShouldBe("Paid");

        // Act & Assert - Second user tries to pay with outdated version
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.MarkInvoiceAsPaidAsync(secondUserPayRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.FailedPrecondition);
        exception.Status.Detail.ShouldContain("Invoice not found, already paid, or was modified by another user");
    }
}
