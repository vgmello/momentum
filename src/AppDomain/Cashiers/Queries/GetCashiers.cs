// Copyright (c) OrgName. All rights reserved.

namespace AppDomain.Cashiers.Queries;

/// <summary>
///     Query to retrieve a paginated list of cashiers for a specific tenant.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="Offset">Number of records to skip for pagination (default: 0)</param>
/// <param name="Limit">Maximum number of records to return (default: 100)</param>
public record GetCashiersQuery(Guid TenantId, int Offset = 0, int Limit = 100) : IQuery<IEnumerable<GetCashiersQuery.Result>>
{
    /// <summary>
    ///     Represents a cashier result with essential information.
    /// </summary>
    /// <param name="TenantId">Unique identifier for the tenant</param>
    /// <param name="CashierId">Unique identifier for the cashier</param>
    /// <param name="Name">Full name of the cashier</param>
    /// <param name="Email">Email address of the cashier</param>
    public record Result(Guid TenantId, Guid CashierId, string Name, string Email);
}

/// <summary>
///     Handler for the GetCashiersQuery that retrieves cashiers using database functions.
/// </summary>
public static partial class GetCashiersQueryHandler
{
    /// <summary>
    ///     Storage / persistence request
    /// </summary>
    /// <remarks>
    ///     This DbCommand/DbQuery leverages the Momentum
    ///     <see cref="Momentum.Extensions.Abstractions.Dapper.DbCommandAttribute">DbCommandAttribute</see>,
    ///     which creates a source generated handler for the DB call.
    ///     <para>
    ///         > Notes:
    ///         - If the function name starts with a $, the function gets executed as `select * from {dbFunction}`
    ///     </para>
    /// </remarks>
    [DbCommand(fn: "$main.cashiers_get_all")]
    public partial record DbQuery(Guid TenantId, int Limit, int Offset) : IQuery<IEnumerable<Data.Entities.Cashier>>;

    /// <summary>
    ///     Handles the GetCashiersQuery by executing a database function and transforming results.
    /// </summary>
    /// <param name="query">The get cashiers query</param>
    /// <param name="messaging">Message bus for executing database queries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of cashier results</returns>
    public static async Task<IEnumerable<GetCashiersQuery.Result>> Handle(GetCashiersQuery query, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.Limit, query.Offset);
        var cashiers = await messaging.InvokeQueryAsync(dbQuery, cancellationToken);

        return cashiers.Select(c => new GetCashiersQuery.Result(c.TenantId, c.CashierId, c.Name, c.Email ?? string.Empty));
    }
}
