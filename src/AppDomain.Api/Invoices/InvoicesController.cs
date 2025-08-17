// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api.Extensions;
using AppDomain.Api.Invoices.Mappers;
using AppDomain.Api.Invoices.Models;
using AppDomain.Invoices.Commands;
using AppDomain.Invoices.Contracts.Models;
using AppDomain.Invoices.Queries;

namespace AppDomain.Api.Invoices;

/// <summary>
///     Manages invoice operations such as retrieval, creation, cancellation, and payment processing.
/// </summary>
[ApiController]
[Route("[controller]")]
public class InvoicesController(IMessageBus bus) : ControllerBase
{
    /// <summary>
    ///     Retrieves a list of invoices with optional filtering and pagination
    /// </summary>
    /// <param name="request">Request parameters for filtering invoices by status, date range, cashier, etc.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of invoices matching the specified criteria</returns>
    /// <response code="200">Returns the list of invoices</response>
    /// <response code="400">If request parameters are invalid</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices([FromQuery] GetInvoicesRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.ToQuery(User.GetTenantId());
        var invoices = await bus.InvokeQueryAsync(query, cancellationToken);

        return Ok(invoices);
    }

    /// <summary>
    ///     Retrieves a specific invoice by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the invoice</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The invoice details if found</returns>
    /// <response code="200" />
    /// <response code="404">If the invoice is not found</response>
    /// <response code="400">If the provided ID is invalid</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Invoice>> GetInvoice([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var query = new GetInvoiceQuery(tenantId, id);
        var queryResult = await bus.InvokeQueryAsync(query, cancellationToken);

        return queryResult.Match<ActionResult<Invoice>>(
            invoice => Ok(invoice),
            errors => NotFound(new { Errors = errors }));
    }

    /// <summary>
    ///     Creates a new invoice in the system
    /// </summary>
    /// <param name="request">The invoice creation request containing name, amount, currency, due date, and optional cashier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created invoice details</returns>
    /// <response code="201">Invoice created successfully</response>
    /// <response code="400">If the request data is invalid or validation fails</response>
    /// <response code="404">If the specified cashier is not found</response>
    [HttpPost]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var command = new CreateInvoiceCommand(
            tenantId,
            request.Name,
            request.Amount,
            request.Currency,
            request.DueDate,
            request.CashierId);

        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<ActionResult<Invoice>>(
            invoice => StatusCode(StatusCodes.Status201Created, invoice),
            errors => BadRequest(new { Errors = errors }));
    }

    /// <summary>
    ///     Cancels an existing invoice, preventing further modifications
    /// </summary>
    /// <param name="id">The unique identifier of the invoice to cancel</param>
    /// <param name="request">Cancel request containing the current version for optimistic concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated invoice details with cancelled status</returns>
    /// <response code="200">Invoice cancelled successfully</response>
    /// <response code="400">If the invoice cannot be cancelled (already paid or cancelled)</response>
    /// <response code="404">If the invoice is not found</response>
    /// <response code="409">If the version is outdated (optimistic concurrency conflict)</response>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Invoice>> CancelInvoice([FromRoute] Guid id, [FromBody] CancelInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var command = new CancelInvoiceCommand(tenantId, id, request.Version);
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<ActionResult<Invoice>>(
            invoice => Ok(invoice),
            errors => errors.IsConcurrencyConflict()
                ? Conflict(new { Errors = errors })
                : BadRequest(new { Errors = errors }));
    }

    /// <summary>
    ///     Marks an invoice as paid with the specified payment amount and date
    /// </summary>
    /// <param name="id">The unique identifier of the invoice to mark as paid</param>
    /// <param name="request">Payment details including version, amount and optional payment date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated invoice details with paid status</returns>
    /// <response code="200">Invoice marked as paid successfully</response>
    /// <response code="400">If the payment amount is insufficient or invoice is already paid/cancelled</response>
    /// <response code="404">If the invoice is not found</response>
    /// <response code="409">If the version is outdated (optimistic concurrency conflict)</response>
    [HttpPut("{id:guid}/mark-paid")]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Invoice>> MarkInvoiceAsPaid([FromRoute] Guid id, [FromBody] MarkInvoiceAsPaidRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var command = new MarkInvoiceAsPaidCommand(tenantId, id, request.Version, request.AmountPaid, request.PaymentDate);
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<ActionResult<Invoice>>(
            invoice => Ok(invoice),
            errors => errors.IsConcurrencyConflict()
                ? Conflict(new { Errors = errors })
                : BadRequest(new { Errors = errors }));
    }

    /// <summary>
    ///     Simulates a payment for testing purposes, triggering payment processing workflow
    /// </summary>
    /// <param name="id">The unique identifier of the invoice to simulate payment for</param>
    /// <param name="request">Payment simulation details including the version, amount, currency, method, and reference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation that payment simulation was triggered</returns>
    /// <response code="200">Payment simulation triggered successfully</response>
    /// <response code="400">If the payment details are invalid or invoice cannot be paid</response>
    /// <response code="404">If the invoice is not found</response>
    /// <response code="409">If the version is outdated (optimistic concurrency conflict)</response>
    [HttpPost("{id:guid}/simulate-payment")]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    [Tags("Testing")]
    public async Task<ActionResult> SimulatePayment([FromRoute] Guid id, [FromBody] SimulatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var command = new SimulatePaymentCommand(
            tenantId,
            id,
            request.Version,
            request.Amount,
            request.Currency ?? "USD",
            request.PaymentMethod ?? "Credit Card",
            request.PaymentReference ?? $"SIM-{Guid.NewGuid():N}"[..8]
        );

        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<ActionResult>(
            _ => Ok(new { Message = "Payment simulation triggered successfully" }),
            errors => errors.IsConcurrencyConflict()
                ? Conflict(new { Errors = errors })
                : BadRequest(new { Errors = errors }));
    }
}
