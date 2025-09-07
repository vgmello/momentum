// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using AppDomain.Tests.Integration._Internal.TestDataGenerators;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Invoices;

public class CreateInvoiceIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);
    private readonly InvoiceFaker _invoiceFaker = new();

    [Fact]
    public async Task CreateInvoice_ShouldCreateInvoiceSuccessfully()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange
        var createRequest = _invoiceFaker
            .WithAmount(100.50)
            .WithCurrency("USD")
            .Generate();

        // Act
        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        createdInvoice.ShouldNotBeNull();
        createdInvoice.Name.ShouldBe(createRequest.Name);
        createdInvoice.Amount.ShouldBe(100.50);
        createdInvoice.Currency.ShouldBe("USD");
        createdInvoice.Status.ShouldBe("Draft");
        createdInvoice.InvoiceId.ShouldNotBeNullOrEmpty();
        Guid.Parse(createdInvoice.InvoiceId).ShouldNotBe(Guid.Empty);

        // Verify in database
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT invoice_id, name, amount, currency, status FROM app_domain.invoices WHERE invoice_id = @Id",
            new { Id = Guid.Parse(createdInvoice.InvoiceId) });

        dbInvoice!.ShouldNotBeNull();
        var name = (string)dbInvoice.name;
        var amount = (decimal)dbInvoice.amount;
        var currency = (string)dbInvoice.currency;
        var status = (string)dbInvoice.status;
        name.ShouldBe(createRequest.Name);
        amount.ShouldBe(100.50m);
        currency.ShouldBe("USD");
        status.ShouldBe("Draft");
    }

    [Fact]
    public async Task CreateInvoice_WithCashier_ShouldCreateInvoiceWithCashierReference()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.cashiers;");

        // Create a cashier first
        var cashierId = Guid.NewGuid();
        await connection.ExecuteAsync(
            "INSERT INTO app_domain.cashiers (tenant_id, cashier_id, name, email) VALUES (@TenantId, @CashierId, @Name, @Email)",
            new
            {
                TenantId = Guid.Parse("12345678-0000-0000-0000-000000000000"),
                CashierId = cashierId,
                Name = "Test Cashier",
                Email = "cashier@test.com"
            });

        // Arrange
        var createRequest = _invoiceFaker
            .WithCashier(cashierId.ToString())
            .WithAmount(250.75)
            .WithCurrency("EUR")
            .Generate();

        // Act
        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        createdInvoice.CashierId.ShouldBe(cashierId.ToString());

        // Verify in database
        var dbInvoice = await connection.QuerySingleOrDefaultAsync(
            "SELECT cashier_id FROM app_domain.invoices WHERE invoice_id = @Id",
            new { Id = Guid.Parse(createdInvoice.InvoiceId) });

        dbInvoice!.ShouldNotBeNull();
        var cashierIdFromDb = (Guid)dbInvoice.cashier_id;
        cashierIdFromDb.ShouldBe(cashierId);
    }

    [Fact]
    public async Task CreateInvoice_WithInvalidData_ShouldThrowInvalidArgumentException()
    {
        // Arrange
        var createRequest = _invoiceFaker.Generate();
        createRequest.Name = ""; // Invalid empty name
        createRequest.Amount = -10m; // Invalid negative amount

        // Act & Assert
        var exception = await Should.ThrowAsync<RpcException>(async () =>
            await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken));

        exception.StatusCode.ShouldBe(StatusCode.InvalidArgument);
        exception.Status.Detail.ShouldContain("Name");
        exception.Status.Detail.ShouldContain("Amount");
    }

    [Fact]
    public async Task CreateInvoice_WithMinimalData_ShouldCreateWithDefaults()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange
        var createRequest = _invoiceFaker
            .WithAmount(50.00)
            .WithoutDueDate()
            .Generate();
        createRequest.Currency = string.Empty; // Clear currency to test defaults
        createRequest.CashierId = string.Empty; // Clear CashierId to test without cashier

        // Act
        var createdInvoice = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        createdInvoice.Name.ShouldBe(createRequest.Name);
        createdInvoice.Amount.ShouldBe(50.00);
        createdInvoice.Currency.ShouldBeNullOrEmpty();
        createdInvoice.Status.ShouldBe("Draft");
        createdInvoice.CashierId.ShouldBeNullOrEmpty();
    }
}
