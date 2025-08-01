// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.Abstractions.Messaging;
using Wolverine;

namespace Operations.Extensions.Messaging;

/// <summary>
///     Provides extension methods for the Wolverine message bus to simplify command and query invocation.
/// </summary>
public static class MessageBusExtensions
{
    /// <summary>
    ///     Invokes a command asynchronously through the message bus.
    /// </summary>
    /// <typeparam name="TCommandResult">The type of result returned by the command.</typeparam>
    /// <param name="bus">The message bus instance.</param>
    /// <param name="command">The command to invoke.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    /// <remarks>
    ///     This method provides a strongly-typed way to invoke commands that implement
    ///     <see cref="ICommand{TResult}" />. The command will be processed by the appropriate
    ///     handler registered in the messaging system.
    /// </remarks>
    public static Task<TCommandResult> InvokeCommandAsync<TCommandResult>(this IMessageBus bus, ICommand<TCommandResult> command,
        CancellationToken cancellationToken = default)
    {
        return bus.InvokeAsync<TCommandResult>(command, cancellationToken);
    }

    /// <summary>
    ///     Invokes a query asynchronously through the message bus.
    /// </summary>
    /// <typeparam name="TQueryResult">The type of result returned by the query.</typeparam>
    /// <param name="bus">The message bus instance.</param>
    /// <param name="query">The query to invoke.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the query result.</returns>
    /// <remarks>
    ///     This method provides a strongly-typed way to invoke queries that implement
    ///     <see cref="IQuery{TResult}" />. The query will be processed by the appropriate
    ///     handler registered in the messaging system.
    /// </remarks>
    public static Task<TQueryResult> InvokeQueryAsync<TQueryResult>(this IMessageBus bus, IQuery<TQueryResult> query,
        CancellationToken cancellationToken = default)
    {
        return bus.InvokeAsync<TQueryResult>(query, cancellationToken);
    }
}
