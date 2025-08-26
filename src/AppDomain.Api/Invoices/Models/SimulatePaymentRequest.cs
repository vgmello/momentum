// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.Api.Invoices.Models;

/// <summary>
///     Request to simulate a payment for testing purposes.
/// </summary>
/// <param name="Version">The current version of the invoice for optimistic concurrency control.</param>
/// <param name="Amount">The payment amount to simulate (required).</param>
/// <param name="Currency">The currency code for the payment (defaults to "USD").</param>
/// <param name="PaymentMethod">The payment method to simulate (defaults to "Credit Card").</param>
/// <param name="PaymentReference">The optional payment reference identifier (auto-generated if not provided).</param>
public record SimulatePaymentRequest(
    [property: JsonRequired] int Version,
    [property: JsonRequired] decimal Amount,
    string Currency = "USD",
    string PaymentMethod = "Credit Card",
    string? PaymentReference = null
);
