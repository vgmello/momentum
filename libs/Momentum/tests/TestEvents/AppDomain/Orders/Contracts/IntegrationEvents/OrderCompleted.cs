using Momentum.Extensions.Abstractions.Messaging;
using AppDomain.Orders.Contracts.Models;

namespace AppDomain.Orders.Contracts.IntegrationEvents;

/// <summary>Published when an order is successfully completed and payment is processed</summary>
/// <param name="TenantId">Identifier of the tenant that owns the order</param>
/// <param name="OrderId">Unique identifier for the completed order</param>
/// <param name="OrderNumber">Human-readable order number for customer reference</param>
/// <param name="Customer">Complete customer information including AppDomain and shipping details</param>
/// <param name="Items">List of all items included in the completed order</param>
/// <param name="TotalAmount">Final total amount charged to the customer</param>
/// <param name="CompletedAt">Date and time when the order was completed</param>
/// <remarks>
/// ## When It's Triggered
///
/// This event is published when:
/// - Payment processing completes successfully
/// - All order items are confirmed as available
/// - Order status is updated to completed
///
/// ## Business Impact
///
/// This event triggers:
/// - Inventory reduction for ordered items
/// - Customer notification emails
/// - Analytics and reporting updates
/// - Loyalty points calculation
/// </remarks>
[EventTopic<Order>]
public sealed record OrderCompleted(
    [PartitionKey] Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    Customer Customer,
    List<OrderItem> Items,
    decimal TotalAmount,
    DateTime CompletedAt
);
