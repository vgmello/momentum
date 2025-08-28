// Copyright (c) Momentum .NET. All rights reserved.

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace Momentum.Extensions.Messaging.Kafka;

/// <summary>
///     Extensions to integrate Wolverine Kafka transport with Aspire Kafka configuration.
/// </summary>
public static class KafkaAspireExtensions
{
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

    public static string GetConsumerGroupId(IConfiguration configuration, string serviceName, string logicalServiceName, string environment)
    {
        var consumerGroupConfig = $"Aspire:Confluent:Kafka:Consumer:{serviceName}:Config:GroupId";
        var consumerGroupId = configuration.GetValue<string>(consumerGroupConfig);

        if (consumerGroupId is null)
        {
            consumerGroupId = $"{logicalServiceName}-{environment}";
            configuration[consumerGroupConfig] = consumerGroupId;
        }

        return consumerGroupId;
    }

    private static void ApplyConfig(IConfiguration configuration, string configName, ClientConfig clientConfig)
    {
        var generalConfigSection = configuration.GetSection($"Aspire:Confluent:Kafka:{configName}");

        if (generalConfigSection.Exists())
        {
            generalConfigSection.Bind(clientConfig);
        }
    }
}
