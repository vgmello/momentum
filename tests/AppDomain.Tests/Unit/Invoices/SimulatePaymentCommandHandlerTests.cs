// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.Messaging;
using Wolverine;
using Momentum.Extensions;

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

        messagingMock.InvokeQueryAsync<Result<AppDomain.Invoices.Contracts.Models.Invoice>>(
                Arg.Any<IQuery<Result<AppDomain.Invoices.Contracts.Models.Invoice>>>(), Arg.Any<CancellationToken>())
            .Returns(new AppDomain.Invoices.Contracts.Models.Invoice(tenantId, invoiceId, "Test", "Draft", 100, "USD", DateTime.UtcNow,
                null, null, null, DateTime.UtcNow, DateTime.UtcNow, 1));

        var handlerResult = await SimulatePaymentCommandHandler.Handle(command, messagingMock, CancellationToken.None);
        var result = handlerResult.Item1;
        var integrationEvent = handlerResult.Item2;

        var success = result.Match(value => value, _ => true);

        success.ShouldBeTrue();

        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<PaymentReceived>();
        integrationEvent.TenantId.ShouldBe(tenantId);
        integrationEvent.InvoiceId.ShouldBe(invoiceId);
        integrationEvent.PaymentAmount.ShouldBe(amount);
        integrationEvent.Currency.ShouldBe(currency);
        integrationEvent.PaymentMethod.ShouldBe(paymentMethod);
        integrationEvent.PaymentReference.ShouldBe(paymentReference);
        integrationEvent.PaymentDate.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));

        await messagingMock.Received(1)
            .InvokeQueryAsync<Result<AppDomain.Invoices.Contracts.Models.Invoice>>(
                Arg.Any<IQuery<Result<AppDomain.Invoices.Contracts.Models.Invoice>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDefaults_ShouldUseDefaultValues()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var amount = 100.00m;

        var tenantId = Guid.NewGuid();
        var command = new SimulatePaymentCommand(tenantId, invoiceId, 1, amount);

        var mockMessageBus = Substitute.For<IMessageBus>();
        mockMessageBus
            .InvokeQueryAsync<Result<AppDomain.Invoices.Contracts.Models.Invoice>>(
                Arg.Any<IQuery<Result<AppDomain.Invoices.Contracts.Models.Invoice>>>(), Arg.Any<CancellationToken>())
            .Returns(new AppDomain.Invoices.Contracts.Models.Invoice(tenantId, invoiceId, "Test", "Draft", 100, "USD", DateTime.UtcNow,
                null, null, null, DateTime.UtcNow, DateTime.UtcNow, 1));

        var handlerResult = await SimulatePaymentCommandHandler.Handle(command, mockMessageBus, CancellationToken.None);
        var integrationEvent = handlerResult.Item2;

        integrationEvent!.Currency.ShouldBe("USD");
        integrationEvent.PaymentMethod.ShouldBe("Credit Card");
        integrationEvent.PaymentReference.ShouldBe("SIM-REF");
    }
}
