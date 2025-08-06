// Copyright (c) ABCDEG. All rights reserved.

namespace Momentum.Extensions.Abstractions.Messaging;

/// <summary>
///     Represents a distributed event that can be published across service boundaries.
/// </summary>
/// <remarks>
///     Events are used to communicate state changes between different services
///     or bounded contexts in a distributed system. They enable eventual consistency
///     by notifying other parts of the system about important domain events.
/// </remarks>
public interface IDistributedEvent
{
    /// <summary>
    ///     Gets the partition key for this event.
    /// </summary>
    /// <returns>
    ///     A string representing the partition key used for message routing and ordering.
    ///     Events with the same partition key are guaranteed to be processed in order.
    /// </returns>
    /// <remarks>
    ///     The partition key is typically derived from an entity identifier or aggregate root ID
    ///     to ensure related events are processed sequentially.
    /// </remarks>
    string GetPartitionKey();
}
