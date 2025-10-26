// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Core;
using Azure.Identity;
using JasperFx.Core.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.Messaging.EventHub.Transport;
using Momentum.ServiceDefaults.Messaging;
using System.Reflection;
using Wolverine;

namespace Momentum.Extensions.Messaging.EventHub.Configuration;

/// <summary>
///     Wolverine extension for configuring distributed events with Event Hubs.
/// </summary>
public class EventHubWolverineExtensions(
    ILogger<EventHubWolverineExtensions> logger,
    IConfiguration configuration,
    IOptions<ServiceBusOptions> serviceBusOptions,
    ITopicNameGenerator topicNameGenerator,
    string serviceName
) : IWolverineExtension
{
    private static readonly MethodInfo SetupEventHubPublisherRouteMethodInfo =
        typeof(EventHubWolverineExtensions).GetMethod(nameof(SetupEventHubPublisherRoute), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    ///     Sets up distributed event routing for Event Hubs based on event attributes.
    /// </summary>
    public void Configure(WolverineOptions options)
    {
        // Get Event Hub connection configuration from Aspire
        var fullyQualifiedNamespace = configuration.GetValue<string>($"{EventHubAspireExtensions.SectionName}:{serviceName}:FullyQualifiedNamespace");
        var connectionString = configuration.GetConnectionString(serviceName);
        var useDefaultCredential = configuration.GetValue($"{EventHubAspireExtensions.SectionName}:{serviceName}:UseDefaultCredential", true);

        // Get Blob Storage configuration for checkpoints
        var checkpointContainerUri = configuration.GetValue<string>($"{EventHubAspireExtensions.BlobStorageSectionName}:{serviceName}-checkpoints:ServiceUri");
        var checkpointConnectionString = configuration.GetConnectionString($"{serviceName}-checkpoints");

        // Configure Event Hub transport
        EventHubTransport eventHubTransport;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            eventHubTransport = options.UseEventHub(connectionString);
            logger.LogDebug("Configured Event Hub transport with connection string");
        }
        else if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
        {
            var credential = useDefaultCredential
                ? new DefaultAzureCredential()
                : null as TokenCredential;

            if (credential == null)
            {
                throw new InvalidOperationException(
                    "Event Hub transport requires either a connection string or DefaultAzureCredential. " +
                    "Set UseDefaultCredential=true or provide a connection string.");
            }

            eventHubTransport = options.UseEventHub(fullyQualifiedNamespace, credential);
            logger.LogDebug("Configured Event Hub transport with fully qualified namespace and DefaultAzureCredential");
        }
        else
        {
            throw new InvalidOperationException(
                $"Event Hub connection not configured. Set either ConnectionStrings:{serviceName} or " +
                $"{EventHubAspireExtensions.SectionName}:{serviceName}:FullyQualifiedNamespace in configuration.");
        }

        // Configure checkpoint storage
        if (!string.IsNullOrWhiteSpace(checkpointConnectionString))
        {
            eventHubTransport.CheckpointBlobConnectionString = checkpointConnectionString;
            logger.LogDebug("Configured checkpoint storage with connection string");
        }
        else if (!string.IsNullOrWhiteSpace(checkpointContainerUri))
        {
            eventHubTransport.CheckpointBlobContainerUri = checkpointContainerUri;
            eventHubTransport.CheckpointCredential = useDefaultCredential
                ? new DefaultAzureCredential()
                : null;

            logger.LogDebug("Configured checkpoint storage with container URI and DefaultAzureCredential");
        }
        else
        {
            logger.LogWarning(
                "Checkpoint storage not configured. Event Hub consumers require Azure Blob Storage for checkpointing. " +
                "Set either ConnectionStrings:{ServiceName}-checkpoints or {BlobSection}:{ServiceName}-checkpoints:ServiceUri",
                serviceName,
                EventHubAspireExtensions.BlobStorageSectionName);
        }

        // Check for auto-provision configuration
        var autoProvisionEnabled = configuration.GetValue($"{ServiceBusOptions.SectionName}:Wolverine:AutoProvision", false);
        if (autoProvisionEnabled)
        {
            eventHubTransport.AutoProvision();
            logger.LogDebug("Event Hub auto-provisioning enabled - Event Hubs will be created automatically in non-production environments");
        }

        // Setup publishers and subscribers
        SetupPublishers(options);
        SetupSubscribers(options);
    }

    private void SetupPublishers(WolverineOptions options)
    {
        var integrationEventTypes = DistributedEventsDiscovery.GetIntegrationEventTypes();
        var publisherEventHubs = new List<string>();

        logger.LogDebug("Setting up Event Hub publishers for integration events");

        foreach (var messageType in integrationEventTypes)
        {
            var topicAttribute = messageType.GetAttribute<EventTopicAttribute>();

            if (topicAttribute is null)
            {
                logger.LogWarning("IntegrationEvent {IntegrationEventType} does not have an EventTopicAttribute", messageType.Name);
                continue;
            }

            var eventHubName = topicNameGenerator.GetTopicName(messageType, topicAttribute);
            publisherEventHubs.Add(eventHubName);

            var setupEventHubRouteMethodInfo = SetupEventHubPublisherRouteMethodInfo.MakeGenericMethod(messageType);
            setupEventHubRouteMethodInfo.Invoke(null, [options, eventHubName]);

            logger.LogDebug("Configured publisher for {EventType} to Event Hub {EventHubName}", messageType.Name, eventHubName);
        }

        if (publisherEventHubs.Count > 0)
        {
            logger.LogInformation("Configured Event Hub publishers for {EventHubCount} Event Hubs: {EventHubs}",
                publisherEventHubs.Count, string.Join(", ", publisherEventHubs));
        }
        else
        {
            logger.LogInformation("No Event Hub publishers configured - no integration events found");
        }
    }

    private void SetupSubscribers(WolverineOptions options)
    {
        var integrationEventTypesWithHandlers = DistributedEventsDiscovery.GetIntegrationEventTypesWithHandlers();
        var eventHubsToSubscribe = new HashSet<string>();

        foreach (var messageType in integrationEventTypesWithHandlers)
        {
            var topicAttribute = messageType.GetAttribute<EventTopicAttribute>();

            if (topicAttribute is null)
            {
                logger.LogWarning("IntegrationEvent {IntegrationEventType} does not have an EventTopicAttribute", messageType.Name);
                continue;
            }

            var eventHubName = topicNameGenerator.GetTopicName(messageType, topicAttribute);
            eventHubsToSubscribe.Add(eventHubName);

            logger.LogDebug("Discovered handler for {EventType} on Event Hub {EventHubName}", messageType.Name, eventHubName);
        }

        foreach (var eventHubName in eventHubsToSubscribe)
        {
            options.ListenToEventHub(eventHubName);
        }

        if (eventHubsToSubscribe.Count > 0)
        {
            logger.LogInformation("Configured Event Hub subscriptions for {EventHubCount} Event Hubs with consumer group {ConsumerGroup}: {EventHubs}",
                eventHubsToSubscribe.Count, options.ServiceName, string.Join(", ", eventHubsToSubscribe));
        }
        else
        {
            logger.LogInformation("No Event Hub subscriptions configured - no event handlers found");
        }
    }

    private static void SetupEventHubPublisherRoute<TEventType>(WolverineOptions options, string eventHubName)
    {
        var partitionKeyGetter = PartitionKeyProviderFactory.GetPartitionKeyFunction<TEventType>();

        options
            .PublishMessage<TEventType>()
            .ToEventHub(eventHubName)
            .CustomizeOutgoing(envelope =>
            {
                if (envelope.Message is IDistributedEvent distributedEvent)
                {
                    envelope.PartitionKey = distributedEvent.GetPartitionKey();
                }
                else if (envelope.Message is TEventType typedMessage && partitionKeyGetter is not null)
                {
                    envelope.PartitionKey = partitionKeyGetter(typedMessage);
                }
            });
    }
}

/// <summary>
///     Factory for creating partition key extraction functions.
/// </summary>
public static class PartitionKeyProviderFactory
{
    public static Func<TMessage, string>? GetPartitionKeyFunction<TMessage>()
    {
        var messageType = typeof(TMessage);
        var partitionKeyProperties = messageType.GetPropertiesWithAttribute<PartitionKeyAttribute>();

        if (partitionKeyProperties.Count == 0)
            return null;

        var primaryConstructor = messageType.GetPrimaryConstructor();

        var orderedPartitionKeyProperties = partitionKeyProperties
            .OrderBy(p => p.GetCustomAttribute<PartitionKeyAttribute>(primaryConstructor)?.Order ?? 0)
            .ThenBy(p => p.Name).ToArray();

        return CreatePartitionKeyGetter<TMessage>(orderedPartitionKeyProperties);
    }

    private static Func<TMessage, string> CreatePartitionKeyGetter<TMessage>(PropertyInfo[] partitionKeyProperties)
    {
        // Simple implementation - concatenate property values
        return message =>
        {
            var values = partitionKeyProperties.Select(prop => prop.GetValue(message)?.ToString() ?? string.Empty);
            return string.Join("|", values);
        };
    }
}
