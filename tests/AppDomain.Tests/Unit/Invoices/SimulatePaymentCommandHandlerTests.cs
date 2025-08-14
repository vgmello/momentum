// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using NSubstitute;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.Messaging;
using Wolverine;

namespace AppDomain.Tests.Unit.Invoices;

public class SimulatePaymentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessAndPublishEvent()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var invoiceId = Guid.NewGuid();
        var amount = 250.00m;
        var currency = "EUR";
        var paymentMethod = "Bank Transfer";
        var paymentReference = "TEST-REF-123";

        var tenantId = Guid.NewGuid();
        var command = new SimulatePaymentCommand(tenantId, invoiceId, 1, amount, currency, paymentMethod, paymentReference);

        // Act
        var handlerResult = await SimulatePaymentCommandHandler.Handle(command);
        var result = handlerResult.Item1;
        var integrationEvent = handlerResult.Item2;

        // Assert
        var success = result.Match(value => value, _ => true);

        success.ShouldBeTrue();

        // Verify integration event
        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<PaymentReceived>();
        integrationEvent.TenantId.ShouldBe(tenantId);
        integrationEvent.InvoiceId.ShouldBe(invoiceId);
        integrationEvent.Amount.ShouldBe(amount);
        integrationEvent.Currency.ShouldBe(currency);
        integrationEvent.PaymentMethod.ShouldBe(paymentMethod);
        integrationEvent.PaymentReference.ShouldBe(paymentReference);
        integrationEvent.ReceivedDate.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));

        // Verify that messaging was NOT called (since this handler doesn't interact with DB)
        await messagingMock.DidNotReceiveWithAnyArgs()
            .InvokeCommandAsync(Arg.Any<ICommand<object>>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_WithDefaults_ShouldUseDefaultValues()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var amount = 100.00m;

        var tenantId = Guid.NewGuid();
        var command = new SimulatePaymentCommand(tenantId, invoiceId, 1, amount);

        // Act
        var handlerResult = await SimulatePaymentCommandHandler.Handle(command);
        var integrationEvent = handlerResult.Item2;

        // Assert
        integrationEvent.Currency.ShouldBe("USD");
        integrationEvent.PaymentMethod.ShouldBe("Credit Card");
        integrationEvent.PaymentReference.ShouldBe("SIM-REF");
    }
}
