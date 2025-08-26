// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Api.Cashiers.Models;

/// <summary>
///     Request to update an existing cashier.
/// </summary>
public record UpdateCashierRequest
{
    /// <summary>
    ///     The updated name of the cashier.
    /// </summary>
    [JsonRequired]
    public required string Name { get; init; }

    /// <summary>
    ///     The updated email address of the cashier.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    ///     The current version of the cashier for optimistic concurrency control.
    /// </summary>
    [JsonRequired]
    public required int Version { get; init; }
}
