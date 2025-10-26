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
///     Inline sender for Event Hubs that sends messages immediately (synchronous send).
/// </summary>
public class InlineEventHubSender : ISender
{
    private readonly EventHubEndpoint _endpoint;
    private readonly IEventHubEnvelopeMapper _mapper;
    private readonly ILogger _logger;
    private EventHubProducerClient? _producer;
    private bool _disposed;

    public InlineEventHubSender(EventHubEndpoint endpoint, IEventHubEnvelopeMapper mapper, ILoggerFactory loggerFactory)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = loggerFactory.CreateLogger<InlineEventHubSender>();
    }

    public bool SupportsNativeScheduledSend => false;

    public Uri Destination => _endpoint.Uri;

    public async Task<bool> PingAsync()
    {
        try
        {
            EnsureProducer();
            var properties = await _producer!.GetEventHubPropertiesAsync();
            return properties != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ping Event Hub '{EventHubName}'", _endpoint.EventHubName);
            return false;
        }
    }

    public async Task SendAsync(Envelope envelope)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InlineEventHubSender));

        try
        {
            EnsureProducer();

            // Create EventData from envelope
            var eventData = new EventData(envelope.Data ?? Array.Empty<byte>());

            // Map envelope to EventData
            _mapper.MapEnvelopeToOutgoing(envelope, eventData);

            // Create send options with partition key if available
            SendEventOptions? sendOptions = null;
            if (envelope.PartitionKey.IsNotEmpty())
            {
                sendOptions = new SendEventOptions
                {
                    PartitionKey = envelope.PartitionKey
                };
            }

            // Send to Event Hub
            if (sendOptions != null)
            {
                await _producer!.SendAsync(new[] { eventData }, sendOptions);
            }
            else
            {
                await _producer!.SendAsync(new[] { eventData });
            }

            _logger.LogDebug(
                "Sent message {MessageId} of type {MessageType} to Event Hub '{EventHubName}'",
                envelope.Id,
                envelope.MessageType,
                _endpoint.EventHubName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send message {MessageId} to Event Hub '{EventHubName}'",
                envelope.Id,
                _endpoint.EventHubName);
            throw;
        }
    }

    private void EnsureProducer()
    {
        if (_producer == null)
        {
            _producer = _endpoint.Parent.CreateProducer(_endpoint.EventHubName);
            _logger.LogInformation("Created Event Hub producer for '{EventHubName}'", _endpoint.EventHubName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_producer != null)
        {
            await _producer.DisposeAsync();
            _logger.LogInformation("Disposed Event Hub producer for '{EventHubName}'", _endpoint.EventHubName);
        }

        GC.SuppressFinalize(this);
    }
}
