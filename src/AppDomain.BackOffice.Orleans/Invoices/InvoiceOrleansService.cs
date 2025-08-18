// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.BackOffice.Orleans.Invoices.Grains;
using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.BackOffice.Orleans.Invoices;

/// <summary>
///     Service for managing invoices using Orleans grains.
/// </summary>
public class InvoiceOrleansService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<InvoiceOrleansService> _logger;

    /// <summary>
    ///     Initializes a new instance of the InvoiceOrleansService class.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory used to create and access invoice grains.</param>
    /// <param name="logger">The logger instance for recording service operations.</param>
    public InvoiceOrleansService(IGrainFactory grainFactory, ILogger<InvoiceOrleansService> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    /// <summary>
    ///     Creates a new invoice using Orleans grain.
    /// </summary>
    /// <param name="invoice">The invoice data to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created invoice.</returns>
    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        var grain = _grainFactory.GetGrain<IInvoiceGrain>(invoice.InvoiceId);

        return await grain.CreateInvoiceAsync(invoice);
    }

    /// <summary>
    ///     Gets an invoice by its ID.
    /// </summary>
    /// <param name="invoiceId">The unique identifier of the invoice to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the invoice if found; otherwise, <c>null</c>.</returns>
    public async Task<Invoice?> GetInvoiceAsync(Guid invoiceId)
    {
        var grain = _grainFactory.GetGrain<IInvoiceGrain>(invoiceId);

        return await grain.GetInvoiceAsync();
    }

    /// <summary>
    ///     Processes a payment for an invoice.
    /// </summary>
    /// <param name="invoiceId">The unique identifier of the invoice to process payment for.</param>
    /// <param name="amount">The payment amount to process.</param>
    /// <param name="paymentMethod">The payment method used for the transaction.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains <c>true</c> if the payment was processed successfully;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> ProcessPaymentAsync(Guid invoiceId, decimal amount, string paymentMethod)
    {
        var grain = _grainFactory.GetGrain<IInvoiceGrain>(invoiceId);

        return await grain.ProcessPaymentAsync(amount, paymentMethod);
    }

    /// <summary>
    ///     Updates the status of an invoice.
    /// </summary>
    /// <param name="invoiceId">The unique identifier of the invoice to update.</param>
    /// <param name="newStatus">The new status to set for the invoice.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated invoice.</returns>
    public async Task<Invoice> UpdateInvoiceStatusAsync(Guid invoiceId, string newStatus)
    {
        var grain = _grainFactory.GetGrain<IInvoiceGrain>(invoiceId);

        return await grain.UpdateStatusAsync(newStatus);
    }
}
