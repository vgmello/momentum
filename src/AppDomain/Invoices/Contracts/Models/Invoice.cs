// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.Invoices.Contracts.Models;

public record Invoice
{
    public Guid TenantId { get; set; }

    public Guid InvoiceId { get; set; }

    public required string Name { get; set; }

    public required string Status { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public DateTime? DueDate { get; set; }

    public Guid? CashierId { get; set; }

    public decimal? AmountPaid { get; set; }

    public DateTime? PaymentDate { get; set; }

    public DateTime CreatedDateUtc { get; set; }

    public DateTime UpdatedDateUtc { get; set; }

    public int Version { get; set; }
}
