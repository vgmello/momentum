// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Queries;

namespace AppDomain.BackOffice.Messaging.AppDomainInboxHandler;

public static class PaymentReceivedHandler
{
    public static async Task Handle(PaymentReceived @event, IMessageBus messaging, CancellationToken cancellationToken)
    {
        // TODO: Get TenantId from the invoice or event
        // In a real scenario, this would be retrieved from the invoice context or the event itself
        var tenantId = Guid.Parse("12345678-0000-0000-0000-000000000000"); // Using the same fake tenant ID for consistency

        // Get the current invoice to obtain its version for optimistic concurrency
        var getInvoiceQuery = new GetInvoiceQuery(tenantId, @event.InvoiceId);
        var invoiceResult = await messaging.InvokeQueryAsync(getInvoiceQuery, cancellationToken);

        var invoice = invoiceResult.Match(
            success => success,
            errors => throw new InvalidOperationException(
                $"Failed to retrieve invoice {@event.InvoiceId} for payment processing: {string.Join(", ", errors)}")
        );

        var markPaidCommand = new MarkInvoiceAsPaidCommand(
            tenantId,
            @event.InvoiceId,
            invoice.Version,
            @event.Amount,
            @event.ReceivedDate
        );

        await messaging.InvokeCommandAsync(markPaidCommand, cancellationToken);
    }
}
