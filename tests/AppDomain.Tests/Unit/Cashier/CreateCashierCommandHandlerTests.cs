< !--#if (INCLUDE_SAMPLE)-->
// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Contracts.IntegrationEvents;
using NSubstitute;
using Momentum.Extensions.Messaging;
using Wolverine;

namespace AppDomain.Tests.Unit.Cashier;

public class CreateCashierCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateCashierAndReturnResult()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();

        messagingMock.InvokeCommandAsync(Arg.Any<CreateCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(x => new AppDomain.Cashiers.Data.Entities.Cashier
            {
                TenantId = ((CreateCashierCommandHandler.DbCommand)x[0]).Cashier.TenantId,
                CashierId = ((CreateCashierCommandHandler.DbCommand)x[0]).Cashier.CashierId,
                Name = ((CreateCashierCommandHandler.DbCommand)x[0]).Cashier.Name,
                Email = ((CreateCashierCommandHandler.DbCommand)x[0]).Cashier.Email,
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow,
                Version = 12345 // Some mock xmin value
            });

        var command = new CreateCashierCommand(Guid.Empty, "John Doe", "john.doe@example.com");

        // Act
        var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var cashier = result.Match(success => success, _ => null!);

        cashier.ShouldNotBeNull();
        cashier.Name.ShouldBe("John Doe");
        cashier.Email.ShouldBe("john.doe@example.com");
        cashier.CashierId.ShouldNotBe(Guid.Empty);

        // Verify integration event
        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<CashierCreated>();
        integrationEvent.Cashier.CashierId.ShouldBe(cashier.CashierId);
        integrationEvent.Cashier.Name.ShouldBe(cashier.Name);
        integrationEvent.Cashier.Email.ShouldBe(cashier.Email);

        // Verify that messaging was called with correct parameters
        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<CreateCashierCommandHandler.DbCommand>(cmd =>
                cmd.Cashier.TenantId == Guid.Empty &&
                cmd.Cashier.CashierId == cashier.CashierId &&
                cmd.Cashier.Name == "John Doe" &&
                cmd.Cashier.Email == "john.doe@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueGuidForEachCall()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeCommandAsync(Arg.Any<CreateCashierCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(x => new AppDomain.Cashiers.Data.Entities.Cashier
            {
                TenantId = ((CreateCashierCommandHandler.DbCommand)x[0]).Cashier.TenantId,
                CashierId = ((CreateCashierCommandHandler.DbCommand)x[0]).Cashier.CashierId,
                Name = ((CreateCashierCommandHandler.DbCommand)x[0]).Cashier.Name,
                Email = ((CreateCashierCommandHandler.DbCommand)x[0]).Cashier.Email,
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow,
                Version = 12345 // Some mock xmin value
            });

        var command1 = new CreateCashierCommand(Guid.Empty, "Cashier 1", "cashier1@test.com");
        var command2 = new CreateCashierCommand(Guid.Empty, "Cashier 2", "cashier2@test.com");

        // Act
        var handlerResult1 = await CreateCashierCommandHandler.Handle(command1, messagingMock, CancellationToken.None);
        var handlerResult2 = await CreateCashierCommandHandler.Handle(command2, messagingMock, CancellationToken.None);

        // Assert
        var cashier1 = handlerResult1.Item1.Match(success => success, _ => null!);
        var cashier2 = handlerResult2.Item1.Match(success => success, _ => null!);

        cashier1.CashierId.ShouldNotBe(cashier2.CashierId);
    }
}
<!--#endif-->
