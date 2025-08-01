// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

[EventTopic("payments")]
public record PaymentReceived(
    [PartitionKey] Guid TenantId,
    Guid InvoiceId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string PaymentReference,
    DateTime ReceivedDate
);
