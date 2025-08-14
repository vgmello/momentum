// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Context.Propagation;
using Momentum.ServiceDefaults.Messaging.Telemetry;
using System.Diagnostics.Metrics;
using OpenTelemetry.Resources;

namespace Momentum.ServiceDefaults.OpenTelemetry;

/// <summary>
///     Provides extension methods for configuring OpenTelemetry instrumentation.
/// </summary>
public static class OpenTelemetrySetupExtensions
{
    /// <summary>
    ///     Adds comprehensive OpenTelemetry instrumentation for production-ready observability including logging, metrics, and distributed tracing.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     <para>This method configures a complete observability stack suitable for cloud-native microservices:</para>
    ///     
    ///     <para><strong>Distributed Tracing:</strong></para>
    ///     <list type="bullet">
    ///         <item>W3C Trace Context and Baggage propagation for cross-service correlation</item>
    ///         <item>ASP.NET Core request tracing with health check filtering</item>
    ///         <item>HTTP client instrumentation with sensitive path exclusion</item>
    ///         <item>gRPC client tracing with URI enrichment</item>
    ///         <item>Environment-aware sampling (100% dev, 10% production)</item>
    ///     </list>
    ///     
    ///     <para><strong>Metrics Collection:</strong></para>
    ///     <list type="bullet">
    ///         <item>ASP.NET Core metrics (request duration, response status)</item>
    ///         <item>HTTP client metrics (request duration, retry counts)</item>
    ///         <item>.NET runtime metrics (GC, memory, thread pool)</item>
    ///         <item>Wolverine messaging metrics (message processing, errors)</item>
    ///         <item>Custom application metrics via configurable meters</item>
    ///     </list>
    ///     
    ///     <para><strong>OTLP Export:</strong></para>
    ///     <list type="bullet">
    ///         <item>OTLP exporter for compatibility with Jaeger, Zipkin, and cloud providers</item>
    ///         <item>Automatic resource attribution with environment labeling</item>
    ///         <item>Efficient batching and compression for production workloads</item>
    ///     </list>
    ///     
    ///     <para><strong>Configuration Options:</strong></para>
    ///     <list type="table">
    ///         <item>
    ///             <term>OpenTelemetry:ActivitySourceName</term>
    ///             <description>Custom activity source name (defaults to application name)</description>
    ///         </item>
    ///         <item>
    ///             <term>OpenTelemetry:MessagingMeterName</term>
    ///             <description>Custom meter name for messaging metrics (defaults to {AppName}.Messaging)</description>
    ///         </item>
    ///         <item>
    ///             <term>OTEL_EXPORTER_OTLP_ENDPOINT</term>
    ///             <description>OTLP collector endpoint (e.g., http://jaeger:4317)</description>
    ///         </item>
    ///         <item>
    ///             <term>OTEL_SERVICE_NAME</term>
    ///             <description>Service name for telemetry attribution</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <para><strong>Basic Usage with Default Configuration:</strong></para>
    ///     <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Adds complete observability stack
    /// builder.AddOpenTelemetry();
    /// 
    /// var app = builder.Build();
    /// 
    /// // Your traces will now include:
    /// // - HTTP request spans with timing
    /// // - Database query spans (if using Entity Framework)
    /// // - Custom business logic spans
    /// app.MapGet("/orders/{id}", async (int id, ActivitySource activitySource) =>
    /// {
    ///     using var activity = activitySource.StartActivity("GetOrder");
    ///     activity?.SetTag("order.id", id);
    ///     
    ///     // This operation will be traced
    ///     return await GetOrderById(id);
    /// });
    /// 
    /// await app.RunAsync(args);
    /// </code>
    ///     
    ///     <para><strong>Custom Configuration for Production:</strong></para>
    ///     <code>
    /// // appsettings.Production.json
    /// {
    ///   "OpenTelemetry": {
    ///     "ActivitySourceName": "ECommerce.OrderService",
    ///     "MessagingMeterName": "ECommerce.Orders.Messaging"
    ///   },
    ///   "OTEL_EXPORTER_OTLP_ENDPOINT": "http://otel-collector:4317",
    ///   "OTEL_SERVICE_NAME": "order-service",
    ///   "OTEL_RESOURCE_ATTRIBUTES": "deployment.environment=production,service.version=1.2.0"
    /// }
    /// </code>
    ///     
    ///     <para><strong>Custom Metrics in Business Logic:</strong></para>
    ///     <code>
    /// public class OrderService(Meter orderMeter)
    /// {
    ///     private readonly Counter&lt;int&gt; _ordersProcessed = 
    ///         orderMeter.CreateCounter&lt;int&gt;("orders_processed_total");
    ///     private readonly Histogram&lt;double&gt; _orderValue = 
    ///         orderMeter.CreateHistogram&lt;double&gt;("order_value_dollars");
    ///         
    ///     public async Task&lt;Order&gt; CreateOrderAsync(CreateOrderRequest request)
    ///     {
    ///         var order = new Order(request);
    ///         await _repository.SaveAsync(order);
    ///         
    ///         // Custom metrics for business insights
    ///         _ordersProcessed.Add(1, new("customer_type", request.CustomerType));
    ///         _orderValue.Record(order.TotalValue, new("product_category", order.PrimaryCategory));
    ///         
    ///         return order;
    ///     }
    /// }
    /// </code>
    ///     
    ///     <para><strong>Distributed Tracing Across Services:</strong></para>
    ///     <code>
    /// // Service A (Order API)
    /// app.MapPost("/orders", async (CreateOrderRequest request, HttpClient httpClient) =>
    /// {
    ///     // Create order locally
    ///     var order = await CreateOrder(request);
    ///     
    ///     // Call inventory service - trace context automatically propagated
    ///     await httpClient.PostAsJsonAsync("http://inventory-service/reserve", 
    ///         new { OrderId = order.Id, Items = order.Items });
    ///         
    ///     return order;
    /// });
    /// 
    /// // Service B (Inventory API) - receives trace context automatically
    /// app.MapPost("/reserve", async (ReserveItemsRequest request) =>
    /// {
    ///     // This operation appears as child span in the same trace
    ///     return await ReserveInventory(request);
    /// });
    /// </code>
    ///     
    ///     <para><strong>Troubleshooting Common Issues:</strong></para>
    ///     <code>
    /// // Problem: No traces appearing in collector
    /// // Solution: Check OTLP endpoint and network connectivity
    /// 
    /// // Problem: Too many traces in production
    /// // Solution: Sampling is automatically set to 10% in non-development environments
    /// 
    /// // Problem: Missing custom spans
    /// // Solution: Ensure ActivitySource is injected and spans are disposed
    /// using var activity = activitySource.StartActivity("MyOperation");
    /// // ... work here ...
    /// // Dispose happens automatically with 'using'
    /// 
    /// // Problem: Missing HTTP client traces
    /// // Solution: Paths containing these patterns are excluded by default:
    /// // - /OrleansSiloInstances (Orleans infrastructure)
    /// // - /$batch (OData batch operations)
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddOpenTelemetry(this IHostApplicationBuilder builder)
    {
        var activitySourceName = builder.Configuration.GetValue<string>("OpenTelemetry:ActivitySourceName")
                                 ?? builder.Environment.ApplicationName;

        var messagingMeterName = builder.Configuration.GetValue<string>("OpenTelemetry:MessagingMeterName")
                                 ?? $"{builder.Environment.ApplicationName}.Messaging";

        builder.Services.AddSingleton(new ActivitySource(activitySourceName));

        builder.Services
            .AddSingleton<MessagingMeterStore>()
            .AddKeyedSingleton<Meter>(MessagingMeterStore.MessagingMeterKey,
                (provider, _) => provider.GetRequiredService<IMeterFactory>().Create(messagingMeterName));

        // Configure W3C Trace Context and Baggage propagation
        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator([
            new TraceContextPropagator(),
            new BaggagePropagator()
        ]));

