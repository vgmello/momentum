// Copyright (c) Momentum .NET. All rights reserved.

using JasperFx.Core.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using System.Reflection;
using Wolverine;
using Wolverine.Kafka;

#pragma warning disable S3011

namespace Momentum.ServiceDefaults.Messaging.Kafka;

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
    ///     Features:
    ///     <list type="bullet">
    ///         <item>Discovers all distributed event types from domain assemblies</item>
    ///         <item>Configures Kafka topic routing based on EventTopicAttribute</item>
    ///         <item>Sets up partition key routing using PartitionKeyAttribute or IDistributedEvent.GetPartitionKey()</item>
    ///         <item>Generates environment-specific topic names (dev, test, prod)</item>
    ///         <item>Automatically configures subscriptions based on integration event handlers</item>
    ///     </list>
    ///     Integration events are identified by:
    ///     <list type="bullet">
    ///         <item>Having their namespace end with "IntegrationEvents"</item>
    ///         <item>Being decorated with EventTopicAttribute</item>
    ///     </list>
    ///     Integration events are identified by:
    ///     <list type="bullet">
    ///         <item>Having their namespace end with "DomainEvents"</item>
    ///         <item>Being decorated with EventTopicAttribute</item>
    ///     </list>
    /// </remarks>
    public void Configure(WolverineOptions options)
    {
        var kafkaConnectionString = configuration.GetConnectionString(ConnectionStringName)!;
        var cloudEventMapper = new CloudEventMapper(serviceBusOptions);

        options
            .UseKafka(kafkaConnectionString)
            .ConfigureSenders(cfg => cfg.UseInterop(cloudEventMapper))
            .ConfigureListeners(cfg => cfg.UseInterop(cloudEventMapper))
            .ConfigureConsumers(consumer =>
            {
                consumer.GroupId = options.ServiceName;
                consumer.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Latest;
                consumer.EnableAutoCommit = true;
                consumer.EnableAutoOffsetStore = false;
            })
            .AutoProvision();

        SetupPublisher(options);
        SetupSubscribers(options);
    }

    private void SetupPublisher(WolverineOptions options)
    {
        var integrationEventTypes = DistributedEventsDiscovery.GetIntegrationEventTypes();

        foreach (var messageType in integrationEventTypes)
        {
            var topicAttribute = messageType.GetAttribute<EventTopicAttribute>();

            if (topicAttribute is null)
            {
                logger.LogWarning("IntegrationEvent {IntegrationEventType} does not have an EventTopicAttribute", messageType.Name);

                continue;
            }

            var topicName = GetTopicName(messageType, topicAttribute, environment.EnvironmentName);

            var setupKafkaRouteMethodInfo = SetupKafkaPublisherRouteMethodInfo.MakeGenericMethod(messageType);

            setupKafkaRouteMethodInfo.Invoke(null, [options, topicName]);
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
            logger.LogInformation("Configured Kafka subscriptions for {TopicCount} topics with consumer group {ConsumerGroup}",
                topicsToSubscribe.Count, options.ServiceName);
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
        var envName = env switch
        {
            "Development" => "dev",
            "Production" => "prod",
            "Test" => "test",
            _ => env.ToLowerInvariant()
        };

        var domainName = !string.IsNullOrWhiteSpace(topicAttribute.Domain)
            ? topicAttribute.Domain
            : messageType.Assembly.GetAttribute<DefaultDomainAttribute>()!.Domain;

        var scope = topicAttribute.Internal ? "internal" : "public";

        var topicName = topicAttribute.ShouldPluralizeTopicName ? topicAttribute.Topic.Pluralize() : topicAttribute.Topic;

        var versionSuffix = string.IsNullOrWhiteSpace(topicAttribute.Version) ? null : $".{topicAttribute.Version}";

        return $"{envName}.{domainName}.{scope}.{topicName}{versionSuffix}".ToLowerInvariant();
    }
}
