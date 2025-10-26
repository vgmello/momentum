// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;
using Momentum.Extensions.Messaging.EventHub.Mapping;
using Momentum.Extensions.Messaging.EventHub.Transport;
using Wolverine;
using Wolverine.Transports.Sending;

namespace Momentum.Extensions.Messaging.EventHub.Sending;

/// <summary>
///     Batched sender protocol for Event Hubs that sends messages in batches for better throughput.
/// </summary>
public class EventHubSenderProtocol : ISenderProtocol
{
    private readonly EventHubEndpoint _endpoint;
    private readonly IEventHubEnvelopeMapper _mapper;
    private readonly ILogger _logger;

    public EventHubSenderProtocol(EventHubEndpoint endpoint, IEventHubEnvelopeMapper mapper, ILoggerFactory loggerFactory)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = loggerFactory.CreateLogger<EventHubSenderProtocol>();
    }

    public async Task SendBatchAsync(ISenderCallback callback, OutgoingMessageBatch batch)
    {
        EventHubProducerClient? producer = null;

        try
        {
            producer = _endpoint.Parent.CreateProducer(_endpoint.EventHubName);

            // Group messages by partition key for more efficient batching
            var messagesByPartitionKey = batch.Messages
                .GroupBy(msg => msg.PartitionKey ?? string.Empty)
                .ToList();

            foreach (var group in messagesByPartitionKey)
            {
                var partitionKey = group.Key;
                var messages = group.ToList();

                // Create event batch with partition key
                var createBatchOptions = new CreateBatchOptions();
                if (partitionKey.IsNotEmpty())
                {
                    createBatchOptions.PartitionKey = partitionKey;
                }

                await using var eventBatch = await producer.CreateBatchAsync(createBatchOptions);

                foreach (var envelope in messages)
                {
                    try
                    {
                        // Create EventData from envelope
                        var eventData = new EventData(envelope.Data ?? Array.Empty<byte>());

                        // Map envelope to EventData
                        _mapper.MapEnvelopeToOutgoing(envelope, eventData);

                        // Try to add to batch
                        if (!eventBatch.TryAdd(eventData))
                        {
                            // Batch is full, send it
                            await producer.SendAsync(eventBatch);

                            // Mark sent messages as successful
                            await callback.MarkSuccessfulAsync(envelope);

                            _logger.LogDebug(
                                "Sent batch of messages to Event Hub '{EventHubName}' (batch full)",
                                _endpoint.EventHubName);

                            // Create new batch and add the current message
                            eventBatch.Clear();

                            if (!eventBatch.TryAdd(eventData))
                            {
                                // Message is too large for batch
                                _logger.LogError(
                                    "Message {MessageId} is too large to fit in Event Hub batch",
                                    envelope.Id);
                                await callback.MarkProcessingFailureAsync(envelope);
                                continue;
                            }
                        }

                        // Mark as successful (will be sent when batch is full or at end)
                        await callback.MarkSuccessfulAsync(envelope);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to add message {MessageId} to Event Hub batch",
                            envelope.Id);
                        await callback.MarkProcessingFailureAsync(envelope);
                    }
                }

                // Send remaining messages in batch
                if (eventBatch.Count > 0)
                {
                    await producer.SendAsync(eventBatch);

                    _logger.LogDebug(
                        "Sent batch of {Count} messages to Event Hub '{EventHubName}'",
                        eventBatch.Count,
                        _endpoint.EventHubName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send batch to Event Hub '{EventHubName}'",
                _endpoint.EventHubName);

            // Mark all messages as failed
            foreach (var envelope in batch.Messages)
            {
                await callback.MarkProcessingFailureAsync(envelope);
            }
        }
        finally
        {
            if (producer != null)
            {
                await producer.DisposeAsync();
            }
        }
    }
}
