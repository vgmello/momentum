// Copyright (c) Momentum .NET. All rights reserved.

using JasperFx.Core.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.ServiceDefaults.Messaging;
using System.Reflection;
using Wolverine;
using Wolverine.Kafka;
using static Momentum.Extensions.Messaging.Kafka.KafkaAspireExtensions;

#pragma warning disable S3011

namespace Momentum.Extensions.Messaging.Kafka;

/// <summary>
///     Wolverine extension for configuring distributed events with Kafka.
/// </summary>
public class KafkaWolverineExtensions(
    ILogger<KafkaWolverineExtensions> logger,
    IConfiguration configuration,
    IOptions<ServiceBusOptions> serviceBusOptions,
    ITopicNameGenerator topicNameGenerator,
    string serviceName
) : IWolverineExtension
{
    private static readonly MethodInfo SetupKafkaPublisherRouteMethodInfo =
        typeof(KafkaWolverineExtensions).GetMethod(nameof(SetupKafkaPublisherRoute), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    ///     Sets up distributed event routing for Kafka based on event attributes.
    /// </summary>
    /// <remarks>
    ///     <!--@include: @code/messaging/kafka-events-detailed.md -->
    /// </remarks>
    public void Configure(WolverineOptions options)
    {
        var kafkaConnectionString = configuration.GetConnectionString(serviceName)
                                    ?? throw new InvalidOperationException($"Kafka connection string '{serviceName}' not set.");

        var cloudEventMapper = new CloudEventMapper(serviceBusOptions);
        var autoProvisionEnabled = configuration.GetValue($"{ServiceBusOptions.SectionName}:Wolverine:AutoProvision", false);

        var kafkaConfig = options
            .UseKafka(kafkaConnectionString)
            .ConfigureSenders(cfg => cfg.UseInterop(cloudEventMapper))
            .ConfigureListeners(cfg => cfg.UseInterop(cloudEventMapper));

        kafkaConfig.ConfigureClient(clientConfig => ApplyAspireClientConfig(configuration, serviceName, clientConfig));
        kafkaConfig.ConfigureConsumers(consumerConfig => ApplyAspireConsumerConfig(configuration, serviceName, consumerConfig));
        kafkaConfig.ConfigureProducers(producerConfig => ApplyAspireProducerConfig(configuration, serviceName, producerConfig));

        if (autoProvisionEnabled)
        {
            kafkaConfig.AutoProvision();
            logger.LogDebug("Kafka auto-provisioning enabled - topics will be created automatically");
        }

        SetupPublisher(options);
        SetupSubscribers(options);
    }

    private void SetupPublisher(WolverineOptions options)
    {
        var integrationEventTypes = DistributedEventsDiscovery.GetIntegrationEventTypes();
        var publisherTopics = new List<string>();

        logger.LogDebug("Setting up Kafka publishers for integration events");

        foreach (var messageType in integrationEventTypes)
        {
            var topicAttribute = messageType.GetAttribute<EventTopicAttribute>();

            if (topicAttribute is null)
            {
                logger.LogWarning("IntegrationEvent {IntegrationEventType} does not have an EventTopicAttribute", messageType.Name);

                continue;
            }

            var topicName = topicNameGenerator.GetTopicName(messageType, topicAttribute);
            publisherTopics.Add(topicName);

            var setupKafkaRouteMethodInfo = SetupKafkaPublisherRouteMethodInfo.MakeGenericMethod(messageType);
            setupKafkaRouteMethodInfo.Invoke(null, [options, topicName]);

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Configured publisher for {EventType} to topic {TopicName}", messageType.Name, topicName);
        }

        if (publisherTopics.Count > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Configured Kafka publishers for {TopicCount} topics: {Topics}",
                publisherTopics.Count, string.Join(", ", publisherTopics));
        }
        else
        {
            logger.LogInformation("No Kafka publishers configured - no integration events found");
        }
    }

    private void SetupSubscribers(WolverineOptions options)
    {
        var integrationEventTypesWithHandlers = DistributedEventsDiscovery.GetIntegrationEventTypesWithHandlers();
        var topicsToSubscribe = new HashSet<string>();

        foreach (var messageType in integrationEventTypesWithHandlers)
        {
            var topicAttribute = messageType.GetAttribute<EventTopicAttribute>();

            if (topicAttribute is null)
            {
                logger.LogWarning("IntegrationEvent {IntegrationEventType} does not have an EventTopicAttribute", messageType.Name);

                continue;
            }

            var topicName = topicNameGenerator.GetTopicName(messageType, topicAttribute);
            topicsToSubscribe.Add(topicName);

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Discovered handler for {EventType} on topic {TopicName}", messageType.Name, topicName);
        }

        foreach (var topicName in topicsToSubscribe)
        {
            options.ListenToKafkaTopic(topicName);
        }

        if (topicsToSubscribe.Count > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Configured Kafka subscriptions for {TopicCount} topics with consumer group {ConsumerGroup}: {Topics}",
                topicsToSubscribe.Count, options.ServiceName, string.Join(", ", topicsToSubscribe));
        }
        else
        {
            logger.LogInformation("No Kafka subscriptions configured - no event handlers found");
        }
    }

    private static void SetupKafkaPublisherRoute<TEventType>(WolverineOptions options, string topicName)
    {
        var partitionKeyGetter = PartitionKeyProviderFactory.GetPartitionKeyFunction<TEventType>();

        options
            .PublishMessage<TEventType>()
            .ToKafkaTopic(topicName)
            .CustomizeOutgoing(e =>
            {
                if (e.Message is IDistributedEvent integrationEvent)
                {
                    e.PartitionKey = integrationEvent.GetPartitionKey();
                }
                else if (e.Message is TEventType typedMessage && partitionKeyGetter is not null)
                {
                    e.PartitionKey = partitionKeyGetter(typedMessage);
                }
            });
    }
}
