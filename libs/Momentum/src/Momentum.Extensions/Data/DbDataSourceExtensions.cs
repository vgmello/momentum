// Copyright (c) Momentum .NET. All rights reserved.

using Dapper;
using Momentum.Extensions.Abstractions.Dapper;
using System.Data;
using System.Data.Common;

namespace Momentum.Extensions.Data;

public static class DbDataSourceExtensions
{
    /// <summary>
    ///     Executes a stored procedure that returns the number of affected rows.
    /// </summary>
    /// <param name="dataSource">The DbDataSource data source.</param>
    /// <param name="spName">The name of the stored procedure.</param>
    /// <param name="parameters">Provider for command parameters.</param>
    /// <param name="transaction">Optional database transaction to associate with the command.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    public static Task<int> SpExecute(this DbDataSource dataSource, string spName, IDbParamsProvider parameters,
        DbTransaction? transaction = null, int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return dataSource.SpCall<int>(spName, parameters, static conn => conn.ExecuteAsync,
            transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    ///     Query data using a stored procedure that returns a collection of TResult.
    /// </summary>
    /// <param name="dataSource">The DbDataSource data source.</param>
    /// <param name="spName">The name of the stored procedure.</param>
    /// <param name="parameters">Provider for sp parameters.</param>
    /// <param name="transaction">Optional database transaction to associate with the command.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of TResult</returns>
    public static Task<IEnumerable<TResult>> SpQuery<TResult>(this DbDataSource dataSource, string spName, IDbParamsProvider parameters,
        DbTransaction? transaction = null, int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return dataSource.SpCall<IEnumerable<TResult>>(
            spName: spName,
            parameters: parameters,
            dbFunction: static conn => conn.QueryAsync<TResult>,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);
    }

    public static async Task<TResult> SpCall<TResult>(this DbDataSource dataSource,
        string spName,
        IDbParamsProvider parameters,
        Func<DbConnection, Func<CommandDefinition, Task<TResult>>> dbFunction,
        DbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var dbFunctionCall = dbFunction(connection);
        var dbParams = parameters.ToDbParams();

        var command = new CommandDefinition(
            commandText: spName,
            parameters: dbParams,
            commandType: CommandType.StoredProcedure,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        return await dbFunctionCall(command);
    }
}
