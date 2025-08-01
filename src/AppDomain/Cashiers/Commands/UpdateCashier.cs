// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.IntegrationEvents;
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Data;
using AppDomain.Core.Data;
using FluentValidation.Results;
using LinqToDB;

namespace AppDomain.Cashiers.Commands;

public record UpdateCashierCommand(Guid TenantId, Guid CashierId, string Name, string? Email, int Version) : ICommand<Result<Cashier>>;

public class UpdateCashierValidator : AbstractValidator<UpdateCashierCommand>
{
    public UpdateCashierValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.CashierId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Name).MaximumLength(100);
        RuleFor(c => c.Name).MinimumLength(2);
    }
}

public static class UpdateCashierCommandHandler
{
    public record DbCommand(Guid TenantId, Guid CashierId, string Name, string? Email, int Version) : ICommand<Data.Entities.Cashier?>;

    public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(UpdateCashierCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        if (command.Name.Contains("error"))
        {
            throw new DivideByZeroException("Forced test unhandled exception to simulate error scenarios");
        }

        var dbCommand = new DbCommand(command.TenantId, command.CashierId, command.Name, command.Email, command.Version);
        var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (updatedCashier is null)
        {
            var failures = new List<ValidationFailure> { new("CashierId", "Cashier not found") };

            return (failures, null);
        }

        var result = updatedCashier.ToModel();
        var updatedEvent = new CashierUpdated(command.TenantId, command.CashierId);

        return (result, updatedEvent);
    }

    public static async Task<Data.Entities.Cashier?> Handle(DbCommand command, AppDomainDb db, CancellationToken cancellationToken)
    {
        var statement = db.Cashiers
            .Where(c => c.TenantId == command.TenantId && c.CashierId == command.CashierId)
            .Where(c => c.Version == command.Version)
            .Set(p => p.Name, command.Name);

        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            statement = statement.Set(p => p.Email, command.Email);
        }

        var updatedRecords = await statement.UpdateWithOutputAsync((_, inserted) => inserted, token: cancellationToken);

        return updatedRecords.FirstOrDefault();
    }
}
