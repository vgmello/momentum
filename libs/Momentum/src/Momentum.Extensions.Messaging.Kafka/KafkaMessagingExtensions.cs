// Copyright (c) Momentum .NET. All rights reserved.

using Aspire.Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.ServiceDefaults.Messaging;
using Wolverine;
using Wolverine.Kafka;

namespace Momentum.Extensions.Messaging.Kafka;

public static class KafkaMessagingExtensions
{
    public const string DefaultConnectionName = "messaging";

    /// <summary>
    /// Adds Kafka messaging extensions with full Aspire integration to the WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder</param>
    /// <param name="connectionName">The connection name for Kafka configuration</param>
    /// <param name="configureProducerSettings">Optional producer settings configuration</param>
    /// <param name="configureConsumerSettings">Optional consumer settings configuration</param>
    /// <param name="configureKafka">Optional Wolverine Kafka configuration</param>
    /// <returns>The WebApplicationBuilder for chaining</returns>
    public static WebApplicationBuilder AddKafkaMessagingExtensions(
        this WebApplicationBuilder builder,
        string connectionName = DefaultConnectionName,
        Action<KafkaProducerSettings>? configureProducerSettings = null,
        Action<KafkaConsumerSettings>? configureConsumerSettings = null,
        Action<KafkaTransportExpression>? configureKafka = null)
    {
        // Register Aspire Kafka producer and consumer services
        builder.AddKafkaProducer<string, byte[]>(connectionName, configureProducerSettings);
        builder.AddKafkaConsumer<string, byte[]>(connectionName, configureConsumerSettings);

        // Register Wolverine extension for event routing and CloudEvents support
        builder.Services.AddSingleton<IWolverineExtension>(sp =>
            new KafkaEventsExtensions(
                sp.GetRequiredService<ILogger<KafkaEventsExtensions>>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IOptions<ServiceBusOptions>>(),
                sp.GetRequiredService<IHostEnvironment>(),
                connectionName,
                configureKafka));

        return builder;
    }

    /// <summary>
    /// Adds Kafka messaging extensions with advanced Aspire configuration integration.
    /// This method provides the full Aspire-Wolverine bridge functionality.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder</param>
    /// <param name="connectionName">The connection name for Kafka configuration</param>
    /// <param name="configureKafka">Wolverine Kafka configuration</param>
    /// <returns>The WebApplicationBuilder for chaining</returns>
    public static WebApplicationBuilder AddKafkaMessagingWithAspire(
        this WebApplicationBuilder builder,
        string connectionName = DefaultConnectionName,
        Action<KafkaTransportExpression>? configureKafka = null)
    {
        // Use Wolverine with the Aspire bridge integration
        builder.UseWolverine(opts =>
        {
            opts.UseKafkaWithAspire(builder, connectionName, configureKafka);
            
            // Register CloudEvents extension
            builder.Services.AddSingleton<IWolverineExtension>(sp =>
                new KafkaEventsExtensions(
                    sp.GetRequiredService<ILogger<KafkaEventsExtensions>>(),
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<IOptions<ServiceBusOptions>>(),
                    sp.GetRequiredService<IHostEnvironment>(),
                    connectionName));
        });

        return builder;
    }
}
