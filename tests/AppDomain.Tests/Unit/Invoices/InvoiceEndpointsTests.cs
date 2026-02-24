// Copyright (c) OrgName. All rights reserved.

#pragma warning disable xUnit1051 // NSubstitute argument matchers require Arg.Any<CancellationToken>()

using System.Net;
using AppDomain.Api.Invoices;
using AppDomain.Api.Invoices.Models;
using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Queries;
using AppDomain.Tests.Unit.Common;
using FluentValidation.Results;
using Momentum.Extensions;
namespace AppDomain.Tests.Unit.Invoices;

public class InvoiceEndpointsTests : EndpointTest
{
    public InvoiceEndpointsTests()
    {
        ConfigureApp(app => app.MapInvoiceEndpoints());
    }

    // -- Helpers --

    private static Invoice CreateSampleInvoice(
        Guid? invoiceId = null,
        InvoiceStatus status = InvoiceStatus.Draft,
        decimal amount = 250.00m,
        int version = 1)
    {
        return new Invoice(
            TenantId: FakeTenantId,
            InvoiceId: invoiceId ?? Guid.NewGuid(),
            Name: "Office Supplies Q1",
            Status: status,
            Amount: amount,
            Currency: "USD",
            DueDate: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            CashierId: Guid.NewGuid(),
            AmountPaid: null,
            PaymentDate: null,
            CreatedDateUtc: DateTime.UtcNow,
            UpdatedDateUtc: DateTime.UtcNow,
            Version: version);
    }

    // -- GetInvoices --

