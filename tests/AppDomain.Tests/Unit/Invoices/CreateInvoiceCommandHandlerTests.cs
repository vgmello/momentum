// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using NSubstitute;
using Momentum.Extensions.Messaging;
using Wolverine;

namespace AppDomain.Tests.Unit.Invoices;

public class CreateInvoiceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateInvoiceAndReturnResult()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        var cashierId = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddDays(30);

        messagingMock.InvokeCommandAsync(Arg.Any<CreateInvoiceCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(x => new AppDomain.Invoices.Data.Entities.Invoice
            {
                TenantId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.TenantId,
                InvoiceId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.InvoiceId,
                Name = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Name,
                Status = "Draft",
                Amount = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Amount,
                Currency = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Currency,
                DueDate = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.DueDate,
                CashierId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.CashierId,
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow,
                Version = 1 // Initial version for new entity
            });

        var tenantId = Guid.NewGuid();
        var command = new CreateInvoiceCommand(tenantId, "Test Invoice", 100.50m, "USD", dueDate, cashierId);

        // Act
        var (result, integrationEvent) = await CreateInvoiceCommandHandler.Handle(command, messagingMock, CancellationToken.None);

        // Assert
        var invoice = result.Match(success => success, _ => null!);

        invoice.ShouldNotBeNull();
        invoice.Name.ShouldBe("Test Invoice");
        invoice.Status.ShouldBe("Draft");
        invoice.Amount.ShouldBe(100.50m);
        invoice.Currency.ShouldBe("USD");
        invoice.DueDate.ShouldBe(dueDate);
        invoice.CashierId.ShouldBe(cashierId);
        invoice.InvoiceId.ShouldNotBe(Guid.Empty);
        invoice.Version.ShouldBe(1);

        // Verify integration event
        integrationEvent.ShouldNotBeNull();
        integrationEvent.ShouldBeOfType<InvoiceCreated>();
        integrationEvent.Invoice.InvoiceId.ShouldBe(invoice.InvoiceId);
        integrationEvent.Invoice.Name.ShouldBe(invoice.Name);

        // Verify that messaging was called with correct parameters
        await messagingMock.Received(1).InvokeCommandAsync(
            Arg.Is<CreateInvoiceCommandHandler.DbCommand>(cmd =>
                cmd.Invoice.TenantId == tenantId &&
                cmd.Invoice.InvoiceId == invoice.InvoiceId &&
                cmd.Invoice.Name == "Test Invoice" &&
                cmd.Invoice.Status == "Draft" &&
                cmd.Invoice.Amount == 100.50m &&
                cmd.Invoice.Currency == "USD" &&
                cmd.Invoice.DueDate == dueDate &&
                cmd.Invoice.CashierId == cashierId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDefaults_ShouldUseDefaultValues()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeCommandAsync(Arg.Any<CreateInvoiceCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(x => new AppDomain.Invoices.Data.Entities.Invoice
            {
                TenantId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.TenantId,
                InvoiceId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.InvoiceId,
                Name = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Name,
                Status = "Draft",
                Amount = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Amount,
                Currency = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Currency,
                DueDate = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.DueDate,
                CashierId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.CashierId,
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow,
                Version = 1 // Initial version for new entity
            });

        var tenantId = Guid.NewGuid();
        var command = new CreateInvoiceCommand(tenantId, "Test Invoice", 50.00m);

        // Act
        var handlerResult = await CreateInvoiceCommandHandler.Handle(command, messagingMock, CancellationToken.None);
        var result = handlerResult.Item1;

        // Assert
        var invoice = result.Match(success => success, _ => null!);

        invoice.Currency.ShouldBe("");
        invoice.DueDate.ShouldBeNull();
        invoice.CashierId.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueGuidForEachCall()
    {
        // Arrange
        var messagingMock = Substitute.For<IMessageBus>();
        messagingMock.InvokeCommandAsync(Arg.Any<CreateInvoiceCommandHandler.DbCommand>(), Arg.Any<CancellationToken>())
            .Returns(x => new AppDomain.Invoices.Data.Entities.Invoice
            {
                TenantId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.TenantId,
                InvoiceId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.InvoiceId,
                Name = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Name,
                Status = "Draft",
                Amount = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Amount,
                Currency = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.Currency,
                DueDate = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.DueDate,
                CashierId = ((CreateInvoiceCommandHandler.DbCommand)x[0]).Invoice.CashierId,
                CreatedDateUtc = DateTime.UtcNow,
                UpdatedDateUtc = DateTime.UtcNow,
                Version = 1 // Initial version for new entity
            });

        var tenantId = Guid.NewGuid();
        var command1 = new CreateInvoiceCommand(tenantId, "Invoice 1", 100.00m);
        var command2 = new CreateInvoiceCommand(tenantId, "Invoice 2", 200.00m);

        // Act
        var handlerResult1 = await CreateInvoiceCommandHandler.Handle(command1, messagingMock, CancellationToken.None);
        var handlerResult2 = await CreateInvoiceCommandHandler.Handle(command2, messagingMock, CancellationToken.None);

        // Assert
        var invoice1 = handlerResult1.Item1.Match(success => success, _ => null!);
        var invoice2 = handlerResult2.Item1.Match(success => success, _ => null!);

        invoice1.InvoiceId.ShouldNotBe(invoice2.InvoiceId);
    }
}
