// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.AppDomain.Inventory.Contracts.IntegrationEvents;

/// <summary>
///     Published when product stock levels are updated in the inventory system
/// </summary>
/// <param name="TenantId">Identifier of the tenant that owns the inventory</param>
/// <param name="WarehouseId">Identifier of the warehouse where stock is managed</param>
/// <param name="ProductCategory">Category of the product for specialized processing</param>
/// <param name="ProductId">Unique identifier of the product</param>
/// <param name="PreviousQuantity">Stock quantity before the update</param>
/// <param name="NewQuantity">Stock quantity after the update</param>
/// <param name="UpdatedAt">Date and time when the stock was updated</param>
/// <remarks>
///     ## When It's Triggered
/// 
///     This event is published when:
///     - Product stock is adjusted manually
///     - Automated stock replenishment occurs
///     - Stock is reserved for orders
///     - Stock reservations are released
/// 
///     ## Partition Strategy
/// 
///     This event uses multiple partition keys to optimize message routing:
///     - Primary partitioning by tenant for isolation
///     - Secondary partitioning by warehouse for regional processing
///     - Tertiary partitioning by product category for specialized handlers
/// </remarks>
[EventTopic<Product>]
public sealed record ProductStockUpdated(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] Guid WarehouseId,
    [PartitionKey(Order = 2)] string ProductCategory,
    Guid ProductId,
    int PreviousQuantity,
    int NewQuantity,
    DateTime UpdatedAt
);

/// <summary>
///     Represents a product in the inventory system
/// </summary>
public record Product
{
    /// <summary>Unique identifier for the product</summary>
    public required Guid Id { get; init; }

    /// <summary>Name of the product</summary>
    public required string Name { get; init; }

    /// <summary>Category of the product</summary>
    public required string Category { get; init; }
}
