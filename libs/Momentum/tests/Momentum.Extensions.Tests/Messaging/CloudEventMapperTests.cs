// Copyright (c) Momentum .NET. All rights reserved.

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Momentum.Extensions.Messaging.Kafka;
using Momentum.ServiceDefaults.Messaging;
using Shouldly;
using System.Text;
using Wolverine;

namespace Momentum.Extensions.Tests.Messaging;

public class CloudEventMapperTests
{
    private readonly CloudEventMapper _mapper;
    private readonly ServiceBusOptions _serviceBusOptions;

    public CloudEventMapperTests()
    {
        _serviceBusOptions = new ServiceBusOptions 
        { 
            Domain = "TestDomain",
            PublicServiceName = "test-service"
        };
        // Use reflection to set the private ServiceUrn property for testing
        typeof(ServiceBusOptions)
            .GetProperty(nameof(ServiceBusOptions.ServiceUrn))!
            .SetValue(_serviceBusOptions, new Uri("urn:momentum:test-service"));
        
        var options = Options.Create(_serviceBusOptions);
        _mapper = new CloudEventMapper(options);
    }

    [Fact]
    public void MapEnvelopeToOutgoing_WithBasicEnvelope_MapsCorrectly()
    {
        // Arrange
        var envelope = new Envelope
        {
            Id = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            MessageType = "TestEvent",
            Data = Encoding.UTF8.GetBytes("{\"test\":\"data\"}"),
            ContentType = "application/json",
            PartitionKey = "test-partition"
        };
        // Use reflection to set readonly SentAt property for testing
        typeof(Envelope)
            .GetProperty(nameof(Envelope.SentAt))!
            .SetValue(envelope, new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        var outgoing = new Message<string, byte[]>
        {
            Headers = new Headers()
        };

        // Act
        _mapper.MapEnvelopeToOutgoing(envelope, outgoing);

        // Assert
        outgoing.Key.ShouldBe("test-partition");
        outgoing.Value.ShouldNotBeNull();
        outgoing.Headers.ShouldNotBeEmpty();
        
        // Verify CloudEvent headers
        GetHeaderValue(outgoing, "ce_id").ShouldBe("123e4567-e89b-12d3-a456-426614174000");
        GetHeaderValue(outgoing, "ce_type").ShouldBe("TestEvent");
        GetHeaderValue(outgoing, "ce_source").ShouldBe("urn:momentum:test-service");
        GetHeaderValue(outgoing, "ce_datacontenttype").ShouldBe("application/json");
        GetHeaderValue(outgoing, "ce_specversion").ShouldBe("1.0");
    }

    [Fact]
    public void MapEnvelopeToOutgoing_WithParentId_IncludesTraceParent()
    {
        // Arrange
        var envelope = new Envelope
        {
            Id = Guid.NewGuid(),
            MessageType = "TestEvent",
            Data = Encoding.UTF8.GetBytes("{\"test\":\"data\"}"),
            ParentId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
        };

        var outgoing = new Message<string, byte[]>
        {
            Headers = new Headers()
        };

        // Act
        _mapper.MapEnvelopeToOutgoing(envelope, outgoing);

        // Assert
        GetHeaderValue(outgoing, "ce_traceparent").ShouldBe("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");
    }

    [Fact]
    public void MapEnvelopeToOutgoing_WithoutPartitionKey_UsesEnvelopeId()
    {
        // Arrange
        var envelopeId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");
        var envelope = new Envelope
        {
            Id = envelopeId,
            MessageType = "TestEvent",
            Data = Encoding.UTF8.GetBytes("{\"test\":\"data\"}")
        };

        var outgoing = new Message<string, byte[]>
        {
            Headers = new Headers()
        };

        // Act
        _mapper.MapEnvelopeToOutgoing(envelope, outgoing);

        // Assert
        outgoing.Key.ShouldBe("123e4567-e89b-12d3-a456-426614174000");
    }

    [Fact]
    public void MapIncomingToEnvelope_WithCloudEventMessage_MapsCorrectly()
    {
        // Arrange
        var incoming = CreateCloudEventMessage();
        var envelope = new Envelope();
        // Initialize Headers using reflection since it's readonly
        var headersProperty = typeof(Envelope).GetProperty(nameof(Envelope.Headers))!;
        headersProperty.SetValue(envelope, new Dictionary<string, string>());

        // Act
        _mapper.MapIncomingToEnvelope(envelope, incoming);

        // Assert
        envelope.Id.ShouldBe(Guid.Parse("123e4567-e89b-12d3-a456-426614174000"));
        envelope.MessageType.ShouldBe("TestEvent");
        envelope.ContentType.ShouldBe("application/json");
        envelope.Headers.ShouldContainKey("source");
        envelope.Headers.ShouldContainKey("time");
    }

    [Fact]
    public void MapIncomingToEnvelope_WithTraceParent_MapsTraceParent()
    {
        // Arrange
        var incoming = CreateCloudEventMessage();
        incoming.Headers.Add("ce_traceparent", Encoding.UTF8.GetBytes("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"));
        
        var envelope = new Envelope();
        // Initialize Headers using reflection since it's readonly
        var headersProperty = typeof(Envelope).GetProperty(nameof(Envelope.Headers))!;
        headersProperty.SetValue(envelope, new Dictionary<string, string>());

        // Act
        _mapper.MapIncomingToEnvelope(envelope, incoming);

        // Assert
        envelope.Headers.ShouldContainKey("traceparent");
        envelope.Headers["traceparent"].ShouldBe("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");
    }

    [Fact]
    public void MapIncomingToEnvelope_WithNonCloudEventMessage_DoesNotModifyEnvelope()
    {
        // Arrange
        var incoming = new Message<string, byte[]>
        {
            Headers = new Headers { { "regular-header", Encoding.UTF8.GetBytes("value") } }
        };
        
        var envelope = new Envelope { MessageType = "OriginalType" };

        // Act
        _mapper.MapIncomingToEnvelope(envelope, incoming);

        // Assert
        envelope.MessageType.ShouldBe("OriginalType");
    }

    [Fact]
    public void MapIncomingToEnvelope_WithInvalidGuidId_DoesNotSetId()
    {
        // Arrange
        var incoming = CreateCloudEventMessage();
        incoming.Headers.Add("ce_id", Encoding.UTF8.GetBytes("invalid-guid"));
        
        var envelope = new Envelope();
        // Initialize Headers using reflection since it's readonly
        var headersProperty = typeof(Envelope).GetProperty(nameof(Envelope.Headers))!;
        headersProperty.SetValue(envelope, new Dictionary<string, string>());

        // Act
        _mapper.MapIncomingToEnvelope(envelope, incoming);

        // Assert
        envelope.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void AllHeaders_ReturnsEmptyEnumerable()
    {
        // Act
        var headers = _mapper.AllHeaders();

        // Assert
        headers.ShouldBeEmpty();
    }

    private static Message<string, byte[]> CreateCloudEventMessage()
    {
        var headers = new Headers();
        headers.Add("ce_specversion", Encoding.UTF8.GetBytes("1.0"));
        headers.Add("ce_id", Encoding.UTF8.GetBytes("123e4567-e89b-12d3-a456-426614174000"));
        headers.Add("ce_type", Encoding.UTF8.GetBytes("TestEvent"));
        headers.Add("ce_source", Encoding.UTF8.GetBytes("urn:momentum:test-service"));
        headers.Add("ce_datacontenttype", Encoding.UTF8.GetBytes("application/json"));
        headers.Add("ce_time", Encoding.UTF8.GetBytes("2024-01-15T10:30:00Z"));
        
        return new Message<string, byte[]>
        {
            Headers = headers,
            Value = Encoding.UTF8.GetBytes("{\"test\":\"data\"}")
        };
    }

    private static string? GetHeaderValue(Message<string, byte[]> message, string headerName)
    {
        return message.Headers?.TryGetLastBytes(headerName, out var value) == true 
            ? Encoding.UTF8.GetString(value) 
            : null;
    }
}