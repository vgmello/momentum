// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

[EventTopic<Invoice>]
public record InvoiceFinalized(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    string PublicInvoiceNumber,
    decimal FinalTotalAmount
);
