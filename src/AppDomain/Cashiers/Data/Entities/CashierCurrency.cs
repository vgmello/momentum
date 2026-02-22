// Copyright (c) OrgName. All rights reserved.

using LinqToDB.Mapping;

namespace AppDomain.Cashiers.Data.Entities;

/// <summary>
///     Represents the association between a cashier and a currency with effective date tracking.
/// </summary>
[ExcludeFromCodeCoverage]
public record CashierCurrency : DbEntity
{
    /// <summary>
    ///     Gets or sets the tenant identifier that owns this association.
    /// </summary>
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the associated cashier.
    /// </summary>
    [PrimaryKey(order: 1)]
    public Guid CashierId { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the associated currency.
    /// </summary>
    [PrimaryKey(order: 2)]
    public Guid CurrencyId { get; set; }

    /// <summary>
    ///     Gets or sets the UTC date when this currency association becomes effective.
    /// </summary>
    [PrimaryKey(order: 3)]
    public DateTime EffectiveDateUtc { get; set; }

    /// <summary>
    ///     Gets or sets the custom currency code for this association.
    /// </summary>
    [Column(Length = 10)]
    public string CustomCurrencyCode { get; set; } = string.Empty;
}
