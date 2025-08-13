< !--#if (INCLUDE_SAMPLE)-->
// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Contracts.IntegrationEvents;
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Data;
using AppDomain.Core.Data;
using FluentValidation;
using LinqToDB;
using Momentum.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using Wolverine;

namespace AppDomain.Cashiers.Commands;

/// <summary>
/// Command to update an existing cashier in the system.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="CashierId">Unique identifier for the cashier to update</param>
/// <param name="Name">Updated cashier name</param>
/// <param name="Email">Updated cashier email</param>
public record UpdateCashierCommand(
    Guid TenantId,
    Guid CashierId,
    string Name,
    string Email
) : ICommand<Result<Cashier>>;

/// <summary>
/// Validator for the UpdateCashierCommand.
/// </summary>
public class UpdateCashierValidator : AbstractValidator<UpdateCashierCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCashierValidator"/> class.
    /// </summary>
    public UpdateCashierValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.CashierId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Name).MinimumLength(2);
        RuleFor(c => c.Name).MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Email).EmailAddress();
    }
}

/// <summary>
/// Handler for the UpdateCashierCommand.
/// </summary>
public static class UpdateCashierCommandHandler
{
    /// <summary>
    /// Database command for updating a cashier.
    /// </summary>
    /// <param name="Cashier">The cashier entity with updated values</param>
    public record DbCommand(Data.Entities.Cashier Cashier) : ICommand<Data.Entities.Cashier>;

    /// <summary>
    /// Handles the UpdateCashierCommand and returns the updated cashier with integration event.
    /// </summary>
    /// <param name="command">The update cashier command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and integration event</returns>
    public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(UpdateCashierCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = CreateUpdateCommand(command);
        var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = updatedCashier.ToModel();
        var updatedEvent = new CashierUpdated(result.TenantId, PartitionKeyTest: 0, result);

        return (result, updatedEvent);
    }

    /// <summary>
    /// Handles the database command for updating a cashier.
    /// </summary>
    /// <param name="command">The database command</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated cashier entity</returns>
    public static async Task<Data.Entities.Cashier> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        await db.Cashiers
            .Where(c => c.TenantId == command.Cashier.TenantId && c.CashierId == command.Cashier.CashierId)
            .UpdateAsync(_ => new Data.Entities.Cashier
            {
                Name = command.Cashier.Name,
                Email = command.Cashier.Email,
                UpdatedDateUtc = command.Cashier.UpdatedDateUtc
            }, cancellationToken);

        var updated = await db.Cashiers
            .FirstAsync(c => c.TenantId == command.Cashier.TenantId && c.CashierId == command.Cashier.CashierId, cancellationToken);

        return updated;
    }

    private static DbCommand CreateUpdateCommand(UpdateCashierCommand command) =>
        new(new Data.Entities.Cashier
        {
            TenantId = command.TenantId,
            CashierId = command.CashierId,
            Name = command.Name,
            Email = command.Email,
            CreatedDateUtc = default, // Will be ignored in update
            UpdatedDateUtc = DateTime.UtcNow
        });
}
<!--#endif-->
