// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Api.Invoices.Models;

/// <summary>
///     Request to create a new invoice in the system.
/// </summary>
/// <param name="Name">The name or description of the invoice.</param>
/// <param name="Amount">The invoice amount (required).</param>
/// <param name="Currency">The currency code for the invoice amount (defaults to empty string).</param>
/// <param name="DueDate">The optional due date for payment.</param>
/// <param name="CashierId">The optional unique identifier of the cashier associated with this invoice.</param>
public record CreateInvoiceRequest(
    string Name,
    [property: JsonRequired] decimal Amount,
    string Currency = "",
    DateTime? DueDate = null,
    Guid? CashierId = null
);
