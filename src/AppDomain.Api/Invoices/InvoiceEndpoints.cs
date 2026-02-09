// Copyright (c) OrgName. All rights reserved.

using AppDomain.Api.Invoices.Mappers;
using AppDomain.Api.Invoices.Models;
using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Queries;

namespace AppDomain.Api.Invoices;

/// <summary>
///     Defines minimal API endpoints for invoice operations.
/// </summary>
public static class InvoiceEndpoints
{
    /// <summary>
    ///     Maps all invoice REST endpoints under the /invoices route group.
    /// </summary>
    public static RouteGroupBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("invoices")
            .WithTags("Invoices");

        group.MapGet("/", GetInvoices)
            .WithName("GetInvoices")
            .WithSummary("Retrieves a list of invoices with optional filtering and pagination")
            .Produces<IEnumerable<Invoice>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetInvoice)
            .WithName("GetInvoice")
            .WithSummary("Retrieves a specific invoice by its unique identifier")
            .Produces<Invoice>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapPost("/", CreateInvoice)
            .WithName("CreateInvoice")
            .WithSummary("Creates a new invoice in the system")
            .Produces<Invoice>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}/cancel", CancelInvoice)
            .WithName("CancelInvoice")
            .WithSummary("Cancels an existing invoice, preventing further modifications")
            .Produces<Invoice>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}/mark-paid", MarkInvoiceAsPaid)
            .WithName("MarkInvoiceAsPaid")
            .WithSummary("Marks an invoice as paid with the specified payment amount and date")
            .Produces<Invoice>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapPost("/{id:guid}/simulate-payment", SimulatePayment)
            .WithName("SimulatePayment")
            .WithSummary("Simulates a payment for testing purposes")
            .WithTags("Testing")
            .Produces<object>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<IResult> GetInvoices([AsParameters] GetInvoicesRequest request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var query = request.ToQuery(context.User.GetTenantId());
        var invoices = await bus.InvokeQueryAsync(query, cancellationToken);

        return TypedResults.Ok(invoices);
    }

    private static async Task<IResult> GetInvoice(Guid id, IMessageBus bus, HttpContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = context.User.GetTenantId();
        var query = new GetInvoiceQuery(tenantId, id);
        var queryResult = await bus.InvokeQueryAsync(query, cancellationToken);

        return queryResult.Match<IResult>(
            invoice => TypedResults.Ok(invoice),
            errors => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, detail: errors.First().ErrorMessage));
    }

    private static async Task<IResult> CreateInvoice(CreateInvoiceRequest request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var tenantId = context.User.GetTenantId();
        var command = new CreateInvoiceCommand(
            tenantId,
            request.Name,
            request.Amount,
            request.Currency,
            request.DueDate,
            request.CashierId);

        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<IResult>(
            invoice => TypedResults.Created($"/invoices/{invoice.InvoiceId}", invoice),
            errors => TypedResults.ValidationProblem(errors.ToValidationErrors()));
    }

    private static async Task<IResult> CancelInvoice(Guid id, CancelInvoiceRequest request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var tenantId = context.User.GetTenantId();
        var command = new CancelInvoiceCommand(tenantId, id, request.Version);
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<IResult>(
            invoice => TypedResults.Ok(invoice),
            errors => errors.IsConcurrencyConflict()
                ? TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: errors.First().ErrorMessage)
                : TypedResults.ValidationProblem(errors.ToValidationErrors()));
    }

    private static async Task<IResult> MarkInvoiceAsPaid(Guid id, MarkInvoiceAsPaidRequest request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var tenantId = context.User.GetTenantId();
        var command = new MarkInvoiceAsPaidCommand(tenantId, id, request.Version, request.AmountPaid, request.PaymentDate);
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<IResult>(
            invoice => TypedResults.Ok(invoice),
            errors => errors.IsConcurrencyConflict()
                ? TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: errors.First().ErrorMessage)
                : TypedResults.ValidationProblem(errors.ToValidationErrors()));
    }

    private static async Task<IResult> SimulatePayment(Guid id, SimulatePaymentRequest request, IMessageBus bus,
        HttpContext context, CancellationToken cancellationToken)
    {
        var tenantId = context.User.GetTenantId();
        var command = new SimulatePaymentCommand(
            tenantId,
            id,
            request.Version,
            request.Amount,
            request.Currency ?? "USD",
            request.PaymentMethod ?? "Credit Card",
            request.PaymentReference ?? $"SIM-{Guid.CreateVersion7():N}"[..16]
        );

        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<IResult>(
            _ => TypedResults.Ok(new { Message = "Payment simulation triggered successfully" }),
            errors => errors.IsConcurrencyConflict()
                ? TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: errors.First().ErrorMessage)
                : TypedResults.ValidationProblem(errors.ToValidationErrors()));
    }
}
