// Copyright (c) OrgName. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppDomain.Cashiers.Data.Entities;

/// <summary>
///     Represents the association between a cashier and a currency with effective date tracking.
/// </summary>
public record CashierCurrency : DbEntity
{
    /// <summary>
    ///     Gets or sets the identifier of the associated cashier.
    /// </summary>
    public Guid CashierId { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the associated currency.
    /// </summary>
    public Guid CurrencyId { get; set; }

    /// <summary>
    ///     Gets or sets the UTC date when this currency association becomes effective.
    /// </summary>
    public DateTime EffectiveDateUtc { get; set; }

    /// <summary>
    ///     Gets or sets the custom currency code for this association.
    /// </summary>
    [MaxLength(10)]
    public string CustomCurrencyCode { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the navigation property to the associated cashier.
    /// </summary>
    [ForeignKey(nameof(CashierId))]
    public Cashier Cashier { get; set; } = null!;
}
