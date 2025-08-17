// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.DomainEvents;

namespace AppDomain.BackOffice.Messaging.AppDomainInboxHandler;

public class InvoiceGeneratedHandler(ILogger<InvoiceGeneratedHandler> logger)
{
    public async Task Handle(InvoiceGenerated invoiceGenerated)
    {
        var invoice = invoiceGenerated.Invoice;
        logger.LogInformation("Processing domain event: InvoiceGenerated with ID {InvoiceId} and Amount {Amount}",
            invoice.InvoiceId, invoice.Amount);

        // Simulate some processing work
        await Task.Delay(100);

        logger.LogInformation("Completed processing InvoiceGenerated domain event for Invoice {InvoiceId}", invoice.InvoiceId);
    }
}
