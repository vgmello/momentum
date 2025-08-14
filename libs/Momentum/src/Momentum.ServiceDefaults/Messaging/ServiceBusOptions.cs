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
///     <para>This class configures the core messaging infrastructure used for CQRS patterns,
///     event-driven architecture, and cross-service communication. It integrates with
///     PostgreSQL for message persistence and supports CloudEvents for standardized messaging.</para>
///     
///     <para>The configuration automatically derives service names and URNs from the application
///     assembly name, following domain-driven design patterns where the assembly name reflects
///     the business domain.</para>
/// </remarks>
/// <example>
///     <para><strong>Basic Configuration in appsettings.json:</strong></para>
///     <code>
/// {
///   "ServiceBus": {
///     "Domain": "ECommerce",
///     "PublicServiceName": "order-service",
///     "CloudEvents": {
///       "Source": "https://api.mystore.com/orders",
///       "DefaultType": "com.mystore.orders"
///     }
///   },
///   "ConnectionStrings": {
///     "ServiceBus": "Host=postgres;Database=order_messaging;Username=app;Password=secret"
///   }
/// }
/// </code>
///     
///     <para><strong>Multi-Environment Configuration:</strong></para>
///     <code>
/// // appsettings.Development.json
/// {
///   "ServiceBus": {
///     "Domain": "ECommerce",
///     "PublicServiceName": "order-service-dev"
///   }
/// }
/// 
/// // appsettings.Production.json
/// {
///   "ServiceBus": {
///     "Domain": "ECommerce", 
///     "PublicServiceName": "order-service",
///     "CloudEvents": {
///       "Source": "https://api.production.mystore.com/orders",
///       "Subject": "orders"
///     }
///   }
/// }
/// </code>
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
    ///     This property is used to group related services and organize message routing.
    ///     It should represent the business domain rather than technical concerns.
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
    ///     <para>This name is used for:</para>
    ///     <list type="bullet">
    ///         <item>Message routing and topic naming</item>
    ///         <item>Service discovery and registration</item>
    ///         <item>CloudEvents source identification</item>
    ///         <item>Database schema naming for message persistence</item>
    ///     </list>
    ///     
    ///     <para>The name should be stable across deployments and follow DNS naming conventions
    ///     (lowercase, hyphens, no underscores).</para>
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
    ///     <para>This URN is automatically generated during configuration and used for:</para>
    ///     <list type="bullet">
    ///         <item>Message routing and subscription patterns</item>
    ///         <item>Service identification in distributed tracing</item>
    ///         <item>Dead letter queue naming</item>
    ///         <item>Health check endpoint registration</item>
    ///     </list>
    ///     
    ///     <para>The URN format ensures uniqueness across different domains and services
    ///     while maintaining readability and following URI conventions.</para>
    /// </remarks>
    /// <example>
    ///     <para>Example URNs generated from configuration:</para>
    ///     <code>
    /// // Domain: "ECommerce", PublicServiceName: "order-service"
    /// // Generated URN: "/e_commerce/order-service"
    /// 
    /// // Domain: "CustomerManagement", PublicServiceName: "customer-api"  
    /// // Generated URN: "/customer_management/customer-api"
    /// </code>
    /// </example>
    public Uri ServiceUrn { get; private set; } = null!;

    /// <summary>
    ///     Gets or sets the CloudEvents configuration for standardized cross-service messaging.
    /// </summary>
    /// <value>
    ///     A <see cref="CloudEventsSettings"/> instance containing CloudEvents specification
    ///     settings for event formatting and routing.
    /// </value>
    /// <remarks>
    ///     <para>CloudEvents provides a standardized format for event data, enabling
    ///     interoperability between different services and platforms. This configuration
    ///     controls how events are formatted when published to external systems.</para>
    ///     
    ///     <para>Key CloudEvents properties configured:</para>
    ///     <list type="bullet">
    ///         <item><strong>Source:</strong> URI identifying the event producer</item>
    ///         <item><strong>Type:</strong> Event type for categorization and routing</item>
    ///         <item><strong>Subject:</strong> Subject of the event for filtering</item>
    ///         <item><strong>DataContentType:</strong> Format of the event data</item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <para><strong>CloudEvents Configuration:</strong></para>
    ///     <code>
    /// {
    ///   "ServiceBus": {
    ///     "CloudEvents": {
    ///       "Source": "https://api.mystore.com/orders",
    ///       "DefaultType": "com.mystore.orders",
    ///       "Subject": "orders",
    ///       "DataContentType": "application/json"
    ///     }
    ///   }
    /// }
    /// </code>
    ///     
    ///     <para><strong>Generated CloudEvent Example:</strong></para>
    ///     <code>
    /// {
    ///   "specversion": "1.0",
    ///   "type": "com.mystore.orders.order-created",
    ///   "source": "https://api.mystore.com/orders",
    ///   "subject": "orders/12345",
    ///   "id": "550e8400-e29b-41d4-a716-446655440000",
    ///   "time": "2024-01-15T10:30:00Z",
    ///   "datacontenttype": "application/json",
    ///   "data": {
    ///     "orderId": "12345",
    ///     "customerId": "67890",
    ///     "totalAmount": 99.99
    ///   }
    /// }
    /// </code>
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
    ///     This method ensures service names are compatible with various infrastructure
    ///     components that require DNS-compliant names, including Kubernetes services,
    ///     Docker containers, and message broker topics.
    /// </remarks>
    /// <example>
    ///     <code>
    /// GetServiceName("ECommerce.OrderService") // Returns: "ecommerce-orderservice"
    /// GetServiceName("Customer.API") // Returns: "customer-api"
    /// GetServiceName("payment-processor") // Returns: "payment-processor"
    /// </code>
    /// </example>
    public static string GetServiceName(string appName) => appName.ToLowerInvariant().Replace('.', '-');

    private static string GetDomainName()
    {
        //TODO: make this better, potentially extract an assembly attribute/csproj config
        var assemblyName = ServiceDefaultsExtensions.EntryAssembly.FullName!;
        var mainNamespaceIndex = assemblyName.IndexOf('.');

        return mainNamespaceIndex >= 0 ? assemblyName[..mainNamespaceIndex] : assemblyName;
    }

    /// <summary>
    ///     Post-configuration processor that validates and completes ServiceBus configuration after binding.
    /// </summary>
    /// <remarks>
    ///     <para>This configurator runs after the options have been bound from configuration
    ///     and performs final setup including:</para>
    ///     <list type="bullet">
    ///         <item>Setting default PublicServiceName if not provided</item>
    ///         <item>Generating the service URN from domain and service name</item>
    ///         <item>Validating required connection strings</item>
    ///         <item>Logging configuration warnings for missing dependencies</item>
    ///     </list>
    ///     
    ///     <para>The configurator follows the .NET options pattern for post-configuration
    ///     processing, ensuring all derived values are computed correctly even when
    ///     base configuration is incomplete.</para>
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
        ///     <para>This method performs the following configuration steps:</para>
        ///     <list type="number">
        ///         <item>Sets PublicServiceName to a default derived from the application name if not specified</item>
        ///         <item>Generates the ServiceUrn by combining the domain (in snake_case) with the service name</item>
        ///         <item>Validates that required connection strings are present</item>
        ///         <item>Logs appropriate warnings for missing or incomplete configuration</item>
        ///     </list>
        ///     
        ///     <para>Configuration validation includes checking for the ServiceBus connection string,
        ///     which is required for message persistence, transactional inbox/outbox patterns,
        ///     and reliable message delivery.</para>
        /// </remarks>
        /// <example>
        ///     <para><strong>Automatic Configuration Example:</strong></para>
        ///     <code>
        /// // Given application name: "ECommerce.OrderService"
        /// // And configuration:
        /// {
        ///   "ServiceBus": {
        ///     "Domain": "ECommerce"
        ///     // PublicServiceName not specified
        ///   }
        /// }
        /// 
        /// // After post-configuration:
        /// // PublicServiceName = "ecommerce-orderservice" (derived from app name)
        /// // ServiceUrn = "/e_commerce/ecommerce-orderservice" (generated URN)
        /// </code>
        ///     
        ///     <para><strong>Warning Scenarios:</strong></para>
        ///     <code>
        /// // Missing connection string warning:
        /// // "ConnectionStrings:ServiceBus is not set. Transactional Inbox/Outbox 
        /// //  and Message Persistence features disabled"
        /// 
        /// // This allows the application to start without messaging persistence
        /// // but logs a clear warning about reduced functionality
        /// </code>
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
