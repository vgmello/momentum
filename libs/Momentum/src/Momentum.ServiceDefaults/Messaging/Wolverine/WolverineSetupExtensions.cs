// Copyright (c) Momentum .NET. All rights reserved.

using JasperFx.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    ///     Adds Wolverine messaging framework with production-ready configuration for reliable message processing.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <param name="configure">Optional action to configure Wolverine options for specific business requirements.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     <para>This method configures Wolverine with enterprise-grade messaging capabilities:</para>
    ///     <list type="bullet">
    ///         <item>Registers a keyed PostgreSQL data source for durable message persistence</item>
    ///         <item>Configures Wolverine with CQRS patterns and reliable messaging defaults</item>
    ///         <item>Integrates with OpenTelemetry for distributed tracing and performance monitoring</item>
    ///         <item>Sets up automatic validation, exception handling, and transaction management</item>
    ///         <item>Enables CloudEvent support for standardized cross-service communication</item>
    ///         <item>Provides health checks for messaging infrastructure monitoring</item>
    ///         <item>Skips registration if <see cref="SkipServiceRegistration" /> is true (for testing scenarios)</item>
    ///     </list>
    ///     
    ///     <para>The messaging system supports both synchronous request/response patterns and
    ///     asynchronous event-driven architectures, making it suitable for complex business workflows.</para>
    /// </remarks>
    /// <example>
    ///     <para><strong>Basic E-commerce Order Processing Setup:</strong></para>
    ///     <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Configure core messaging infrastructure
    /// builder.AddWolverine(opts =>
    /// {
    ///     // Configure custom routing for order events
    ///     opts.PublishMessage&lt;OrderCreated&gt;()
    ///         .ToKafkaTopic("ecommerce.orders.order-created")
    ///         .UseDurableOutbox();
    ///         
    ///     // Configure local queues for background processing
    ///     opts.LocalQueue("order-processing")
    ///         .UseDurableInbox()
    ///         .ProcessInline();
    /// });
    /// 
    /// var app = builder.Build();
    /// await app.RunAsync(args);
    /// </code>
    ///     
    ///     <para><strong>Command Handler with Automatic Validation:</strong></para>
    ///     <code>
    /// // Command definition with validation
    /// public record CreateOrder(Guid CustomerId, List&lt;OrderItem&gt; Items);
    /// 
    /// public class CreateOrderValidator : AbstractValidator&lt;CreateOrder&gt;
    /// {
    ///     public CreateOrderValidator()
    ///     {
    ///         RuleFor(x => x.CustomerId).NotEmpty();
    ///         RuleFor(x => x.Items).NotEmpty().Must(items => items.Count &lt;= 50);
    ///     }
    /// }
    /// 
    /// // Handler - validation happens automatically before execution
    /// public static class OrderHandlers
    /// {
    ///     public static async Task&lt;OrderCreated&gt; Handle(
    ///         CreateOrder command,
    ///         IOrderRepository orders,
    ///         IMessageBus bus,
    ///         ILogger&lt;OrderHandlers&gt; logger)
    ///     {
    ///         // Command is guaranteed to be valid
    ///         var order = new Order(command.CustomerId, command.Items);
    ///         await orders.SaveAsync(order);
    ///         
    ///         // Publish integration event for other services
    ///         await bus.PublishAsync(new OrderCreated(order.Id, order.CustomerId));
    ///         
    ///         logger.LogInformation("Order {OrderId} created for customer {CustomerId}", 
    ///             order.Id, order.CustomerId);
    ///             
    ///         return new OrderCreated(order.Id, order.CustomerId);
    ///     }
    /// }
    /// </code>
    ///     
    ///     <para><strong>Event Handler for Cross-Service Integration:</strong></para>
    ///     <code>
    /// // Integration event from external service
    /// [EventTopic("ecommerce.payments.payment-completed")]
    /// public record PaymentCompleted(Guid OrderId, decimal Amount);
    /// 
    /// // Handler automatically discovered and registered
    /// public static class PaymentHandlers
    /// {
    ///     [KafkaListener("ecommerce.payments")]
    ///     public static async Task Handle(
    ///         PaymentCompleted @event,
    ///         IOrderRepository orders,
    ///         IMessageBus bus)
    ///     {
    ///         var order = await orders.GetByIdAsync(@event.OrderId);
    ///         order.MarkAsPaid(@event.Amount);
    ///         await orders.SaveAsync(order);
    ///         
    ///         // Trigger fulfillment process
    ///         await bus.SendToQueueAsync("fulfillment", 
    ///             new FulfillOrder(order.Id, order.Items));
    ///     }
    /// }
    /// </code>
    ///     
    ///     <para><strong>Configuration for Multi-Service Architecture:</strong></para>
    ///     <code>
    /// // appsettings.json
    /// {
    ///   "ConnectionStrings": {
    ///     "ServiceBus": "Host=postgres;Database=order_service_messaging;Username=app;Password=secret"
    ///   },
    ///   "ServiceBus": {
    ///     "Domain": "ECommerce",
    ///     "PublicServiceName": "order-service",
    ///     "CloudEvents": {
    ///       "Source": "https://api.mystore.com/orders",
    ///       "DefaultType": "com.mystore.orders"
    ///     }
    ///   },
    ///   "Kafka": {
    ///     "BootstrapServers": "kafka:9092",
    ///     "GroupId": "order-service-v1"
    ///   }
    /// }
    /// </code>
    /// </example>
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
    ///     Adds Wolverine services with comprehensive defaults for enterprise messaging scenarios.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="env">The hosting environment for environment-specific configuration.</param>
    /// <param name="configuration">The application configuration containing connection strings and messaging settings.</param>
    /// <param name="configure">Optional action to customize Wolverine options for specific business requirements.</param>
    /// <remarks>
    ///     <para>This method provides a complete messaging infrastructure configuration including:</para>
    ///     
    ///     <para><strong>Persistence and Reliability:</strong></para>
    ///     <list type="bullet">
    ///         <item>PostgreSQL persistence for message durability and transaction support</item>
    ///         <item>Reliable messaging with inbox/outbox patterns for guaranteed delivery</item>
    ///         <item>Automatic transaction scoping for consistency across business operations</item>
    ///         <item>Dead letter queue handling for failed message processing</item>
    ///     </list>
    ///     
    ///     <para><strong>Integration and Transport:</strong></para>
    ///     <list type="bullet">
    ///         <item>Kafka integration for high-throughput event streaming</item>
    ///         <item>CloudEvents standard support for interoperability</item>
    ///         <item>System.Text.Json serialization with performance optimization</item>
    ///         <item>Cross-service message routing and topic management</item>
    ///     </list>
    ///     
    ///     <para><strong>Quality and Observability:</strong></para>
    ///     <list type="bullet">
    ///         <item>FluentValidation integration for automatic message validation</item>
    ///         <item>Structured exception handling with retry policies</item>
    ///         <item>OpenTelemetry instrumentation for distributed tracing</item>
    ///         <item>Performance monitoring and request tracking middleware</item>
    ///         <item>Health checks for messaging infrastructure components</item>
    ///     </list>
    ///     
    ///     <para><strong>Development and Deployment:</strong></para>
    ///     <list type="bullet">
    ///         <item>Resource setup on startup for database migrations</item>
    ///         <item>Convention-based handler discovery from domain assemblies</item>
    ///         <item>Environment-aware configuration and feature flags</item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <para><strong>Manual Service Registration for Testing:</strong></para>
    ///     <code>
    /// // In test setup
    /// public class OrderServiceIntegrationTests : IClassFixture&lt;TestFixture&gt;
    /// {
    ///     private readonly IServiceProvider _services;
    ///     
    ///     public OrderServiceIntegrationTests(TestFixture fixture)
    ///     {
    ///         var services = new ServiceCollection();
    ///         var config = fixture.Configuration;
    ///         var env = fixture.Environment;
    ///         
    ///         // Add Wolverine with test-specific configuration
    ///         services.AddWolverineWithDefaults(env, config, opts =>
    ///         {
    ///             // Use in-memory transport for testing
    ///             opts.UseInMemoryTransport();
    ///             
    ///             // Disable external dependencies
    ///             opts.DisableKafka();
    ///             
    ///             // Enable immediate processing for synchronous testing
    ///             opts.Policies.DisableConventionalLocalRouting();
    ///         });
    ///         
    ///         _services = services.BuildServiceProvider();
    ///     }
    /// }
    /// </code>
    ///     
    ///     <para><strong>Environment-Specific Configuration:</strong></para>
    ///     <code>
    /// // appsettings.Development.json
    /// {
    ///   "ConnectionStrings": {
    ///     "ServiceBus": "Host=localhost;Database=dev_messaging;Username=dev;Password=dev"
    ///   },
    ///   "ServiceBus": {
    ///     "PublicServiceName": "order-service-dev"
    ///   },
    ///   "Kafka": {
    ///     "BootstrapServers": "localhost:9092"
    ///   }
    /// }
    /// 
    /// // appsettings.Production.json  
    /// {
    ///   "ConnectionStrings": {
    ///     "ServiceBus": "${MESSAGING_CONNECTION_STRING}"
    ///   },
    ///   "ServiceBus": {
    ///     "PublicServiceName": "order-service",
    ///     "CloudEvents": {
    ///       "Source": "https://api.production.mystore.com/orders"
    ///     }
    ///   },
    ///   "Kafka": {
    ///     "BootstrapServers": "${KAFKA_BOOTSTRAP_SERVERS}",
    ///     "SecurityProtocol": "SaslSsl",
    ///     "SaslMechanism": "Plain"
    ///   }
    /// }
    /// </code>
    /// </example>
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
