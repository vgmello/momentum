// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;

namespace AppDomain.Invoices.Commands;

/// <summary>
///     Command to cancel an existing invoice.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="InvoiceId">Unique identifier for the invoice to cancel</param>
/// <param name="Version">Version number for optimistic concurrency control</param>
public record CancelInvoiceCommand(Guid TenantId, Guid InvoiceId, int Version) : ICommand<Result<Invoice>>;

/// <summary>
///     Validator for the CancelInvoiceCommand.
/// </summary>
public class CancelInvoiceValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.InvoiceId).NotEmpty();
        RuleFor(c => c.Version).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
///     Handler for the CancelInvoiceCommand.
/// </summary>
public static partial class CancelInvoiceCommandHandler
{
    /// <summary>
    ///     Storage / persistence request
    /// </summary>
    /// <remarks>
    ///     This DbCommand/DbQuery leverages the Momentum
    ///     <see cref="Momentum.Extensions.Abstractions.Dapper.DbCommandAttribute">DbCommandAttribute</see>,
    ///     which creates a source generated handler for the DB call.
    ///     <para>
    ///         > Notes:
    ///         - If the function name starts with a $, the function gets executed as `select * from {dbFunction}`
    ///     </para>
    /// </remarks>
    [DbCommand(fn: "$app_domain.invoices_cancel")]
    public partial record DbCommand(Guid TenantId, Guid InvoiceId, int Version) : ICommand<Data.Entities.Invoice?>;

    /// <summary>
    ///     Handles the CancelInvoiceCommand and returns the cancelled invoice with integration event.
    /// </summary>
    /// <param name="command">The cancel invoice command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and integration event</returns>
    public static async Task<(Result<Invoice>, InvoiceCancelled?)> Handle(CancelInvoiceCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(command.TenantId, command.InvoiceId, command.Version);
        var updatedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (updatedInvoice is null)
        {
            var failures = new List<ValidationFailure>
            {
                new("Version", "Invoice not found, cannot be cancelled, or was modified by another user. " +
                               "Please refresh and try again.")
            };

            return (failures, null);
        }

        var result = updatedInvoice.ToModel();
        var cancelledEvent = new InvoiceCancelled(command.TenantId, command.InvoiceId, result);

        return (result, cancelledEvent);
    }
}
