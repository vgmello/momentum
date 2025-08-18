// Copyright (c) Momentum .NET. All rights reserved.

using JasperFx.Core.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.ServiceDefaults.Messaging;
using System.Reflection;
using Wolverine;
using Wolverine.Kafka;

#pragma warning disable S3011

namespace Momentum.Extensions.Messaging.Kafka;

/// <summary>
///     Wolverine extension for configuring distributed events with Kafka.
/// </summary>
public class KafkaEventsExtensions(
    ILogger<KafkaEventsExtensions> logger,
    IConfiguration configuration,
    IOptions<ServiceBusOptions> serviceBusOptions,
    IHostEnvironment environment) : IWolverineExtension
{
    internal const string ConnectionStringName = "Messaging";

    private static readonly MethodInfo SetupKafkaPublisherRouteMethodInfo =
        typeof(KafkaEventsExtensions).GetMethod(nameof(SetupKafkaPublisherRoute), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    ///     Sets up distributed event routing for Kafka based on event attributes.
    /// </summary>
    /// <remarks>
    ///     <!--@include: @code/messaging/kafka-events-detailed.md -->
    /// </remarks>
    public void Configure(WolverineOptions options)
    {
        var kafkaConnectionString = configuration.GetConnectionString(ConnectionStringName)!;
        var cloudEventMapper = new CloudEventMapper(serviceBusOptions);
        var consumerGroupId = $"{options.ServiceName}-{GetEnvNameShort(environment.EnvironmentName)}";

        var autoProvisionEnabled = configuration.GetValue("Kafka:AutoProvision", false);

        logger.LogInformation("Configuring Kafka messaging for service {ServiceName} in environment {Environment}",
            options.ServiceName, environment.EnvironmentName);
        logger.LogInformation("Kafka bootstrap servers: {BootstrapServers}", kafkaConnectionString);
        logger.LogInformation("Consumer group ID: {GroupId}", consumerGroupId);
        logger.LogInformation("Auto-provision enabled: {AutoProvisionEnabled}", autoProvisionEnabled);

        var kafkaConfig = options
            .UseKafka(kafkaConnectionString)
            .ConfigureSenders(cfg => cfg.UseInterop(cloudEventMapper))
            .ConfigureListeners(cfg => cfg.UseInterop(cloudEventMapper))
            .ConfigureConsumers(consumer =>
            {
                consumer.GroupId = consumerGroupId;
                consumer.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Latest;
                consumer.EnableAutoCommit = true;
                consumer.EnableAutoOffsetStore = false;
            });

        if (autoProvisionEnabled)
        {
            kafkaConfig.AutoProvision();
            logger.LogDebug("Kafka auto-provisioning enabled - topics will be created automatically");
        }
        else
        {
            logger.LogDebug("Kafka auto-provisioning disabled - topics must be created manually");
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

            var topicName = GetTopicName(messageType, topicAttribute, environment.EnvironmentName);
            publisherTopics.Add(topicName);

            var setupKafkaRouteMethodInfo = SetupKafkaPublisherRouteMethodInfo.MakeGenericMethod(messageType);
            setupKafkaRouteMethodInfo.Invoke(null, [options, topicName]);

            logger.LogDebug("Configured publisher for {EventType} to topic {TopicName}", messageType.Name, topicName);
        }

        if (publisherTopics.Count > 0)
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

            var topicName = GetTopicName(messageType, topicAttribute, environment.EnvironmentName);
            topicsToSubscribe.Add(topicName);

            logger.LogDebug("Discovered handler for {EventType} on topic {TopicName}", messageType.Name, topicName);
        }

        foreach (var topicName in topicsToSubscribe)
        {
            options.ListenToKafkaTopic(topicName);
        }

        if (topicsToSubscribe.Count > 0)
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

    /// <summary>
    ///     Generates a fully qualified topic name based on environment and domain.
    /// </summary>
    /// <param name="messageType">The integration event type.</param>
    /// <param name="topicAttribute">The event topic attribute.</param>
    /// <param name="env">Environment name.</param>
    /// <returns>A topic name in the format: {env}.{domain}.{scope}.{topic}.{version}</returns>
    private static string GetTopicName(Type messageType, EventTopicAttribute topicAttribute, string env)
    {
        var envName = GetEnvNameShort(env);
        var domainName = !string.IsNullOrWhiteSpace(topicAttribute.Domain)
            ? topicAttribute.Domain
            : messageType.Assembly.GetAttribute<DefaultDomainAttribute>()!.Domain;

        var scope = topicAttribute.Internal ? "internal" : "public";

        var topicName = topicAttribute.ShouldPluralizeTopicName ? topicAttribute.Topic.Pluralize() : topicAttribute.Topic;

        var versionSuffix = string.IsNullOrWhiteSpace(topicAttribute.Version) ? null : $".{topicAttribute.Version}";

        return $"{envName}.{domainName}.{scope}.{topicName}{versionSuffix}".ToLowerInvariant();
    }

    private static string GetEnvNameShort(string env)
    {
        var envLower = env.ToLowerInvariant();

        return envLower switch
        {
            "development" => "dev",
            _ => envLower[..4]
        };
    }
}
