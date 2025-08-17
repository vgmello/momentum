// Copyright (c) ORG_NAME. All rights reserved.

using LinqToDB.Mapping;

namespace AppDomain.Invoices.Data.Entities;

public record Invoice : DbEntity
{
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    [PrimaryKey(order: 1)]
    public Guid InvoiceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public DateTime? DueDate { get; set; }

    public Guid? CashierId { get; set; }

    public decimal? AmountPaid { get; set; }

    public DateTime? PaymentDate { get; set; }
}
