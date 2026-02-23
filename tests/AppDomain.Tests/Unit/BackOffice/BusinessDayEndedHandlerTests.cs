// Copyright (c) OrgName. All rights reserved.

#pragma warning disable xUnit1051

using AppDomain.BackOffice.Messaging.AccountingInboxHandler;
using AppDomain.Invoices.Contracts.DomainEvents;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using Wolverine;

namespace AppDomain.Tests.Unit.BackOffice;

public class BusinessDayEndedHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPublishInvoiceGeneratedAndInvoiceFinalizedEvents()
    {
        // Arrange
        var messageBus = Substitute.For<IMessageBus>();
        var logger = Substitute.For<ILogger<BusinessDayEndedHandler>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var capturedGenerated = new List<InvoiceGenerated>();
        var capturedFinalized = new List<InvoiceFinalized>();

        messageBus.When(x => x.PublishAsync(Arg.Any<InvoiceGenerated>(), Arg.Any<DeliveryOptions?>()))
            .Do(ci => capturedGenerated.Add(ci.ArgAt<InvoiceGenerated>(0)));

        messageBus.When(x => x.PublishAsync(Arg.Any<InvoiceFinalized>(), Arg.Any<DeliveryOptions?>()))
            .Do(ci => capturedFinalized.Add(ci.ArgAt<InvoiceFinalized>(0)));

        var handler = new BusinessDayEndedHandler(logger, messageBus);
        var @event = new BusinessDayEnded(
            new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc), "US", "East");

        // Act
        await handler.Handle(@event);

        // Assert
        capturedGenerated.Count.ShouldBe(1);
        var generated = capturedGenerated[0];
        generated.Invoice.Amount.ShouldBe(500.75m);
        generated.Invoice.Currency.ShouldBe("USD");
        generated.Invoice.Status.ShouldBe(InvoiceStatus.Draft);
        generated.Invoice.Name.ShouldContain("2026-02-20");

        capturedFinalized.Count.ShouldBe(1);
        var finalized = capturedFinalized[0];
        finalized.FinalTotalAmount.ShouldBe(500.75m);
        finalized.PublicInvoiceNumber.ShouldStartWith("INV-20260220-");
    }

    [Fact]
    public async Task Handle_WhenLoggingDisabled_ShouldStillPublishEvents()
    {
        // Arrange
        var messageBus = Substitute.For<IMessageBus>();
        var logger = Substitute.For<ILogger<BusinessDayEndedHandler>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(false);

        var publishCount = 0;
        messageBus.When(x => x.PublishAsync(Arg.Any<InvoiceGenerated>(), Arg.Any<DeliveryOptions?>()))
            .Do(_ => publishCount++);
        messageBus.When(x => x.PublishAsync(Arg.Any<InvoiceFinalized>(), Arg.Any<DeliveryOptions?>()))
            .Do(_ => publishCount++);

        var handler = new BusinessDayEndedHandler(logger, messageBus);
        var @event = new BusinessDayEnded(
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), "EU", "West");

        // Act
        await handler.Handle(@event);

        // Assert
        publishCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueIdsAcrossCalls()
    {
        // Arrange
        var messageBus = Substitute.For<IMessageBus>();
        var logger = Substitute.For<ILogger<BusinessDayEndedHandler>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(false);

        var capturedEvents = new List<InvoiceGenerated>();
        messageBus.When(x => x.PublishAsync(Arg.Any<InvoiceGenerated>(), Arg.Any<DeliveryOptions?>()))
            .Do(ci => capturedEvents.Add(ci.ArgAt<InvoiceGenerated>(0)));

        var handler = new BusinessDayEndedHandler(logger, messageBus);
        var @event = new BusinessDayEnded(DateTime.UtcNow, "US", "East");

        // Act
        await handler.Handle(@event);
        await handler.Handle(@event);

        // Assert
        capturedEvents.Count.ShouldBe(2);
        capturedEvents[0].TenantId.ShouldNotBe(capturedEvents[1].TenantId);
    }
}
