// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using System.Text.RegularExpressions;

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
public partial class ServiceBusOptions
{
    /// <summary>
    ///     Gets the configuration section name used to bind these options.
    /// </summary>
    /// <value>
    ///     Returns "ServiceBus" which corresponds to the ServiceBus section in configuration files.
    /// </value>
    public const string SectionName = "ServiceBus";

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
    public string Domain { get; set; } = DefaultDomainAttribute.GetDomainName(ServiceDefaultsExtensions.EntryAssembly);

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
    ///     Enables reliable message delivery. (Default: true)
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
    public bool ReliableMessaging { get; set; } = true;

    /// <summary>
    ///     Post-configuration processor that validates and completes ServiceBus configuration after binding.
    /// </summary>
    /// <remarks>
    ///     See <see cref="ServiceBusOptions" /> for detailed configuration information.
    /// </remarks>
    public partial class Configurator(IHostEnvironment env)
        : IPostConfigureOptions<ServiceBusOptions>
    {
        // Valid service name: lowercase alphanumeric with hyphens (kebab-case)
        [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
        private static partial Regex ServiceNameValidationRegex();

        // Valid domain name: alphanumeric starting with a letter (PascalCase or simple)
        [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$")]
        private static partial Regex DomainNameValidationRegex();

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
        /// <exception cref="InvalidOperationException">
        ///     Thrown when domain or service name validation fails.
        /// </exception>
        public void PostConfigure(string? name, ServiceBusOptions options)
        {
            if (options.PublicServiceName.Length == 0)
                options.PublicServiceName = GetServiceName(env.ApplicationName);

            // Validate domain name format
            if (string.IsNullOrWhiteSpace(options.Domain))
                throw new InvalidOperationException("ServiceBus Domain cannot be empty.");

            if (!DomainNameValidationRegex().IsMatch(options.Domain))
                throw new InvalidOperationException(
                    $"Invalid ServiceBus Domain format: '{options.Domain}'. " +
                    "Domain must start with a letter and contain only alphanumeric characters.");

            // Validate service name format
            if (string.IsNullOrWhiteSpace(options.PublicServiceName))
                throw new InvalidOperationException("ServiceBus PublicServiceName cannot be empty.");

            if (!ServiceNameValidationRegex().IsMatch(options.PublicServiceName))
                throw new InvalidOperationException(
                    $"Invalid ServiceBus PublicServiceName format: '{options.PublicServiceName}'. " +
                    "Service name must be lowercase alphanumeric with hyphens (kebab-case).");

            var urnPath = $"/{options.Domain.ToSnakeCase()}/{GetServiceName(options.PublicServiceName)}";

            if (!Uri.TryCreate(urnPath, UriKind.Relative, out var serviceUrn))
                throw new InvalidOperationException($"Failed to create valid URN from path: {urnPath}");

            options.ServiceUrn = serviceUrn;
        }

        private static string GetServiceName(string appName) => appName.ToLowerInvariant().Replace('.', '-');
    }
}
