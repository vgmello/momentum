<!--#if (includeSample)-->
// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;
using AppDomain.Cashiers.Data;
using AppDomain.Core.Data;
using LinqToDB;
using Momentum.Extensions;
using Momentum.Extensions.Abstractions.Messaging;

namespace AppDomain.Cashiers.Queries;

/// <summary>
/// Query to retrieve a single cashier by its identifier.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="CashierId">Unique identifier for the cashier</param>
public record GetCashierQuery(
    Guid TenantId,
    Guid CashierId
) : IQuery<Result<Cashier>>;

/// <summary>
/// Handler for the GetCashierQuery.
/// </summary>
public static class GetCashierQueryHandler
{
    /// <summary>
    /// Handles the GetCashierQuery and returns the requested cashier.
    /// </summary>
    /// <param name="query">The get cashier query</param>
    /// <param name="db">The database context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cashier if found, or a failure result</returns>
    public static async Task<Result<Cashier>> Handle(GetCashierQuery query, AppDomainDb db, CancellationToken cancellationToken)
    {
        var cashier = await db.Cashiers
            .Where(c => c.TenantId == query.TenantId && c.CashierId == query.CashierId)
            .FirstOrDefaultAsync(cancellationToken);

        if (cashier == null)
        {
            return Result<Cashier>.Failure("Cashier not found");
        }

        return Result<Cashier>.Success(cashier.ToModel());
    }
}
<!--#endif-->