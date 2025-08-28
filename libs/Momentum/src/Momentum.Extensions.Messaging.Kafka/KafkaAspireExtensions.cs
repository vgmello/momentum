// Copyright (c) Momentum .NET. All rights reserved.

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace Momentum.Extensions.Messaging.Kafka;

/// <summary>
///     Extensions to integrate Wolverine Kafka transport with Aspire Kafka configuration.
/// </summary>
public static class KafkaAspireExtensions
{
    public const string SectionName = "Aspire:Confluent:Kafka";

    /// <summary>
    ///     Applies Aspire producer configuration to Wolverine's producer config.
    /// </summary>
    public static void ApplyAspireProducerConfig(IConfiguration configuration, string serviceName, ProducerConfig producerConfig)
    {
        ApplyConfig(configuration, "Producer:Config", producerConfig);
        ApplyConfig(configuration, $"Producer:{serviceName}:Config", producerConfig);
    }

    /// <summary>
    ///     Applies Aspire consumer configuration to Wolverine's consumer config.
    /// </summary>
    public static void ApplyAspireConsumerConfig(IConfiguration configuration, string serviceName, ConsumerConfig consumerConfig)
    {
        ApplyConfig(configuration, "Consumer:Config", consumerConfig);
        ApplyConfig(configuration, $"Consumer:{serviceName}:Config", consumerConfig);
    }

    /// <summary>
    ///     Applies Aspire client configuration to Wolverine's client config.
    /// </summary>
    public static void ApplyAspireClientConfig(IConfiguration configuration, string serviceName, ClientConfig clientConfig)
    {
        ApplyConfig(configuration, "Security", clientConfig);
        ApplyConfig(configuration, $"Security:{serviceName}:Config", clientConfig);
    }

    public static void SetConfigConsumerGroupId(IConfiguration configuration, string serviceName, string groupPrefix, string environment)
    {
        var consumerGroupIdConfig = $"{SectionName}:Consumer:{serviceName}:Config:GroupId";
        var consumerGroupId = configuration.GetValue<string>(consumerGroupIdConfig);

        if (consumerGroupId is null)
        {
            configuration[consumerGroupIdConfig] = $"{groupPrefix}-{environment}";
        }
    }

    public static void SetConfigClientId(IConfiguration configuration, string serviceName, string clientId)
    {
        const string clientIdConfig = $"{SectionName}:ClientId";
        var configClientId = configuration.GetValue<string>(clientIdConfig);

        if (configClientId is null)
        {
            configClientId = clientId;
            configuration[clientIdConfig] = configClientId;
        }

        var consumerClientIdConfig = $"{SectionName}:Consumer:{serviceName}:Config:ClientId";
        var consumerClientId = configuration.GetValue<string>(consumerClientIdConfig);

        if (consumerClientId is null)
        {
            configuration[consumerClientIdConfig] = configClientId;
        }

        var producerClientIdConfig = $"{SectionName}:Producer:{serviceName}:Config:ClientId";
        var producerClientId = configuration.GetValue<string>(producerClientIdConfig);

        if (producerClientId is null)
        {
            configuration[producerClientIdConfig] = configClientId;
        }
    }

    private static void ApplyConfig(IConfiguration configuration, string configName, ClientConfig clientConfig)
    {
        var configSection = configuration.GetSection($"{SectionName}:{configName}");

        if (configSection.Exists())
            configSection.Bind(clientConfig);
    }
}
