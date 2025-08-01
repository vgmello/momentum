// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Core.Data;
using AppDomain.Invoices.Contracts.IntegrationEvents;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Data;
using LinqToDB;

namespace AppDomain.Invoices.Commands;

public record CreateInvoiceCommand(
    Guid TenantId,
    string Name,
    decimal Amount,
    string? Currency = "",
    DateTime? DueDate = null,
    Guid? CashierId = null
) : ICommand<Result<Invoice>>;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Name).MaximumLength(100);
        RuleFor(c => c.Name).MinimumLength(2);
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).MaximumLength(3).When(c => !string.IsNullOrEmpty(c.Currency));
    }
}

public static class CreateInvoiceCommandHandler
{
    public record DbCommand(Data.Entities.Invoice Invoice) : ICommand<Data.Entities.Invoice>;

    public static async Task<(Result<Invoice>, InvoiceCreated)> Handle(CreateInvoiceCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = new DbCommand(new Data.Entities.Invoice
        {
            TenantId = command.TenantId,
            InvoiceId = Guid.CreateVersion7(),
            Name = command.Name,
            Status = "Draft",
            Amount = command.Amount,
            Currency = command.Currency,
            DueDate = command.DueDate,
            CashierId = command.CashierId,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });

        var insertedInvoice = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedInvoice.ToModel();
        var createdEvent = new InvoiceCreated(command.TenantId, result);

        return (result, createdEvent);
    }

    public static async Task<Data.Entities.Invoice> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        var inserted = await db.Invoices.InsertWithOutputAsync(command.Invoice, token: cancellationToken);

        return inserted;
    }
}
