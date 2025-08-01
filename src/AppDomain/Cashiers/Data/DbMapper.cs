// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;
using Riok.Mapperly.Abstractions;

namespace AppDomain.Cashiers.Data;

[Mapper]
public static partial class DbMapper
{
    [MapperIgnoreSource(nameof(Entities.Cashier.CreatedDateUtc))]
    [MapperIgnoreSource(nameof(Entities.Cashier.UpdatedDateUtc))]
    [MapperIgnoreTarget(nameof(Cashier.CashierPayments))]
    public static partial Cashier ToModel(this Entities.Cashier cashier);

    private static string ToStringSafe(string? value) => value ?? string.Empty;
}
