< !--#if (includeSample)-->
// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;
using AppDomain.Core.Data;
using FluentValidation;
using LinqToDB;
using Momentum.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using Wolverine;

namespace AppDomain.Invoices.Commands;

/// <summary>
/// Command to mark an existing invoice as paid.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="InvoiceId">Unique identifier for the invoice to mark as paid</param>
/// <param name="AmountPaid">Amount that was paid</param>
/// <param name="PaymentDate">Date when the payment was received</param>
/// <param name="PaymentMethod">Method used for payment</param>
public record MarkInvoiceAsPaidCommand(
    Guid TenantId,
    Guid InvoiceId,
    decimal AmountPaid,
    DateTime PaymentDate,
    string? PaymentMethod = null
) : ICommand<Result<Invoice>>;

/// <summary>
/// Validator for the MarkInvoiceAsPaidCommand.
/// </summary>
public class MarkInvoiceAsPaidValidator : AbstractValidator<MarkInvoiceAsPaidCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkInvoiceAsPaidValidator"/> class.
    /// </summary>
    public MarkInvoiceAsPaidValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.InvoiceId).NotEmpty();
        RuleFor(c => c.AmountPaid).GreaterThan(0);
        RuleFor(c => c.PaymentDate).NotEmpty();
    }
}

/// <summary>
/// Handler for the MarkInvoiceAsPaidCommand.
/// </summary>
public static class MarkInvoiceAsPaidCommandHandler
{
    /// <summary>
    /// Database command for marking an invoice as paid.
    /// </summary>
    /// <param name="TenantId">Unique identifier for the tenant</param>
    /// <param name="InvoiceId">Unique identifier for the invoice</param>
    /// <param name="AmountPaid">Amount that was paid</param>
    /// <param name="PaymentDate">Date when payment was received</param>
    public record MarkInvoiceAsPaidDbCommand(
        Guid TenantId,
        Guid InvoiceId,
        decimal AmountPaid,
        DateTime PaymentDate
    ) : ICommand<Data.Entities.Invoice?>;

    /// <summary>
    /// Handles the MarkInvoiceAsPaidCommand and returns the updated invoice with integration events.
    /// </summary>
    /// <param name="command">The mark invoice as paid command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and integration events</returns>
    public static async Task<(Result<Invoice>, InvoicePaid?, PaymentReceived?)> Handle(MarkInvoiceAsPaidCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = new MarkInvoiceAsPaidDbCommand(command.TenantId, command.InvoiceId, command.AmountPaid, command.PaymentDate);
        var updatedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (updatedInvoice == null)
        {
            return (Result<Invoice>.Failure("Invoice not found"), null, null);
        }

        var result = updatedInvoice.ToModel();
        var paidEvent = new InvoicePaid(result.TenantId, PartitionKeyTest: 0, result);
        var paymentEvent = new PaymentReceived(result.TenantId, PartitionKeyTest: 0, result.InvoiceId,
            command.AmountPaid, result.Currency ?? "USD", command.PaymentDate, command.PaymentMethod);

        return (result, paidEvent, paymentEvent);
    }

    /// <summary>
    /// Handles the database command for marking an invoice as paid.
    /// </summary>
    /// <param name="command">The database command</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated invoice entity, or null if not found</returns>
    public static async Task<Data.Entities.Invoice?> Handle(MarkInvoiceAsPaidDbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        var updated = await db.Invoices
            .Where(i => i.TenantId == command.TenantId && i.InvoiceId == command.InvoiceId)
            .UpdateAsync(_ => new Data.Entities.Invoice
            {
                Status = "Paid",
                AmountPaid = command.AmountPaid,
                PaymentDate = command.PaymentDate,
                UpdatedDateUtc = DateTime.UtcNow
            }, cancellationToken);

        if (updated == 0)
        {
            return null;
        }

        return await db.Invoices
            .FirstOrDefaultAsync(i => i.TenantId == command.TenantId && i.InvoiceId == command.InvoiceId, cancellationToken);
    }
}
<!--#endif-->
