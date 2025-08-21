// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.Invoices.Actors;

/// <summary>
///     Grain interface for managing invoice state and processing in Orleans.
/// </summary>
public interface IInvoiceActor : IGrainWithGuidKey
{
    /// <summary>
    ///     Gets the current invoice data.
    /// </summary>
    /// <returns>The current invoice or null if not found</returns>
    Task<Invoice?> GetInvoiceAsync();

    /// <summary>
    ///     Marks the invoice as paid with the specified amount and date.
    /// </summary>
    /// <param name="amountPaid">Amount that was paid</param>
    /// <param name="paymentDate">Date when payment was received</param>
    /// <returns>The updated invoice</returns>
    Task<Invoice> MarkAsPaidAsync(decimal amountPaid, DateTime paymentDate);

    /// <summary>
    ///     Updates the invoice status.
    /// </summary>
    /// <param name="newStatus">New status to set</param>
    /// <returns>The updated invoice</returns>
    Task<Invoice> UpdateStatusAsync(string newStatus);

    /// <summary>
    ///     Processes a payment for this invoice.
    /// </summary>
    /// <param name="amount">Payment amount</param>
    /// <param name="paymentMethod">Method of payment</param>
    /// <returns>Processing result</returns>
    Task<bool> ProcessPaymentAsync(decimal amount, string paymentMethod);
}
