// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Invoices.Contracts.Models;

/// <summary>
///     Represents the possible states of an invoice.
/// </summary>
public enum InvoiceStatus
{
    Draft,

    Paid,

    Cancelled
}
