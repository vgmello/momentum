// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.Messaging.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Momentum.ServiceDefaults.OpenTelemetry;

/// <summary>
///     Provides extension methods for configuring OpenTelemetry instrumentation.
/// </summary>
public static class OpenTelemetrySetupExtensions
{
    /// <summary>
    ///     Sampling rate for development environment (100% - capture all traces).
    /// </summary>
    private const double DevelopmentSamplingRate = 1.0;

    /// <summary>
    ///     Sampling rate for production environment (10% - balance between observability and performance).
    /// </summary>
    private const double ProductionSamplingRate = 0.1;
    /// <summary>
    ///     Adds comprehensive OpenTelemetry instrumentation for production-ready observability including logging, metrics, and distributed
    ///     tracing.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     <!--@include: @code/service-configuration/opentelemetry-setup-detailed.md -->
    /// </remarks>
    /// <example>
    ///     <!--@include: @code/examples/opentelemetry-setup-examples.md -->
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
                        var path = ctx.Request.Path.Value;

                        if (string.IsNullOrEmpty(path)) return true;

                        return !(path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                                 || path.StartsWith("/status", StringComparison.OrdinalIgnoreCase));
                    };

                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.FilterHttpRequestMessage = message =>
                    {
                        var requestPath = message.RequestUri?.AbsolutePath;

                        return requestPath is null || !ExcludedClientPaths.Any(requestPath.Contains);
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
                .SetSampler(new TraceIdRatioBasedSampler(
                    builder.Environment.IsDevelopment() ? DevelopmentSamplingRate : ProductionSamplingRate)));

        return builder;
    }

    private static readonly List<string> ExcludedClientPaths =
    [
        "/OrleansSiloInstances",
        "/$batch"
    ];
}
