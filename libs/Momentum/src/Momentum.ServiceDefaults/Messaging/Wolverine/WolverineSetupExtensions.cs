// Copyright (c) Momentum .NET. All rights reserved.

using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Momentum.ServiceDefaults.Messaging.Middlewares;
using Npgsql;
using System.Reflection;
using Wolverine.ErrorHandling;
using Wolverine.Runtime;

namespace Momentum.ServiceDefaults.Messaging.Wolverine;

/// <summary>
///     Provides extension methods for configuring Wolverine messaging framework.
/// </summary>
[ExcludeFromCodeCoverage]
public static class WolverineSetupExtensions
{
    public const string SectionName = "Wolverine";

    /// <summary>
    ///     Adds Wolverine services with comprehensive defaults for enterprise messaging scenarios.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing connection strings and messaging settings.</param>
    /// <param name="configure">Optional action to customize Wolverine options for specific business requirements.</param>
    /// <remarks>
    ///     <para>This method provides a complete messaging infrastructure configuration including:</para>
    ///
    ///     <para>
    ///         <strong>Persistence and Reliability:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>PostgreSQL persistence for message durability (inbox/outbox patterns)</item>
    ///         <item>Automatic transaction scoping for Wolverine-integrated persistence operations</item>
    ///         <item>Default retry policy for transient database/timeout failures, then dead letter queue</item>
    ///     </list>
    ///     <para>
    ///         <strong>Important:</strong> message persistence (inbox/outbox) and the PostgreSQL queue
    ///         transport share the database behind the <c>ServiceBus</c> connection string — Wolverine
    ///         does not support splitting them. Point <c>ServiceBus</c> at the application database
    ///         (the template default) so the outbox lives beside the business data in its own schema;
    ///         pointing it at a separate database reopens a crash window between the business commit
    ///         and the outbox persist in which outgoing messages can be lost.
    ///     </para>
    ///
    ///     <para>
    ///         <strong>Integration and Transport:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Kafka integration for high-throughput event streaming</item>
    ///         <item>CloudEvents standard support for interoperability</item>
    ///         <item>System.Text.Json serialization with performance optimization</item>
    ///         <item>Cross-service message routing and topic management</item>
    ///     </list>
    ///
    ///     <para>
    ///         <strong>Quality and Observability:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>FluentValidation integration for automatic message validation</item>
    ///         <item>Structured exception handling with retry policies</item>
    ///         <item>OpenTelemetry instrumentation for distributed tracing</item>
    ///         <item>Performance monitoring and request tracking middleware</item>
    ///         <item>Health checks for messaging infrastructure components</item>
    ///     </list>
    ///
    ///     <para>
    ///         <strong>Development and Deployment:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Resource setup on startup for database migrations</item>
    ///         <item>Convention-based handler discovery from domain assemblies</item>
    ///         <item>Environment-aware configuration and feature flags</item>
    ///     </list>
    /// </remarks>
    public static void AddWolverineWithDefaults(this IServiceCollection services,
        IConfiguration configuration, Action<WolverineOptions>? configure)
    {
        var wolverineRegistered = services.Any(s => s.ServiceType == typeof(IWolverineRuntime));

        if (wolverineRegistered)
            return;

        var wolverineConfig = configuration.GetSection(SectionName);
        services.Configure<WolverineOptions>(wolverineConfig);

        var serviceNameCustomized = false;

        services.AddWolverine(ExtensionDiscovery.ManualOnly, opts =>
        {
            opts.ApplicationAssembly = ServiceDefaultsExtensions.EntryAssembly;

            opts.UseSystemTextJsonForSerialization();

            // Middlewares & Policies
            opts.Policies.AddMiddleware(typeof(OpenTelemetryInstrumentationMiddleware));
            opts.Policies.Add<ExceptionHandlingPolicy>();
            opts.Policies.Add<FluentValidationPolicy>();
            opts.Policies.AddMiddleware<RequestPerformanceMiddleware>();

            // Local conventional routing deliberately stays non-additive: an integration event with
            // both a local handler and an explicit external route (e.g. Kafka) is published only to
            // the external transport and processed once when it is consumed back, instead of being
            // handled locally AND again via the broker round trip.
            opts.ConfigureAppHandlers(opts.ApplicationAssembly);

            var codegenEnabled = wolverineConfig.GetValue<bool>("CodegenEnabled");
            opts.CodeGeneration.TypeLoadMode = codegenEnabled ? TypeLoadMode.Dynamic : TypeLoadMode.Static;

            if (codegenEnabled)
            {
                // WolverineFx.RuntimeCompilation normally self-registers via assembly-attribute
                // discovery, but ExtensionDiscovery.ManualOnly above disables that scan, so the
                // Roslyn-backed IAssemblyGenerator TypeLoadMode.Dynamic needs must be wired up explicitly.
                opts.UseRuntimeCompilation();
            }

            var autoProvision = wolverineConfig.GetValue<bool>("AutoProvision");
            var configAutoProvisionStorage = wolverineConfig.GetValue<string>("AutoBuildMessageStorageOnStartup");

            if (!autoProvision && configAutoProvisionStorage is null)
            {
                opts.AutoBuildMessageStorageOnStartup = AutoCreate.None;
            }

            opts.Services.AddResourceSetupOnStartup();

            var conventionServiceName = opts.ServiceName;
            configure?.Invoke(opts);
            serviceNameCustomized = !string.Equals(opts.ServiceName, conventionServiceName, StringComparison.Ordinal);
        });

        services.AddSingleton<IConfigureOptions<WolverineOptions>>(prov =>
            new ConfigureNamedOptions<WolverineOptions>(string.Empty, wolverineOptions =>
            {
                // A ServiceName explicitly set in the user's configure callback wins over the
                // ServiceBus-derived default.
                if (serviceNameCustomized)
                    return;

                var options = prov.GetRequiredService<IOptions<ServiceBusOptions>>();

                wolverineOptions.ServiceName = options.Value.PublicServiceName;
            }));
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
    ///     Configures Wolverine for reliable message delivery.
    /// </summary>
    /// <remarks>
    ///     Enables:
    ///     <list type="bullet">
    ///         <item>Automatic transaction middleware</item>
    ///         <item>Durable local queues for reliable processing</item>
    ///         <item>Durable outbox pattern on all sending endpoints</item>
    ///         <item>
    ///             Durable inbox on all listening endpoints — incoming messages (e.g. from Kafka) are
    ///             persisted before processing, giving at-least-once delivery with duplicate detection
    ///             by message id across redeliveries
    ///         </item>
    ///         <item>
    ///             Transient-failure policy (<see cref="NpgsqlException" /> marked transient, and
    ///             <see cref="TimeoutException" />): two quick in-process retries (50ms/250ms), then
    ///             durable scheduled retries (5s/30s) that release the listener instead of blocking it
    ///             (important for Kafka partitions), then the dead letter queue
    ///         </item>
    ///     </list>
    ///     Failure rules added by the application (via the <c>configure</c> callback of
    ///     <c>AddServiceBus</c>) are evaluated before these defaults and therefore take precedence.
    ///     Dead-lettered messages are stored in the Wolverine persistence schema
    ///     (<c>svcbus_*.wolverine_dead_letters</c>) and can be inspected and replayed with the
    ///     <c>storage</c> CLI command or <c>IDeadLetterAdminService</c>.
    /// </remarks>
    public static WolverineOptions ConfigureReliableMessaging(this WolverineOptions options)
    {
        options.Policies.AutoApplyTransactions();
        options.Policies.UseDurableLocalQueues();
        options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        options.Policies.UseDurableInboxOnAllListeners();

        options.OnException<NpgsqlException>(ex => ex.IsTransient)
            .Or<TimeoutException>()
            .RetryWithCooldown(
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(250))
            .Then.ScheduleRetry(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(30))
            .Then.MoveToErrorQueue();

        return options;
    }
}
