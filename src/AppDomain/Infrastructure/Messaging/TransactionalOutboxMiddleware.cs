// Copyright (c) OrgName. All rights reserved.

//#if (USE_PGSQL)
using Npgsql;
using Wolverine.RDBMS;
using Wolverine.Runtime;

namespace AppDomain.Infrastructure.Messaging;

/// <summary>
///     Wolverine middleware that wraps command handling in a single database transaction shared by
///     LinqToDB writes and Wolverine's outbox, making business data and cascaded messages atomic.
/// </summary>
/// <remarks>
///     Before the handler runs, a transaction is opened on the application database and exposed via
///     <see cref="TransactionalOutbox.CurrentTransaction" />. <c>AppDomainDb</c> instances resolved
///     during the message execution attach to that transaction instead of opening their own
///     connection (see <c>DependencyInjection.AddAppDomainServices</c>), and cascaded messages are
///     persisted to Wolverine's outgoing envelope tables on the same transaction via
///     <c>MessageContext.EnlistInOutboxAsync</c>. The commit therefore covers both the
///     business writes and the outgoing integration events; on failure everything rolls back
///     together. Nested command invocations (e.g. DbCommand handlers invoked from a parent command)
///     detect the ambient transaction and join it instead of starting their own.
/// </remarks>
public class TransactionalOutboxMiddleware
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public async Task BeforeAsync(MessageContext context, NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        if (TransactionalOutbox.CurrentTransaction is not null)
            return;

        _connection = await dataSource.OpenConnectionAsync(cancellationToken);
        _transaction = await _connection.BeginTransactionAsync(cancellationToken);

        TransactionalOutbox.CurrentTransaction = _transaction;

        // IMessageStore is not codegen-resolvable as a parameter, so it is reached via the runtime
        if (context.Runtime.Storage is IMessageDatabase messageDatabase)
            await context.EnlistInOutboxAsync(new DatabaseEnvelopeTransaction(messageDatabase, _transaction));
    }

    public async Task AfterAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
            await _transaction.CommitAsync(cancellationToken);
    }

    public async Task FinallyAsync()
    {
        if (_transaction is not null)
        {
            TransactionalOutbox.CurrentTransaction = null;

            // Disposing an uncommitted transaction rolls it back
            await _transaction.DisposeAsync();
        }

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}

/// <summary>
///     Ambient accessor for the transaction opened by <see cref="TransactionalOutboxMiddleware" />.
/// </summary>
public static class TransactionalOutbox
{
    private static readonly AsyncLocal<NpgsqlTransaction?> Transaction = new();

    /// <summary>
    ///     The transaction of the currently executing command, or <c>null</c> outside a
    ///     command handling pipeline. Flows across awaits and nested inline invocations.
    /// </summary>
    public static NpgsqlTransaction? CurrentTransaction
    {
        get => Transaction.Value;
        internal set => Transaction.Value = value;
    }
}
//#endif
