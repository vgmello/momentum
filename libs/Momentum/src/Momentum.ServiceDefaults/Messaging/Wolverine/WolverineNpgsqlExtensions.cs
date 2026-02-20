// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Wolverine.Postgresql;

namespace Momentum.ServiceDefaults.Messaging.Wolverine;

public class WolverineNpgsqlExtensions(IConfiguration configuration, IOptions<ServiceBusOptions> serviceBusOptions)
    : IConfigureOptions<WolverineOptions>
{
    /// <summary>
    ///     Configures PostgreSQL for message persistence and transport.
    /// </summary>
    /// <param name="options">The Wolverine options to configure.</param>
    /// <returns>The configured Wolverine options for method chaining.</returns>
    /// <remarks>
    ///     This method:
    ///     <list type="bullet">
    ///         <item>Sets up PostgreSQL for both persistence and transport</item>
    ///         <item>Creates a schema based on the service name</item>
    ///         <item>Enables auto-provisioning of database objects</item>
    ///         <item>Uses "queues" as the transport schema</item>
    ///     </list>
    ///     The persistence schema name is derived from the service name by replacing
    ///     dots and hyphens with underscores and converting to lowercase.
    /// </remarks>
    public void Configure(WolverineOptions options)
    {
        if (serviceBusOptions.Value.ReliableMessaging)
        {
            options.ConfigureReliableMessaging();
        }

        var connectionString = configuration.GetConnectionString(ServiceBusOptions.SectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"The DB string '{ServiceBusOptions.SectionName}' is not set.");

        try
        {
            _ = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"The DB connection string '{ServiceBusOptions.SectionName}' has an invalid format: {ex.Message}", ex);
        }

        var persistenceSchema = options.ServiceName
            .Replace(".", "_")
            .Replace("-", "_")
            .ToLowerInvariant();

        options
            .PersistMessagesWithPostgresql(connectionString, schemaName: persistenceSchema)
            .EnableMessageTransport(transport => transport.TransportSchemaName("queues"));
    }
}
