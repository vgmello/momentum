// Copyright (c) ABCDEG. All rights reserved.

using CloudNative.CloudEvents;
using static CloudNative.CloudEvents.CloudEventAttribute;

namespace Operations.ServiceDefaults.Messaging.Kafka;

public static class DistributedTracingExtensions
{
    public static CloudEventAttribute TraceParentAttribute { get; } = CreateExtension("traceparent", CloudEventAttributeType.String);

    public static CloudEvent SetTraceParent(this CloudEvent cloudEvent, string traceParent)
    {
        cloudEvent[TraceParentAttribute] = traceParent;

        return cloudEvent;
    }

    public static string? GetTraceParent(this CloudEvent cloudEvent) => (string?)cloudEvent[TraceParentAttribute];
}
