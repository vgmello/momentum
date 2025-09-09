// Copyright (c) OrgName. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Data;

namespace AppDomain.Cashiers.Queries;

/// <summary>
///     Query to retrieve a single cashier by its identifier.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="CashierId">Unique identifier for the cashier</param>
public record GetCashierQuery(Guid TenantId, Guid CashierId) : IQuery<Result<Cashier>>;

/// <summary>
///     Handler for the GetCashierQuery.
/// </summary>
public static class GetCashierQueryHandler
{
    /// <summary>
    ///     Handles the GetCashierQuery and returns the requested cashier.
    /// </summary>
    /// <param name="query">The get cashier query</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cashier if found, or a failure result</returns>
    /// <remarks>
    ///     This is just an example of a get handler with the DB call in the main body, OK for simple scenarios, however,
    ///     makes harder to test it via unit tests, requiring us to the whole handler with integration tests
    /// </remarks>
    public static async Task<Result<Cashier>> Handle(GetCashierQuery query, AppDomainDb db, CancellationToken cancellationToken)
    {
        var cashier = await db.Cashiers
            .FirstOrDefaultAsync(c => c.TenantId == query.TenantId && c.CashierId == query.CashierId, cancellationToken);

        if (cashier is not null)
        {
            return cashier.ToModel();
        }

        return new List<ValidationFailure> { new("Id", "Cashier not found") };
    }
}
