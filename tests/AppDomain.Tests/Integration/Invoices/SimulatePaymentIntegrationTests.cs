// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.TestDataGenerators;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Invoices;

public class SimulatePaymentIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);
    private readonly InvoiceFaker _invoiceFaker = new();

    [Fact]
    public async Task SimulatePayment_ShouldTriggerPaymentSimulationSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = _invoiceFaker
            .WithAmount(200.00)
            .WithCurrency("USD")
            .Generate();

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var simulateFaker = new SimulatePaymentFaker(createdInvoice.InvoiceId)
            .WithAmount(200.00)
            .WithPaymentMethod("credit_card");
        var simulateRequest = simulateFaker.Generate();
        simulateRequest.Version = createdInvoice.Version;
        simulateRequest.Currency = "USD";
        simulateRequest.PaymentReference = "TEST-REF-123";

        // Act
        var response = await _client.SimulatePaymentAsync(simulateRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.Message.ShouldBe("Payment simulation triggered successfully");

        // Note: SimulatePayment doesn't actually modify the invoice status in the database
        // It just triggers a payment event for processing. The invoice should still be in Draft status.
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT status FROM app_domain.invoices WHERE invoice_id = @Id",
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
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = _invoiceFaker
            .WithAmount(100.00)
            .WithCurrency("EUR")
            .Generate();

        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        var simulateFaker = new SimulatePaymentFaker(createdInvoice.InvoiceId)
            .WithAmount(100.00);
        var simulateRequest = simulateFaker.Generate();
        simulateRequest.Version = createdInvoice.Version;
        simulateRequest.Currency = "EUR"; // Use invoice currency as default
        simulateRequest.PaymentMethod = "Default";
        simulateRequest.PaymentReference = "DEFAULT-REF";

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
        var simulateFaker = new SimulatePaymentFaker(Guid.NewGuid().ToString())
            .WithAmount(100.00);
        var simulateRequest = simulateFaker.Generate();
        simulateRequest.Version = 1;
        simulateRequest.Currency = "USD";

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
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange - Create an invoice first
        var createRequest = _invoiceFaker
            .WithAmount(100.00)
            .WithCurrency("USD")
            .Generate();

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
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

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
