// Copyright (c) Momentum .NET. All rights reserved.

using FluentValidation;
using JasperFx;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.Logging;
using Momentum.ServiceDefaults.Messaging.Wolverine;
using Momentum.ServiceDefaults.OpenTelemetry;
using Serilog;
using System.Reflection;

namespace Momentum.ServiceDefaults;

/// <summary>
///     Provides extension methods for configuring service defaults in the application.
/// </summary>
public static class ServiceDefaultsExtensions
{
    private static Assembly? _entryAssembly;

    /// <summary>
    ///     Gets or sets the entry assembly for the application.
    /// </summary>
    /// <value>
    ///     The entry assembly used for discovering domain assemblies and validators.
    ///     If not explicitly set, it defaults to the application's entry assembly.
    /// </value>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when attempting to get the entry assembly, and it cannot be determined.
    /// </exception>
    public static Assembly EntryAssembly
    {
        get => _entryAssembly ??= GetEntryAssembly();
        set => _entryAssembly = value;
    }

    /// <summary>
    ///     Adds common service defaults to the application builder for production-ready microservices.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     This method configures essential infrastructure for microservices including:
    ///     <list type="bullet">
    ///         <item>HTTPS configuration for Kestrel with secure communication</item>
    ///         <item>Structured logging with Serilog and OpenTelemetry integration</item>
    ///         <item>Distributed tracing and metrics collection via OpenTelemetry</item>
    ///         <item>Wolverine messaging framework with PostgreSQL persistence</item>
    ///         <item>FluentValidation validators discovery from domain assemblies</item>
    ///         <item>Health checks with multiple endpoints for liveness/readiness probes</item>
    ///         <item>Service discovery for container orchestration environments</item>
    ///         <item>HTTP client resilience with circuit breakers and retry policies</item>
    ///     </list>
    ///     
    ///     This method is designed to be the single entry point for configuring a production-ready
    ///     service with observability, resilience, and messaging capabilities required for
    ///     cloud-native microservices architectures.
    /// </remarks>
    /// <example>
    ///     <para><strong>Basic E-commerce API Setup:</strong></para>
    ///     <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Configure core service infrastructure
    /// builder.AddServiceDefaults();
    /// 
    /// // Add your business services
    /// builder.Services.AddScoped&lt;IOrderService, OrderService&gt;();
    /// builder.Services.AddDbContext&lt;EcommerceDbContext&gt;();
    /// 
    /// var app = builder.Build();
    /// 
    /// // Map health check endpoints
    /// app.MapDefaultHealthCheckEndpoints();
    /// 
    /// // Map your API endpoints
    /// app.MapGroup("/api/orders").MapOrdersApi();
    /// 
    /// // Run with proper initialization and error handling
    /// await app.RunAsync(args);
    /// </code>
    ///     
    ///     <para><strong>Configuration in appsettings.json:</strong></para>
    ///     <code>
    /// {
    ///   "ConnectionStrings": {
    ///     "ServiceBus": "Host=localhost;Database=ecommerce_messaging;Username=app;Password=secret"
    ///   },
    ///   "OpenTelemetry": {
    ///     "ActivitySourceName": "ECommerceAPI",
    ///     "MessagingMeterName": "ECommerce.Messaging"
    ///   },
    ///   "ServiceBus": {
    ///     "Domain": "ECommerce",
    ///     "PublicServiceName": "orders-api"
    ///   }
    /// }
    /// </code>
    ///     
    ///     <para><strong>Domain Assembly Registration:</strong></para>
    ///     <code>
    /// // In AssemblyInfo.cs or GlobalUsings.cs
    /// [assembly: DomainAssembly(typeof(Order), typeof(Customer), typeof(Product))]
    /// 
    /// // This enables automatic discovery of:
    /// // - Command/Query handlers in the Order, Customer, Product assemblies
    /// // - FluentValidation validators
    /// // - Integration events
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrelHttpsConfiguration();

        builder.AddLogging();
        builder.AddOpenTelemetry();
        builder.AddWolverine();
        builder.AddValidators();

