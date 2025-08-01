// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

[EventTopic<Invoice>]
public record InvoiceCancelled(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId
);
