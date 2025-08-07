// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.Platform.AppDomain.Invoices.Contracts.IntegrationEvents;

/// <summary>
///     Published when an invoice is generated for a customer
/// </summary>
/// <param name="TenantId">Tenant identifier</param>
/// <param name="InvoiceId">Invoice identifier</param>
/// <param name="Amount">Invoice amount</param>
[EventTopic<InvoiceGenerated>]
public sealed record InvoiceGenerated(
    [PartitionKey(Order = 0)] Guid TenantId,
    string InvoiceId,
    decimal Amount
);
