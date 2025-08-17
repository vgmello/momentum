// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.DomainEvents;
using AppDomain.Invoices.Contracts.IntegrationEvents;

namespace AppDomain.BackOffice.Messaging.AccountingInboxHandler;

public class BusinessDayEndedHandler(ILogger<BusinessDayEndedHandler> logger, IMessageBus messageBus)
{
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
[EventTopic("momentum", domain: "accounting")]
public record BusinessDayEnded(DateTime BusinessDate, string Market, string Region);
