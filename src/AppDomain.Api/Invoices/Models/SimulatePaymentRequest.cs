// Copyright (c) ORG_NAME. All rights reserved.

using System.Text.Json.Serialization;

namespace AppDomain.Api.Invoices.Models;

public record SimulatePaymentRequest(
    [property: JsonRequired] int Version,
    [property: JsonRequired] decimal Amount,
    string? Currency = "USD",
    string? PaymentMethod = "Credit Card",
    string? PaymentReference = null
);