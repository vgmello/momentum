// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Invoices.Contracts.IntegrationEvents;

namespace AppDomain.Invoices.Commands;

public record SimulatePaymentCommand(
    Guid TenantId,
    Guid InvoiceId,
    int Version,
    decimal Amount,
    string Currency = "USD",
    string PaymentMethod = "Credit Card",
    string PaymentReference = "SIM-REF"
) : ICommand<Result<bool>>;

public class SimulatePaymentValidator : AbstractValidator<SimulatePaymentCommand>
{
    public SimulatePaymentValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.InvoiceId).NotEmpty();
        RuleFor(c => c.Version).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty();
        RuleFor(c => c.PaymentMethod).NotEmpty();
        RuleFor(c => c.PaymentReference).NotEmpty();
    }
}

public static class SimulatePaymentCommandHandler
{
    public static Task<(Result<bool>, PaymentReceived)> Handle(SimulatePaymentCommand command)
    {
        var paymentReceivedEvent = new PaymentReceived(
            TenantId: command.TenantId,
            InvoiceId: command.InvoiceId,
            Currency: command.Currency,
            PaymentAmount: command.Amount,
            PaymentDate: DateTime.UtcNow,
            PaymentMethod: command.PaymentMethod,
            PaymentReference: command.PaymentReference
        );

        return Task.FromResult<(Result<bool>, PaymentReceived)>((true, paymentReceivedEvent));
    }
}
