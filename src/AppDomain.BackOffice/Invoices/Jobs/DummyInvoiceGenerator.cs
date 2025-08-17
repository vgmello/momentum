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
            await bus.PublishAsync(new InvoicePaid(
                Guid.Empty,
                new Invoice { Name = "Fake Invoice", Status = "Paid" }
            ));

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
