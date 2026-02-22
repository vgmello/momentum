// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Contracts.DomainEvents;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;

namespace AppDomain.Invoices.Commands;

/// <summary>
///     Command to create a new invoice in the system.
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
///     Validator for the CreateInvoiceCommand.
/// </summary>
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty().WithMessage("Tenant ID is required");
        RuleFor(c => c.Name).NotEmpty().WithMessage("Invoice name is required");
        RuleFor(c => c.Name).MinimumLength(2).WithMessage("Invoice name must be at least 2 characters");
        RuleFor(c => c.Name).MaximumLength(100).WithMessage("Invoice name cannot exceed 100 characters");
        RuleFor(c => c.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");
        RuleFor(c => c.Amount).LessThanOrEqualTo(1_000_000).WithMessage("Amount cannot exceed 1,000,000");
        RuleFor(c => c.Currency).Length(3).WithMessage("Currency must be a 3-character ISO code")
            .When(c => !string.IsNullOrWhiteSpace(c.Currency));
        RuleFor(c => c.DueDate).GreaterThanOrEqualTo(DateTime.Today).WithMessage("Due date cannot be in the past")
            .When(c => c.DueDate.HasValue);
    }
}

/// <summary>
///     Handler for the CreateInvoiceCommand.
/// </summary>
public static class CreateInvoiceCommandHandler
{
    /// <summary>
    ///     Storage / persistence request
    /// </summary>
    public record DbCommand(Data.Entities.Invoice Invoice) : ICommand<Data.Entities.Invoice>;

    /// <summary>
    ///     Handles the CreateInvoiceCommand and returns the created invoice with integration event.
    /// </summary>
    /// <param name="command">The create invoice command</param>
    /// <param name="messaging">The message bus for database operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the result and MULTIPLE events</returns>
    public static async Task<(Result<Invoice>, InvoiceCreated?, InvoiceGenerated?)> Handle(CreateInvoiceCommand command,
        IMessageBus messaging, CancellationToken cancellationToken)
    {
        var dbCommand = CreateInsertCommand(command);
        var insertedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedInvoice.ToModel();
        var createdEvent = new InvoiceCreated(command.TenantId, result.InvoiceId, result);
        var invoiceGenerated = new InvoiceGenerated(result.TenantId, result, result.CreatedDateUtc);

        return (result, createdEvent, invoiceGenerated);
    }

    /// <summary>
    ///     Database logic for creating a new invoice.
    /// </summary>
    /// <param name="command">The database command</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The inserted invoice entity</returns>
    [ExcludeFromCodeCoverage]
    public static async Task<Data.Entities.Invoice> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        var inserted = await db.Invoices.InsertWithOutputAsync(command.Invoice, cancellationToken);

        return inserted;
    }

    /// <summary>
    ///     Creates a database command from the create invoice command.
    /// </summary>
    /// <param name="command">The create invoice command</param>
    /// <returns>A database command with the invoice entity</returns>
    private static DbCommand CreateInsertCommand(CreateInvoiceCommand command) =>
        new(new Data.Entities.Invoice
        {
            TenantId = command.TenantId,
            InvoiceId = Guid.CreateVersion7(),
            Name = command.Name,
            Status = nameof(InvoiceStatus.Draft),
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
