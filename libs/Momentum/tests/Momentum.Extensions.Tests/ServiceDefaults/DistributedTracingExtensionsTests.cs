// Copyright (c) Momentum .NET. All rights reserved.

using CloudNative.CloudEvents;
using Momentum.ServiceDefaults.Messaging;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class DistributedTracingExtensionsTests
{
    [Fact]
    public void TraceParentAttribute_ShouldHaveCorrectName()
    {
        DistributedTracingExtensions.TraceParentAttribute.Name.ShouldBe("traceparent");
    }

    [Fact]
    public void SetTraceParent_ShouldSetAttribute()
    {
        var cloudEvent = new CloudEvent
        {
            Type = "test.event",
            Source = new Uri("urn:test:source")
        };
        const string traceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

        cloudEvent.SetTraceParent(traceParent);

        cloudEvent.GetTraceParent().ShouldBe(traceParent);
    }

    [Fact]
    public void GetTraceParent_ShouldReturnSetValue()
    {
        var cloudEvent = new CloudEvent
        {
            Type = "test.event",
            Source = new Uri("urn:test:source")
        };
        const string traceParent = "00-abcdef1234567890abcdef1234567890-1234567890abcdef-01";

        cloudEvent.SetTraceParent(traceParent);

        cloudEvent.GetTraceParent().ShouldBe(traceParent);
    }

    [Fact]
    public void GetTraceParent_WithNoValue_ShouldReturnNull()
    {
        var cloudEvent = new CloudEvent
        {
            Type = "test.event",
            Source = new Uri("urn:test:source")
        };

        cloudEvent.GetTraceParent().ShouldBeNull();
    }
}
