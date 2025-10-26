// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Messaging.EventHubs;
using JasperFx.Core;
using Microsoft.Extensions.Logging;
using Momentum.Extensions.Messaging.EventHub.Mapping;
using Momentum.Extensions.Messaging.EventHub.Receiving;
using Momentum.Extensions.Messaging.EventHub.Sending;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.Runtime;
using Wolverine.Transports;

namespace Momentum.Extensions.Messaging.EventHub.Transport;

/// <summary>
///     Represents an Event Hub endpoint for sending and receiving messages.
/// </summary>
public class EventHubEndpoint : Endpoint<IEventHubEnvelopeMapper, EventHubEnvelopeMapper>, IBrokerEndpoint
{
    private readonly EventHubTransport _parent;

    public EventHubEndpoint(EventHubTransport parent, string eventHubName, EndpointRole role) : base(
        new Uri($"{EventHubTransport.ProtocolName}://{eventHubName}"), role)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        EventHubName = eventHubName ?? throw new ArgumentNullException(nameof(eventHubName));
        EndpointName = eventHubName;

        // Default mode is BufferedInMemory for better throughput
        Mode = EndpointMode.BufferedInMemory;
    }

    /// <summary>
    ///     Gets the Event Hub name.
    /// </summary>
    public string EventHubName { get; }

    /// <summary>
    ///     Gets the parent transport.
    /// </summary>
    public EventHubTransport Parent => _parent;

    /// <summary>
    ///     Consumer group for Event Hub consumers. Defaults to $Default.
    /// </summary>
    public string ConsumerGroup { get; set; } = EventHubConsumerClient.DefaultConsumerGroupName;

    /// <summary>
    ///     Number of partitions for auto-provisioning. Default is 4.
    /// </summary>
    public int PartitionCount { get; set; } = 4;

    /// <summary>
    ///     Message retention period for auto-provisioning. Default is 7 days.
    /// </summary>
    public TimeSpan MessageRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    ///     Extracts Event Hub name from a URI.
    /// </summary>
    public static string EventHubNameForUri(Uri uri)
    {
        if (uri.Scheme == EventHubTransport.ProtocolName)
        {
            return uri.Host;
        }

        // Handle path-based URIs
        return uri.Segments.Last().TrimEnd('/');
    }

    /// <summary>
    ///     Builds a URI for an Event Hub name.
    /// </summary>
    public static Uri BuildUri(string eventHubName)
    {
        return new Uri($"{EventHubTransport.ProtocolName}://{eventHubName}");
    }

    public override ValueTask<IListener> BuildListenerAsync(IWolverineRuntime runtime, IReceiver receiver)
    {
        var mapper = runtime.Options.As<WolverineOptions>().TryFindExtension<IEventHubEnvelopeMapper>()
                     ?? new EventHubEnvelopeMapper();

        var listener = new EventHubListener(this, receiver, mapper, runtime.LoggerFactory);
        return new ValueTask<IListener>(listener);
    }

    protected override ISender CreateSender(IWolverineRuntime runtime)
    {
        var mapper = runtime.Options.As<WolverineOptions>().TryFindExtension<IEventHubEnvelopeMapper>()
                     ?? new EventHubEnvelopeMapper();

        // Use inline sender for simplicity (can be enhanced with batched sender later)
        return new InlineEventHubSender(this, mapper, runtime.LoggerFactory);
    }

    public override IDictionary<string, object> DescribeSelf()
    {
        var description = base.DescribeSelf();
        description[nameof(EventHubName)] = EventHubName;
        description[nameof(ConsumerGroup)] = ConsumerGroup;
        return description;
    }

    #region IBrokerEndpoint Implementation

    public async ValueTask<bool> CheckAsync()
    {
        try
        {
            // Create a temporary producer to test connectivity
            await using var producer = _parent.CreateProducer(EventHubName);

            // Getting properties validates the connection and that the Event Hub exists
            var properties = await producer.GetEventHubPropertiesAsync();
            return properties != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async ValueTask TeardownAsync(ILogger logger)
    {
        // Event Hubs don't support programmatic deletion via producer/consumer clients
        // Teardown would require Azure Resource Manager, which is out of scope for runtime operations
        logger.LogInformation("Event Hub '{EventHubName}' teardown requested but not implemented (requires Azure Resource Manager)", EventHubName);
        await ValueTask.CompletedTask;
    }

    public async ValueTask SetupAsync(ILogger logger)
    {
        if (!_parent.AutoProvisionEnabled)
        {
            logger.LogDebug("Auto-provisioning is disabled for Event Hub '{EventHubName}'", EventHubName);
            return;
        }

        // Check if running in production
        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isProduction)
        {
            logger.LogWarning(
                "Auto-provisioning is not supported in production environments. Event Hub '{EventHubName}' must be created manually.",
                EventHubName);
            return;
        }

        logger.LogWarning(
            "Auto-provisioning enabled for Event Hub '{EventHubName}' - this should only be used in development!",
            EventHubName);

        try
        {
            // Check if Event Hub already exists
            var exists = await CheckAsync();

            if (exists)
            {
                logger.LogInformation("Event Hub '{EventHubName}' already exists", EventHubName);
                return;
            }

            logger.LogWarning(
                "Event Hub '{EventHubName}' does not exist. Auto-provisioning requires Azure Resource Manager SDK and proper credentials. " +
                "Please create the Event Hub manually or implement Azure Resource Manager provisioning.",
                EventHubName);

            // TODO: Implement Azure Resource Manager-based provisioning if needed
            // This would require additional dependencies and Azure subscription/resource group configuration
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to setup Event Hub '{EventHubName}'", EventHubName);
            throw;
        }

        // Ensure checkpoint container exists
        try
        {
            var checkpointStore = _parent.CreateCheckpointStore();
            await checkpointStore.CreateIfNotExistsAsync();
            logger.LogInformation("Checkpoint container '{ContainerName}' created or already exists", _parent.CheckpointContainerName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create checkpoint container '{ContainerName}'", _parent.CheckpointContainerName);
            throw;
        }
    }

    public ValueTask<bool> InitializeAsync(ILogger logger)
    {
        // Initialization logic if needed
        return new ValueTask<bool>(true);
    }

    #endregion
}
