// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using JasperFx.Core;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.Runtime;
using Wolverine.Transports;

namespace Momentum.Extensions.Messaging.EventHub.Transport;

/// <summary>
///     Wolverine transport implementation for Azure Event Hubs.
/// </summary>
public class EventHubTransport : BrokerTransport<EventHubEndpoint>
{
    public const string ProtocolName = "eventhub";

    /// <summary>
    ///     Cache of Event Hub endpoints keyed by Event Hub name.
    /// </summary>
    public Cache<string, EventHubEndpoint> EventHubs { get; }

    /// <summary>
    ///     Fully qualified namespace for Event Hubs (e.g., namespace.servicebus.windows.net).
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    ///     Azure credential for authentication (DefaultAzureCredential, etc.).
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    ///     Connection string for Event Hubs (alternative to namespace + credential).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    ///     Blob container URI for checkpoint storage.
    /// </summary>
    public string? CheckpointBlobContainerUri { get; set; }

    /// <summary>
    ///     Credential for blob storage checkpoint access.
    /// </summary>
    public TokenCredential? CheckpointCredential { get; set; }

    /// <summary>
    ///     Connection string for blob storage (alternative to URI + credential).
    /// </summary>
    public string? CheckpointBlobConnectionString { get; set; }

    /// <summary>
    ///     Blob container name for checkpoints.
    /// </summary>
    public string CheckpointContainerName { get; set; } = "eventhub-checkpoints";

    /// <summary>
    ///     Configuration hook for producer client options.
    /// </summary>
    public Action<EventHubProducerClientOptions> ConfigureProducerOptions { get; set; } = _ => { };

    /// <summary>
    ///     Configuration hook for event processor client options.
    /// </summary>
    public Action<EventProcessorClientOptions> ConfigureProcessorOptions { get; set; } = _ => { };

    /// <summary>
    ///     Enable automatic provisioning of Event Hubs (development only).
    /// </summary>
    public bool AutoProvisionEnabled { get; set; }

    public EventHubTransport() : this(ProtocolName)
    {
    }

    public EventHubTransport(string protocol) : base(protocol, "Event Hubs")
    {
        EventHubs = new Cache<string, EventHubEndpoint>(
            eventHubName => new EventHubEndpoint(this, eventHubName, EndpointRole.Application)
        );
    }

    public override Uri ResourceUri
    {
        get
        {
            var uri = new Uri($"{Protocol}://");
            if (FullyQualifiedNamespace.IsNotEmpty())
            {
                uri = new Uri(uri, FullyQualifiedNamespace);
            }
            return uri;
        }
    }

    protected override IEnumerable<EventHubEndpoint> endpoints()
    {
        return EventHubs;
    }

    protected override EventHubEndpoint findEndpointByUri(Uri uri)
    {
        var eventHubName = EventHubEndpoint.EventHubNameForUri(uri);
        return EventHubs[eventHubName];
    }

    protected override void tryBuildSystemEndpoints(IWolverineRuntime runtime)
    {
        // Create a system endpoint for topic-based routing similar to Kafka's "wolverine.topics"
        var systemEndpoint = EventHubs["wolverine.eventhubs"];
        systemEndpoint.RoutingType = RoutingMode.ByTopic;
        systemEndpoint.OutgoingRules.Add(new EventHubRoutingRule());
    }

    public override async ValueTask ConnectAsync(IWolverineRuntime runtime)
    {
        // Compile all endpoints
        foreach (var endpoint in EventHubs)
        {
            endpoint.Compile(runtime);
        }

        await ValueTask.CompletedTask;
    }

    public override IEnumerable<PropertyColumn> DiagnosticColumns()
    {
        yield break; // No custom diagnostic columns for Event Hubs
    }

    /// <summary>
    ///     Creates an Event Hub producer client for sending messages.
    /// </summary>
    internal EventHubProducerClient CreateProducer(string eventHubName)
    {
        var options = new EventHubProducerClientOptions();
        ConfigureProducerOptions(options);

        if (ConnectionString.IsNotEmpty())
        {
            return new EventHubProducerClient(ConnectionString, eventHubName, options);
        }

        if (FullyQualifiedNamespace.IsNotEmpty() && Credential != null)
        {
            return new EventHubProducerClient(FullyQualifiedNamespace, eventHubName, Credential, options);
        }

        throw new InvalidOperationException(
            $"Event Hub transport not properly configured. Either set ConnectionString or both FullyQualifiedNamespace and Credential.");
    }

    /// <summary>
    ///     Creates an Event Processor client for receiving messages with checkpointing.
    /// </summary>
    internal EventProcessorClient CreateProcessor(string eventHubName, string consumerGroup)
    {
        var checkpointStore = CreateCheckpointStore();

        var options = new EventProcessorClientOptions();
        ConfigureProcessorOptions(options);

        if (ConnectionString.IsNotEmpty())
        {
            return new EventProcessorClient(
                checkpointStore,
                consumerGroup,
                ConnectionString,
                eventHubName,
                options);
        }

        if (FullyQualifiedNamespace.IsNotEmpty() && Credential != null)
        {
            return new EventProcessorClient(
                checkpointStore,
                consumerGroup,
                FullyQualifiedNamespace,
                eventHubName,
                Credential,
                options);
        }

        throw new InvalidOperationException(
            $"Event Hub transport not properly configured. Either set ConnectionString or both FullyQualifiedNamespace and Credential.");
    }

    /// <summary>
    ///     Creates a blob container client for checkpoint storage.
    /// </summary>
    internal BlobContainerClient CreateCheckpointStore()
    {
        if (CheckpointBlobConnectionString.IsNotEmpty())
        {
            return new BlobContainerClient(CheckpointBlobConnectionString, CheckpointContainerName);
        }

        if (CheckpointBlobContainerUri.IsNotEmpty() && CheckpointCredential != null)
        {
            var containerUri = new Uri(CheckpointBlobContainerUri);
            return new BlobContainerClient(containerUri, CheckpointCredential);
        }

        throw new InvalidOperationException(
            $"Checkpoint storage not properly configured. Either set CheckpointBlobConnectionString or both CheckpointBlobContainerUri and CheckpointCredential.");
    }
}

/// <summary>
///     Routing rule for Event Hub topic-based routing.
/// </summary>
internal class EventHubRoutingRule : IMessageRoutingConvention
{
    public void DiscoverListeners(IWolverineRuntime runtime, IReadOnlyList<Type> handledMessageTypes)
    {
        // No listener discovery needed for outgoing-only system endpoint
    }

    public IEnumerable<Endpoint> DiscoverSenders(Type messageType, IWolverineRuntime runtime)
    {
        // Route based on message type to appropriate Event Hub
        // This will be enhanced with EventTopic attribute discovery
        yield break;
    }
}
