// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Momentum.ServiceDefaults.Messaging.Telemetry;

/// <summary>
///     Provides a store for messaging metrics, creating and caching metrics instances per message type.
/// </summary>
public class MessagingMeterStore([FromKeyedServices(MessagingMeterStore.MessagingMeterKey)] Meter meter)
{
    /// <summary>
    ///     The key used to register the messaging meter in the DI container.
    /// </summary>
    public const string MessagingMeterKey = "App.Messaging.Meter";

    private const string CommandHandlerSuffix = "_command_handler";
    private const string QueryHandlerSuffix = "_query_handler";
    private const string CommandSuffix = "_command";
    private const string QuerySuffix = "_query";

    private readonly ConcurrentDictionary<string, MessagingMetrics> _metrics = new();

    /// <summary>
    ///     Gets or creates metrics for the specified message type.
    /// </summary>
    /// <param name="messageType">The full type name of the message.</param>
    /// <returns>The metrics instance for the message type.</returns>
    public MessagingMetrics GetOrCreateMetrics(string messageType)
    {
        return _metrics.GetOrAdd(messageType, static (key, m) => CreateMessagingMetrics(key, m), meter);
    }

    private static MessagingMetrics CreateMessagingMetrics(string messageType, Meter meter)
    {
        var metricName = string.Join('.', messageType.Split('.').Select(s => s.ToSnakeCase()));
        metricName = NormalizeMetricName(metricName);
        return new MessagingMetrics(metricName, meter);
    }

    private static string NormalizeMetricName(string metricName)
    {
        if (metricName.Contains(QueryHandlerSuffix))
            metricName = metricName.Replace(QueryHandlerSuffix, string.Empty);

        if (metricName.Contains(CommandHandlerSuffix))
            metricName = metricName.Replace(CommandHandlerSuffix, string.Empty);

        if (metricName.EndsWith(CommandSuffix))
            return metricName[..^CommandSuffix.Length];

        if (metricName.EndsWith(QuerySuffix))
            return metricName[..^QuerySuffix.Length];

        return metricName;
    }
}