        builder.Services.AddOpenTelemetry()
            .UseOtlpExporter()
            .ConfigureResource(resource => resource
                .AddAttributes(new Dictionary<string, object>
                {
                    ["env"] = builder.Environment.EnvironmentName
                }))
            .WithMetrics(metrics => metrics
                .AddMeter(activitySourceName)
                .AddMeter(messagingMeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(nameof(Wolverine)))
            .WithTracing(tracing => tracing
                .AddSource(activitySourceName)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = ctx =>
                    {
                        if (ctx.Request.Path.Value?.StartsWith("/health/") == true)
                            return false;

                        return true;
                    };
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.FilterHttpRequestMessage = message =>
                    {
                        var requestPath = message.RequestUri?.AbsolutePath;

                        if (requestPath is null)
                            return true;

                        return !ExcludedClientPaths.Any(requestPath.Contains);
                    };

                    options.RecordException = true;
                })
                .AddGrpcClientInstrumentation(options =>
                {
                    options.SuppressDownstreamInstrumentation = false;
                    options.EnrichWithHttpRequestMessage = (activity, message) =>
                    {
                        activity.SetTag("grpc.request.uri", message.RequestUri?.ToString());
                    };
                })
                .SetSampler(new TraceIdRatioBasedSampler(builder.Environment.IsDevelopment() ? 1.0 : 0.1)));

        return builder;
    }

    private static readonly List<string> ExcludedClientPaths =
    [
        "/OrleansSiloInstances",
        "/$batch"
    ];
}
