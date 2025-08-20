// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api.Invoices.Mappers;
using AppDomain.Invoices.Grpc;
using InvoiceModel = AppDomain.Invoices.Grpc.Models.Invoice;

namespace AppDomain.Api.Invoices;

/// <summary>
///     gRPC service for invoice operations including retrieval, creation, cancellation, and payment processing.
/// </summary>
public class InvoiceService(IMessageBus bus) : InvoicesService.InvoicesServiceBase
{
    /// <summary>
    ///     Retrieves a specific invoice by its unique identifier.
    /// </summary>
    /// <param name="request">The gRPC request containing the invoice ID.</param>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>The invoice details if found.</returns>
    /// <exception cref="RpcException">Thrown when the invoice is not found.</exception>
    public override async Task<InvoiceModel> GetInvoice(GetInvoiceRequest request, ServerCallContext context)
    {
        var query = request.ToQuery(context.GetTenantId());
        var result = await bus.InvokeQueryAsync(query, context.CancellationToken);

        return result.Match(
            invoice => invoice.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.NotFound, string.Join("; ", errors))));
    }

    /// <summary>
    ///     Retrieves a list of invoices with optional filtering and pagination.
    /// </summary>
    /// <param name="request">The gRPC request containing filter criteria and pagination settings.</param>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>A response containing the list of invoices matching the criteria.</returns>
    public override async Task<GetInvoicesResponse> GetInvoices(GetInvoicesRequest request, ServerCallContext context)
    {
        var query = request.ToQuery(context.GetTenantId());
        var invoices = await bus.InvokeQueryAsync(query, context.CancellationToken);

        var invoicesGrpc = invoices.Select(i => i.ToGrpc());

        return new GetInvoicesResponse
        {
            Invoices = { invoicesGrpc }
        };
    }

    /// <summary>
    ///     Creates a new invoice in the system.
    /// </summary>
    /// <param name="request">The gRPC request containing invoice details like name, amount, currency, and due date.</param>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>The created invoice details.</returns>
    /// <exception cref="RpcException">Thrown when the request data is invalid or validation fails.</exception>
    public override async Task<InvoiceModel> CreateInvoice(CreateInvoiceRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            invoice => invoice.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }

    /// <summary>
    ///     Cancels an existing invoice, preventing further modifications.
    /// </summary>
    /// <param name="request">The gRPC request containing the invoice ID and version for optimistic concurrency.</param>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>The updated invoice details with cancelled status.</returns>
    /// <exception cref="RpcException">Thrown when the invoice cannot be cancelled or version conflicts occur.</exception>
    public override async Task<InvoiceModel> CancelInvoice(CancelInvoiceRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            invoice => invoice.ToGrpc(),
            errors => throw new RpcException(new Status(
                errors.IsConcurrencyConflict() ? StatusCode.FailedPrecondition : StatusCode.InvalidArgument,
                string.Join("; ", errors))));
    }

    /// <summary>
    ///     Marks an invoice as paid with the specified payment amount and date.
    /// </summary>
    /// <param name="request">The gRPC request containing payment details including version, amount, and optional payment date.</param>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>The updated invoice details with paid status.</returns>
    /// <exception cref="RpcException">Thrown when payment amount is insufficient or version conflicts occur.</exception>
    public override async Task<InvoiceModel> MarkInvoiceAsPaid(MarkInvoiceAsPaidRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            invoice => invoice.ToGrpc(),
            errors => throw new RpcException(new Status(
                errors.IsConcurrencyConflict() ? StatusCode.FailedPrecondition : StatusCode.InvalidArgument,
                string.Join("; ", errors))));
    }

    /// <summary>
    ///     Simulates a payment for testing purposes, triggering payment processing workflow.
    /// </summary>
    /// <param name="request">The gRPC request containing payment simulation details including version, amount, currency, method, and reference.</param>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>A response confirming that payment simulation was triggered.</returns>
    /// <exception cref="RpcException">Thrown when payment details are invalid or version conflicts occur.</exception>
    public override async Task<SimulatePaymentResponse> SimulatePayment(SimulatePaymentRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            _ => new SimulatePaymentResponse { Message = "Payment simulation triggered successfully" },
            errors => throw new RpcException(new Status(
                errors.IsConcurrencyConflict() ? StatusCode.FailedPrecondition : StatusCode.InvalidArgument,
                string.Join("; ", errors))));
    }
}
