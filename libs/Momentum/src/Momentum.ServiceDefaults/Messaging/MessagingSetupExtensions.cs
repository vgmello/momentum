// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.Messaging.Wolverine;

namespace Momentum.ServiceDefaults.Messaging;

public static class MessagingSetupExtensions
{
    /// <summary>
    ///     Adds ServiceBus with Wolverine messaging framework with production-ready configuration for reliable message processing.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <param name="configure">Optional action to configure Wolverine options for specific business requirements.</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    /// <remarks>
    ///     <!--@include: @code/messaging/wolverine-setup-detailed.md -->
    /// </remarks>
    /// <example>
    ///     <!--@include: @code/examples/wolverine-setup-examples.md -->
    /// </example>
    public static IHostApplicationBuilder AddServiceBus(this IHostApplicationBuilder builder, Action<WolverineOptions>? configure = null)
    {
        var serviceBusConfig = builder.Configuration.GetSection(ServiceBusOptions.SectionName);

        builder.Services
            .ConfigureOptions<ServiceBusOptions.Configurator>()
            .AddOptions<ServiceBusOptions>()
            .BindConfiguration(ServiceBusOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddWolverineWithDefaults(serviceBusConfig, configure);

        builder.AddKeyedNpgsqlDataSource(ServiceBusOptions.SectionName);
        builder.Services.ConfigureOptions<WolverineNpgsqlExtensions>();

        return builder;
    }
}
