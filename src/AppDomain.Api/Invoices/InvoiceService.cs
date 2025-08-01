// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Api.Extensions;
using AppDomain.Api.Invoices.Mappers;
using AppDomain.Invoices.Grpc;
using InvoiceModel = AppDomain.Invoices.Grpc.Models.Invoice;

namespace AppDomain.Api.Invoices;

public class InvoiceService(IMessageBus bus) : InvoicesService.InvoicesServiceBase
{
    public override async Task<InvoiceModel> GetInvoice(GetInvoiceRequest request, ServerCallContext context)
    {
        var query = request.ToQuery(context.GetTenantId());
        var result = await bus.InvokeQueryAsync(query, context.CancellationToken);

        return result.Match(
            invoice => invoice.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.NotFound, string.Join("; ", errors))));
    }

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

    public override async Task<InvoiceModel> CreateInvoice(CreateInvoiceRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            invoice => invoice.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }

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
