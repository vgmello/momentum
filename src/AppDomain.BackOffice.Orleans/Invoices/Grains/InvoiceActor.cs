// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Actors;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;

namespace AppDomain.BackOffice.Orleans.Invoices.Grains;

/// <summary>
///     Orleans grain implementation for managing invoice state and processing.
/// </summary>
/// <remarks>
///     Initializes a new instance of the InvoiceGrain class.
/// </remarks>
/// <param name="invoiceState">The persistent state for the invoice</param>
/// <param name="logger">Logger instance</param>
public class InvoiceActor(
    [PersistentState("invoice", "Default")]
    IPersistentState<InvoiceActorState> invoiceState,
    ILogger<InvoiceActor> logger) : Grain, IInvoiceActor
{
    /// <inheritdoc />
    public async Task<Invoice> LoadInvoice(Invoice invoice)
    {
        if (invoiceState.State.Invoice != null)
        {
            throw new InvalidOperationException($"Invoice {this.GetPrimaryKey()} already exists");
        }

        invoiceState.State.Invoice = invoice;
        invoiceState.State.LastUpdated = DateTime.UtcNow;

        await invoiceState.WriteStateAsync();

        logger.LogInformation("Created invoice {InvoiceId} for tenant {TenantId}",
            invoice.InvoiceId, invoice.TenantId);

        // Publish integration event
        await PublishIntegrationEventAsync(new InvoiceCreated(
            invoice.TenantId,
            invoice.InvoiceId,
            invoice));

        return invoice;
    }

    /// <inheritdoc />
    public Task<Invoice?> GetInvoiceAsync()
    {
        return Task.FromResult(invoiceState.State.Invoice);
    }

    /// <inheritdoc />
    public async Task<Invoice> MarkAsPaidAsync(decimal amountPaid, DateTime paymentDate)
    {
        if (invoiceState.State.Invoice == null)
        {
            throw new InvalidOperationException($"Invoice {this.GetPrimaryKey()} not found");
        }

        var updatedInvoice = invoiceState.State.Invoice with
        {
            AmountPaid = amountPaid,
            PaymentDate = paymentDate,
            Status = "Paid",
            UpdatedDateUtc = DateTime.UtcNow
        };

        invoiceState.State.Invoice = updatedInvoice;
        invoiceState.State.LastUpdated = DateTime.UtcNow;

        await invoiceState.WriteStateAsync();

        logger.LogInformation("Marked invoice {InvoiceId} as paid with amount {Amount}",
            updatedInvoice.InvoiceId, amountPaid);

        // Publish integration event
        await PublishIntegrationEventAsync(new InvoicePaid(
            updatedInvoice.TenantId,
            updatedInvoice.InvoiceId,
            updatedInvoice));

        return updatedInvoice;
    }

    /// <inheritdoc />
    public async Task<Invoice> UpdateStatusAsync(string newStatus)
    {
        if (invoiceState.State.Invoice == null)
        {
            throw new InvalidOperationException($"Invoice {this.GetPrimaryKey()} not found");
        }

        var updatedInvoice = invoiceState.State.Invoice with
        {
            Status = newStatus,
            UpdatedDateUtc = DateTime.UtcNow
        };

        invoiceState.State.Invoice = updatedInvoice;
        invoiceState.State.LastUpdated = DateTime.UtcNow;

        await invoiceState.WriteStateAsync();

        logger.LogInformation("Updated invoice {InvoiceId} status to {Status}",
            updatedInvoice.InvoiceId, newStatus);

        return updatedInvoice;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessPaymentAsync(decimal amount, string paymentMethod)
    {
        if (invoiceState.State.Invoice == null)
        {
            throw new InvalidOperationException($"Invoice {this.GetPrimaryKey()} not found");
        }

        var invoice = invoiceState.State.Invoice;

        // Business logic for payment processing
        if (amount <= 0)
        {
            logger.LogWarning("Invalid payment amount {Amount} for invoice {InvoiceId}",
                amount, invoice.InvoiceId);

            return false;
        }

        if (invoice.Status == "Paid")
        {
            logger.LogWarning("Invoice {InvoiceId} is already paid", invoice.InvoiceId);

            return false;
        }

        // Process payment
        await MarkAsPaidAsync(amount, DateTime.UtcNow);

        logger.LogInformation("Processed payment of {Amount} via {PaymentMethod} for invoice {InvoiceId}",
            amount, paymentMethod, invoice.InvoiceId);

        // Publish payment event
        await PublishIntegrationEventAsync(new PaymentReceived(
            invoice.TenantId,
            invoice.InvoiceId,
            invoice.Currency ?? "USD",
            amount,
            DateTime.UtcNow,
            paymentMethod,
            Guid.NewGuid().ToString()));

        return true;
    }

    /// <summary>
    ///     Publishes an integration event to the messaging infrastructure.
    /// </summary>
    /// <typeparam name="T">The type of integration event to publish</typeparam>
    /// <param name="integrationEvent">The integration event to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private Task PublishIntegrationEventAsync<T>(T integrationEvent) where T : class
    {
        // TODO: Implement integration event publishing via Kafka/Wolverine
        // This would typically use the messaging infrastructure to publish events
        logger.LogDebug("Publishing integration event {EventType}: {Event}",
            typeof(T).Name, integrationEvent);

        return Task.CompletedTask;
    }
}
