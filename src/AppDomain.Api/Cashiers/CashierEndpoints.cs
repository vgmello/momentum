// Copyright (c) OrgName. All rights reserved.

using AppDomain.Api.Cashiers.Models;
using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Queries;

namespace AppDomain.Api.Cashiers;

/// <summary>
///     Defines minimal API endpoints for cashier operations.
/// </summary>
public static class CashierEndpoints
{
    /// <summary>
    ///     Maps all cashier REST endpoints under the /cashiers route group.
    /// </summary>
    public static RouteGroupBuilder MapCashierEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("cashiers")
            .WithTags("Cashiers");

        group.MapGet("/{id:guid}", GetCashier)
            .WithName("GetCashier")
            .WithSummary("Retrieves a specific cashier by their unique identifier")
            .Produces<Cashier>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGet("/", GetCashiers)
            .WithName("GetCashiers")
            .WithSummary("Retrieves a list of cashiers with optional filtering")
            .Produces<IEnumerable<GetCashiersQuery.Result>>()
            .ProducesValidationProblem();

        group.MapPost("/", CreateCashier)
            .WithName("CreateCashier")
            .WithSummary("Creates a new cashier in the system")
            .Produces<Cashier>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", UpdateCashier)
            .WithName("UpdateCashier")
            .WithSummary("Updates an existing cashier's information")
            .Produces<Cashier>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", DeleteCashier)
            .WithName("DeleteCashier")
            .WithSummary("Deletes a cashier from the system")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<IResult> GetCashier(Guid id, IMessageBus bus, HttpContext context,
        CancellationToken cancellationToken)
    {
        var query = new GetCashierQuery(context.User.GetTenantId(), id);
        var queryResult = await bus.InvokeQueryAsync(query, cancellationToken);

        return queryResult.Match<IResult>(
            TypedResults.Ok,
            errors => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, detail: errors[0].ErrorMessage));
    }

    private static async Task<IResult> GetCashiers([AsParameters] GetCashiersRequest request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var query = request.ToQuery(context.User.GetTenantId());
        var cashiers = await bus.InvokeQueryAsync(query, cancellationToken);

        return TypedResults.Ok(cashiers);
    }

    private static async Task<IResult> CreateCashier(CreateCashierRequest request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(context.User.GetTenantId());
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<IResult>(
            cashier => TypedResults.Created($"/cashiers/{cashier.CashierId}", cashier),
            errors => TypedResults.ValidationProblem(errors.ToValidationErrors()));
    }

    private static async Task<IResult> UpdateCashier(Guid id, UpdateCashierRequest request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(context.User.GetTenantId(), id);
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<IResult>(
            TypedResults.Ok,
            errors => errors.IsConcurrencyConflict()
                ? TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: errors[0].ErrorMessage)
                : TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, detail: errors[0].ErrorMessage));
    }

    private static async Task<IResult> DeleteCashier(Guid id, IMessageBus bus, HttpContext context,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCashierCommand(context.User.GetTenantId(), id);
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<IResult>(
            _ => TypedResults.NoContent(),
            errors => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, detail: errors[0].ErrorMessage));
    }
}
