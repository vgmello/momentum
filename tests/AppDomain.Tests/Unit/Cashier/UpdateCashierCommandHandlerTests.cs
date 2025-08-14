// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Contracts.IntegrationEvents;
using NSubstitute;
using Momentum.Extensions.Messaging;
using Wolverine;
using CashierEntity = AppDomain.Cashiers.Data.Entities.Cashier;

namespace AppDomain.Tests.Unit.Cashier;

public class UpdateCashierCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCashier_ShouldUpdateAndReturnResult()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        messagingMock.InvokeCommandAsync(Arg.Any<UpdateCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(x => new CashierEntity
            {
                TenantId = ((UpdateCashierCommandHandler.DbCommand)x[0]).TenantId,
                CashierId = ((UpdateCashierCommandHandler.DbCommand)x[0]).CashierId,
                Name = ((UpdateCashierCommandHandler.DbCommand)x[0]).Name,
                Email = ((UpdateCashierCommandHandler.DbCommand)x[0]).Email,
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow,
                Version = 2 // Version should be incremented after update
            });

        var command = new UpdateCashierCommand(tenantId, cashierId, "Updated Name", "updated@example.com", Version: 1);

        // Act
        var (result, integrationEvent) = await UpdateCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var cashier = result.Match(success => success, _ => null!);

        cashier.TenantId.ShouldBe(tenantId);
        cashier.CashierId.ShouldBe(cashierId);
        cashier.Name.ShouldBe("Updated Name");
        cashier.Email.ShouldBe("updated@example.com");

        // Verify integration event
        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<CashierUpdated>();
        integrationEvent.TenantId.ShouldBe(tenantId);
        integrationEvent.CashierId.ShouldBe(cashierId);

        // Verify that messaging was called with correct parameters
        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<UpdateCashierCommandHandler.DbCommand>(cmd =>
                cmd.TenantId == tenantId &&
                cmd.CashierId == cashierId &&
                cmd.Name == "Updated Name" &&
                cmd.Email == "updated@example.com" &&
                cmd.Version == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentCashier_ShouldReturnValidationFailure()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        messagingMock.InvokeCommandAsync(Arg.Any<UpdateCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns((CashierEntity?)null);

        var command = new UpdateCashierCommand(tenantId, cashierId, "Updated Name", "updated@example.com", Version: 1);

        // Act
        var (result, integrationEvent) = await UpdateCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var failures = result.Match(_ => null!, validationFailures => validationFailures
        );

        failures.ShouldNotBeNull();
        failures.Count.ShouldBe(1);
        failures[0].PropertyName.ShouldBe("CashierId");
        failures[0].ErrorMessage.ShouldBe("Cashier not found");

        integrationEvent.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithNullEmail_ShouldReturnNotUpdatedForEmail()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        messagingMock.InvokeCommandAsync(Arg.Any<UpdateCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(x => new CashierEntity
            {
                TenantId = ((UpdateCashierCommandHandler.DbCommand)x[0]).TenantId,
                CashierId = ((UpdateCashierCommandHandler.DbCommand)x[0]).CashierId,
                Name = ((UpdateCashierCommandHandler.DbCommand)x[0]).Name,
                Email = "Not Updated", // When email is null in command, it should remain unchanged (original value)
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow,
                Version = 2 // Version should be incremented after update
            });

        var command = new UpdateCashierCommand(tenantId, cashierId, "Updated Name", null, Version: 1);

        // Act
        var handlerResult = await UpdateCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None);
        var result = handlerResult.Item1;

        // Assert
        var cashier = result.Match(success => success, _ => null!);

        cashier.Email.ShouldBe("Not Updated");
    }

    [Fact]
    public async Task Handle_WithVersionConflict_ShouldReturnValidationFailure()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        // Mock the scenario where the cashier exists but version doesn't match
        // This simulates the optimistic concurrency check failing
        messagingMock.InvokeCommandAsync(Arg.Any<UpdateCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns((CashierEntity?)null); // Simulating version conflict - no rows updated due to version mismatch

        var command = new UpdateCashierCommand(tenantId, cashierId, "Updated Name", "updated@example.com",
            Version: 99); // Using obviously wrong version

        // Act
        var (result, integrationEvent) = await UpdateCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var failures = result.Match(_ => null!, validationFailures => validationFailures);

        failures.ShouldNotBeNull();
        failures.Count.ShouldBe(1);
        failures[0].PropertyName.ShouldBe("CashierId");
        failures[0].ErrorMessage.ShouldBe("Cashier not found"); // The error message is generic for both non-existent and version conflict

        integrationEvent.ShouldBeNull();

        // Verify the specific version was passed to the DB command
        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<UpdateCashierCommandHandler.DbCommand>(cmd => cmd.Version == 99),
            Arg.Any<CancellationToken>());
    }
}
