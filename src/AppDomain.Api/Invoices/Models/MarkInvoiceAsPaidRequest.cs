// Copyright (c) ABCDEG. All rights reserved.

using System.Text.Json.Serialization;

namespace AppDomain.Api.Invoices.Models;

public record MarkInvoiceAsPaidRequest(
    [property: JsonRequired] int Version,
    [property: JsonRequired] decimal AmountPaid,
    DateTime? PaymentDate = null);
