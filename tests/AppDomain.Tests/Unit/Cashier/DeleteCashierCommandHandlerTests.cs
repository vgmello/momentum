<!--#if (includeSample)-->
// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Contracts.IntegrationEvents;
using NSubstitute;
using Momentum.Extensions.Messaging;
using Wolverine;

namespace AppDomain.Tests.Unit.Cashier;

public class DeleteCashierCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCashier_ShouldDeleteAndReturnResult()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        messagingMock.InvokeCommandAsync(Arg.Any<DeleteCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var command = new DeleteCashierCommand(tenantId, cashierId);

        // Act
        var (result, integrationEvent) = await DeleteCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var success = result.Match(value => value, _ => false);

        success.ShouldBeTrue();

        // Verify integration event
        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<CashierDeleted>();
        integrationEvent.TenantId.ShouldBe(tenantId);
        integrationEvent.CashierId.ShouldBe(cashierId);

        // Verify that messaging was called with correct parameters
        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<DeleteCashierCommandHandler.DbCommand>(cmd =>
                cmd.TenantId == tenantId &&
                cmd.CashierId == cashierId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentCashier_ShouldReturnValidationFailure()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        messagingMock.InvokeCommandAsync(Arg.Any<DeleteCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(0); // Simulating non-existent cashier

        var command = new DeleteCashierCommand(tenantId, cashierId);

        // Act
        var (result, integrationEvent) = await DeleteCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var failures = result.Match(_ => null!, validationFailures => validationFailures);

        failures.ShouldNotBeNull();
        failures.Count.ShouldBe(1);
        failures[0].PropertyName.ShouldBe("CashierId");
        failures[0].ErrorMessage.ShouldBe("Cashier not found");

        integrationEvent.ShouldBeNull();
    }
}
<!--#endif-->
