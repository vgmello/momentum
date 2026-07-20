// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Wolverine.Postgresql;

namespace Momentum.ServiceDefaults.Messaging.Wolverine;

[ExcludeFromCodeCoverage]
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
    ///         <item>Creates a persistence schema named "svcbus_{service-name}" (inbox/outbox/dead letters)</item>
    ///         <item>Enables auto-provisioning of database objects</item>
    ///         <item>Uses "svcbus_queues" as the transport schema</item>
    ///     </list>
    ///     The service-name part is derived by replacing dots and hyphens with underscores and
    ///     converting to lowercase. The "svcbus_" prefix keeps Wolverine's schemas clearly
    ///     distinguishable from application schemas in the shared database.
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

        var persistenceSchema = "svcbus_" + options.ServiceName
            .Replace(".", "_")
            .Replace("-", "_")
            .ToLowerInvariant();

        options
            .PersistMessagesWithPostgresql(connectionString, schemaName: persistenceSchema)
            .EnableMessageTransport(transport => transport.TransportSchemaName("svcbus_queues"));
    }
}
