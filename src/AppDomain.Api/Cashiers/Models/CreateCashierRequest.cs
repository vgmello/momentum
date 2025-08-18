// Copyright (c) ORG_NAME. All rights reserved.

using System.Text.Json.Serialization;

namespace AppDomain.Api.Cashiers.Models;

/// <summary>
///     Request to create a new cashier.
/// </summary>
public record CreateCashierRequest
{
    /// <summary>
    ///     The name of the cashier.
    /// </summary>
    [JsonRequired]
    public required string Name { get; init; }

    /// <summary>
    ///     The email address of the cashier.
    /// </summary>
    [JsonRequired]
    public required string Email { get; init; }
}
