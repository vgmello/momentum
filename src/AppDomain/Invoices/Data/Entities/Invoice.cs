// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Core.Data;
using LinqToDB.Mapping;

namespace AppDomain.Invoices.Data.Entities;

/// <summary>
/// Database entity representing an invoice in the AppDomain system.
/// </summary>
[Table("invoices")]
public record Invoice : DbEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant.
    /// </summary>
    [Column("tenant_id")]
    public required Guid TenantId { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier for the invoice.
    /// </summary>
    [Column("invoice_id")]
    [PrimaryKey]
    public required Guid InvoiceId { get; init; }

    /// <summary>
    /// Gets or sets the invoice name or description.
    /// </summary>
    [Column("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the current status of the invoice.
    /// </summary>
    [Column("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Gets or sets the total amount of the invoice.
    /// </summary>
    [Column("amount")]
    public required decimal Amount { get; init; }

    /// <summary>
    /// Gets or sets the currency code for the invoice amount.
    /// </summary>
    [Column("currency")]
    public string? Currency { get; init; }

    /// <summary>
    /// Gets or sets the due date for the invoice payment.
    /// </summary>
    [Column("due_date")]
    public DateTime? DueDate { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the cashier handling this invoice.
    /// </summary>
    [Column("cashier_id")]
    public Guid? CashierId { get; init; }

    /// <summary>
    /// Gets or sets the amount that has been paid towards this invoice.
    /// </summary>
    [Column("amount_paid")]
    public decimal? AmountPaid { get; init; }

    /// <summary>
    /// Gets or sets the date when the invoice was paid.
    /// </summary>
    [Column("payment_date")]
    public DateTime? PaymentDate { get; init; }

    /// <summary>
    /// Gets or sets the UTC date and time when the invoice was created.
    /// </summary>
    [Column("created_date_utc")]
    public required DateTime CreatedDateUtc { get; init; }

    /// <summary>
    /// Gets or sets the UTC date and time when the invoice was last updated.
    /// </summary>
    [Column("updated_date_utc")]
    public required DateTime UpdatedDateUtc { get; init; }
}