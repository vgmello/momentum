// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.Extensions.Logging;
using Operations.ServiceDefaults.Messaging.Telemetry;
using Operations.ServiceDefaults.Messaging.Wolverine;
using System.Diagnostics;
using Wolverine;

namespace Operations.ServiceDefaults.Messaging.Middlewares;

/// <summary>
///     Wolverine middleware that tracks message processing performance and logs execution metrics.
/// </summary>
/// <remarks>
///     This middleware:
///     <list type="bullet">
///         <item>Records message processing start and end times</item>
///         <item>Logs message receipt, completion, and failures</item>
///         <item>Updates messaging metrics for monitoring</item>
///         <item>Tracks message processing duration</item>
///     </list>
///     The middleware is automatically applied to all message handlers via the messaging policy.
/// </remarks>
public partial class RequestPerformanceMiddleware
{
    private string _messageTypeName = string.Empty;
    private long _startedTime;
    private MessagingMetrics? _messagingMetrics;

    /// <summary>
    ///     Executes before message processing begins.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="envelope">The message envelope containing metadata.</param>
    /// <param name="meterStore">The metrics store for recording telemetry.</param>
    /// <remarks>
    ///     This method captures the start time, logs message receipt, and increments
    ///     the message received counter in the metrics system.
    /// </remarks>
    public void Before(ILogger logger, Envelope envelope, MessagingMeterStore meterStore)
    {
        _messageTypeName = envelope.GetMessageName();
        _startedTime = Stopwatch.GetTimestamp();

        LogRequestReceived(logger, _messageTypeName, envelope.Message);

        var metricName = envelope.GetMessageName(fullName: true);
        _messagingMetrics = meterStore.GetOrCreateMetrics(metricName);

        _messagingMetrics.MessageReceived();
    }

    /// <summary>
    ///     Executes after message processing completes (successfully or with failure).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="envelope">The message envelope containing metadata and failure information.</param>
    /// <remarks>
    ///     This method calculates the elapsed time, logs the outcome (success or failure),
    ///     and records the processing duration and any exceptions in the metrics system.
    /// </remarks>
    public void Finally(ILogger logger, Envelope envelope)
    {
        var elapsedTime = Stopwatch.GetElapsedTime(_startedTime);
        _messagingMetrics?.RecordProcessingTime(elapsedTime);

        if (envelope.Failure is null)
        {
            LogRequestCompleted(logger, _messageTypeName, elapsedTime);
        }
        else
        {
            LogRequestFailed(logger, envelope.Failure, _messageTypeName, elapsedTime);
            _messagingMetrics?.ExceptionHappened(envelope.Failure);
        }
    }

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
}
