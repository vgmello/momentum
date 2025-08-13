< !--#if (INCLUDE_SAMPLE)-->
// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Api.Cashiers.Mappers;
using AppDomain.Api.Cashiers.Models;
using AppDomain.Api.Extensions;
using AppDomain.Cashiers.Commands;
using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Queries;
using Wolverine;

namespace AppDomain.Api.Cashiers;

/// <summary>
/// REST API controller for managing cashiers.
/// </summary>
/// <param name="bus">The message bus for command and query handling</param>
[ApiController]
[Route("[controller]")]
public class CashiersController(IMessageBus bus) : ControllerBase
{

    /// <summary>
    /// Creates a new cashier.
    /// </summary>
    /// <param name="request">The create cashier request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created cashier</returns>
    [HttpPost]
    [ProducesResponseType<Contracts.Models.Cashier>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCashier([FromBody] CreateCashierRequest request, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var command = request.ToCommand(tenantId);

        var result = await bus.InvokeAsync<Result<Cashier>>(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetCashier), new { cashierId = result.Value.CashierId }, result.Value);
    }

    /// <summary>
    /// Gets a cashier by its identifier.
    /// </summary>
    /// <param name="cashierId">The cashier identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cashier if found</returns>
    [HttpGet("{cashierId:guid}")]
    [ProducesResponseType<Contracts.Models.Cashier>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCashier([FromRoute] Guid cashierId, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var query = new GetCashierQuery(tenantId, cashierId);

        var result = await bus.InvokeAsync<Result<Contracts.Models.Cashier>>(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a paginated list of cashiers.
    /// </summary>
    /// <param name="request">The get cashiers request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The paginated cashier list</returns>
    [HttpGet]
    [ProducesResponseType<IEnumerable<GetCashiersQuery.Result>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCashiers([FromQuery] GetCashiersRequest request, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var query = request.ToQuery(tenantId);

        var result = await bus.InvokeAsync<IEnumerable<GetCashiersQuery.Result>>(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Updates an existing cashier.
    /// </summary>
    /// <param name="cashierId">The cashier identifier</param>
    /// <param name="request">The update cashier request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated cashier</returns>
    [HttpPut("{cashierId:guid}")]
    [ProducesResponseType<Contracts.Models.Cashier>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCashier([FromRoute] Guid cashierId, [FromBody] UpdateCashierRequest request, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var command = request.ToCommand(tenantId, cashierId);

        var result = await bus.InvokeAsync<Result<Contracts.Models.Cashier>>(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Deletes a cashier.
    /// </summary>
    /// <param name="cashierId">The cashier identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{cashierId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCashier([FromRoute] Guid cashierId, CancellationToken cancellationToken)
    {
        var tenantId = User.GetTenantId();
        var command = new DeleteCashierCommand(tenantId, cashierId);

        var result = await bus.InvokeAsync<Result>(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound();
        }

        return NoContent();
    }
}
<!--#endif-->
