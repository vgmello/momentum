using Momentum.Extensions.Abstractions.Messaging;

namespace Missing.Docs;

/// <summary>
///
/// </summary>
[EventTopic<EmptySummaryEvent>]
public sealed record EmptySummaryEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    string Data
);
