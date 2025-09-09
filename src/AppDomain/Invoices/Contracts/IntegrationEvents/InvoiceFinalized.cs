// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

/// <summary>
///     Published when an invoice is finalized and ready for processing in the AppDomain system.
///     This event contains the essential invoice information for external systems.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="InvoiceId">Unique identifier for the invoice</param>
/// <param name="CustomerId">Unique identifier for the customer</param>
/// <param name="PublicInvoiceNumber">Public-facing invoice number for customer reference</param>
/// <param name="FinalTotalAmount">Final total amount of the invoice</param>
/// <remarks>
///     ## When It's Triggered
///
///     This event is published when:
///     - An invoice completes the finalization process
///     - All invoice line items and calculations are confirmed
///     - Invoice is ready for customer delivery or payment collection
///
///     ## Event Usage
///
///     This event can be used by other services to:
///     - Generate invoice documents for customer delivery
///     - Initialize payment collection processes
///     - Update customer relationship management systems
///     - Trigger invoice delivery workflows
/// </remarks>
[EventTopic<Invoice>]
public record InvoiceFinalized(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] Guid InvoiceId,
    Guid CustomerId,
    string PublicInvoiceNumber,
    decimal FinalTotalAmount
);
