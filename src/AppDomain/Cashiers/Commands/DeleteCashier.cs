<!--#if (includeSample)-->
// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.IntegrationEvents;
using AppDomain.Core.Data;
using FluentValidation;
using LinqToDB;
using Momentum.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using Wolverine;

namespace AppDomain.Cashiers.Commands;

/// <summary>
/// Command to delete an existing cashier from the system.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="CashierId">Unique identifier for the cashier to delete</param>
public record DeleteCashierCommand(
    Guid TenantId,
    Guid CashierId
) : ICommand<Result>;

/// <summary>
/// Validator for the DeleteCashierCommand.
/// </summary>
public class DeleteCashierValidator : AbstractValidator<DeleteCashierCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCashierValidator"/> class.
    /// </summary>
    public DeleteCashierValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.CashierId).NotEmpty();
    }
}

/// <summary>
/// Handler for the DeleteCashierCommand.
/// </summary>
public static class DeleteCashierCommandHandler
{
    /// <summary>
    /// Database command for deleting a cashier.
    /// </summary>
    /// <param name="TenantId">Unique identifier for the tenant</param>
    /// <param name="CashierId">Unique identifier for the cashier to delete</param>
    public record DbCommand(Guid TenantId, Guid CashierId) : ICommand<bool>;

    /// <summary>
    /// Handles the DeleteCashierCommand and returns the result with integration event.
    /// </summary>
    /// <param name="command">The delete cashier command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and integration event</returns>
    public static async Task<(Result, CashierDeleted?)> Handle(DeleteCashierCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(command.TenantId, command.CashierId);
        var deleted = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (!deleted)
        {
            return (Result.Failure("Cashier not found or could not be deleted"), null);
        }

        var deletedEvent = new CashierDeleted(command.TenantId, PartitionKeyTest: 0, command.CashierId, DeletedAt: DateTime.UtcNow);

        return (Result.Success(), deletedEvent);
    }

    /// <summary>
    /// Handles the database command for deleting a cashier.
    /// </summary>
    /// <param name="command">The database command</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the cashier was deleted, false otherwise</returns>
    public static async Task<bool> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        var deletedCount = await db.Cashiers
            .Where(c => c.TenantId == command.TenantId && c.CashierId == command.CashierId)
            .DeleteAsync(cancellationToken);

        return deletedCount > 0;
    }
}
<!--#endif-->