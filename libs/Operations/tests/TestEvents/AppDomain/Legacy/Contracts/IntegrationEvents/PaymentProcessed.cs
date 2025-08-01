using Operations.Extensions.Abstractions.Messaging;

namespace AppDomain.Legacy.Contracts.IntegrationEvents;

/// <summary>
/// Published when a payment is processed through the legacy payment system
/// </summary>
/// <param name="TenantId">Identifier of the tenant</param>
/// <param name="Amount">Payment amount that was processed</param>
/// <param name="TransactionId">Legacy transaction identifier</param>
/// <remarks>
/// ## When It's Triggered
/// 
/// This event was published when:
/// - Payment was processed through the old payment gateway
/// - Transaction was completed successfully
/// 
/// ## Migration Notes
/// 
/// This event has been replaced by the new PaymentCompleted event which provides:
/// - Enhanced payment method tracking
/// - Better error handling and retry logic
/// - Improved audit trail capabilities
/// </remarks>
[Obsolete("This event has been replaced by the new PaymentCompleted event. Please migrate to use the new event structure for enhanced functionality.")]
[EventTopic<PaymentProcessed>]
public sealed record PaymentProcessed(
    [PartitionKey(Order = 0)] Guid TenantId,
    decimal Amount,
    string TransactionId
);