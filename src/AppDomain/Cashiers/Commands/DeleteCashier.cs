// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.IntegrationEvents;
using AppDomain.Core.Data;
using FluentValidation.Results;
using LinqToDB;

namespace AppDomain.Cashiers.Commands;

public record DeleteCashierCommand(Guid TenantId, Guid CashierId) : ICommand<Result<bool>>;

public class DeleteCashierValidator : AbstractValidator<DeleteCashierCommand>
{
    public DeleteCashierValidator()
    {
        RuleFor(c => c.CashierId).NotEmpty();
    }
}

public static class DeleteCashierCommandHandler
{
    public record DbCommand(Guid TenantId, Guid CashierId) : ICommand<int>;

    public static async Task<(Result<bool>, CashierDeleted?)> Handle(DeleteCashierCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(command.TenantId, command.CashierId);
        var rowsAffected = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (rowsAffected == 0)
        {
            var failures = new List<ValidationFailure> { new("CashierId", "Cashier not found") };

            return (failures, null);
        }

        var deletedEvent = new CashierDeleted(command.TenantId, command.CashierId);

        return (true, deletedEvent);
    }

    public static async Task<int> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        return await db.Cashiers
            .Where(c => c.TenantId == command.TenantId && c.CashierId == command.CashierId)
            .DeleteAsync(cancellationToken);
    }
}
