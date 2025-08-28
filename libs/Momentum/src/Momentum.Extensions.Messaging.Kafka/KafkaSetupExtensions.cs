// Copyright (c) Momentum .NET. All rights reserved.

using Aspire.Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.ServiceDefaults.Messaging;
using Wolverine;
using static Momentum.Extensions.Messaging.Kafka.KafkaAspireExtensions;

namespace Momentum.Extensions.Messaging.Kafka;

public static class KafkaSetupExtensions
{
    public const string SectionName = "Messaging";

    /// <summary>
    ///     Adds Kafka messaging extensions with full Aspire integration to the WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder</param>
    /// <param name="serviceName">The connection name for Kafka configuration</param>
    /// <param name="configureProducerSettings">Optional producer settings configuration</param>
    /// <param name="configureConsumerSettings">Optional consumer settings configuration</param>
    /// <returns>The WebApplicationBuilder for chaining</returns>
    public static WebApplicationBuilder AddKafkaMessagingExtensions(
        this WebApplicationBuilder builder,
        string serviceName = SectionName,
        Action<KafkaProducerSettings>? configureProducerSettings = null,
        Action<KafkaConsumerSettings>? configureConsumerSettings = null)
    {
        var clientId = builder.Environment.ApplicationName.ToKebabCase();

        SetConfigConsumerGroupId(
            builder.Configuration,
            serviceName,
            clientId,
            builder.Environment.GetEnvNameShort());

        SetConfigClientId(builder.Configuration, serviceName, clientId);

        builder.AddKafkaProducer<string, byte[]>(serviceName, configureProducerSettings);
        builder.AddKafkaConsumer<string, byte[]>(serviceName, configureConsumerSettings);

        builder.Services.AddSingleton<ITopicNameGenerator, TopicNameGenerator>();

        builder.Services.AddSingleton<IWolverineExtension>(provider =>
            new KafkaWolverineExtensions(
                provider.GetRequiredService<ILogger<KafkaWolverineExtensions>>(),
                provider.GetRequiredService<IConfiguration>(),
                provider.GetRequiredService<IOptions<ServiceBusOptions>>(),
                provider.GetRequiredService<ITopicNameGenerator>(),
                serviceName)
        );

        return builder;
    }
}
