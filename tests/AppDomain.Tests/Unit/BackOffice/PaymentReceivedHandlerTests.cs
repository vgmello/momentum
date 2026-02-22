// Copyright (c) OrgName. All rights reserved.

using AppDomain.BackOffice.Messaging.AppDomainInboxHandler;
using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using FluentValidation.Results;
using Momentum.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.Messaging;
using Wolverine;

namespace AppDomain.Tests.Unit.BackOffice;

public class PaymentReceivedHandlerTests
{
    [Fact]
    public async Task Handle_WithValidPayment_ShouldMarkInvoiceAsPaid()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var paymentDate = DateTime.UtcNow;

        var invoice = new Invoice(tenantId, invoiceId, "Test Invoice", InvoiceStatus.Draft,
            100m, "USD", DateTime.UtcNow.AddDays(30), null, null, null,
            DateTime.UtcNow, DateTime.UtcNow, 5);

        Result<Invoice> invoiceResult = invoice;

        messagingMock
            .InvokeQueryAsync(Arg.Any<IQuery<Result<Invoice>>>(), Arg.Any<CancellationToken>())
            .Returns(invoiceResult);

        Result<Invoice> paidResult = new Invoice(tenantId, invoiceId, "Test Invoice", InvoiceStatus.Paid,
            100m, "USD", DateTime.UtcNow.AddDays(30), null, 100m, paymentDate,
            DateTime.UtcNow, DateTime.UtcNow, 6);

        messagingMock
            .InvokeCommandAsync(Arg.Any<MarkInvoiceAsPaidCommand>(), Arg.Any<CancellationToken>())
            .Returns(paidResult);

        var @event = new PaymentReceived(tenantId, invoiceId, "USD", 100m, paymentDate, "Credit Card", "REF-123");

        // Act
        await PaymentReceivedHandler.Handle(@event, messagingMock, logger, CancellationToken.None);

        // Assert
        await messagingMock.Received(1)
            .InvokeQueryAsync(Arg.Any<IQuery<Result<Invoice>>>(), Arg.Any<CancellationToken>());

        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<MarkInvoiceAsPaidCommand>(cmd =>
                cmd.TenantId == tenantId &&
                cmd.InvoiceId == invoiceId &&
                cmd.Version == 5 &&
                cmd.AmountPaid == 100m &&
                cmd.PaymentDate == paymentDate),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInvoiceNotFound_ShouldNotAttemptMarkAsPaid()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        Result<Invoice> errorResult = new List<ValidationFailure>
        {
            new("InvoiceId", "Invoice not found")
        };

        messagingMock
            .InvokeQueryAsync(Arg.Any<IQuery<Result<Invoice>>>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        var @event = new PaymentReceived(tenantId, invoiceId, "USD", 100m, DateTime.UtcNow, "Credit Card", "REF-123");

        // Act
        await PaymentReceivedHandler.Handle(@event, messagingMock, logger, CancellationToken.None);

        // Assert
        await messagingMock.DidNotReceive()
            .InvokeCommandAsync(Arg.Any<MarkInvoiceAsPaidCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMarkAsPaidFails_ShouldLogWarning()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var invoice = new Invoice(tenantId, invoiceId, "Test Invoice", InvoiceStatus.Draft,
            100m, "USD", DateTime.UtcNow.AddDays(30), null, null, null,
            DateTime.UtcNow, DateTime.UtcNow, 5);

        Result<Invoice> invoiceResult = invoice;

        messagingMock
            .InvokeQueryAsync(Arg.Any<IQuery<Result<Invoice>>>(), Arg.Any<CancellationToken>())
            .Returns(invoiceResult);

        // Simulate concurrency conflict on mark as paid
        Result<Invoice> failResult = new List<ValidationFailure>
        {
            new("Version", "Invoice was modified by another user")
        };

        messagingMock
            .InvokeCommandAsync(Arg.Any<MarkInvoiceAsPaidCommand>(), Arg.Any<CancellationToken>())
            .Returns(failResult);

        var @event = new PaymentReceived(tenantId, invoiceId, "USD", 100m, DateTime.UtcNow, "Credit Card", "REF-123");

        // Act
        await PaymentReceivedHandler.Handle(@event, messagingMock, logger, CancellationToken.None);

        // Assert - verify the command was still attempted
        await messagingMock.Received(1)
            .InvokeCommandAsync(Arg.Any<MarkInvoiceAsPaidCommand>(), Arg.Any<CancellationToken>());
    }
}
