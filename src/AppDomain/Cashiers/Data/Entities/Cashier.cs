// Copyright (c) ORG_NAME. All rights reserved.

using LinqToDB.Mapping;

namespace AppDomain.Cashiers.Data.Entities;

/// <summary>
///     Represents a cashier entity in the database with tenant-scoped identification.
/// </summary>
public record Cashier : DbEntity
{
    /// <summary>
    ///     Gets or sets the tenant identifier that owns this cashier.
    /// </summary>
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier for this cashier within the tenant.
    /// </summary>
    [PrimaryKey(order: 1)]
    public Guid CashierId { get; set; }

    /// <summary>
    ///     Gets or sets the display name of the cashier.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the optional email address of the cashier.
    /// </summary>
    public string? Email { get; set; }
}
