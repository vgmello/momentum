// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Configuration;

namespace Momentum.Extensions.Messaging.EventHub.Configuration;

/// <summary>
///     Extensions to integrate Wolverine Event Hub transport with Aspire configuration.
/// </summary>
public static class EventHubAspireExtensions
{
    public const string SectionName = "Aspire:Azure:EventHubs";
    public const string BlobStorageSectionName = "Aspire:Azure:BlobStorage";

    /// <summary>
    ///     Applies Aspire configuration to Event Hub transport.
    /// </summary>
    public static void ApplyAspireConfiguration(
        IConfiguration configuration,
        string serviceName,
        string clientId,
        string environment)
    {
        // Set default consumer group if not specified
        SetConfigConsumerGroup(configuration, serviceName, clientId, environment);
    }

    /// <summary>
    ///     Applies Aspire producer configuration.
    /// </summary>
    public static void ApplyAspireProducerConfig(
        IConfiguration configuration,
        string serviceName,
        EventHubProducerClientOptions options)
    {
        ApplyConfig(configuration, $"Producer:{serviceName}:Options", options);
    }

    /// <summary>
    ///     Applies Aspire processor configuration.
    /// </summary>
    public static void ApplyAspireProcessorConfig(
        IConfiguration configuration,
        string serviceName,
        EventProcessorClientOptions options)
    {
        ApplyConfig(configuration, $"Processor:{serviceName}:Options", options);
    }

    /// <summary>
    ///     Sets default consumer group configuration.
    /// </summary>
    public static void SetConfigConsumerGroup(
        IConfiguration configuration,
        string serviceName,
        string groupPrefix,
        string environment)
    {
        var consumerGroupConfig = $"{SectionName}:Processor:{serviceName}:ConsumerGroup";
        var consumerGroup = configuration.GetValue<string>(consumerGroupConfig);

        if (consumerGroup is null)
        {
            configuration[consumerGroupConfig] = $"{groupPrefix}-{environment}";
        }
    }

    private static void ApplyConfig(IConfiguration configuration, string configName, object options)
    {
        var configSection = configuration.GetSection($"{SectionName}:{configName}");

        if (configSection.Exists())
        {
            configSection.Bind(options);
        }
    }
}
