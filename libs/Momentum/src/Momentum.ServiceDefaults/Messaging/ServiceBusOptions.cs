// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Extensions;

namespace Momentum.ServiceDefaults.Messaging;

/// <summary>
///     Configuration options for the Wolverine service bus and messaging infrastructure.
/// </summary>
/// <remarks>
///     <!--@include: @code/service-configuration/service-bus-options-detailed.md -->
/// </remarks>
/// <example>
///     <!--@include: @code/examples/service-bus-options-examples.md -->
/// </example>
public class ServiceBusOptions
{
    /// <summary>
    ///     Gets the configuration section name used to bind these options.
    /// </summary>
    /// <value>
    ///     Returns "ServiceBus" which corresponds to the ServiceBus section in configuration files.
    /// </value>
    public static string SectionName => "ServiceBus";

    /// <summary>
    ///     Gets or sets the domain name for the service, used for message routing and URN generation.
    /// </summary>
    /// <value>
    ///     The business domain name. Defaults to the main namespace of the entry assembly.
    ///     For example, if the assembly is "ECommerce.OrderService", the domain defaults to "ECommerce".
    /// </value>
    /// <remarks>
    ///     See <see cref="ServiceBusOptions" /> for detailed configuration information.
    /// </remarks>
    public string Domain { get; set; } = GetDomainName();

    /// <summary>
    ///     Gets or sets the public service name used for external identification and message routing.
    /// </summary>
    /// <value>
    ///     The service name in kebab-case format. If not explicitly set, defaults to the
    ///     application name converted to lowercase with dots replaced by hyphens.
    /// </value>
    /// <remarks>
    ///     See <see cref="ServiceBusOptions" /> for detailed configuration information.
    /// </remarks>
    /// <example>
    ///     <para>Examples of good service names:</para>
    ///     <list type="bullet">
    ///         <item>order-service</item>
    ///         <item>payment-processor</item>
    ///         <item>inventory-manager</item>
    ///         <item>customer-portal</item>
    ///     </list>
    /// </example>
    public string PublicServiceName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the service URN (Uniform Resource Name) used for message routing and identification.
    /// </summary>
    /// <value>
    ///     A relative URI in the format "/{domain_snake_case}/{service_name}" that uniquely
    ///     identifies this service within the messaging infrastructure.
    /// </value>
    /// <remarks>
    ///     See <see cref="ServiceBusOptions" /> for detailed configuration information.
    /// </remarks>
    /// <example>
    ///     See class-level examples in <see cref="ServiceBusOptions" />.
    /// </example>
    public Uri ServiceUrn { get; private set; } = null!;

    /// <summary>
    ///     Gets or sets the CloudEvents configuration for standardized cross-service messaging.
    /// </summary>
    /// <value>
    ///     A <see cref="CloudEventsSettings" /> instance containing CloudEvents specification
    ///     settings for event formatting and routing.
    /// </value>
    /// <remarks>
    ///     See <see cref="ServiceBusOptions" /> for detailed configuration information.
    /// </remarks>
    /// <example>
    ///     See class-level examples in <see cref="ServiceBusOptions" />.
    /// </example>
    public CloudEventsSettings CloudEvents { get; set; } = new();

    /// <summary>
    ///     Converts an application name to a service name following DNS naming conventions.
    /// </summary>
    /// <param name="appName">The application name to convert.</param>
    /// <returns>
    ///     A service name in lowercase with dots replaced by hyphens, suitable for
    ///     DNS naming, Kubernetes services, and message routing.
    /// </returns>
    /// <remarks>
    ///     See <see cref="ServiceBusOptions" /> for detailed configuration information.
    /// </remarks>
    /// <example>
    ///     See class-level examples in <see cref="ServiceBusOptions" />.
    /// </example>
    public static string GetServiceName(string appName) => appName.ToLowerInvariant().Replace('.', '-');

    private static string GetDomainName()
    {
        // TODO: consider a dedicated assembly attribute or csproj property to override explicitly if needed
        var simpleName = ServiceDefaultsExtensions.EntryAssembly.GetName().Name!;
        var mainNamespaceIndex = simpleName.IndexOf('.');

        return mainNamespaceIndex >= 0 ? simpleName[..mainNamespaceIndex] : simpleName;
    }

    /// <summary>
    ///     Post-configuration processor that validates and completes ServiceBus configuration after binding.
    /// </summary>
    /// <remarks>
    ///     See <see cref="ServiceBusOptions" /> for detailed configuration information.
    /// </remarks>
    public class Configurator(ILogger<Configurator> logger, IHostEnvironment env, IConfiguration config)
        : IPostConfigureOptions<ServiceBusOptions>
    {
        /// <summary>
        ///     Completes the configuration of ServiceBus options after initial binding.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The options instance to post-configure.</param>
        /// <remarks>
        ///     See <see cref="ServiceBusOptions" /> for detailed configuration information.
        /// </remarks>
        /// <example>
        ///     See class-level examples in <see cref="ServiceBusOptions" />.
        /// </example>
        public void PostConfigure(string? name, ServiceBusOptions options)
        {
            if (options.PublicServiceName.Length == 0)
                options.PublicServiceName = GetServiceName(env.ApplicationName);

            options.ServiceUrn = new Uri($"/{options.Domain.ToSnakeCase()}/{GetServiceName(options.PublicServiceName)}", UriKind.Relative);

            var connectionString = config.GetConnectionString(SectionName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogWarning("ConnectionStrings:ServiceBus is not set. " +
                                  "Transactional Inbox/Outbox and Message Persistence features disabled");
            }
        }
    }
}
