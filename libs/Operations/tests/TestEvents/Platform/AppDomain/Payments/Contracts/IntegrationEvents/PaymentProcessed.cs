using Operations.Extensions.Abstractions.Messaging;

namespace Platform.AppDomain.Payments.Contracts.IntegrationEvents;

/// <summary>
/// Published when a payment is successfully processed
/// </summary>
/// <param name="TenantId">Tenant identifier</param>
/// <param name="PaymentId">Payment identifier</param>
/// <param name="Amount">Payment amount</param>
[EventTopic<PaymentProcessed>]
public sealed record PaymentProcessed(
    [PartitionKey(Order = 0)] Guid TenantId,
    string PaymentId,
    decimal Amount
);