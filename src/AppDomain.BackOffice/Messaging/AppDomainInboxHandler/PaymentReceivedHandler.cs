// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Queries;

namespace AppDomain.BackOffice.Messaging.AppDomainInboxHandler;

/// <summary>
///     Handles payment received integration events by marking invoices as paid.
/// </summary>
public static class PaymentReceivedHandler
{
    /// <summary>
    ///     Processes a payment received event by retrieving the associated invoice and marking it as paid.
    /// </summary>
    /// <param name="event">The payment received integration event containing payment details.</param>
    /// <param name="messaging">Message bus for executing commands and queries.</param>
    /// <param name="logger">Logger for tracking payment processing.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Handle(PaymentReceived @event, IMessageBus messaging, ILogger logger, CancellationToken cancellationToken)
    {
        var tenantId = @event.TenantId;

        // Get the current invoice to obtain its version for optimistic concurrency
        var getInvoiceQuery = new GetInvoiceQuery(tenantId, @event.InvoiceId);
        var invoiceResult = await messaging.InvokeQueryAsync(getInvoiceQuery, cancellationToken);

        var invoice = invoiceResult.Match<Invoice?>(
            success => success,
            errors =>
            {
                logger.LogWarning("Failed to retrieve invoice {InvoiceId} for payment processing: {Errors}",
                    @event.InvoiceId, string.Join(", ", errors));
                return null;
            }
        );

        if (invoice is null) return;

        var markPaidCommand = new MarkInvoiceAsPaidCommand(
            tenantId,
            @event.InvoiceId,
            invoice.Version,
            @event.PaymentAmount,
            @event.PaymentDate
        );

        var markPaidResult = await messaging.InvokeCommandAsync(markPaidCommand, cancellationToken);

        markPaidResult.Switch(
            _ =>
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Invoice {InvoiceId} marked as paid for tenant {TenantId}",
                        @event.InvoiceId, tenantId);
                }
            },
            errors => logger.LogWarning("Failed to mark invoice {InvoiceId} as paid: {Errors}",
                @event.InvoiceId, string.Join(", ", errors.Select(e => e.ErrorMessage)))
        );
    }
}
