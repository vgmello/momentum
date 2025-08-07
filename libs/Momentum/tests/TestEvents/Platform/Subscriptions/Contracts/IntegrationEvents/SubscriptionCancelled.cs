// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace Platform.Subscriptions.Contracts.IntegrationEvents;

/// <summary>
///     Published when a subscription is cancelled
/// </summary>
/// <param name="TenantId">Tenant identifier</param>
/// <param name="SubscriptionId">Subscription identifier</param>
/// <param name="Reason">Cancellation reason</param>
/// <param name="CancelledAt">Cancellation timestamp</param>
[EventTopic<SubscriptionCancelled>]
public sealed record SubscriptionCancelled(
    [PartitionKey(Order = 0)] Guid TenantId,
    string SubscriptionId,
    string Reason,
    DateTime CancelledAt
);
