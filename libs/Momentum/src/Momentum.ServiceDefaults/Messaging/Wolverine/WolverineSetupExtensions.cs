// Copyright (c) ABCDEG. All rights reserved.

using JasperFx.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.Messaging.Kafka;
using Momentum.ServiceDefaults.Messaging.Middlewares;
using System.Reflection;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.Runtime;

namespace Momentum.ServiceDefaults.Messaging.Wolverine;

/// <summary>
///     Provides extension methods for configuring Wolverine messaging framework.
/// </summary>
public static class WolverineSetupExtensions
{
    /// <summary>
    ///     Gets or sets a value indicating whether to skip service registration.
    /// </summary>
    /// <remarks>
    ///     Used primarily for testing scenarios where manual service registration is preferred.
    /// </remarks>
    public static bool SkipServiceRegistration { get; set; }

    /// <summary>
    ///     Adds Wolverine messaging framework with default configuration.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <param name="configure">Optional action to configure Wolverine options.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     This method:
    ///     <list type="bullet">
    ///         <item>Registers a keyed PostgreSQL data source for the service bus</item>
    ///         <item>Configures Wolverine with sensible defaults</item>
    ///         <item>Skips registration if <see cref="SkipServiceRegistration" /> is true</item>
    ///     </list>
    /// </remarks>
    public static IHostApplicationBuilder AddWolverine(this IHostApplicationBuilder builder, Action<WolverineOptions>? configure = null)
    {
        if (!SkipServiceRegistration)
        {
            builder.AddKeyedNpgsqlDataSource("ServiceBus");

            AddWolverineWithDefaults(builder.Services, builder.Environment, builder.Configuration, configure);
        }

        return builder;
    }

    /// <summary>
    ///     Adds Wolverine services with default configuration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="env">The hosting environment.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configure">Optional action to configure Wolverine options.</param>
    /// <remarks>
    ///     Configures:
    ///     <list type="bullet">
    ///         <item>PostgreSQL persistence and transport if connection string is available</item>
    ///         <item>Kafka integration for event streaming if configured</item>
    ///         <item>System.Text.Json serialization</item>
    ///         <item>Exception handling and validation policies</item>
    ///         <item>OpenTelemetry instrumentation middleware</item>
    ///         <item>CloudEvent support</item>
    ///         <item>Request performance tracking</item>
    ///         <item>Health checks for messaging infrastructure</item>
    ///     </list>
    /// </remarks>
    public static void AddWolverineWithDefaults(
        this IServiceCollection services, IHostEnvironment env, IConfiguration configuration, Action<WolverineOptions>? configure)
    {
        var wolverineRegistered = services.Any(s => s.ServiceType == typeof(IWolverineRuntime));

        if (wolverineRegistered)
            return;

        services
            .ConfigureOptions<ServiceBusOptions.Configurator>()
            .AddOptions<ServiceBusOptions>()
            .BindConfiguration(ServiceBusOptions.SectionName)
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString(ServiceBusOptions.SectionName);

        services.AddWolverine(ExtensionDiscovery.ManualOnly, opts =>
        {
            opts.ApplicationAssembly = ServiceDefaultsExtensions.EntryAssembly;
            opts.ServiceName = ServiceBusOptions.GetServiceName(env.ApplicationName);

            opts.UseSystemTextJsonForSerialization();

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                opts.ConfigurePostgresql(connectionString);
                opts.ConfigureReliableMessaging();
            }

            opts.Policies.Add<ExceptionHandlingPolicy>();
            opts.Policies.Add<FluentValidationPolicy>();

            opts.Policies.AddMiddleware<RequestPerformanceMiddleware>();
            opts.Policies.AddMiddleware(typeof(OpenTelemetryInstrumentationMiddleware));

            opts.Policies.ConventionalLocalRoutingIsAdditive();

            var kafkaConnectionString = configuration.GetConnectionString(KafkaEventsExtensions.ConnectionStringName);

            if (!string.IsNullOrEmpty(kafkaConnectionString))
            {
                services.AddSingleton<IWolverineExtension, KafkaEventsExtensions>();

                services
                    .AddHealthChecks()
                    .AddKafka(options =>
                    {
                        options.BootstrapServers = kafkaConnectionString;
                    }, name: "kafka", tags: ["messaging", "kafka"]);
            }

            opts.ConfigureAppHandlers(opts.ApplicationAssembly);

            opts.Services.AddResourceSetupOnStartup();

            configure?.Invoke(opts);
        });
    }

    /// <summary>
    ///     Configures Wolverine to discover and register message handlers from domain assemblies.
    /// </summary>
    /// <param name="options">The Wolverine options to configure.</param>
    /// <param name="applicationAssembly">Optional application assembly. If null, uses the entry assembly.</param>
    /// <returns>The configured Wolverine options for method chaining.</returns>
    /// <remarks>
    ///     Discovers handlers from all assemblies marked with <see cref="DomainAssemblyAttribute" />.
    /// </remarks>
    public static WolverineOptions ConfigureAppHandlers(this WolverineOptions options, Assembly? applicationAssembly = null)
    {
        var handlerAssemblies = DomainAssemblyAttribute.GetDomainAssemblies(applicationAssembly);

        foreach (var handlerAssembly in handlerAssemblies)
        {
            options.Discovery.IncludeAssembly(handlerAssembly);
        }

        return options;
    }

    /// <summary>
    ///     Configures PostgreSQL for message persistence and transport.
    /// </summary>
    /// <param name="options">The Wolverine options to configure.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
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
    public static WolverineOptions ConfigurePostgresql(this WolverineOptions options, string connectionString)
    {
        var persistenceSchema = options.ServiceName
            .Replace(".", "_")
            .Replace("-", "_")
            .ToLowerInvariant();

        options
            .PersistMessagesWithPostgresql(connectionString, schemaName: persistenceSchema)
            .EnableMessageTransport(transport => transport.TransportSchemaName("queues"));

        return options;
    }

    /// <summary>
    ///     Configures Wolverine for reliable message delivery.
    /// </summary>
    /// <remarks>
    ///     Enables:
    ///     <list type="bullet">
    ///         <item>Automatic transaction middleware</item>
    ///         <item>Durable local queues for reliable processing</item>
    ///         <item>Durable outbox pattern on all sending endpoints</item>
    ///     </list>
    ///     These settings ensure message delivery reliability and prevent message loss
    ///     in case of failures.
    /// </remarks>
    public static WolverineOptions ConfigureReliableMessaging(this WolverineOptions options)
    {
        options.Policies.AutoApplyTransactions();
        options.Policies.UseDurableLocalQueues();
        options.Policies.UseDurableOutboxOnAllSendingEndpoints();

        return options;
    }
}
