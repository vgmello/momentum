// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.Models;
using Riok.Mapperly.Abstractions;

namespace AppDomain.Invoices.Data;

/// <summary>
///     Provides mapping functionality between database entities and domain models for invoices.
/// </summary>
[Mapper]
public static partial class DbMapper
{
    /// <summary>
    ///     Converts an invoice database entity to a domain model.
    /// </summary>
    /// <param name="invoice">The invoice database entity to convert.</param>
    /// <returns>The converted invoice domain model.</returns>
    public static partial Invoice ToModel(this Entities.Invoice invoice);
}
