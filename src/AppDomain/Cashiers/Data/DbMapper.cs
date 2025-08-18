// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;
using Riok.Mapperly.Abstractions;

namespace AppDomain.Cashiers.Data;

/// <summary>
///     Provides mapping functionality between database entities and domain models for Cashiers.
/// </summary>
[Mapper]
public static partial class DbMapper
{
    /// <summary>
    ///     Converts a Cashier database entity to a domain model.
    /// </summary>
    /// <param name="cashier">The Cashier database entity to convert.</param>
    /// <returns>A Cashier domain model.</returns>
    [MapperIgnoreSource(nameof(Entities.Cashier.CreatedDateUtc))]
    [MapperIgnoreSource(nameof(Entities.Cashier.UpdatedDateUtc))]
    [MapperIgnoreTarget(nameof(Cashier.CashierPayments))]
    public static partial Cashier ToModel(this Entities.Cashier cashier);

    /// <summary>
    ///     Safely converts a nullable string to a non-null string.
    /// </summary>
    /// <param name="value">The nullable string value.</param>
    /// <returns>The original value or an empty string if null.</returns>
    private static string ToStringSafe(string? value) => value ?? string.Empty;
}
