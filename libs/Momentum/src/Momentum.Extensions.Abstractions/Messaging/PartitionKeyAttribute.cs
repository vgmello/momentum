// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.Abstractions.Messaging;

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
    /// <summary>
    ///     Gets or sets the order of this partition key component when composing
    ///     a composite partition key from multiple properties.
    /// </summary>
    /// <remarks>
    ///     When multiple properties are marked with <see cref="PartitionKeyAttribute"/>,
    ///     they are combined in ascending order to form the final partition key.
    ///     Lower values are processed first.
    /// </remarks>
    /// <value>The sort order for composite keys. Default is 0.</value>
    public int Order { get; set; }
}
