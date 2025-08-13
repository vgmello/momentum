// Copyright (c) OrgName. All rights reserved.

using AppDomain.Core.Data;
using LinqToDB.Mapping;

namespace AppDomain.Cashiers.Data.Entities;

public record Cashier : DbEntity
{
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    [PrimaryKey(order: 1)]
    public Guid CashierId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }
}
