// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api.Extensions;
using AppDomain.Cashiers.Grpc;
using Google.Protobuf.WellKnownTypes;
using CashierModel = AppDomain.Cashiers.Grpc.Models.Cashier;

namespace AppDomain.Api.Cashiers;

public class CashierService(IMessageBus bus) : CashiersService.CashiersServiceBase
{
    public override async Task<CashierModel> GetCashier(GetCashierRequest request, ServerCallContext context)
    {
        var query = request.ToQuery(context.GetTenantId());
        var result = await bus.InvokeQueryAsync(query, context.CancellationToken);

        return result.Match(
            cashier => cashier.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.NotFound, string.Join("; ", errors))));
    }

    public override async Task<GetCashiersResponse> GetCashiers(GetCashiersRequest request, ServerCallContext context)
    {
        var query = request.ToQuery(context.GetTenantId());
        var cashiers = await bus.InvokeQueryAsync(query, context.CancellationToken);

        var cashiersGrpc = cashiers.Select(c => c.ToGrpc());

        return new GetCashiersResponse
        {
            Cashiers = { cashiersGrpc }
        };
    }

    public override async Task<CashierModel> CreateCashier(CreateCashierRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            cashier => cashier.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }

    public override async Task<CashierModel> UpdateCashier(UpdateCashierRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId(), Guid.Parse(request.CashierId));
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            cashier => cashier.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }

    public override async Task<Empty> DeleteCashier(DeleteCashierRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            _ => new Empty(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }
}