        builder.Services.AddHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();
        });

        return builder;
    }

    /// <summary>
    ///     Adds FluentValidation validators from the entry assembly and all domain assemblies.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <remarks>
    ///     This method automatically discovers and registers FluentValidation validators from:
    ///     <list type="bullet">
    ///         <item>The entry assembly (your main application assembly)</item>
    ///         <item>All assemblies marked with <see cref="DomainAssemblyAttribute" /></item>
    ///     </list>
    ///     
    ///     Validators are registered as scoped services and integrated with Wolverine's
    ///     message handling pipeline for automatic validation of commands and queries.
    ///     
    ///     This approach supports the Domain-Driven Design pattern where validation
    ///     rules are defined close to the domain entities and business logic.
    /// </remarks>
    /// <example>
    ///     <para><strong>Domain Assembly Configuration:</strong></para>
    ///     <code>
    /// // In your API project's GlobalUsings.cs or AssemblyInfo.cs
    /// [assembly: DomainAssembly(typeof(Order), typeof(Customer))]
    /// 
    /// // This will scan Order and Customer assemblies for validators like:
    /// // Orders.Domain.Commands.CreateOrderValidator
    /// // Customers.Domain.Queries.GetCustomerValidator
    /// </code>
    ///     
    ///     <para><strong>Example Validator in Domain Assembly:</strong></para>
    ///     <code>
    /// // In Orders.Domain assembly
    /// public class CreateOrderValidator : AbstractValidator&lt;CreateOrder&gt;
    /// {
    ///     public CreateOrderValidator()
    ///     {
    ///         RuleFor(x => x.CustomerId)
    ///             .NotEmpty()
    ///             .WithMessage("Customer ID is required");
    ///             
    ///         RuleFor(x => x.Items)
    ///             .NotEmpty()
    ///             .WithMessage("Order must contain at least one item");
    ///             
    ///         RuleForEach(x => x.Items)
    ///             .SetValidator(new OrderItemValidator());
    ///     }
    /// }
    /// </code>
    ///     
    ///     <para><strong>Automatic Integration with Wolverine:</strong></para>
    ///     <code>
    /// // Validation happens automatically in message handlers
    /// public static async Task&lt;OrderCreated&gt; Handle(
    ///     CreateOrder command,  // Automatically validated
    ///     IOrderRepository orders,
    ///     ILogger logger)
    /// {
    ///     // Command is guaranteed to be valid when this executes
    ///     // ValidationException is thrown automatically if invalid
    ///     
    ///     var order = new Order(command.CustomerId, command.Items);
    ///     await orders.SaveAsync(order);
    ///     
    ///     return new OrderCreated(order.Id, order.CustomerId);
    /// }
    /// </code>
    /// </example>
    public static void AddValidators(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(EntryAssembly);

        foreach (var assembly in DomainAssemblyAttribute.GetDomainAssemblies())
            builder.Services.AddValidatorsFromAssembly(assembly);
    }

    /// <summary>
    ///     Runs the web application with proper initialization, error handling, and command-line tool support.
    /// </summary>
    /// <param name="app">The web application to run.</param>
    /// <param name="args">Command line arguments passed to the application.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    ///     This method provides robust application lifecycle management including:
    ///     <list type="bullet">
    ///         <item>Bootstrap logger initialization for early-stage logging</item>
    ///         <item>Wolverine CLI command detection and execution for DevOps tasks</item>
    ///         <item>Graceful exception handling with structured logging</item>
    ///         <item>Proper log flushing on application shutdown</item>
    ///         <item>Support for containerized environments and orchestrators</item>
    ///     </list>
    ///     
    ///     <para><strong>Supported Wolverine Commands:</strong></para>
    ///     <list type="table">
    ///         <item>
    ///             <term>check-env</term>
    ///             <description>Validates messaging infrastructure connectivity</description>
    ///         </item>
    ///         <item>
    ///             <term>codegen</term>
    ///             <description>Generates Wolverine handler code for optimization</description>
    ///         </item>
    ///         <item>
    ///             <term>db-apply, db-assert</term>
    ///             <description>Database schema migration and validation</description>
    ///         </item>
    ///         <item>
    ///             <term>storage</term>
    ///             <description>Message persistence storage management</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <para><strong>Standard Application Startup:</strong></para>
    ///     <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddServiceDefaults();
    /// 
    /// var app = builder.Build();
    /// app.MapDefaultHealthCheckEndpoints();
    /// app.MapOrdersApi();
    /// 
    /// // This handles both normal execution and CLI commands
    /// await app.RunAsync(args);
    /// </code>
    ///     
    ///     <para><strong>Database Migration in Docker:</strong></para>
    ///     <code>
    /// # Dockerfile for migration container
    /// FROM mcr.microsoft.com/dotnet/aspnet:9.0
    /// COPY publish/ /app
    /// WORKDIR /app
    /// ENTRYPOINT ["dotnet", "OrderService.dll", "db-apply"]
    /// </code>
    ///     
    ///     <para><strong>Health Check Validation:</strong></para>
    ///     <code>
    /// # In CI/CD pipeline or health monitoring
    /// docker run --rm order-service:latest check-env
    /// 
    /// # Returns exit code 0 if healthy, non-zero if issues detected
    /// </code>
    /// </example>
    public static async Task RunAsync(this WebApplication app, string[] args)
    {
        app.UseInitializationLogger();

        try
        {
            if (args.Length > 0 && WolverineCommands.Contains(args[0]))
            {
                await app.RunJasperFxCommands(args);
            }

            await app.RunAsync();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static readonly HashSet<string> WolverineCommands =
    [
        "check-env",
        "codegen",
        "db-apply",
        "db-assert",
        "db-dump",
        "db-patch",
        "describe",
        "help",
        "resources",
        "storage"
    ];

    private static Assembly GetEntryAssembly()
    {
        return Assembly.GetEntryAssembly() ??
               throw new InvalidOperationException(
                   "Unable to identify entry assembly. Please provide an assembly via the Extensions.AssemblyMarker property.");
    }
}
