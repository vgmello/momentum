// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Momentum.ServiceDefaults.Messaging;

[ExcludeFromCodeCoverage]
public static class MessagingSetupExtensions
{
    /// <summary>
    ///     Configures the service bus messaging infrastructure with the specified messaging provider.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <param name="configure">Action to configure the service bus builder (e.g., bus => bus.UseWolverine()).</param>
    /// <returns>The configured host application builder for method chaining.</returns>
    public static IHostApplicationBuilder AddServiceBus(this IHostApplicationBuilder builder, Action<ServiceBusBuilder> configure)
    {
        builder.Services
            .ConfigureOptions<ServiceBusOptions.Configurator>()
            .AddOptions<ServiceBusOptions>()
            .BindConfiguration(ServiceBusOptions.SectionName)
            .ValidateOnStart();

        var serviceBusBuilder = new ServiceBusBuilder(builder);
        configure(serviceBusBuilder);

        return builder;
    }
}
