// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Contracts.IntegrationEvents;
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Data;

namespace AppDomain.Cashiers.Commands;

/// <summary>
///     Command to update an existing cashier in the system.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="CashierId">Unique identifier for the cashier to update</param>
/// <param name="Name">Updated cashier name</param>
/// <param name="Email">Updated cashier email</param>
/// <param name="Version">Version for optimistic concurrency control.</param>
public record UpdateCashierCommand(Guid TenantId, Guid CashierId, string Name, string? Email, int Version) : ICommand<Result<Cashier>>;

/// <summary>
///     Validator for the UpdateCashierCommand.
/// </summary>
public class UpdateCashierValidator : AbstractValidator<UpdateCashierCommand>
{
    public UpdateCashierValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.CashierId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Name).MinimumLength(2);
        RuleFor(c => c.Name).MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().When(c => c.Email != null);
        RuleFor(c => c.Email).EmailAddress().When(c => !string.IsNullOrEmpty(c.Email));
    }
}

/// <summary>
///     Handler for the UpdateCashierCommand.
/// </summary>
public static class UpdateCashierCommandHandler
{
    /// <summary>
    ///     Storage / persistence request
    /// </summary>
    public record DbCommand(Guid TenantId, Guid CashierId, string Name, string? Email, int Version) : ICommand<Data.Entities.Cashier?>;

    /// <summary>
    ///     Handles the UpdateCashierCommand and returns the updated cashier with integration event.
    /// </summary>
    /// <param name="command">The update cashier command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and integration event</returns>
    public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(UpdateCashierCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(command.TenantId, command.CashierId, command.Name, command.Email, command.Version);
        var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (updatedCashier is null)
        {
            var failures = new List<ValidationFailure> { new("CashierId", "Cashier not found") };

            return (failures, null);
        }

        var result = updatedCashier.ToModel();
        var updatedEvent = new CashierUpdated(result.TenantId, result);

        return (result, updatedEvent);
    }

    /// <summary>
    ///     Database logic for updating a cashier.
    /// </summary>
    /// <param name="command">The database command</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated cashier entity</returns>
    public static async Task<Data.Entities.Cashier?> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        var statement = db.Cashiers
            .Where(c => c.TenantId == command.TenantId && c.CashierId == command.CashierId)
            .Where(c => c.Version == command.Version)
            .Set(p => p.Name, command.Name);

        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            statement = statement.Set(p => p.Email, command.Email);
        }

        var updatedRecords = await statement.UpdateWithOutputAsync((_, inserted) => inserted, token: cancellationToken);

        return updatedRecords.FirstOrDefault();
    }
}
