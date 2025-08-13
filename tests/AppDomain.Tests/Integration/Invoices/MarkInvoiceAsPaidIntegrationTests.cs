< !--#if (INCLUDE_SAMPLE)-->
// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using Dapper;
using System.Data.Common;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;

namespace AppDomain.Tests.Integration.Invoices;

public class MarkInvoiceAsPaidIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task MarkInvoiceAsPaid_ShouldMarkInvoiceAsPaidSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice to Pay",
            Amount = 150.75,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var paymentDate = DateTime.UtcNow;
        var markPaidRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            AmountPaid = 150.75,
            PaymentDate = Timestamp.FromDateTime(paymentDate)
        };

        // Act
        var paidInvoice = await _client.MarkInvoiceAsPaidAsync(markPaidRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        paidInvoice.ShouldNotBeNull();
        paidInvoice.InvoiceId.ShouldBe(createdInvoice.InvoiceId);
        paidInvoice.Status.ShouldBe("Paid");
        paidInvoice.Name.ShouldBe("Invoice to Pay");
        paidInvoice.Amount.ShouldBe(150.75);

        // Verify in database
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT status, amount_paid, payment_date FROM AppDomain.invoices WHERE invoice_id = @Id",
            new { Id = Guid.Parse(createdInvoice.InvoiceId) });

        dbInvoice!.ShouldNotBeNull();
        var status = (string)dbInvoice.status;
        var amountPaid = (decimal)dbInvoice.amount_paid;
        var paymentDateFromDb = (DateTime)dbInvoice.payment_date;
        status.ShouldBe("Paid");
        amountPaid.ShouldBe(150.75m);
        paymentDateFromDb.ShouldBeInRange(paymentDate.AddSeconds(-5), paymentDate.AddSeconds(5));
    }

    [Fact]
    public async Task MarkInvoiceAsPaid_WithoutPaymentDate_ShouldUseCurrentTime()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice to Pay Without Date",
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var markPaidRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            AmountPaid = 100.00
            // PaymentDate is null - should use current time
        };

        var beforePayment = DateTime.UtcNow;

        // Act
        var paidInvoice = await _client.MarkInvoiceAsPaidAsync(markPaidRequest, cancellationToken: TestContext.Current.CancellationToken);

        var afterPayment = DateTime.UtcNow;

        // Assert
        paidInvoice.Status.ShouldBe("Paid");

        // Verify payment date is set to current time
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT payment_date FROM AppDomain.invoices WHERE invoice_id = @Id",
            new { Id = Guid.Parse(createdInvoice.InvoiceId) });

        dbInvoice!.ShouldNotBeNull();
        var paymentDateFromDb = (DateTime)dbInvoice.payment_date;
        paymentDateFromDb.ShouldBeInRange(beforePayment, afterPayment);
    }

    [Fact]
    public async Task MarkInvoiceAsPaid_WithNonExistentInvoice_ShouldThrowFailedPreconditionException()
    {
        // Arrange
        var markPaidRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = Guid.NewGuid().ToString(),
            Version = 1,
            AmountPaid = 100.00
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.MarkInvoiceAsPaidAsync(markPaidRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.FailedPrecondition);
        exception.Status.Detail.ShouldContain("Invoice not found, already paid, or was modified by another user");
    }

    [Fact]
    public async Task MarkInvoiceAsPaid_WithInvalidAmount_ShouldThrowInvalidArgumentException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice with Invalid Payment",
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var markPaidRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            AmountPaid = -50.00 // Invalid negative amount
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.MarkInvoiceAsPaidAsync(markPaidRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
        exception.Status.Detail.ShouldContain("Amount Paid");
    }

    [Fact]
    public async Task MarkInvoiceAsPaid_AlreadyPaidInvoice_ShouldThrowInvalidArgumentException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create and pay an invoice
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice to Pay Twice",
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Pay it first time
        var markPaidRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            AmountPaid = 100.00
        };

        await _client.MarkInvoiceAsPaidAsync(markPaidRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert - Try to pay again with old version
        var secondPayRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version, // Using old version should fail
            AmountPaid = 100.00
        };
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.MarkInvoiceAsPaidAsync(secondPayRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.FailedPrecondition);
        exception.Status.Detail.ShouldContain("Invoice not found, already paid, or was modified by another user");
    }
}
<!--#endif-->
