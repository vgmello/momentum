namespace AppDomain.Orders.Contracts.Models;

/// <summary>Represents an order entity for event topic configuration</summary>
public record Order
{
    /// <summary>Unique identifier for the order</summary>
    public required Guid Id { get; init; }

    /// <summary>Human-readable order number</summary>
    public required string OrderNumber { get; init; }
}