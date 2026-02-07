// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Cashiers.Contracts.Models;

/// <summary>
///     Represents a payment transaction processed by a cashier.
/// </summary>
public record CashierPayment
{
    /// <summary>
    ///     Gets or sets the unique identifier of the cashier who processed this payment.
    /// </summary>
    public Guid CashierId { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier for this payment transaction.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when the payment was processed.
    /// </summary>
    public DateTime PaymentDate { get; set; }
}
