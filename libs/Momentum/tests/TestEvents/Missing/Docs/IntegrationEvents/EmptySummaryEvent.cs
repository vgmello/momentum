// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.Missing.Docs.IntegrationEvents;

/// <summary>
///
/// </summary>
[EventTopic<EmptySummaryEvent>]
public sealed record EmptySummaryEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    string Data
);
