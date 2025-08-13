< !--#if (INCLUDE_SAMPLE)-->
// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Data.Entities;
using NSubstitute;
using Momentum.Extensions.Messaging;
using Wolverine;

namespace AppDomain.Tests.Unit.Invoices;

public class CancelInvoiceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidInvoice_ShouldCancelAndReturnResult()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var dbInvoice = new Invoice
        {
            TenantId = tenantId,
            InvoiceId = Guid.NewGuid(),
            Status = "Cancelled",
            Name = "Test Invoice",
            Amount = 100.50m,
            Version = 10
        };

        messagingMock
            .InvokeCommandAsync(Arg.Any<CancelInvoiceCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(dbInvoice);

        var command = new CancelInvoiceCommand(tenantId, dbInvoice.InvoiceId, 1);

        // Act
        var (result, integrationEvent) = await CancelInvoiceCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var invoice = result.Match(success => success, _ => null!);

        invoice.InvoiceId.ShouldBe(dbInvoice.InvoiceId);
        invoice.Status.ShouldBe(dbInvoice.Status);
        invoice.Name.ShouldBe(dbInvoice.Name);
        invoice.Amount.ShouldBe(dbInvoice.Amount);
        invoice.Version.ShouldBe(dbInvoice.Version);

        // Verify integration event
        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<InvoiceCancelled>();
        integrationEvent.InvoiceId.ShouldBe(dbInvoice.InvoiceId);

        // Verify that messaging was called with correct parameters
        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<CancelInvoiceCommandHandler.DbCommand>(cmd =>
                cmd.TenantId == tenantId && cmd.InvoiceId == dbInvoice.InvoiceId && cmd.Version == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentInvoice_ShouldReturnValidationFailure()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var invoiceId = Guid.NewGuid();

        messagingMock.InvokeCommandAsync(Arg.Any<CancelInvoiceCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        var tenantId = Guid.NewGuid();
        var command = new CancelInvoiceCommand(tenantId, invoiceId, 1);

        // Act
        var (result, integrationEvent) = await CancelInvoiceCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var failures = result.Match(_ => null!, validationFailures => validationFailures);

        failures.ShouldNotBeNull();
        failures.Count.ShouldBe(1);
        failures[0].PropertyName.ShouldBe("Version");
        failures[0].ErrorMessage
            .ShouldBe("Invoice not found, cannot be cancelled, or was modified by another user. Please refresh and try again.");

        integrationEvent.ShouldBeNull();
    }
}
<!--#endif-->
