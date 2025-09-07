// Copyright (c) OrgName. All rights reserved.

using AppDomain.Common.Grpc;
using AppDomain.Invoices.Grpc;
using AppDomain.Tests.Integration._Internal;
using System.Data.Common;

namespace AppDomain.Tests.Integration.Invoices;

public class GetInvoicesIntegrationTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    private readonly InvoicesService.InvoicesServiceClient _client = new(fixture.GrpcChannel);

    [Fact]
    public async Task GetInvoices_ShouldReturnInvoices()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange - Create a few invoices first
        var createRequests = new[]
        {
            new CreateInvoiceRequest { Name = "Invoice 1", Amount = 100m, Currency = "USD" },
            new CreateInvoiceRequest { Name = "Invoice 2", Amount = 200m, Currency = "EUR" }
        };

        var createdInvoices = new List<AppDomain.Invoices.Grpc.Models.Invoice>();

        foreach (var createRequest in createRequests)
        {
            var createResponse = await _client.CreateInvoiceAsync(createRequest, cancellationToken: TestContext.Current.CancellationToken);
            createdInvoices.Add(createResponse);
        }

        var request = new GetInvoicesRequest
        {
            Limit = 10,
            Offset = 0
        };

        // Act
        var response = await _client.GetInvoicesAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Invoices.Count.ShouldBeGreaterThanOrEqualTo(2);

        // Find our created invoices in the response
        var invoice1 = response.Invoices.FirstOrDefault(i => i.Name == "Invoice 1");
        var invoice2 = response.Invoices.FirstOrDefault(i => i.Name == "Invoice 2");

        invoice1.ShouldNotBeNull();
        ((decimal)invoice1.Amount).ShouldBe(100.00m);
        invoice1.Currency.ShouldBe("USD");
        invoice1.Status.ShouldBe("Draft");

        invoice2.ShouldNotBeNull();
        ((decimal)invoice2.Amount).ShouldBe(200.00m);
        invoice2.Currency.ShouldBe("EUR");
        invoice2.Status.ShouldBe("Draft");
    }

    [Fact]
    public async Task GetInvoices_WithLimitAndOffset_ShouldReturnPaginatedResults()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange - Create multiple invoices
        for (var i = 1; i <= 5; i++)
        {
            await _client.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                Name = $"Invoice {i}",
                Amount = i * 50m,
                Currency = "USD"
            }, cancellationToken: TestContext.Current.CancellationToken);
        }

        var request = new GetInvoicesRequest
        {
            Limit = 2,
            Offset = 1
        };

        // Act
        var response = await _client.GetInvoicesAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Invoices.Count.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetInvoices_WithStatusFilter_ShouldReturnFilteredResults()
    {
        var dataSource = Fixture.Services.GetRequiredService<DbDataSource>();
        var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync("TRUNCATE TABLE app_domain.invoices;");

        // Arrange - Create invoices with different statuses
        var draftInvoice = await _client.CreateInvoiceAsync(new CreateInvoiceRequest
        {
            Name = "Draft Invoice",
            Amount = 100m,
            Currency = "USD"
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Cancel one invoice to change its status
        await _client.CancelInvoiceAsync(new CancelInvoiceRequest
        {
            InvoiceId = draftInvoice.InvoiceId,
            Version = draftInvoice.Version
        }, cancellationToken: TestContext.Current.CancellationToken);

        var request = new GetInvoicesRequest
        {
            Limit = 10,
            Offset = 0,
            Status = "Cancelled"
        };

        // Act
        var response = await _client.GetInvoicesAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Invoices.Count.ShouldBeGreaterThanOrEqualTo(1);
        response.Invoices.All(i => i.Status == "Cancelled").ShouldBeTrue();
    }
}
