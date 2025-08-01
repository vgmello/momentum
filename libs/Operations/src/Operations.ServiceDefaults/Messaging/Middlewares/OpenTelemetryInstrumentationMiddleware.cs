// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.Abstractions.Messaging;
using Operations.ServiceDefaults.Messaging.Wolverine;
using Operations.ServiceDefaults.Messaging.Kafka;
using System.Diagnostics;
using Wolverine;

namespace Operations.ServiceDefaults.Messaging.Middlewares;

/// <summary>
///     Wolverine middleware that provides OpenTelemetry distributed tracing for message processing.
/// </summary>
/// <remarks>
///     This middleware creates activity spans for each message being processed, enabling:
///     <list type="bullet">
///         <item>Distributed tracing across service boundaries</item>
///         <item>Message type and operation type tagging</item>
///         <item>Error tracking and status reporting</item>
///         <item>Correlation of message processing in observability platforms</item>
///     </list>
/// </remarks>
public static class OpenTelemetryInstrumentationMiddleware
{
    /// <summary>
    ///     Starts a new activity for message processing.
    /// </summary>
    /// <param name="activitySource">The activity source for creating spans.</param>
    /// <param name="envelope">The message envelope containing metadata.</param>
    /// <returns>The started activity, or null if tracing is not enabled.</returns>
    /// <remarks>
    ///     This method creates an activity with tags for:
    ///     <list type="bullet">
    ///         <item>message.id - The unique message identifier</item>
    ///         <item>message.name - The full type name of the message</item>
    ///         <item>operation.type - Whether it's a command or query</item>
    ///         <item>message.source - The source of the message (if available)</item>
    ///     </list>
    /// </remarks>
    public static Activity? Before(ActivitySource activitySource, Envelope envelope)
    {
        var activityName = envelope.GetMessageName();

        var parentTraceId = ExtractParentTraceIdFromIncomingMessage(envelope);
        var activity = activitySource.StartActivity(activityName, ActivityKind.Consumer, parentId: parentTraceId);

        if (activity is null)
            return null;

        activity.SetTag("message.id", envelope.Id.ToString());
        activity.SetTag("messaging.destination", envelope.Destination?.ToString() ?? "unknown");

        if (envelope.Message is not null)
        {
            activity.SetTag("message.name", envelope.GetMessageName(fullName: true));

            if (IsCommand(envelope.Message))
            {
                activity.SetTag("operation.type", "command");
            }
            else if (IsQuery(envelope.Message))
            {
                activity.SetTag("operation.type", "query");
            }
        }

        if (!string.IsNullOrEmpty(envelope.Source))
        {
            activity.SetTag("message.source", envelope.Source);
        }

        return activity;
    }

    /// <summary>
    ///     Completes the activity and sets its final status.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="envelope">The message envelope containing processing results.</param>
    /// <remarks>
    ///     Sets the activity status to OK for successful processing or Error with
    ///     exception details if the message processing failed.
    /// </remarks>
    public static void Finally(Activity? activity, Envelope envelope)
    {
        if (activity is null)
            return;

        if (envelope.Failure is null)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error, envelope.Failure.Message);
            activity.SetTag("error.type", envelope.Failure.GetType().Name);
        }

        activity.Stop();
    }

    private static bool IsCommand(object message) =>
        message.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

    private static bool IsQuery(object message) =>
        message.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

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
}
