// Copyright (c) ORG_NAME. All rights reserved.

using System.Text.Json.Serialization;

namespace AppDomain.Api.Invoices.Models;

/// <summary>
///     Request to mark an invoice as paid with payment details.
/// </summary>
/// <param name="Version">The current version of the invoice for optimistic concurrency control.</param>
/// <param name="AmountPaid">The amount that was paid (required).</param>
/// <param name="PaymentDate">The optional date when payment was received (defaults to current time if not specified).</param>
public record MarkInvoiceAsPaidRequest(
    [property: JsonRequired] int Version,
    [property: JsonRequired] decimal AmountPaid,
    DateTime? PaymentDate = null);
