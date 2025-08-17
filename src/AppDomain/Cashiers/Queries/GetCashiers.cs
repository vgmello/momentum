// Copyright (c) ORG_NAME. All rights reserved.

using Momentum.Extensions.Abstractions.Dapper;

namespace AppDomain.Cashiers.Queries;

public record GetCashiersQuery(Guid TenantId, int Offset = 0, int Limit = 1000) : IQuery<IEnumerable<GetCashiersQuery.Result>>
{
    public record Result(Guid TenantId, Guid CashierId, string Name, string Email);
}

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
    [DbCommand(fn: "$app_domain.cashiers_get_all")]
    public partial record DbQuery(Guid TenantId, int Limit, int Offset) : IQuery<IEnumerable<Data.Entities.Cashier>>;

    /// <summary>
    ///     Example: "Complex" Get handler executing a DB function via DbCommand
    /// </summary>
    public static async Task<IEnumerable<GetCashiersQuery.Result>> Handle(GetCashiersQuery query, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbQuery = new DbQuery(query.TenantId, query.Limit, query.Offset);
        var cashiers = await messaging.InvokeQueryAsync(dbQuery, cancellationToken);

        return cashiers.Select(c => new GetCashiersQuery.Result(c.TenantId, c.CashierId, c.Name, c.Email ?? "N/A"));
    }
}
