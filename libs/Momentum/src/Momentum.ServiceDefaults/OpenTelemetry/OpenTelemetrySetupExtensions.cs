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
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Momentum.ServiceDefaults.OpenTelemetry;

/// <summary>
///     Provides extension methods for configuring OpenTelemetry instrumentation.
/// </summary>
public static class OpenTelemetrySetupExtensions
{
    private const double DefaultDevelopmentSamplingRate = 1.0;
    private const double DefaultProductionSamplingRate = 0.1;

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
        var otelOptions = new OpenTelemetryOptions();
        builder.Configuration.GetSection(OpenTelemetryOptions.SectionName).Bind(otelOptions);

        if (otelOptions.SamplingRate is < 0.0 or > 1.0)
        {
            throw new InvalidOperationException(
                $"OpenTelemetry:SamplingRate must be between 0.0 and 1.0, got {otelOptions.SamplingRate}");
        }

        var samplingRate = otelOptions.SamplingRate
                           ?? (builder.Environment.IsDevelopment() ? DefaultDevelopmentSamplingRate : DefaultProductionSamplingRate);

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
                    ["env"] = builder.Environment.EnvironmentName,
                    ["service.instance.id"] = Environment.MachineName
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
                .SetSampler(new TraceIdRatioBasedSampler(samplingRate)));

        return builder;
    }

    private static readonly FrozenSet<string> ExcludedClientPaths = new[]
    {
        "/OrleansSiloInstances",
        "/$batch"
    }.ToFrozenSet();
}
