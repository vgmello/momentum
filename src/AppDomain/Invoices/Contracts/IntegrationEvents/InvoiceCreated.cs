< !--#if (includeSample)-->
// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

/// <summary>
/// Published when a new invoice is successfully created in the AppDomain system.
/// This event contains the complete invoice data and partition key information for proper message routing.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="PartitionKeyTest">Additional partition key for message routing</param>
/// <param name="Invoice">Invoice object containing all invoice data and configuration</param>
/// <remarks>
/// ## When It's Triggered
///
/// This event is published when:
/// - The invoice creation process completes successfully
/// - All validation rules pass
/// - The invoice data has been persisted to the database
///
/// ## Event Usage
///
/// This event can be used by other services to:
/// - Update accounting systems
/// - Trigger billing workflows
/// - Send notifications to relevant stakeholders
/// - Update customer portals with new invoice information
/// </remarks>
[EventTopic<Invoice>]
public record InvoiceCreated(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] int PartitionKeyTest,
    Invoice Invoice
);
<!--#endif-->
