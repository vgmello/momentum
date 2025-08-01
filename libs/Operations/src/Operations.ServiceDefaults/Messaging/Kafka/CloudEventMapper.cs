// Copyright (c) ABCDEG. All rights reserved.

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text;
using Wolverine;
using Wolverine.Kafka;

namespace Operations.ServiceDefaults.Messaging.Kafka;

public class CloudEventMapper(IOptions<ServiceBusOptions> serviceBusOptions) : IKafkaEnvelopeMapper
{
    private static readonly CloudEventFormatter Formatter = new JsonEventFormatter();

    public void MapEnvelopeToOutgoing(Envelope envelope, Message<string, byte[]> outgoing)
    {
        var cloudEvent = new CloudEvent
        {
            Id = envelope.Id.ToString(),
            Type = envelope.MessageType,
            Time = envelope.SentAt,
            Data = envelope.Data,
            DataContentType = envelope.ContentType,
            Source = serviceBusOptions.Value.ServiceUrn
        };

        if (envelope.ParentId is not null)
            cloudEvent.SetTraceParent(envelope.ParentId);

        var kafkaMessage = cloudEvent.ToKafkaMessage(ContentMode.Binary, Formatter);

        outgoing.Key = envelope.PartitionKey ?? envelope.Id.ToString();
        outgoing.Value = kafkaMessage.Value;

        foreach (var header in kafkaMessage.Headers)
        {
            outgoing.Headers.Add(header.Key, header.GetValueBytes());
        }
    }

    public void MapIncomingToEnvelope(Envelope envelope, Message<string, byte[]> incoming)
    {
        if (!incoming!.IsCloudEvent())
            return;

        var cloudEventsSpecVersionId = GetHeaderValue(incoming, CloudEventsSpecVersion.SpecVersionAttribute.Name);
        var spec = CloudEventsSpecVersion.FromVersionId(cloudEventsSpecVersionId);

        if (spec is null)
            return;

        envelope.MessageType = GetHeaderValue(incoming, spec.TypeAttribute.Name);
        envelope.ContentType = GetHeaderValue(incoming, spec.DataContentTypeAttribute.Name);

        if (Guid.TryParse(GetHeaderValue(incoming, spec.IdAttribute.Name), out var messageId))
        {
            envelope.Id = messageId;
        }

        var traceParent = GetHeaderValue(incoming, DistributedTracingExtensions.TraceParentAttribute.Name);

        if (traceParent is not null)
        {
            envelope.Headers[DistributedTracingExtensions.TraceParentAttribute.Name] = traceParent;
        }

        var source = GetHeaderValue(incoming, spec.SourceAttribute.Name);

        if (source is not null)
        {
            envelope.Headers[spec.SourceAttribute.Name] = source;
        }

        var time = GetHeaderValue(incoming, spec.TimeAttribute.Name);

        if (time is not null)
        {
            envelope.Headers[spec.TimeAttribute.Name] = time;
        }
    }

    public IEnumerable<string> AllHeaders()
    {
        yield break;
    }

    private static string? GetHeaderValue(Message<string, byte[]> message, string headerName)
    {
        const string kafkaHeaderPrefix = "ce_";

        if (message.Headers is null)
            return null;

        return message.Headers.TryGetLastBytes(kafkaHeaderPrefix + headerName, out var value) ? Encoding.UTF8.GetString(value) : null;
    }
}
