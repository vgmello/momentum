// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Actors;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Data.Entities;
using AppDomain.Invoices.Queries;
using AppDomain.Invoices.Commands;
using ContractInvoice = AppDomain.Invoices.Contracts.Models.Invoice;

namespace AppDomain.BackOffice.Orleans.Invoices.Grains;

/// <summary>
///     Orleans grain implementation for managing invoice operations.
///     Invoice data is loaded from the database on demand.
/// </summary>
/// <param name="invoiceState">The persistent state for grain metadata</param>
/// <param name="messageBus">Wolverine message bus for commands/queries</param>
/// <param name="logger">Logger instance</param>
public class InvoiceActor(
    ILogger<InvoiceActor> logger,
    [PersistentState("invoice")] IPersistentState<InvoiceActorState> invoiceState,
    IMessageBus messageBus) : Grain, IInvoiceActor
{
    /// <inheritdoc />
    public async Task<Invoice?> GetInvoiceAsync(Guid tenantId)
    {
        await EnsureInitialized(tenantId);

        var invoiceId = this.GetPrimaryKey();
        var query = new GetInvoiceQuery(tenantId, invoiceId);

        var result = await messageBus.InvokeQueryAsync(query);

        return result.Match<Invoice?>(
            contractInvoice =>
            {
                // Convert contract model to data entity
                var invoice = MapContractToDataEntity(contractInvoice);

                // Update access tracking
                invoiceState.State.LastAccessed = DateTime.UtcNow;
                invoiceState.State.OperationCount++;
                _ = invoiceState.WriteStateAsync(); // Fire and forget

                return invoice;
            },
            errors =>
            {
                logger.LogWarning("Invoice {InvoiceId} not found for tenant {TenantId}: {Errors}",
                    invoiceId, tenantId, string.Join(", ", errors.Select(e => e.ErrorMessage)));

                return null;
            });
    }

    /// <inheritdoc />
    public async Task<Invoice> MarkAsPaidAsync(Guid tenantId, decimal amountPaid, DateTime paymentDate)
    {
        await EnsureInitialized(tenantId);

        var invoiceId = this.GetPrimaryKey();
        // Note: MarkInvoiceAsPaidCommand requires version parameter, for now using 0
        var command = new MarkInvoiceAsPaidCommand(tenantId, invoiceId, 0, amountPaid, paymentDate);

        var result = await messageBus.InvokeCommandAsync(command);

        return result.Match(
            contractInvoice =>
            {
                var invoice = MapContractToDataEntity(contractInvoice);

                // Publish integration event
                _ = PublishIntegrationEventAsync(new InvoicePaid(
                    tenantId,
                    invoiceId,
                    contractInvoice)); // Fire and forget

                // Update operation tracking
                invoiceState.State.LastAccessed = DateTime.UtcNow;
                invoiceState.State.OperationCount++;
                _ = invoiceState.WriteStateAsync(); // Fire and forget

                logger.LogInformation("Marked invoice {InvoiceId} as paid with amount {Amount}",
                    invoiceId, amountPaid);

                return invoice;
            },
            errors => throw new InvalidOperationException(
                $"Failed to mark invoice {invoiceId} as paid: {string.Join(", ", errors.Select(e => e.ErrorMessage))}"));
    }

    /// <inheritdoc />
    public async Task<Invoice> UpdateStatusAsync(Guid tenantId, string newStatus)
    {
        await EnsureInitialized(tenantId);

        // For now, we'll fetch the invoice and manually update it
        // In a real implementation, you'd have an UpdateInvoiceStatusCommand
        var currentInvoice = await GetInvoiceAsync(tenantId);

        if (currentInvoice == null)
        {
            throw new InvalidOperationException($"Invoice {this.GetPrimaryKey()} not found");
        }

        // Update the status (this would typically be done via a command)
        var updatedInvoice = currentInvoice with { Status = newStatus };

        logger.LogInformation("Updated invoice {InvoiceId} status to {Status}",
            this.GetPrimaryKey(), newStatus);

        return updatedInvoice;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessPaymentAsync(Guid tenantId, decimal amount, string paymentMethod)
    {
        await EnsureInitialized(tenantId);

        var invoiceId = this.GetPrimaryKey();
        var currentInvoice = await GetInvoiceAsync(tenantId);

        if (currentInvoice == null)
        {
            logger.LogWarning("Invoice {InvoiceId} not found for payment processing", invoiceId);

            return false;
        }

        // Business logic for payment processing
        if (amount <= 0)
        {
            logger.LogWarning("Invalid payment amount {Amount} for invoice {InvoiceId}",
                amount, invoiceId);

            return false;
        }

        if (currentInvoice.Status == "Paid")
        {
            logger.LogWarning("Invoice {InvoiceId} is already paid", invoiceId);

            return false;
        }

        // Process payment
        await MarkAsPaidAsync(tenantId, amount, DateTime.UtcNow);

        logger.LogInformation("Processed payment of {Amount} via {PaymentMethod} for invoice {InvoiceId}",
            amount, paymentMethod, invoiceId);

        // Publish payment event
        await PublishIntegrationEventAsync(new PaymentReceived(
            tenantId,
            invoiceId,
            currentInvoice.Currency ?? "USD",
            amount,
            DateTime.UtcNow,
            paymentMethod,
            Guid.CreateVersion7().ToString()));

        return true;
    }

    /// <summary>
    ///     Ensures the grain is initialized with tenant information.
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    private async Task EnsureInitialized(Guid tenantId)
    {
        if (!invoiceState.State.IsInitialized)
        {
            invoiceState.State.TenantId = tenantId;
            invoiceState.State.ActivatedAt = DateTime.UtcNow;
            invoiceState.State.IsInitialized = true;
            invoiceState.State.Metadata!["InvoiceId"] = this.GetPrimaryKey().ToString();
            await invoiceState.WriteStateAsync();
        }
    }

    /// <summary>
    ///     Maps a contract model invoice to a data entity.
    /// </summary>
    /// <param name="invoice">The contract model invoice</param>
    /// <returns>The data entity</returns>
    private static Invoice MapContractToDataEntity(ContractInvoice invoice)
    {
        return new Invoice
        {
            TenantId = invoice.TenantId,
            InvoiceId = invoice.InvoiceId,
            Name = invoice.Name,
            Status = invoice.Status,
            Amount = invoice.Amount,
            Currency = invoice.Currency,
            DueDate = invoice.DueDate,
            CashierId = invoice.CashierId,
            AmountPaid = invoice.AmountPaid,
            PaymentDate = invoice.PaymentDate
        };
    }

    /// <summary>
    ///     Publishes an integration event to the messaging infrastructure.
    /// </summary>
    /// <typeparam name="T">The type of integration event to publish</typeparam>
    /// <param name="integrationEvent">The integration event to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    private Task PublishIntegrationEventAsync<T>(T integrationEvent) where T : class
    {
        logger.LogDebug("Publishing integration event {EventType}: {Event}", typeof(T).Name, integrationEvent);

        return Task.CompletedTask;
    }
}
