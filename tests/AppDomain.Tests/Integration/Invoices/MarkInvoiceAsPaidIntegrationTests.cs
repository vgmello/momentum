// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.TestDataGenerators;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Invoices;

public class MarkInvoiceAsPaidIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);
    private readonly InvoiceFaker _invoiceFaker = new();

    [Fact]
    public async Task MarkInvoiceAsPaid_ShouldMarkInvoiceAsPaidSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE main.invoices;");

        // Arrange - Create an invoice first
        var createRequest = _invoiceFaker
            .WithAmount(150.75m)
            .WithCurrency("USD")
            .Generate();

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var paymentDate = DateTime.UtcNow;
        var markPaidFaker = new MarkInvoiceAsPaidFaker(createdInvoice.InvoiceId);
        var markPaidRequest = markPaidFaker.Generate();
        markPaidRequest.Version = createdInvoice.Version;
        markPaidRequest.AmountPaid = 150.75m;
        markPaidRequest.PaymentDate = Timestamp.FromDateTime(paymentDate);

        // Act
        var paidInvoice = await _client.MarkInvoiceAsPaidAsync(markPaidRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        paidInvoice.ShouldNotBeNull();
        paidInvoice.InvoiceId.ShouldBe(createdInvoice.InvoiceId);
        paidInvoice.Status.ShouldBe("Paid");
        paidInvoice.Name.ShouldBe(createRequest.Name);
        ((decimal)paidInvoice.Amount).ShouldBe(150.75m);

        // Verify in database
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT status, amount_paid, payment_date FROM main.invoices WHERE invoice_id = @Id",
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
        await connection.ExecuteAsync("TRUNCATE TABLE main.invoices;");

        // Arrange - Create an invoice first
        var createRequest = _invoiceFaker
            .WithAmount(100.00m)
            .WithCurrency("USD")
            .Generate();

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var markPaidFaker = new MarkInvoiceAsPaidFaker(createdInvoice.InvoiceId);
        var markPaidRequest = markPaidFaker.Generate();
        markPaidRequest.Version = createdInvoice.Version;
        markPaidRequest.AmountPaid = 100m;
        markPaidRequest.PaymentDate = null; // Should use current time

        var beforePayment = DateTime.UtcNow;

        // Act
        var paidInvoice = await _client.MarkInvoiceAsPaidAsync(markPaidRequest, cancellationToken: TestContext.Current.CancellationToken);

        var afterPayment = DateTime.UtcNow;

        // Assert
        paidInvoice.Status.ShouldBe("Paid");

        // Verify payment date is set to current time
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT payment_date FROM main.invoices WHERE invoice_id = @Id",
            new { Id = Guid.Parse(createdInvoice.InvoiceId) });

        dbInvoice!.ShouldNotBeNull();
        var paymentDateFromDb = (DateTime)dbInvoice.payment_date;
        paymentDateFromDb.ShouldBeInRange(beforePayment, afterPayment);
    }

    [Fact]
    public async Task MarkInvoiceAsPaid_WithNonExistentInvoice_ShouldThrowFailedPreconditionException()
    {
        // Arrange
        var markPaidFaker = new MarkInvoiceAsPaidFaker(Guid.NewGuid().ToString());
        var markPaidRequest = markPaidFaker.Generate();
        markPaidRequest.Version = 1;
        markPaidRequest.AmountPaid = 100m;

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
        await connection.ExecuteAsync("TRUNCATE TABLE main.invoices;");

        // Arrange - Create an invoice first
        var createRequest = _invoiceFaker
            .WithAmount(100.00m)
            .WithCurrency("USD")
            .Generate();

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var markPaidFaker = new MarkInvoiceAsPaidFaker(createdInvoice.InvoiceId);
        var markPaidRequest = markPaidFaker.Generate();
        markPaidRequest.Version = createdInvoice.Version;
        markPaidRequest.AmountPaid = -50m; // Invalid negative amount

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
        await connection.ExecuteAsync("TRUNCATE TABLE main.invoices;");

        // Arrange - Create and pay an invoice
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice to Pay Twice",
            Amount = 100m,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Pay it first time
        var markPaidRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            AmountPaid = 100m
        };

        await _client.MarkInvoiceAsPaidAsync(markPaidRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert - Try to pay again with old version
        var secondPayRequest = new MarkInvoiceAsPaidRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version, // Using old version should fail
            AmountPaid = 100m
        };
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.MarkInvoiceAsPaidAsync(secondPayRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.FailedPrecondition);
        exception.Status.Detail.ShouldContain("Invoice not found, already paid, or was modified by another user");
    }
}
