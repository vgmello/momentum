// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.DomainEvents;
using AppDomain.Invoices.Contracts.IntegrationEvents;

namespace AppDomain.BackOffice.Messaging.AccountingInboxHandler;

/// <summary>
/// Handles business day ended events from the accounting domain to generate invoices.
/// </summary>
/// <param name="logger">Logger for tracking handler execution.</param>
/// <param name="messageBus">Message bus for publishing integration events.</param>
public class BusinessDayEndedHandler(ILogger<BusinessDayEndedHandler> logger, IMessageBus messageBus)
{
    /// <summary>
    /// Processes a business day ended event by generating and finalizing invoices.
    /// </summary>
    /// <param name="businessDayEnded">The business day ended event containing date, market, and region information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Handle(BusinessDayEnded businessDayEnded)
    {
        logger.LogInformation("Processing business day ended for {BusinessDate} in {Market}/{Region}",
            businessDayEnded.BusinessDate, businessDayEnded.Market, businessDayEnded.Region);

        var tenantId = Guid.CreateVersion7();
        var invoiceId = Guid.CreateVersion7();
        var customerId = Guid.CreateVersion7();

        var invoice = new AppDomain.Invoices.Contracts.Models.Invoice(
            TenantId: tenantId,
            InvoiceId: invoiceId,
            Name: $"Invoice for {businessDayEnded.BusinessDate:yyyy-MM-dd}",
            Status: "Generated",
            Amount: 500.75m,
            Currency: "USD",
            DueDate: DateTime.UtcNow.AddDays(30),
            CashierId: null,
            AmountPaid: 0m,
            PaymentDate: null,
            CreatedDateUtc: DateTime.UtcNow,
            UpdatedDateUtc: DateTime.UtcNow,
            Version: 1
        );

        await messageBus.PublishAsync(new InvoiceGenerated(tenantId, invoice, DateTime.UtcNow));

        await messageBus.PublishAsync(new InvoiceFinalized(
            tenantId,
            invoiceId,
            customerId,
            $"INV-{businessDayEnded.BusinessDate:yyyyMMdd}-{invoiceId:N}",
            500.75m
        ));
    }
}

// This declared in this file, for example purposes,
// in a real-world scenario is supposed to be declared in a different domain/project
/// <summary>
/// Represents an event indicating that a business day has ended for a specific market and region.
/// </summary>
/// <param name="BusinessDate">The date of the business day that ended.</param>
/// <param name="Market">The market identifier where the business day ended.</param>
/// <param name="Region">The region identifier where the business day ended.</param>
[EventTopic("momentum", domain: "accounting")]
public record BusinessDayEnded(DateTime BusinessDate, string Market, string Region);
