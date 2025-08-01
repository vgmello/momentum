// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Context.Propagation;
using Operations.ServiceDefaults.Messaging.Telemetry;
using System.Diagnostics.Metrics;
using OpenTelemetry.Resources;

namespace Operations.ServiceDefaults.OpenTelemetry;

/// <summary>
///     Provides extension methods for configuring OpenTelemetry instrumentation.
/// </summary>
public static class OpenTelemetrySetupExtensions
{
    /// <summary>
    ///     Adds OpenTelemetry instrumentation for logging, metrics, and distributed tracing.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     This method configures:
    ///     <list type="bullet">
    ///         <item>W3C Trace Context and Baggage propagation for distributed tracing</item>
    ///         <item>Activity source for custom tracing with configurable name</item>
    ///         <item>Messaging meter for custom metrics collection</item>
    ///         <item>OpenTelemetry logging with formatted messages and scopes</item>
    ///         <item>OTLP exporter for sending telemetry data</item>
    ///         <item>Metrics collection for ASP.NET Core, HTTP client, runtime, and Wolverine</item>
    ///         <item>Distributed tracing with filtering for health check endpoints</item>
    ///         <item>HTTP client instrumentation with path filtering and exception recording</item>
    ///         <item>gRPC client instrumentation for service-to-service calls</item>
    ///         <item>Configurable sampling (100% in development, 10% in production)</item>
    ///     </list>
    ///     Configuration can be customized via:
    ///     <list type="bullet">
    ///         <item>OpenTelemetry:ActivitySourceName - Custom activity source name (defaults to application name)</item>
    ///         <item>OpenTelemetry:MessagingMeterName - Custom meter name for messaging (defaults to {AppName}.Messaging)</item>
    ///     </list>
    /// </remarks>
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
