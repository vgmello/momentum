// Copyright (c) ABCDEG. All rights reserved.

namespace AppDomain.Invoices.Contracts.Models;

/// <summary>
/// Domain model representing an invoice in the AppDomain system.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="InvoiceId">Unique identifier for the invoice</param>
/// <param name="Name">Invoice name or description</param>
/// <param name="Status">Current status of the invoice</param>
/// <param name="Amount">Total amount of the invoice</param>
/// <param name="Currency">Currency code for the invoice amount</param>
/// <param name="DueDate">Due date for the invoice payment</param>
/// <param name="CashierId">Identifier of the cashier handling this invoice</param>
/// <param name="AmountPaid">Amount that has been paid towards this invoice</param>
/// <param name="PaymentDate">Date when the invoice was paid</param>
/// <param name="CreatedDateUtc">Date and time when the invoice was created (UTC)</param>
/// <param name="UpdatedDateUtc">Date and time when the invoice was last updated (UTC)</param>
public record Invoice(
    Guid TenantId,
    Guid InvoiceId,
    string Name,
    string Status,
    decimal Amount,
    string? Currency,
    DateTime? DueDate,
    Guid? CashierId,
    decimal? AmountPaid,
    DateTime? PaymentDate,
    DateTime CreatedDateUtc,
    DateTime UpdatedDateUtc
);