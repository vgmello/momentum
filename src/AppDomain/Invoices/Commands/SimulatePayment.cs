// Copyright (c) OrgName. All rights reserved.

using AppDomain.Invoices.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace AppDomain.Invoices.Commands;

/// <summary>
///     Command to simulate a payment for testing purposes.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="InvoiceId">Unique identifier for the invoice</param>
/// <param name="Version">Version number for optimistic concurrency control</param>
/// <param name="Amount">Payment amount to simulate</param>
/// <param name="Currency">Currency code for the payment amount</param>
/// <param name="PaymentMethod">Method used for the simulated payment</param>
/// <param name="PaymentReference">Reference identifier for the simulated payment</param>
public record SimulatePaymentCommand(
    Guid TenantId,
    Guid InvoiceId,
    int Version,
    decimal Amount,
    string Currency = "USD",
    string PaymentMethod = "Credit Card",
    string PaymentReference = "SIM-REF"
) : ICommand<Result<bool>>;

/// <summary>
///     Validator for the SimulatePaymentCommand.
/// </summary>
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

/// <summary>
///     Handler for the SimulatePaymentCommand.
/// </summary>
public static class SimulatePaymentCommandHandler
{
    /// <summary>
    ///     Handles the SimulatePaymentCommand and generates a payment received event for testing.
    /// </summary>
    /// <param name="command">The simulate payment command</param>
    /// <param name="messaging">The message bus for database operations (to check if invoice exists)</param>
    /// <param name="logger">Logger for tracking simulation operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the success result and payment received event</returns>
    public static async Task<(Result<bool>, PaymentReceived?)> Handle(
        SimulatePaymentCommand command, IMessageBus messaging, ILogger logger, CancellationToken cancellationToken)
    {
        var getInvoiceQuery = new Queries.GetInvoiceQuery(command.TenantId, command.InvoiceId);

        var invoiceResult = await messaging.InvokeQueryAsync(getInvoiceQuery, cancellationToken);

        var invoiceFound = invoiceResult.Match(
            invoice => invoice != null,
            errors =>
            {
                logger.LogWarning("Failed to retrieve invoice {InvoiceId}: {Errors}",
                    command.InvoiceId, string.Join(", ", errors.Select(e => e.ErrorMessage)));
                return false;
            }
        );

        if (!invoiceFound)
        {
            var failures = new List<ValidationFailure>
            {
                new("InvoiceId", "Invoice not found.")
            };

            return (failures, null);
        }

        var paymentReceivedEvent = new PaymentReceived(
            TenantId: command.TenantId,
            InvoiceId: command.InvoiceId,
            Currency: command.Currency,
            PaymentAmount: command.Amount,
            PaymentDate: DateTime.UtcNow,
            PaymentMethod: command.PaymentMethod,
            PaymentReference: command.PaymentReference
        );

        return (true, paymentReceivedEvent);
    }
}
