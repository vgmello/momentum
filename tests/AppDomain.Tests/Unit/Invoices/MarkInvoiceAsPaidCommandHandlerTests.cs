// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using NSubstitute;
using Momentum.Extensions.Messaging;
using Wolverine;

namespace AppDomain.Tests.Unit.Invoices;

public class MarkInvoiceAsPaidCommandHandlerTests
{
    [Fact(Skip = "Not ready yet")]
    public async Task Handle_WithValidInvoice_ShouldMarkAsPaidAndReturnResult()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var amountPaid = 150.75m;
        var paymentDate = DateTime.UtcNow;

        var mockInvoice = new AppDomain.Invoices.Data.Entities.Invoice
        {
            TenantId = tenantId,
            InvoiceId = invoiceId,
            Name = "Test Invoice",
            Status = "Paid",
            Amount = amountPaid,
            Currency = "USD",
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        };

        messagingMock.InvokeCommandAsync(Arg.Any<MarkInvoiceAsPaidCommandHandler.MarkInvoiceAsPaidDbCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(mockInvoice);

        var command = new MarkInvoiceAsPaidCommand(tenantId, invoiceId, 1, amountPaid, paymentDate);

        // Act
        var (result, integrationEvent) = await MarkInvoiceAsPaidCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var invoice = result.Match(success => success, _ => null!);

        invoice.InvoiceId.ShouldBe(invoiceId);
        invoice.Status.ShouldBe("Paid");
        invoice.Amount.ShouldBe(amountPaid);

        // Verify integration event
        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<InvoicePaid>();
        integrationEvent.Invoice.InvoiceId.ShouldBe(invoiceId);
        integrationEvent.Invoice.AmountPaid.ShouldBe(amountPaid);
        integrationEvent.Invoice.PaymentDate.ShouldBe(paymentDate);

        // Verify that messaging was called with correct parameters
        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<MarkInvoiceAsPaidCommandHandler.MarkInvoiceAsPaidDbCommand>(cmd =>
                cmd.InvoiceId == invoiceId &&
                cmd.Version == 1 &&
                cmd.AmountPaid == amountPaid &&
                cmd.PaymentDate == paymentDate),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Not ready yet")]
    public async Task Handle_WithNullPaymentDate_ShouldUseUtcNow()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        const decimal amountPaid = 100.00m;

        var mockInvoice = new AppDomain.Invoices.Data.Entities.Invoice
        {
            TenantId = tenantId,
            InvoiceId = invoiceId,
            Name = "Test Invoice",
            Status = "Paid",
            Amount = amountPaid,
            Currency = "USD",
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        };

        messagingMock.InvokeCommandAsync(
                Arg.Any<MarkInvoiceAsPaidCommandHandler.MarkInvoiceAsPaidDbCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(mockInvoice);

        var command = new MarkInvoiceAsPaidCommand(tenantId, invoiceId, 1, amountPaid);

        // Act
        var handlerResult = await MarkInvoiceAsPaidCommandHandler.Handle(command, messagingMock, CancellationToken.None);
        var integrationEvent = handlerResult.Item2;

        // Assert
        integrationEvent.ShouldNotBeNull();
        integrationEvent.Invoice.PaymentDate!.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task Handle_WithNonExistentInvoice_ShouldReturnValidationFailure()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        messagingMock.InvokeCommandAsync(Arg.Any<MarkInvoiceAsPaidCommandHandler.MarkInvoiceAsPaidDbCommand>(),
                Arg.Any<CancellationToken>())
            .Returns((AppDomain.Invoices.Data.Entities.Invoice?)null); // Simulate not found

        var command = new MarkInvoiceAsPaidCommand(tenantId, invoiceId, 1, 100.00m);

        // Act
        var (result, integrationEvent) = await MarkInvoiceAsPaidCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var failures = result.Match(_ => null!, validationFailures => validationFailures);

        failures.ShouldNotBeNull();
        failures.Count.ShouldBe(1);
        failures[0].PropertyName.ShouldBe("Version");
        failures[0].ErrorMessage
            .ShouldBe("Invoice not found, already paid, or was modified by another user. Please refresh and try again.");

        integrationEvent.ShouldBeNull();
    }
}
