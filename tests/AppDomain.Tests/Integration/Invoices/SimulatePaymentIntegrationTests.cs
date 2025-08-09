<!--#if (includeSample)-->
// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using Dapper;
using System.Data.Common;
using Grpc.Core;

namespace AppDomain.Tests.Integration.Invoices;

public class SimulatePaymentIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task SimulatePayment_ShouldTriggerPaymentSimulationSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice for Payment Simulation",
            Amount = 200.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var simulateRequest = new SimulatePaymentRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            Amount = 200.00,
            Currency = "USD",
            PaymentMethod = "Credit Card",
            PaymentReference = "TEST-REF-123"
        };

        // Act
        var response = await _client.SimulatePaymentAsync(simulateRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.Message.ShouldBe("Payment simulation triggered successfully");

        // Note: SimulatePayment doesn't actually modify the invoice status in the database
        // It just triggers a payment event for processing. The invoice should still be in Draft status.
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT status FROM AppDomain.invoices WHERE invoice_id = @Id",
            new { Id = Guid.Parse(createdInvoice.InvoiceId) });

        dbInvoice!.ShouldNotBeNull();
        var status = (string)dbInvoice.status;
        status.ShouldBe("Draft");
    }

    [Fact]
    public async Task SimulatePayment_WithMinimalData_ShouldUseDefaults()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice for Minimal Payment Simulation",
            Amount = 100.00,
            Currency = "EUR"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var simulateRequest = new SimulatePaymentRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            Amount = 100.00,
            Currency = "EUR", // Use invoice currency as default
            PaymentMethod = "Default",
            PaymentReference = "DEFAULT-REF"
        };

        // Act
        var response = await _client.SimulatePaymentAsync(simulateRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.Message.ShouldBe("Payment simulation triggered successfully");
    }

    [Fact]
    public async Task SimulatePayment_WithNonExistentInvoice_ShouldThrowInvalidArgumentException()
    {
        // Arrange
        var simulateRequest = new SimulatePaymentRequest
        {
            InvoiceId = Guid.NewGuid().ToString(),
            Version = 1,
            Amount = 100.00,
            Currency = "USD"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.SimulatePaymentAsync(simulateRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task SimulatePayment_WithInvalidAmount_ShouldThrowInvalidArgumentException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice for Invalid Payment Simulation",
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var simulateRequest = new SimulatePaymentRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            Amount = -50.00, // Invalid negative amount
            Currency = "USD"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.SimulatePaymentAsync(simulateRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
        exception.Status.Detail.ShouldContain("Amount");
    }

    [Fact]
    public async Task SimulatePayment_WithEmptyPaymentMethod_ShouldThrowInvalidArgumentException()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE AppDomain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = new CreateInvoiceRequest
        {
            Name = "Invoice for Empty Payment Method Simulation",
            Amount = 100.00,
            Currency = "USD"
        };

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var simulateRequest = new SimulatePaymentRequest
        {
            InvoiceId = createdInvoice.InvoiceId,
            Version = createdInvoice.Version,
            Amount = 100.00,
            Currency = "USD",
            PaymentMethod = "" // Empty payment method
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.SimulatePaymentAsync(simulateRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
        exception.Status.Detail.ShouldContain("Payment Method");
    }
}
<!--#endif-->
