// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.BackOffice.Orleans.Invoices.Grains;
using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.BackOffice.Orleans.Invoices;

/// <summary>
/// Service for managing invoices using Orleans grains.
/// </summary>
public class InvoiceOrleansService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<InvoiceOrleansService> _logger;

    /// <summary>
    /// Initializes a new instance of the InvoiceOrleansService class.
    /// </summary>
    /// <param name="grainFactory">Orleans grain factory</param>
    /// <param name="logger">Logger instance</param>
    public InvoiceOrleansService(IGrainFactory grainFactory, ILogger<InvoiceOrleansService> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new invoice using Orleans grain.
    /// </summary>
    /// <param name="invoice">Invoice to create</param>
    /// <returns>The created invoice</returns>
    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        var grain = _grainFactory.GetGrain<IInvoiceGrain>(invoice.InvoiceId);
        return await grain.CreateInvoiceAsync(invoice);
    }

    /// <summary>
    /// Gets an invoice by its ID.
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <returns>The invoice if found, otherwise null</returns>
    public async Task<Invoice?> GetInvoiceAsync(Guid invoiceId)
    {
        var grain = _grainFactory.GetGrain<IInvoiceGrain>(invoiceId);
        return await grain.GetInvoiceAsync();
    }

    /// <summary>
    /// Processes a payment for an invoice.
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="paymentMethod">Payment method</param>
    /// <returns>True if payment was processed successfully</returns>
    public async Task<bool> ProcessPaymentAsync(Guid invoiceId, decimal amount, string paymentMethod)
    {
        var grain = _grainFactory.GetGrain<IInvoiceGrain>(invoiceId);
        return await grain.ProcessPaymentAsync(amount, paymentMethod);
    }

    /// <summary>
    /// Updates the status of an invoice.
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="newStatus">New status</param>
    /// <returns>The updated invoice</returns>
    public async Task<Invoice> UpdateInvoiceStatusAsync(Guid invoiceId, string newStatus)
    {
        var grain = _grainFactory.GetGrain<IInvoiceGrain>(invoiceId);
        return await grain.UpdateStatusAsync(newStatus);
    }
}
