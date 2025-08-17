// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.BackOffice.Orleans.Invoices.Grains;

/// <summary>
/// Represents the persistent state for an invoice grain in Orleans storage.
/// Contains the essential invoice data that needs to be persisted across grain activations.
/// </summary>
[GenerateSerializer]
public sealed class InvoiceState
{
    /// <summary>
    /// Gets or sets the total amount of the invoice.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the invoice has been paid.
    /// </summary>
    [Id(1)]
    public bool Paid { get; set; }
}
