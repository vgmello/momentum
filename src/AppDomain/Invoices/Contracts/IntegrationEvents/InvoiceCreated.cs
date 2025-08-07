// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

[EventTopic<Invoice>]
public record InvoiceCreated(
    [PartitionKey] Guid TenantId,
    Invoice Invoice
);
