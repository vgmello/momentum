<!--#if (includeSample)-->
// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;
using AppDomain.Core.Data;
using FluentValidation;
using LinqToDB;
using Momentum.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using Wolverine;

namespace AppDomain.Invoices.Commands;

/// <summary>
/// Command to create a new invoice in the system.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="Name">Invoice name or description</param>
/// <param name="Amount">Total amount of the invoice</param>
/// <param name="Currency">Currency code for the invoice amount</param>
/// <param name="DueDate">Due date for the invoice payment</param>
/// <param name="CashierId">Identifier of the cashier handling this invoice</param>
public record CreateInvoiceCommand(
    Guid TenantId,
    string Name,
    decimal Amount,
    string? Currency,
    DateTime? DueDate,
    Guid? CashierId
) : ICommand<Result<Invoice>>;

/// <summary>
/// Validator for the CreateInvoiceCommand.
/// </summary>
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateInvoiceValidator"/> class.
    /// </summary>
    public CreateInvoiceValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Name).MinimumLength(2);
        RuleFor(c => c.Name).MaximumLength(200);
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).MaximumLength(3);
    }
}

/// <summary>
/// Handler for the CreateInvoiceCommand.
/// </summary>
public static class CreateInvoiceCommandHandler
{
    /// <summary>
    /// Database command for inserting an invoice.
    /// </summary>
    /// <param name="Invoice">The invoice entity to insert</param>
    public record DbCommand(Data.Entities.Invoice Invoice) : ICommand<Data.Entities.Invoice>;

    /// <summary>
    /// Handles the CreateInvoiceCommand and returns the created invoice with integration event.
    /// </summary>
    /// <param name="command">The create invoice command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and integration event</returns>
    public static async Task<(Result<Invoice>, InvoiceCreated?)> Handle(CreateInvoiceCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = CreateInsertCommand(command);
        var insertedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedInvoice.ToModel();
        var createdEvent = new InvoiceCreated(result.TenantId, PartitionKeyTest: 0, result);

        return (result, createdEvent);
    }

    /// <summary>
    /// Handles the database command for inserting an invoice.
    /// </summary>
    /// <param name="command">The database command</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The inserted invoice entity</returns>
    public static async Task<Data.Entities.Invoice> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        var inserted = await db.Invoices.InsertWithOutputAsync(command.Invoice, token: cancellationToken);

        return inserted;
    }

    private static DbCommand CreateInsertCommand(CreateInvoiceCommand command) =>
        new(new Data.Entities.Invoice
        {
            TenantId = command.TenantId,
            InvoiceId = Guid.CreateVersion7(),
            Name = command.Name,
            Status = "Draft",
            Amount = command.Amount,
            Currency = command.Currency,
            DueDate = command.DueDate,
            CashierId = command.CashierId,
            AmountPaid = null,
            PaymentDate = null,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
<!--#endif-->