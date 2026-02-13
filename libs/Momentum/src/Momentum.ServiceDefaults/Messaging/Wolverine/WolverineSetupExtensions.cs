// Copyright (c) Momentum .NET. All rights reserved.

using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Momentum.ServiceDefaults.Messaging.Middlewares;
using System.Reflection;
using Wolverine;
using Wolverine.Runtime;

namespace Momentum.ServiceDefaults.Messaging.Wolverine;

/// <summary>
///     Provides extension methods for configuring Wolverine messaging framework.
/// </summary>
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
    ///         <item>PostgreSQL persistence for message durability and transaction support</item>
    ///         <item>Reliable messaging with inbox/outbox patterns for guaranteed delivery</item>
    ///         <item>Automatic transaction scoping for consistency across business operations</item>
    ///         <item>Dead letter queue handling for failed message processing</item>
    ///     </list>
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

        services.AddWolverine(ExtensionDiscovery.ManualOnly, opts =>
        {
            opts.ApplicationAssembly = ServiceDefaultsExtensions.EntryAssembly;

            opts.UseSystemTextJsonForSerialization();

            // Middlewares & Policies
            opts.Policies.AddMiddleware(typeof(OpenTelemetryInstrumentationMiddleware));
            opts.Policies.Add<ExceptionHandlingPolicy>();
            opts.Policies.Add<FluentValidationPolicy>();
            opts.Policies.AddMiddleware<RequestPerformanceMiddleware>();

            opts.Policies.ConventionalLocalRoutingIsAdditive();

            opts.ConfigureAppHandlers(opts.ApplicationAssembly);

            var codegenEnabled = wolverineConfig.GetValue<bool>("CodegenEnabled");
            opts.CodeGeneration.TypeLoadMode = codegenEnabled ? TypeLoadMode.Dynamic : TypeLoadMode.Static;

            var autoProvision = configuration.GetValue<bool>("AutoProvision");
            var configAutoProvisionStorage = configuration.GetValue<string>("AutoBuildMessageStorageOnStartup");

            if (!autoProvision && configAutoProvisionStorage is null)
            {
                opts.AutoBuildMessageStorageOnStartup = AutoCreate.None;
            }

            opts.Services.AddResourceSetupOnStartup();

            configure?.Invoke(opts);
        });

        services.AddSingleton<IConfigureOptions<WolverineOptions>>(prov =>
            new ConfigureNamedOptions<WolverineOptions>(string.Empty, wolverineOptions =>
            {
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
