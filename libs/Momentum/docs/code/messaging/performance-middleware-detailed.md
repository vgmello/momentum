# Request Performance Middleware

## Middleware Overview {#middleware-overview}

This middleware:

- **Records Message Processing**: Start and end times for each message
- **Logs Message Lifecycle**: Receipt, completion, and failures
- **Updates Messaging Metrics**: For monitoring and alerting
- **Tracks Duration**: Message processing execution time

The middleware is automatically applied to all message handlers via the messaging policy.

## Pre-Processing Setup {#pre-processing-setup}

This method captures the start time, logs message receipt, and increments the message received counter in the metrics system.

### Timing Initialization

```csharp
_messageTypeName = envelope.GetMessageName();
_startedTime = Stopwatch.GetTimestamp();
```

### Message Receipt Logging

```csharp
LogRequestReceived(logger, _messageTypeName, envelope.Message);
```

### Metrics Collection

```csharp
var metricName = envelope.GetMessageName(fullName: true);
_messagingMetrics = meterStore.GetOrCreateMetrics(metricName);
_messagingMetrics.MessageReceived();
```

## Post-Processing Analysis {#post-processing-analysis}

This method calculates the elapsed time, logs the outcome (success or failure), and records the processing duration and any exceptions in the metrics system.

### Duration Calculation

```csharp
var elapsedTime = Stopwatch.GetElapsedTime(_startedTime);
_messagingMetrics?.RecordProcessingTime(elapsedTime);
```

### Success Path Handling

```csharp
if (envelope.Failure is null)
{
    LogRequestCompleted(logger, _messageTypeName, elapsedTime);
}
```

### Failure Path Handling

```csharp
else
{
    LogRequestFailed(logger, envelope.Failure, _messageTypeName, elapsedTime);
    _messagingMetrics?.ExceptionHappened(envelope.Failure);
}
```

## Logging Infrastructure

### Structured Logging

The middleware uses high-performance logger message generators:

```csharp
[LoggerMessage(
    EventId = 1,
    Level = LogLevel.Debug,
    Message = "{MessageType} received. Message: {@Message}")]
private static partial void LogRequestReceived(ILogger logger, string messageType, object? message);

[LoggerMessage(
    EventId = 2,
    Level = LogLevel.Debug,
    Message = "{MessageType} completed in {MessageExecutionTime}")]
private static partial void LogRequestCompleted(ILogger logger, string messageType, TimeSpan messageExecutionTime);

[LoggerMessage(
    EventId = 3,
    Level = LogLevel.Error,
    Message = "{MessageType} failed after {MessageExecutionTime}")]
private static partial void LogRequestFailed(ILogger logger, Exception ex, string messageType, TimeSpan messageExecutionTime);
```

### Event ID Structure

- **Event ID 1**: Message received (Debug level)
- **Event ID 2**: Message completed successfully (Debug level)  
- **Event ID 3**: Message processing failed (Error level)

## Metrics Integration

### MessagingMetrics Integration

The middleware integrates with the messaging metrics system:

```csharp
var metricName = envelope.GetMessageName(fullName: true);
_messagingMetrics = meterStore.GetOrCreateMetrics(metricName);
```

### Collected Metrics

- **Message Received Count**: Incremented on message receipt
- **Processing Duration**: Recorded for performance analysis
- **Exception Tracking**: Captured for failure analysis

### Performance Monitoring

The middleware enables:

- **Throughput Analysis**: Messages processed per time unit
- **Latency Tracking**: Processing time distribution
- **Error Rate Monitoring**: Failure percentage calculation
- **Performance Regression Detection**: Baseline comparison

## Middleware State Management

### Instance Variables

```csharp
private string _messageTypeName = string.Empty;
private long _startedTime;
private MessagingMetrics? _messagingMetrics;
```

### Thread Safety

The middleware maintains per-message state safely through:

- **Instance Isolation**: Each message gets its own middleware instance
- **Immutable Capture**: Message type and start time captured atomically
- **Metrics Reference**: Safe reference to metrics store

## Integration with Wolverine

### Automatic Registration

The middleware is automatically applied through messaging policies:

```csharp
public partial class RequestPerformanceMiddleware
{
    public void Before(ILogger logger, Envelope envelope, MessagingMeterStore meterStore) { }
    public void Finally(ILogger logger, Envelope envelope) { }
}
```

### Pipeline Integration

- **Before**: Executed before message handler
- **Finally**: Executed after handler completion (success or failure)
- **Exception Safe**: Metrics recorded even if handler throws