// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;

namespace AppDomain.Invoices.Commands;

/// <summary>
///     Command to mark an existing invoice as paid.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="InvoiceId">Unique identifier for the invoice to mark as paid</param>
/// <param name="Version">TODO</param>
/// <param name="AmountPaid">Amount that was paid</param>
/// <param name="PaymentDate">Date when the payment was received</param>
public record MarkInvoiceAsPaidCommand(
    Guid TenantId,
    Guid InvoiceId,
    int Version,
    decimal AmountPaid,
    DateTime? PaymentDate = null) : ICommand<Result<Invoice>>;

/// <summary>
///     Validator for the MarkInvoiceAsPaidCommand.
/// </summary>
public class MarkInvoiceAsPaidValidator : AbstractValidator<MarkInvoiceAsPaidCommand>
{
    public MarkInvoiceAsPaidValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.InvoiceId).NotEmpty();
        RuleFor(c => c.AmountPaid).GreaterThan(0);
        RuleFor(c => c.PaymentDate).NotEmpty().When(c => c.PaymentDate.HasValue);
    }
}

/// <summary>
///     Handler for the MarkInvoiceAsPaidCommand.
/// </summary>
public static partial class MarkInvoiceAsPaidCommandHandler
{
    /// <summary>
    ///     Database command for marking an invoice as paid.
    /// </summary>
    [DbCommand(fn: "$billing.invoices_mark_paid")]
    public partial record DbCommand(
        Guid TenantId,
        Guid InvoiceId,
        int Version,
        decimal AmountPaid,
        DateTime PaymentDate
    ) : ICommand<Data.Entities.Invoice?>;

    /// <summary>
    ///     Handles the MarkInvoiceAsPaidCommand and returns the updated invoice with integration events.
    /// </summary>
    /// <param name="command">The mark invoice as paid command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and integration events</returns>
    public static async Task<(Result<Invoice>, InvoicePaid?)> Handle(
        MarkInvoiceAsPaidCommand command, IMessageBus messaging, CancellationToken cancellationToken)
    {
        var paymentDate = command.PaymentDate ?? DateTime.UtcNow;
        var dbCommand = new DbCommand(command.TenantId, command.InvoiceId, command.Version, command.AmountPaid, paymentDate);

        var updatedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (updatedInvoice is null)
        {
            var failures = new List<ValidationFailure>
            {
                new("Version", "Invoice not found, already paid, or was modified by another user. " +
                               "Please refresh and try again.")
            };

            return (failures, null);
        }

        var result = updatedInvoice.ToModel();
        var paidEvent = new InvoicePaid(updatedInvoice.TenantId, result.InvoiceId, result);

        return (result, paidEvent);
    }
}
