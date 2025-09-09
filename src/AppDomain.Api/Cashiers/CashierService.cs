// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Grpc;
using Google.Protobuf.WellKnownTypes;
using CashierModel = AppDomain.Cashiers.Grpc.Models.Cashier;

namespace AppDomain.Api.Cashiers;

/// <summary>
///     gRPC service for managing cashiers.
/// </summary>
/// <param name="bus">The message bus for command and query handling</param>
public class CashierService(IMessageBus bus) : CashiersService.CashiersServiceBase
{
    /// <summary>
    ///     Gets a cashier by its identifier.
    /// </summary>
    /// <param name="request">The get cashier request containing the cashier identifier</param>
    /// <param name="context">The server call context</param>
    /// <returns>The cashier if found</returns>
    /// <exception cref="RpcException">Thrown when the cashier is not found</exception>
    public override async Task<CashierModel> GetCashier(GetCashierRequest request, ServerCallContext context)
    {
        var query = request.ToQuery(context.GetTenantId());
        var result = await bus.InvokeQueryAsync(query, context.CancellationToken);

        return result.Match(
            cashier => cashier.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.NotFound, string.Join("; ", errors))));
    }

    /// <summary>
    ///     Gets a list of cashiers with pagination.
    /// </summary>
    /// <param name="request">The get cashiers request containing pagination parameters</param>
    /// <param name="context">The server call context</param>
    /// <returns>A response containing the list of cashiers</returns>
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

    /// <summary>
    ///     Creates a new cashier.
    /// </summary>
    /// <param name="request">The create cashier request containing cashier details</param>
    /// <param name="context">The server call context</param>
    /// <returns>The created cashier</returns>
    /// <exception cref="RpcException">Thrown when the request is invalid</exception>
    public override async Task<CashierModel> CreateCashier(CreateCashierRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            cashier => cashier.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }

    /// <summary>
    ///     Updates an existing cashier.
    /// </summary>
    /// <param name="request">The update cashier request containing updated cashier details and identifier</param>
    /// <param name="context">The server call context</param>
    /// <returns>The updated cashier</returns>
    /// <exception cref="RpcException">Thrown when the request is invalid or the cashier is not found</exception>
    public override async Task<CashierModel> UpdateCashier(UpdateCashierRequest request, ServerCallContext context)
    {
        var cashierId = request.CashierId.ToGuidSafe("Invalid cashier ID format");
        var command = request.ToCommand(context.GetTenantId(), cashierId);
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            cashier => cashier.ToGrpc(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }

    /// <summary>
    ///     Deletes a cashier.
    /// </summary>
    /// <param name="request">The delete cashier request containing the cashier identifier</param>
    /// <param name="context">The server call context</param>
    /// <returns>An empty response if successful</returns>
    /// <exception cref="RpcException">Thrown when the request is invalid or the cashier is not found</exception>
    public override async Task<Empty> DeleteCashier(DeleteCashierRequest request, ServerCallContext context)
    {
        var command = request.ToCommand(context.GetTenantId());
        var result = await bus.InvokeCommandAsync(command, context.CancellationToken);

        return result.Match(
            _ => new Empty(),
            errors => throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join("; ", errors))));
    }
}
