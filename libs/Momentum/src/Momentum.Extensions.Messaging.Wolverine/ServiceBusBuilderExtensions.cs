// Copyright (c) Momentum .NET. All rights reserved.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Momentum.Extensions.Messaging.Wolverine.Telemetry;
using Momentum.ServiceDefaults.Messaging;
using OpenTelemetry.Metrics;

namespace Momentum.Extensions.Messaging.Wolverine;

/// <summary>
///     Extension methods for configuring Wolverine as the messaging provider.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ServiceBusBuilderExtensions
{
    /// <summary>
    ///     Configures Wolverine as the messaging provider with production-ready defaults.
    /// </summary>
    /// <param name="builder">The service bus builder.</param>
    /// <param name="configure">Optional action to customize Wolverine options.</param>
    /// <returns>The service bus builder for method chaining.</returns>
    public static ServiceBusBuilder UseWolverine(this ServiceBusBuilder builder, Action<WolverineOptions>? configure = null)
    {
        var hostBuilder = builder.HostBuilder;
        var serviceBusConfig = hostBuilder.Configuration.GetSection(ServiceBusOptions.SectionName);

        hostBuilder.Services.AddWolverineWithDefaults(serviceBusConfig, configure);
        hostBuilder.AddKeyedNpgsqlDataSource(ServiceBusOptions.SectionName);
        hostBuilder.Services.ConfigureOptions<WolverineNpgsqlExtensions>();

        hostBuilder.Services.AddSingleton<ICliCommandHandler, WolverineCliCommandHandler>();

        AddMessagingTelemetry(hostBuilder);

        return builder;
    }

    private static void AddMessagingTelemetry(IHostApplicationBuilder builder)
    {
        var messagingMeterName = builder.Configuration.GetValue<string>("OpenTelemetry:MessagingMeterName")
                                 ?? $"{builder.Environment.ApplicationName}.Messaging";

        builder.Services
            .AddSingleton<MessagingMeterStore>()
            .AddKeyedSingleton<Meter>(MessagingMeterStore.MessagingMeterKey,
                (provider, _) => provider.GetRequiredService<IMeterFactory>().Create(messagingMeterName));

        builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics
            .AddMeter(messagingMeterName)
            .AddMeter(nameof(Wolverine)));
    }
}
