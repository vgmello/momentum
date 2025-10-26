// Copyright (c) Momentum .NET. All rights reserved.

using Azure.Messaging.EventHubs;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Options;
using Momentum.ServiceDefaults.Messaging;
using System.Text;
using Wolverine;

namespace Momentum.Extensions.Messaging.EventHub.Mapping;

/// <summary>
///     Maps Wolverine envelopes to/from CloudEvents format for Event Hubs.
/// </summary>
public class CloudEventMapper(IOptions<ServiceBusOptions> serviceBusOptions) : IEventHubEnvelopeMapper
{
    private static readonly CloudEventFormatter Formatter = new JsonEventFormatter();
    private const string CloudEventsContentType = "application/cloudevents+json";
    private const string SpecVersionKey = "ce_specversion";
    private const string TypeKey = "ce_type";
    private const string SourceKey = "ce_source";
    private const string IdKey = "ce_id";
    private const string TimeKey = "ce_time";
    private const string DataContentTypeKey = "ce_datacontenttype";
    private const string TraceParentKey = "traceparent";

    public void MapEnvelopeToOutgoing(Envelope envelope, EventData outgoing)
    {
        // Create CloudEvent from envelope
        var cloudEvent = new CloudEvent
        {
            Id = envelope.Id.ToString(),
            Type = envelope.MessageType,
            Time = envelope.SentAt,
            Data = envelope.Data,
            DataContentType = envelope.ContentType ?? "application/json",
            Source = serviceBusOptions.Value.ServiceUrn
        };

        // Add distributed tracing
        if (envelope.ParentId.IsNotEmpty())
        {
            cloudEvent.SetAttributeFromString(TraceParentKey, envelope.ParentId);
        }

        // Serialize CloudEvent to JSON
        var cloudEventBytes = Formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);

        // Set EventData body
        outgoing.EventBody = new BinaryData(cloudEventBytes.ToArray());

        // Set CloudEvent metadata in properties for easy filtering
        outgoing.Properties[SpecVersionKey] = CloudEventsSpecVersion.Default.VersionId;
        outgoing.Properties[TypeKey] = cloudEvent.Type;
        outgoing.Properties[SourceKey] = cloudEvent.Source?.ToString() ?? string.Empty;
        outgoing.Properties[IdKey] = cloudEvent.Id;

        if (cloudEvent.Time.HasValue)
            outgoing.Properties[TimeKey] = cloudEvent.Time.Value.ToString("O");

        outgoing.Properties[DataContentTypeKey] = cloudEvent.DataContentType ?? string.Empty;

        // Set content type
        outgoing.ContentType = contentType?.ToString() ?? CloudEventsContentType;

        // Set partition key
        if (envelope.PartitionKey.IsNotEmpty())
        {
            outgoing.Properties[nameof(EventData.PartitionKey)] = envelope.PartitionKey;
        }

