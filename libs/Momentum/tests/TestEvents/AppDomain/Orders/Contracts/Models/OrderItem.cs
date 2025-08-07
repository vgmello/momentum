// Copyright (c) Momentum .NET. All rights reserved.

namespace TestEvents.AppDomain.Orders.Contracts.Models;

/// <summary>Represents an individual item within an order</summary>
public record OrderItem
{
    /// <summary>Unique identifier for the ordered product</summary>
    public required Guid ProductId { get; init; }

    /// <summary>Display name of the ordered product</summary>
    public required string ProductName { get; init; }

    /// <summary>Number of units ordered for this product</summary>
    public required int Quantity { get; init; }

    /// <summary>Price per unit of the product at time of order</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>Total price for this line item (quantity * unit price)</summary>
    public required decimal TotalPrice { get; init; }
}
