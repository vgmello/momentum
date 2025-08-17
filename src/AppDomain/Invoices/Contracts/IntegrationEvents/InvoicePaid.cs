// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

/// <summary>
///     Published when an invoice is successfully marked as paid in the AppDomain system.
///     This event contains the updated invoice data with payment information for proper message routing.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="Invoice">Updated invoice object with payment information</param>
/// <remarks>
///     ## When It's Triggered
///
///     This event is published when:
///     - An invoice payment is successfully recorded
///     - Payment validation completes successfully
///     - Invoice status is updated to paid in the database
///
///     ## Event Usage
///
///     This event can be used by other services to:
///     - Update customer account balances
///     - Trigger revenue recognition processes
///     - Send payment confirmation notifications
///     - Update financial reporting systems
/// </remarks>
[EventTopic<Invoice>]
public record InvoicePaid(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] Guid InvoiceId,
    Invoice Invoice
);
