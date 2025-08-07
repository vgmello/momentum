// Copyright (c) Momentum .NET. All rights reserved.

namespace AppDomain.Orders.Contracts.Models;

/// <summary>Represents a physical address for AppDomain or shipping</summary>
public record Address
{
    /// <summary>Street address including number and name</summary>
    public required string Street { get; init; }

    /// <summary>City name</summary>
    public required string City { get; init; }

    /// <summary>State or province</summary>
    public required string State { get; init; }

    /// <summary>Postal or ZIP code</summary>
    public required string PostalCode { get; init; }

    /// <summary>Country name or code</summary>
    public required string Country { get; init; }
}
