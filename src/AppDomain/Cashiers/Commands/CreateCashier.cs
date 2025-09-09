// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Contracts.IntegrationEvents;
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Data;

namespace AppDomain.Cashiers.Commands;

/// <summary>
///     Command to create a new cashier in the system.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="Name">Cashier name</param>
/// <param name="Email">Cashier email</param>
public record CreateCashierCommand(Guid TenantId, string Name, string Email) : ICommand<Result<Cashier>>;

/// <summary>
///     Validator for the CreateCashierCommand.
/// </summary>
public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Name).MinimumLength(2);
        RuleFor(c => c.Name).MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Email).EmailAddress();
    }
}

/// <summary>
///     Handler for the CreateCashierCommand.
/// </summary>
public static class CreateCashierCommandHandler
{
    /// <summary>
    ///     Storage / persistence request
    /// </summary>
    public record DbCommand(Data.Entities.Cashier Cashier) : ICommand<Data.Entities.Cashier>;

    /// <summary>
    ///     Handles the CreateCashierCommand and returns the created cashier with integration event.
    /// </summary>
    /// <param name="command">The create cashier command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and integration event</returns>
    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(CreateCashierCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = CreateInsertCommand(command);
        var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedCashier.ToModel();
        var createdEvent = new CashierCreated(result.TenantId, result);

        return (result, createdEvent);
    }

    /// <summary>
    ///     Handles the database command for creating a cashier.
    /// </summary>
    /// <param name="command">The database command</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created cashier entity</returns>
    public static async Task<Data.Entities.Cashier> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        var inserted = await db.Cashiers.InsertWithOutputAsync(command.Cashier, token: cancellationToken);

        return inserted;
    }

    private static DbCommand CreateInsertCommand(CreateCashierCommand command) =>
        new(new Data.Entities.Cashier
        {
            TenantId = command.TenantId,
            CashierId = Guid.CreateVersion7(),
            Name = command.Name,
            Email = command.Email,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
