// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

[EventTopic<Invoice>]
public record InvoiceFinalized(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] Guid InvoiceId,
    Guid CustomerId,
    string PublicInvoiceNumber,
    decimal FinalTotalAmount
);
