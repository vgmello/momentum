// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Contracts.Models;
using Riok.Mapperly.Abstractions;

namespace AppDomain.Invoices.Data;

[Mapper]
public static partial class DbMapper
{
    public static partial Invoice ToModel(this Entities.Invoice invoice);
}