        // Add tracing header
        if (envelope.ParentId.IsNotEmpty())
        {
            outgoing.Properties[TraceParentKey] = envelope.ParentId;
        }
    }

    public void MapIncomingToEnvelope(Envelope envelope, EventData incoming)
    {
        // Check if this is a CloudEvent
        if (!IsCloudEvent(incoming))
        {
            // Fall back to base mapping
            var baseMapper = new EventHubEnvelopeMapper();
            baseMapper.MapIncomingToEnvelope(envelope, incoming);
            return;
        }

        try
        {
            // Decode CloudEvent from EventData
            var cloudEvent = DecodeCloudEvent(incoming);

            if (cloudEvent == null)
                return;

            // Map CloudEvent to envelope
            envelope.MessageType = cloudEvent.Type!;

            if (Guid.TryParse(cloudEvent.Id, out var id))
                envelope.Id = id;

            envelope.ContentType = cloudEvent.DataContentType;
            envelope.SentAt = cloudEvent.Time;

            if (cloudEvent.Source != null)
                envelope.Source = cloudEvent.Source.ToString();

            // Extract trace parent
            if (cloudEvent.TryGetAttribute(TraceParentKey, out var traceParent))
            {
                envelope.ParentId = traceParent?.ToString();
                envelope.Headers[TraceParentKey] = traceParent?.ToString() ?? string.Empty;
            }

            // Extract data
            if (cloudEvent.Data is BinaryData binaryData)
            {
                envelope.Data = binaryData.ToArray();
            }
            else if (cloudEvent.Data is byte[] bytes)
            {
                envelope.Data = bytes;
            }
            else if (cloudEvent.Data != null)
            {
                // Serialize data to JSON
                var json = System.Text.Json.JsonSerializer.Serialize(cloudEvent.Data);
                envelope.Data = Encoding.UTF8.GetBytes(json);
            }

            // Set partition key from properties
            if (incoming.Properties.TryGetValue(nameof(EventData.PartitionKey), out var partitionKey) && partitionKey != null)
                envelope.PartitionKey = partitionKey.ToString();
        }
        catch (Exception)
        {
            // If CloudEvent parsing fails, fall back to base mapping
            var baseMapper = new EventHubEnvelopeMapper();
            baseMapper.MapIncomingToEnvelope(envelope, incoming);
        }
    }

    public IEnumerable<string> AllHeaders()
    {
        yield return SpecVersionKey;
        yield return TypeKey;
        yield return SourceKey;
        yield return IdKey;
        yield return TimeKey;
        yield return DataContentTypeKey;
        yield return TraceParentKey;
    }

    private static bool IsCloudEvent(EventData eventData)
    {
        // Check if message has CloudEvents headers
        return eventData.Properties.ContainsKey(SpecVersionKey) ||
               eventData.ContentType?.Contains("cloudevents") == true;
    }

    private static CloudEvent? DecodeCloudEvent(EventData eventData)
    {
        try
        {
            // Decode from structured mode (JSON body)
            if (eventData.ContentType?.Contains("cloudevents+json") == true ||
                eventData.ContentType?.Contains("application/json") == true)
            {
                var bodyBytes = eventData.EventBody.ToArray();
                return Formatter.DecodeStructuredModeMessage(bodyBytes, new System.Net.Mime.ContentType(eventData.ContentType), extensionAttributes: null);
            }

            // Decode from binary mode (headers + data)
            if (eventData.Properties.ContainsKey(SpecVersionKey))
            {
                var specVersion = eventData.Properties[SpecVersionKey]?.ToString();
                var spec = CloudEventsSpecVersion.FromVersionId(specVersion);

                if (spec == null)
                    return null;

                var cloudEvent = new CloudEvent(spec);

                // Map properties to CloudEvent attributes
                if (eventData.Properties.TryGetValue(TypeKey, out var type))
                    cloudEvent.Type = type?.ToString();

                if (eventData.Properties.TryGetValue(SourceKey, out var source) && source != null)
                    cloudEvent.Source = new Uri(source.ToString()!, UriKind.RelativeOrAbsolute);

                if (eventData.Properties.TryGetValue(IdKey, out var id))
                    cloudEvent.Id = id?.ToString();

                if (eventData.Properties.TryGetValue(TimeKey, out var time) && time != null)
                {
                    if (DateTimeOffset.TryParse(time.ToString(), out var timeValue))
                        cloudEvent.Time = timeValue;
                }

                if (eventData.Properties.TryGetValue(DataContentTypeKey, out var dataContentType))
                    cloudEvent.DataContentType = dataContentType?.ToString();

                if (eventData.Properties.TryGetValue(TraceParentKey, out var traceParent))
                    cloudEvent.SetAttributeFromString(TraceParentKey, traceParent?.ToString() ?? string.Empty);

                // Set data from EventData body
                cloudEvent.Data = eventData.EventBody;

                return cloudEvent;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
