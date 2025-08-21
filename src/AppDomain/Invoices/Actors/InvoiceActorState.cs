// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Actors;

/// <summary>
///     State class for persisting invoice data in Orleans grain storage.
///     Contains the invoice data and metadata for Orleans grain persistence.
/// </summary>
[GenerateSerializer]
public sealed class InvoiceActorState
{
    /// <summary>
    ///     Gets or sets the invoice domain record.
    /// </summary>
    [Id(0)]
    public Invoice? Invoice { get; set; }

    /// <summary>
    ///     Gets or sets the last updated timestamp.
    /// </summary>
    [Id(1)]
    public DateTime LastUpdated { get; set; }
}
