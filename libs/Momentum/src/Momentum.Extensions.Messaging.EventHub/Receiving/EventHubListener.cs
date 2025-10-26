// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using Momentum.Extensions.Messaging.EventHub.Mapping;
using Momentum.Extensions.Messaging.EventHub.Transport;
using Wolverine;
using Wolverine.Runtime;
using Wolverine.Transports;

namespace Momentum.Extensions.Messaging.EventHub.Receiving;

/// <summary>
///     Listener for Event Hubs that uses EventProcessorClient for partition-aware processing with checkpointing.
/// </summary>
public class EventHubListener : IListener, IChannelCallback, IAsyncDisposable
{
    private readonly EventHubEndpoint _endpoint;
    private readonly IReceiver _receiver;
    private readonly IEventHubEnvelopeMapper _mapper;
    private readonly ILogger _logger;
    private EventProcessorClient? _processor;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _disposed;

    public EventHubListener(
        EventHubEndpoint endpoint,
        IReceiver receiver,
        IEventHubEnvelopeMapper mapper,
        ILoggerFactory loggerFactory)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = loggerFactory.CreateLogger<EventHubListener>();

        Address = endpoint.Uri;
    }

    public Uri Address { get; }

    public async ValueTask CompleteAsync(Envelope envelope)
    {
        // Completion is handled via checkpoint in OnProcessEventAsync
        await ValueTask.CompletedTask;
    }

    public async ValueTask DeferAsync(Envelope envelope)
    {
        // Event Hubs doesn't support deferral - message will be reprocessed on next read
        _logger.LogWarning(
            "Message {MessageId} deferred - Event Hubs will reprocess on next partition read",
            envelope.Id);
        await ValueTask.CompletedTask;
    }

    public async Task<bool> TryRequeueAsync(Envelope envelope)
    {
        // Event Hubs doesn't support requeuing - message must be resent
        _logger.LogWarning(
            "Message {MessageId} requeue requested - not supported by Event Hubs, message will be reprocessed",
            envelope.Id);
        await Task.CompletedTask;
        return false;
    }

    public ValueTask DisposeAsync()
    {
        return DisposeAsync(true);
    }

    private async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _cancellation.Cancel();

            if (_processor != null)
            {
                try
                {
                    await _processor.StopProcessingAsync();
                    _logger.LogInformation(
                        "Stopped Event Hub processor for '{EventHubName}'",
                        _endpoint.EventHubName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error stopping Event Hub processor for '{EventHubName}'",
                        _endpoint.EventHubName);
                }
            }

            _cancellation.Dispose();
        }

        _disposed = true;
    }

    public async Task StartAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EventHubListener));

        try
        {
            // Create EventProcessorClient
            _processor = _endpoint.Parent.CreateProcessor(
                _endpoint.EventHubName,
                _endpoint.ConsumerGroup);

            // Register event handlers
            _processor.ProcessEventAsync += OnProcessEventAsync;
            _processor.ProcessErrorAsync += OnProcessErrorAsync;

            // Start processing
            await _processor.StartProcessingAsync(_cancellation.Token);

            _logger.LogInformation(
                "Started Event Hub listener for '{EventHubName}' with consumer group '{ConsumerGroup}'",
                _endpoint.EventHubName,
                _endpoint.ConsumerGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to start Event Hub listener for '{EventHubName}'",
                _endpoint.EventHubName);
            throw;
        }
    }

    public async Task StopAsync()
    {
        await DisposeAsync();
    }

    private async Task OnProcessEventAsync(ProcessEventArgs args)
    {
        if (args.CancellationToken.IsCancellationRequested)
            return;

        try
        {
            // Skip empty events
            if (!args.HasEvent)
                return;

            var eventData = args.Data;

            _logger.LogDebug(
                "Received event from Event Hub '{EventHubName}' partition '{PartitionId}' offset {Offset}",
                _endpoint.EventHubName,
                args.Partition.PartitionId,
                eventData.Offset);

            // Create Wolverine envelope
            var envelope = new Envelope
            {
                Data = eventData.EventBody.ToArray(),
                ContentType = eventData.ContentType ?? "application/json"
            };

            // Map EventData to envelope
            _mapper.MapIncomingToEnvelope(envelope, eventData);

            // Set envelope metadata
            envelope.Destination = Address;

            // Receive the message through Wolverine
            await _receiver.ReceivedAsync(this, envelope);

            // Checkpoint after successful processing (at-least-once delivery)
            // In a production scenario, you might want to checkpoint less frequently for performance
            if (!args.CancellationToken.IsCancellationRequested)
            {
                await args.UpdateCheckpointAsync(args.CancellationToken);

                _logger.LogDebug(
                    "Checkpointed Event Hub '{EventHubName}' partition '{PartitionId}' at offset {Offset}",
                    _endpoint.EventHubName,
                    args.Partition.PartitionId,
                    eventData.Offset);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing event from Event Hub '{EventHubName}' partition '{PartitionId}'",
                _endpoint.EventHubName,
                args.Partition.PartitionId);

            // Don't checkpoint on error - message will be reprocessed
            // This ensures at-least-once delivery semantics
        }
    }

    private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error in Event Hub processor for '{EventHubName}': {Operation}",
            _endpoint.EventHubName,
            args.Operation);

        return Task.CompletedTask;
    }
}
