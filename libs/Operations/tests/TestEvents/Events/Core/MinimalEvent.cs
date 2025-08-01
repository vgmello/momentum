using Operations.Extensions.Abstractions.Messaging;

namespace Events.Core;

/// <summary>
/// Simple event with minimal documentation
/// </summary>
/// <param name="Id">Event identifier</param>
[EventTopic<MinimalEvent>]
public sealed record MinimalEvent(
    [PartitionKey(Order = 0)] Guid Id
);