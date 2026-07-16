// Copyright (c) OrgName. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.BackOffice.Invoices.Jobs;

/// <summary>
///     Background service that generates dummy invoice paid events for testing and demonstration purposes.
///     Publishes fake invoice events at regular intervals to simulate invoice processing activity.
/// </summary>
/// <param name="scopeFactory">Used to resolve the scoped message bus per iteration.</param>
/// <param name="logger">Logger instance for tracking operations.</param>
[ExcludeFromCodeCoverage]
public class DummyInvoiceGenerator(IServiceScopeFactory scopeFactory, ILogger<DummyInvoiceGenerator> logger) : BackgroundService
{
    /// <summary>
    ///     Executes the background job, continuously publishing dummy invoice paid events.
    /// </summary>
    /// <param name="stoppingToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tenantId = Guid.CreateVersion7();
                var invoiceId = Guid.CreateVersion7();
                var now = DateTime.UtcNow;

                var invoice = new Invoice(
                    TenantId: tenantId,
                    InvoiceId: invoiceId,
                    Name: "Fake Invoice",
                    Status: InvoiceStatus.Paid,
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

                await using var scope = scopeFactory.CreateAsyncScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                await bus.PublishAsync(new InvoicePaid(
                    tenantId,
                    invoiceId,
                    invoice
                ));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error generating dummy invoice event");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
