// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Contracts.DomainEvents;

namespace AppDomain.BackOffice.Messaging.AppDomainInboxHandler;

/// <summary>
///     Handles invoice generated domain events for back office processing.
/// </summary>
/// <param name="logger">Logger for tracking handler execution.</param>
public class InvoiceGeneratedHandler(ILogger<InvoiceGeneratedHandler> logger)
{
    /// <summary>
    ///     Processes an invoice generated domain event by performing post-generation tasks.
    /// </summary>
    /// <param name="invoiceGenerated">The invoice generated domain event containing invoice details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Handle(InvoiceGenerated invoiceGenerated)
    {
        var invoice = invoiceGenerated.Invoice;
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Processing domain event: InvoiceGenerated with ID {InvoiceId} and Amount {Amount}",
                invoice.InvoiceId, invoice.Amount);
        }

        // Simulate some processing work
        await Task.Delay(100);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Completed processing InvoiceGenerated domain event for Invoice {InvoiceId}", invoice.InvoiceId);
        }
    }
}
