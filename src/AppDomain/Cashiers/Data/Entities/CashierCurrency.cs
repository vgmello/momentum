// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Core.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppDomain.Cashiers.Data.Entities;

public record CashierCurrency : DbEntity
{
    public Guid CashierId { get; set; }

    public Guid CurrencyId { get; set; }

    public DateTime EffectiveDateUtc { get; set; }

    [MaxLength(10)]
    public string CustomCurrencyCode { get; set; } = string.Empty;

    [ForeignKey(nameof(CashierId))]
    public Cashier Cashier { get; set; } = null!;
}