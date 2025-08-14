// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Invoices.Contracts.IntegrationEvents;

/// <summary>
/// Published when a payment is received for an invoice in the AppDomain system.
/// This event contains the payment details for proper message routing and processing.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="PartitionKeyTest">Additional partition key for message routing</param>
/// <param name="InvoiceId">Unique identifier of the invoice the payment is for</param>
/// <param name="PaymentAmount">Amount of the payment received</param>
/// <param name="Currency">Currency of the payment</param>
/// <param name="PaymentDate">Date and time when the payment was received</param>
/// <param name="PaymentMethod">Method used for the payment (optional)</param>
/// <remarks>
/// ## When It's Triggered
///
/// This event is published when:
/// - A payment is received and processed for an invoice
/// - Payment validation completes successfully
/// - Payment details are recorded in the system
///
/// ## Event Usage
///
/// This event can be used by other services to:
/// - Update invoice payment status
/// - Process partial or full payment reconciliation
/// - Send payment received notifications
/// - Update accounting and financial systems
/// </remarks>
[EventTopic<Guid>]
public record PaymentReceived(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] int PartitionKeyTest,
    Guid InvoiceId,
    decimal PaymentAmount,
    string Currency,
    DateTime PaymentDate,
    string? PaymentMethod
);
