// Copyright (c) ABCDEG. All rights reserved.

namespace Operations.Extensions.Abstractions.Messaging;

/// <summary>
///     Marks a property as the source for the partition key in distributed events.
/// </summary>
/// <remarks>
///     Apply this attribute to a property in a distributed event class to indicate
///     that its value should be used as the partition key for message routing.
/// </remarks>
/// <example>
///     <code>
/// public class OrderCreatedEvent
/// {
///     [PartitionKey]
///     public Guid OrderId { get; set; }
/// }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class PartitionKeyAttribute : Attribute
{
    public int Order { get; set; }
}
