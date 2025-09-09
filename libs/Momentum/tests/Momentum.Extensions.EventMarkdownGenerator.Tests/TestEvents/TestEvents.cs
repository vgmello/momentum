using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents;

/// <summary>
/// Test event for customer creation
/// </summary>
[EventTopic("test.customer.created")]
public record CustomerCreated(
    [PartitionKey] Guid CustomerId,
    string Name,
    string Email
);

/// <summary>
/// Test event for order placement
/// </summary>
[EventTopic("test.order.placed")]
public record OrderPlaced(
    [PartitionKey] Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount
);