// Copyright (c) ABCDEG. All rights reserved.

using Billing.Invoices.Contracts.Models;

namespace Billing.Invoices.Contracts.DomainEvents;

/// <summary>
///     Published when an invoice is generated in the system.
/// 
///     This event is triggered during the invoice creation process
///     and contains the essential invoice information needed for
///     downstream processing.
/// 
///     Key details:
///     - Contains tenant isolation data
///     - Includes invoice identification
///     - Provides total amount for processing
/// </summary>
[EventTopic<Invoice>(Internal = true)]
public record InvoiceGenerated([PartitionKey] Guid TenantId, Guid InvoiceId, decimal InvoiceAmount);
