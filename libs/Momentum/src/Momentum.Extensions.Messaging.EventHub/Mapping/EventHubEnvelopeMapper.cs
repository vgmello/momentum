// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Messaging.EventHubs;
using System.Text;
using Wolverine;

namespace Momentum.Extensions.Messaging.EventHub.Mapping;

/// <summary>
///     Base implementation of Event Hub envelope mapper.
/// </summary>
public class EventHubEnvelopeMapper : IEventHubEnvelopeMapper
{
    private const string MessageIdKey = "message-id";
    private const string MessageTypeKey = "message-type";
    private const string ContentTypeKey = "content-type";
    private const string SentAtKey = "sent-at";
    private const string SourceKey = "source";
    private const string CorrelationIdKey = "correlation-id";
    private const string ConversationIdKey = "conversation-id";
    private const string ParentIdKey = "parent-id";

    public virtual void MapEnvelopeToOutgoing(Envelope envelope, EventData outgoing)
    {
        // Set partition key if available
        if (envelope.PartitionKey.IsNotEmpty())
        {
            // EventData doesn't have a direct PartitionKey property
            // It's set during SendAsync, but we store it in properties for reference
            outgoing.Properties[nameof(EventData.PartitionKey)] = envelope.PartitionKey;
        }

        // Map envelope properties to EventData properties
        outgoing.Properties[MessageIdKey] = envelope.Id.ToString();
        outgoing.Properties[MessageTypeKey] = envelope.MessageType;

        if (envelope.ContentType.IsNotEmpty())
            outgoing.Properties[ContentTypeKey] = envelope.ContentType;

        if (envelope.SentAt.HasValue)
            outgoing.Properties[SentAtKey] = envelope.SentAt.Value.ToString("O");

        if (envelope.Source.IsNotEmpty())
            outgoing.Properties[SourceKey] = envelope.Source;

        if (envelope.CorrelationId.IsNotEmpty())
            outgoing.Properties[CorrelationIdKey] = envelope.CorrelationId;

        if (envelope.ConversationId != Guid.Empty)
            outgoing.Properties[ConversationIdKey] = envelope.ConversationId.ToString();

        if (envelope.ParentId.IsNotEmpty())
            outgoing.Properties[ParentIdKey] = envelope.ParentId;

        // Map additional headers
        foreach (var header in envelope.Headers)
        {
            outgoing.Properties[$"header-{header.Key}"] = header.Value?.ToString() ?? string.Empty;
        }
    }

    public virtual void MapIncomingToEnvelope(Envelope envelope, EventData incoming)
    {
        // Map EventData properties back to envelope
        if (incoming.Properties.TryGetValue(MessageIdKey, out var messageId) && messageId != null)
        {
            if (Guid.TryParse(messageId.ToString(), out var id))
                envelope.Id = id;
        }

        if (incoming.Properties.TryGetValue(MessageTypeKey, out var messageType) && messageType != null)
            envelope.MessageType = messageType.ToString()!;

        if (incoming.Properties.TryGetValue(ContentTypeKey, out var contentType) && contentType != null)
            envelope.ContentType = contentType.ToString();

        if (incoming.Properties.TryGetValue(SentAtKey, out var sentAt) && sentAt != null)
        {
            if (DateTimeOffset.TryParse(sentAt.ToString(), out var sentAtValue))
                envelope.SentAt = sentAtValue;
        }

        if (incoming.Properties.TryGetValue(SourceKey, out var source) && source != null)
            envelope.Source = source.ToString();

        if (incoming.Properties.TryGetValue(CorrelationIdKey, out var correlationId) && correlationId != null)
            envelope.CorrelationId = correlationId.ToString();

        if (incoming.Properties.TryGetValue(ConversationIdKey, out var conversationId) && conversationId != null)
        {
            if (Guid.TryParse(conversationId.ToString(), out var convId))
                envelope.ConversationId = convId;
        }

        if (incoming.Properties.TryGetValue(ParentIdKey, out var parentId) && parentId != null)
            envelope.ParentId = parentId.ToString();

        // Map additional headers
        foreach (var prop in incoming.Properties.Where(p => p.Key.StartsWith("header-")))
        {
            var headerKey = prop.Key.Substring("header-".Length);
            envelope.Headers[headerKey] = prop.Value?.ToString() ?? string.Empty;
        }

        // Set partition key from properties if available
        if (incoming.Properties.TryGetValue(nameof(EventData.PartitionKey), out var partitionKey) && partitionKey != null)
            envelope.PartitionKey = partitionKey.ToString();
    }

    public virtual IEnumerable<string> AllHeaders()
    {
        yield return MessageIdKey;
        yield return MessageTypeKey;
        yield return ContentTypeKey;
        yield return SentAtKey;
        yield return SourceKey;
        yield return CorrelationIdKey;
        yield return ConversationIdKey;
        yield return ParentIdKey;
    }
}
