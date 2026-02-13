// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Data.Entities;
using InvoiceStatus = AppDomain.Invoices.Contracts.Models.InvoiceStatus;

namespace AppDomain.Invoices.Actors;

/// <summary>
///     Grain interface for managing invoice operations in Orleans.
///     Invoice data is loaded from the database on demand.
/// </summary>
public interface IInvoiceActor : IGrainWithGuidKey
{
    /// <summary>
    ///     Gets the current invoice data from the database.
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <returns>The current invoice or null if not found</returns>
    Task<Invoice?> GetInvoiceAsync(Guid tenantId);

    /// <summary>
    ///     Marks the invoice as paid with the specified amount and date.
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="amountPaid">Amount that was paid</param>
    /// <param name="paymentDate">Date when payment was received</param>
    /// <returns>The updated invoice</returns>
    Task<Invoice> MarkAsPaidAsync(Guid tenantId, decimal amountPaid, DateTime paymentDate);

    /// <summary>
    ///     Updates the invoice status.
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="newStatus">New status to set</param>
    /// <returns>The updated invoice</returns>
    Task<Invoice> UpdateStatusAsync(Guid tenantId, InvoiceStatus newStatus);

    /// <summary>
    ///     Processes a payment for this invoice.
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="paymentMethod">Method of payment</param>
    /// <returns>Processing result</returns>
    Task<bool> ProcessPaymentAsync(Guid tenantId, decimal amount, string paymentMethod);
}
