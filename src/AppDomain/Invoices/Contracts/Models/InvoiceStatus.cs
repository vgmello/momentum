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

/// <summary>
///     Extension methods for <see cref="InvoiceStatus" />.
/// </summary>
public static class InvoiceStatusExtensions
{
    /// <summary>
    ///     Converts the status to its lowercase database string representation.
    /// </summary>
    public static string ToDbString(this InvoiceStatus status) => status.ToString().ToLowerInvariant();
}
