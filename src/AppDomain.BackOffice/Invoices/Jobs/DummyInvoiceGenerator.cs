// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.BackOffice.Invoices.Jobs;

public class DummyInvoiceGenerator(IMessageBus bus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var tenantId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var invoice = new Invoice(
                TenantId: tenantId,
                InvoiceId: invoiceId,
                Name: "Fake Invoice",
                Status: "Paid",
                Amount: 100m,
                Currency: "USD",
                DueDate: now.AddDays(30),
                CashierId: null,
                AmountPaid: 100m,
                PaymentDate: now,
                CreatedDateUtc: now,
                UpdatedDateUtc: now,
                Version: 1
            );

            await bus.PublishAsync(new InvoicePaid(
                tenantId,
                invoiceId,
                invoice
            ));

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
