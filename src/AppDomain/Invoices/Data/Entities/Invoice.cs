// Copyright (c) ORG_NAME. All rights reserved.

using LinqToDB.Mapping;
//#if (INCLUDE_ORLEANS)
using Orleans;
//#endif

namespace AppDomain.Invoices.Data.Entities;

#if (INCLUDE_ORLEANS)
/// <summary>
///     Invoice entity with Orleans serialization attributes.
/// </summary>
[GenerateSerializer]
[Alias("AppDomain.Invoices.Data.Entities.Invoice")]
public record Invoice : DbEntity
{
    /// <summary>
    ///     Gets or sets the tenant identifier.
    /// </summary>
    [PrimaryKey(order: 0)]
    [Id(3)]
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the invoice identifier.
    /// </summary>
    [PrimaryKey(order: 1)]
    [Id(4)]
    public Guid InvoiceId { get; set; }

    /// <summary>
    ///     Gets or sets the invoice name or description.
    /// </summary>
    [Id(5)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the invoice status.
    /// </summary>
    [Id(6)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the total amount of the invoice.
    /// </summary>
    [Id(7)]
    public decimal Amount { get; set; }

    /// <summary>
    ///     Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    [Id(8)]
    public string? Currency { get; set; }

    /// <summary>
    ///     Gets or sets the due date for payment.
    /// </summary>
    [Id(9)]
    public DateTime? DueDate { get; set; }

    /// <summary>
    ///     Gets or sets the cashier identifier handling this invoice.
    /// </summary>
    [Id(10)]
    public Guid? CashierId { get; set; }

    /// <summary>
    ///     Gets or sets the amount that has been paid.
    /// </summary>
    [Id(11)]
    public decimal? AmountPaid { get; set; }

    /// <summary>
    ///     Gets or sets the date when payment was received.
    /// </summary>
    [Id(12)]
    public DateTime? PaymentDate { get; set; }
}

#else
/// <summary>
///     Invoice entity for database operations.
/// </summary>
public record Invoice : DbEntity
{
    /// <summary>
    ///     Gets or sets the tenant identifier.
    /// </summary>
    [PrimaryKey(order: 0)]
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the invoice identifier.
    /// </summary>
    [PrimaryKey(order: 1)]
    public Guid InvoiceId { get; set; }

    /// <summary>
    ///     Gets or sets the invoice name or description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the invoice status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the total amount of the invoice.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    ///     Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    ///     Gets or sets the due date for payment.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    ///     Gets or sets the cashier identifier handling this invoice.
    /// </summary>
    public Guid? CashierId { get; set; }

    /// <summary>
    ///     Gets or sets the amount that has been paid.
    /// </summary>
    public decimal? AmountPaid { get; set; }

    /// <summary>
    ///     Gets or sets the date when payment was received.
    /// </summary>
    public DateTime? PaymentDate { get; set; }
}
#endif
