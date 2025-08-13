< !--#if (includeSample)-->
// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

/// <summary>
/// Published when an invoice is successfully cancelled in the AppDomain system.
/// This event contains the cancelled invoice data for proper message routing.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="PartitionKeyTest">Additional partition key for message routing</param>
/// <param name="Invoice">Cancelled invoice object with updated status</param>
/// <remarks>
/// ## When It's Triggered
///
/// This event is published when:
/// - An invoice is successfully cancelled
/// - Cancellation validation passes
/// - Invoice status is updated to cancelled in the database
///
/// ## Event Usage
///
/// This event can be used by other services to:
/// - Update customer account records
/// - Reverse any pending payment processes
/// - Send cancellation notifications
/// - Update financial reporting systems
/// </remarks>
[EventTopic<Invoice>]
public record InvoiceCancelled(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] int PartitionKeyTest,
    Invoice Invoice
);
<!--#endif-->
