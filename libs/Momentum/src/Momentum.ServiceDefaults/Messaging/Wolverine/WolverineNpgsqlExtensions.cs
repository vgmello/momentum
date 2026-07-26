// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Wolverine.Postgresql;

namespace Momentum.ServiceDefaults.Messaging.Wolverine;

[ExcludeFromCodeCoverage]
public class WolverineNpgsqlExtensions(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IOptions<ServiceBusOptions> serviceBusOptions)
    : IConfigureOptions<WolverineOptions>
{
    /// <summary>
    ///     Configures PostgreSQL for message persistence and transport.
    /// </summary>
    /// <param name="options">The Wolverine options to configure.</param>
    /// <remarks>
    ///     This method:
    ///     <list type="bullet">
    ///         <item>Sets up PostgreSQL for both persistence and transport</item>
    ///         <item>Creates a persistence schema named "svcbus_{service-name}" (inbox/outbox/dead letters)</item>
    ///         <item>Enables auto-provisioning of database objects</item>
    ///         <item>Uses "svcbus_queues" as the transport schema</item>
    ///     </list>
    ///     The service-name part is derived by replacing dots and hyphens with underscores and
    ///     converting to lowercase. The "svcbus_" prefix keeps Wolverine's schemas clearly
    ///     distinguishable from application schemas in the shared database.
    ///     <para>
    ///         Persistence reuses the application's registered <see cref="NpgsqlDataSource" /> (the same
    ///         one <c>TransactionalOutboxMiddleware</c> opens its transaction on), so the outbox tables
    ///         always live in the application database. This makes the atomic business-write + outbox
    ///         commit structural rather than configuration-dependent: there is no separate "ServiceBus"
    ///         connection string that could be pointed at a different database and silently break the
    ///         outbox. When no application data source is registered (standalone library use), it falls
    ///         back to the <c>ServiceBus</c> connection string.
    ///     </para>
    /// </remarks>
    public void Configure(WolverineOptions options)
    {
        if (serviceBusOptions.Value.ReliableMessaging)
        {
            options.ConfigureReliableMessaging();
        }

        var persistenceSchema = "svcbus_" + options.ServiceName
            .Replace(".", "_")
            .Replace("-", "_")
            .ToLowerInvariant();

        var appDataSource = serviceProvider.GetService<NpgsqlDataSource>();

        if (appDataSource is not null)
        {
            options
                .PersistMessagesWithPostgresql(appDataSource, schemaName: persistenceSchema)
                .EnableMessageTransport(transport => transport.TransportSchemaName("svcbus_queues"));

            return;
        }

        // Standalone fallback: no application NpgsqlDataSource in the container, so fall back to an
        // explicit ServiceBus connection string. The generated services always register a data source,
        // so this path is only hit when the library is used on its own.
        var connectionString = configuration.GetConnectionString(ServiceBusOptions.SectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"No NpgsqlDataSource is registered and the DB string '{ServiceBusOptions.SectionName}' is not set.");

        try
        {
            _ = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"The DB connection string '{ServiceBusOptions.SectionName}' has an invalid format: {ex.Message}", ex);
        }

        options
            .PersistMessagesWithPostgresql(connectionString, schemaName: persistenceSchema)
            .EnableMessageTransport(transport => transport.TransportSchemaName("svcbus_queues"));
    }
}