    [Fact]
    public async Task GetInvoices_ReturnsOkWithInvoiceList()
    {
        // Arrange
        var invoices = new List<Invoice> { CreateSampleInvoice(), CreateSampleInvoice() };

        MockBus.InvokeAsync<IEnumerable<Invoice>>(Arg.Any<GetInvoicesQuery>(), Arg.Any<CancellationToken>())
            .Returns(invoices);

        // Act
        var response = await Client.GetAsync("/invoices?limit=50&offset=0");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<Invoice>>();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetInvoices_WithPagination_PassesParametersToQuery()
    {
        // Arrange
        MockBus.InvokeAsync<IEnumerable<Invoice>>(Arg.Any<GetInvoicesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Invoice>());

        // Act
        var response = await Client.GetAsync("/invoices?limit=25&offset=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await MockBus.Received(1).InvokeAsync<IEnumerable<Invoice>>(
            Arg.Is<GetInvoicesQuery>(q => q.TenantId == FakeTenantId && q.Limit == 25 && q.Offset == 10),
            Arg.Any<CancellationToken>());
    }

    // -- GetInvoice --

    [Fact]
    public async Task GetInvoice_WhenFound_ReturnsOkWithInvoice()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateSampleInvoice(invoiceId: invoiceId);
        Result<Invoice> successResult = invoice;

        MockBus.InvokeAsync<Result<Invoice>>(
                Arg.Is<GetInvoiceQuery>(q => q.Id == invoiceId && q.TenantId == FakeTenantId),
                Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        var response = await Client.GetAsync($"/invoices/{invoiceId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Invoice>();
        result.ShouldNotBeNull();
        result.InvoiceId.ShouldBe(invoiceId);
    }

    [Fact]
    public async Task GetInvoice_WhenNotFound_Returns404Problem()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        Result<Invoice> errorResult = new List<ValidationFailure>
        {
            new("InvoiceId", "Invoice not found.")
        };

        MockBus.InvokeAsync<Result<Invoice>>(Arg.Any<GetInvoiceQuery>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        // Act
        var response = await Client.GetAsync($"/invoices/{invoiceId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -- CreateInvoice --

    [Fact]
    public async Task CreateInvoice_WithValidRequest_Returns201Created()
    {
        // Arrange
        var invoice = CreateSampleInvoice();
        Result<Invoice> successResult = invoice;

        MockBus.InvokeAsync<Result<Invoice>>(Arg.Any<CreateInvoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        var request = new CreateInvoiceRequest("Office Supplies Q1", 250.00m, "USD",
            new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc), null);

        // Act
        var response = await Client.PostAsJsonAsync("/invoices", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldContain($"/invoices/{invoice.InvoiceId}");

        var result = await response.Content.ReadFromJsonAsync<Invoice>();
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Office Supplies Q1");
    }

    [Fact]
    public async Task CreateInvoice_WithValidationErrors_ReturnsValidationProblem()
    {
        // Arrange
        Result<Invoice> errorResult = new List<ValidationFailure>
        {
            new("Name", "Name is required."),
            new("Amount", "Amount must be greater than zero.")
        };

        MockBus.InvokeAsync<Result<Invoice>>(Arg.Any<CreateInvoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var request = new CreateInvoiceRequest("", 0m);

        // Act
        var response = await Client.PostAsJsonAsync("/invoices", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // -- CancelInvoice --

    [Fact]
    public async Task CancelInvoice_WhenSuccessful_ReturnsOkWithInvoice()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var cancelledInvoice = CreateSampleInvoice(invoiceId: invoiceId, status: InvoiceStatus.Cancelled, version: 2);
        Result<Invoice> successResult = cancelledInvoice;

        MockBus.InvokeAsync<Result<Invoice>>(
                Arg.Is<CancelInvoiceCommand>(c => c.InvoiceId == invoiceId && c.Version == 1),
                Arg.Any<CancellationToken>())
            .Returns(successResult);

        var request = new CancelInvoiceRequest(Version: 1);

        // Act
        var response = await Client.PutAsJsonAsync($"/invoices/{invoiceId}/cancel", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Invoice>();
        result.ShouldNotBeNull();
        result.Status.ShouldBe(InvoiceStatus.Cancelled);
    }

    [Fact]
    public async Task CancelInvoice_WithConcurrencyConflict_Returns409Conflict()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        Result<Invoice> errorResult = new List<ValidationFailure>
        {
            new("Version", "The invoice has been modified by another user. Please refresh and try again.")
        };

        MockBus.InvokeAsync<Result<Invoice>>(Arg.Any<CancelInvoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var request = new CancelInvoiceRequest(Version: 1);

        // Act
        var response = await Client.PutAsJsonAsync($"/invoices/{invoiceId}/cancel", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelInvoice_WithValidationErrors_ReturnsValidationProblem()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        Result<Invoice> errorResult = new List<ValidationFailure>
        {
            new("InvoiceId", "Invoice not found.")
        };

        MockBus.InvokeAsync<Result<Invoice>>(Arg.Any<CancelInvoiceCommand>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var request = new CancelInvoiceRequest(Version: 1);

        // Act
        var response = await Client.PutAsJsonAsync($"/invoices/{invoiceId}/cancel", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // -- MarkInvoiceAsPaid --

    [Fact]
    public async Task MarkInvoiceAsPaid_WhenSuccessful_ReturnsOkWithInvoice()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var paymentDate = new DateTime(2026, 3, 1, 14, 30, 0, DateTimeKind.Utc);
        var paidInvoice = CreateSampleInvoice(invoiceId: invoiceId, status: InvoiceStatus.Paid, version: 2);
        Result<Invoice> successResult = paidInvoice;

        MockBus.InvokeAsync<Result<Invoice>>(
                Arg.Is<MarkInvoiceAsPaidCommand>(c =>
                    c.InvoiceId == invoiceId && c.AmountPaid == 250.00m && c.PaymentDate == paymentDate),
                Arg.Any<CancellationToken>())
            .Returns(successResult);

        var request = new MarkInvoiceAsPaidRequest(Version: 1, AmountPaid: 250.00m, PaymentDate: paymentDate);

        // Act
        var response = await Client.PutAsJsonAsync($"/invoices/{invoiceId}/mark-paid", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Invoice>();
        result.ShouldNotBeNull();
        result.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task MarkInvoiceAsPaid_WithConcurrencyConflict_Returns409Conflict()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        Result<Invoice> errorResult = new List<ValidationFailure>
        {
            new("Version", "The invoice has been modified by another user. Please refresh and try again.")
        };

        MockBus.InvokeAsync<Result<Invoice>>(Arg.Any<MarkInvoiceAsPaidCommand>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var request = new MarkInvoiceAsPaidRequest(Version: 1, AmountPaid: 250.00m);

        // Act
        var response = await Client.PutAsJsonAsync($"/invoices/{invoiceId}/mark-paid", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // -- SimulatePayment --

    [Fact]
    public async Task SimulatePayment_WhenSuccessful_ReturnsOkWithMessage()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        Result<bool> successResult = true;

        MockBus.InvokeAsync<Result<bool>>(
                Arg.Is<SimulatePaymentCommand>(c =>
                    c.InvoiceId == invoiceId && c.Amount == 100.00m && c.Currency == "EUR"),
                Arg.Any<CancellationToken>())
            .Returns(successResult);

        var request = new SimulatePaymentRequest(Version: 1, Amount: 100.00m, Currency: "EUR",
            PaymentMethod: "Wire Transfer", PaymentReference: "SIM-TEST-001");

        // Act
        var response = await Client.PostAsJsonAsync($"/invoices/{invoiceId}/simulate-payment", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Payment simulation triggered successfully");
    }

    [Fact]
    public async Task SimulatePayment_WithConcurrencyConflict_Returns409Conflict()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        Result<bool> errorResult = new List<ValidationFailure>
        {
            new("Version", "The invoice has been modified by another user. Please refresh and try again.")
        };

        MockBus.InvokeAsync<Result<bool>>(Arg.Any<SimulatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var request = new SimulatePaymentRequest(Version: 1, Amount: 100.00m);

        // Act
        var response = await Client.PostAsJsonAsync($"/invoices/{invoiceId}/simulate-payment", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SimulatePayment_WithValidationErrors_ReturnsValidationProblem()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        Result<bool> errorResult = new List<ValidationFailure>
        {
            new("InvoiceId", "Invoice not found.")
        };

        MockBus.InvokeAsync<Result<bool>>(Arg.Any<SimulatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var request = new SimulatePaymentRequest(Version: 1, Amount: 100.00m);

        // Act
        var response = await Client.PostAsJsonAsync($"/invoices/{invoiceId}/simulate-payment", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
