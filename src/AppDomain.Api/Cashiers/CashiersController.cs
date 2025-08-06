// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Api.Cashiers.Models;
using AppDomain.Api.Extensions;
using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Queries;

namespace AppDomain.Api.Cashiers;

/// <summary>
///     Manages cashier Momentum such as retrieval, creation, updating, and deletion.
/// </summary>
[ApiController]
[Route("[controller]")]
public class CashiersController(IMessageBus bus) : ControllerBase
{
    /// <summary>
    ///     Retrieves a specific cashier by their unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the cashier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cashier details if found</returns>
    /// <response code="200" />
    /// <response code="404">If the cashier is not found</response>
    /// <response code="400">If the provided ID is invalid</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Cashier>> GetCashier([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCashierQuery(User.GetTenantId(), id);
        var queryResult = await bus.InvokeQueryAsync(query, cancellationToken);

        return queryResult.Match<ActionResult<Cashier>>(
            cashier => Ok(cashier),
            errors => NotFound(new { Errors = errors }));
    }

    /// <summary>
    ///     Retrieves a list of cashiers with optional filtering
    /// </summary>
    /// <param name="request">Request parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of cashiers matching the specified criteria</returns>
    /// <response code="200" />
    /// <response code="400">If request parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<GetCashiersQuery.Result>>> GetCashiers([FromQuery] GetCashiersRequest request,
        CancellationToken cancellationToken)
    {
        var query = request.ToQuery(User.GetTenantId());
        var cashiers = await bus.InvokeQueryAsync(query, cancellationToken);

        return Ok(cashiers);
    }

    /// <summary>
    ///     Creates a new cashier in the system
    /// </summary>
    /// <param name="request">The cashier creation request containing name and email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created cashier details</returns>
    /// <response code="201" />
    /// <response code="400">If the request data is invalid or validation fails</response>
    /// <response code="409">If a cashier with the same email already exists</response>
    [HttpPost]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Cashier>> CreateCashier([FromBody] CreateCashierRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(User.GetTenantId());
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match(
            cashier => StatusCode(StatusCodes.Status201Created, cashier),
            errors => BadRequest(new { Errors = errors }));
    }

    /// <summary>
    ///     Updates an existing cashier's information
    /// </summary>
    /// <param name="id">The unique identifier of the cashier to update</param>
    /// <param name="request">The updated cashier information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated cashier details</returns>
    /// <response code="200" />
    /// <response code="400">If the request data is invalid or validation fails</response>
    /// <response code="404">If the cashier is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Cashier>> UpdateCashier([FromRoute] Guid id,
        [FromBody] UpdateCashierRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(User.GetTenantId(), id);
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<ActionResult<Cashier>>(
            cashier => Ok(cashier),
            errors => BadRequest(new { Errors = errors }));
    }

    /// <summary>
    ///     Deletes a cashier from the system
    /// </summary>
    /// <param name="id">The unique identifier of the cashier to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Cashier deleted successfully</response>
    /// <response code="400">If the cashier ID is invalid or cashier has active invoices</response>
    /// <response code="404">If the cashier is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType<object>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteCashier([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteCashierCommand(User.GetTenantId(), id);
        var commandResult = await bus.InvokeCommandAsync(command, cancellationToken);

        return commandResult.Match<ActionResult>(
            _ => NoContent(),
            errors => BadRequest(new { Errors = errors }));
    }

    /// <summary>
    ///     Testing endpoint that throws an exception for error handling validation
    /// </summary>
    /// <returns>Never returns successfully - always throws an exception</returns>
    /// <response code="500">Always throws a DivideByZeroException for testing purposes</response>
    [HttpGet("fake-error")]
    [Tags("Testing")]
    public Task<ActionResult<Cashier>> FakeError() => throw new DivideByZeroException("Fake error");
}
