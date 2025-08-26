# OpenTelemetry Instrumentation Middleware

## Middleware Overview

This middleware creates activity spans for each message being processed, enabling:

- **Distributed Tracing**: Across service boundaries
- **Message Type Tagging**: Automatic categorization of operations
- **Operation Type Classification**: Command vs Query identification
- **Error Tracking**: Status reporting and exception details
- **Correlation**: Message processing in observability platforms

## Activity Creation and Tagging

This method creates an activity with tags for:

- **message.id**: The unique message identifier
- **message.name**: The full type name of the message
- **operation.type**: Whether it's a command or query
- **message.source**: The source of the message (if available)

### Activity Span Structure

```csharp
var activity = activitySource.StartActivity(activityName, ActivityKind.Consumer, parentId: parentTraceId);

activity.SetTag("message.id", envelope.Id.ToString());
activity.SetTag("messaging.destination", envelope.Destination?.ToString() ?? "unknown");
activity.SetTag("message.name", envelope.GetMessageName(fullName: true));
```

### Operation Type Detection

The middleware automatically categorizes operations:

```csharp
if (IsCommand(envelope.Message))
{
    activity.SetTag("operation.type", "command");
}
else if (IsQuery(envelope.Message))
{
    activity.SetTag("operation.type", "query");
}
```

### Command and Query Identification

Operations are classified based on interface implementations:

```csharp
private static bool IsCommand(object message) =>
    message.GetType().GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

private static bool IsQuery(object message) =>
    message.GetType().GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
```

## Activity Completion and Status

Sets the activity status to OK for successful processing or Error with exception details if the message processing failed.

### Success Handling

For successful message processing:

```csharp
if (envelope.Failure is null)
{
    activity.SetStatus(ActivityStatusCode.Ok);
}
```

### Error Handling

For failed message processing:

```csharp
else
{
    activity.SetStatus(ActivityStatusCode.Error, envelope.Failure.Message);
    activity.SetTag("error.type", envelope.Failure.GetType().Name);
}
```

### Activity Lifecycle

The activity is properly disposed:

```csharp
activity.Stop();
```

## Distributed Tracing Integration

### Parent Trace Extraction

The middleware extracts parent trace context from incoming messages:

```csharp
private static string? ExtractParentTraceIdFromIncomingMessage(Envelope envelope)
{
    if (envelope.Headers.TryGetValue(DistributedTracingExtensions.TraceParentAttribute.Name, out var traceParentHeader))
    {
        return traceParentHeader;
    }

    if (!string.IsNullOrEmpty(envelope.ParentId))
    {
        return envelope.ParentId;
    }

    return null;
}
```

### Trace Correlation

This enables:

- **Cross-Service Correlation**: Linking spans across service boundaries
- **Request Flow Tracking**: Following requests through multiple services
- **Performance Analysis**: Understanding end-to-end request latency
- **Error Propagation**: Tracking errors through the request chain

## Observability Platform Integration

### Standard Tags

The middleware uses standard OpenTelemetry semantic conventions:

- **messaging.destination**: Target endpoint or queue
- **message.id**: Unique identifier for correlation
- **operation.type**: Semantic operation classification
- **error.type**: Exception classification for monitoring

### Platform Compatibility

Works with observability platforms like:

- **Jaeger**: Distributed tracing visualization
- **Zipkin**: Request flow analysis
- **Application Insights**: Azure monitoring integration
- **Prometheus**: Metrics collection and alerting
- **Grafana**: Visualization and dashboards
