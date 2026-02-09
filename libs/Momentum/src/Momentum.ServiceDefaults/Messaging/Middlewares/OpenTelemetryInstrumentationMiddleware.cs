// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using Momentum.ServiceDefaults.Messaging.Wolverine;
using System.Collections.Concurrent;
using System.Diagnostics;
using Wolverine;

namespace Momentum.ServiceDefaults.Messaging.Middlewares;

/// <summary>
///     Wolverine middleware that provides OpenTelemetry distributed tracing for message processing.
/// </summary>
/// <remarks>
///     <!--@include: @code/messaging/telemetry-middleware-detailed.md#middleware-overview -->
/// </remarks>
public static class OpenTelemetryInstrumentationMiddleware
{
    private static readonly ConcurrentDictionary<Type, string?> OperationTypeCache = new();
    /// <summary>
    ///     Starts a new activity for message processing.
    /// </summary>
    /// <param name="activitySource">The activity source for creating spans.</param>
    /// <param name="envelope">The message envelope containing metadata.</param>
    /// <returns>The started activity, or null if tracing is not enabled.</returns>
    /// <remarks>
    ///     <!--@include: @code/messaging/telemetry-middleware-detailed.md#activity-creation -->
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

            var operationType = GetOperationType(envelope.Message.GetType());

            if (operationType is not null)
                activity.SetTag("operation.type", operationType);
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
    ///     <!--@include: @code/messaging/telemetry-middleware-detailed.md#activity-completion -->
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

        activity.Dispose();
    }

    private static string? GetOperationType(Type messageType)
    {
        return OperationTypeCache.GetOrAdd(messageType, static type =>
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                var genericDef = iface.GetGenericTypeDefinition();

                if (genericDef == typeof(ICommand<>))
                    return "command";

                if (genericDef == typeof(IQuery<>))
                    return "query";
            }

            return null;
        });
    }

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
