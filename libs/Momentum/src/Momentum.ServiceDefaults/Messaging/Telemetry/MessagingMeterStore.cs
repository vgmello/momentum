// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Momentum.Extensions.Abstractions.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Momentum.ServiceDefaults.Messaging.Telemetry;

public class MessagingMeterStore([FromKeyedServices(MessagingMeterStore.MessagingMeterKey)] Meter meter)
{
    public const string MessagingMeterKey = "App.Messaging.Meter";

    private const string CommandHandlerPattern = "_command_handler";
    private const string QueryHandlerPattern = "_query_handler";
    private const string CommandPattern = "_command";
    private const string QueryPattern = "_query";

    private readonly ConcurrentDictionary<string, MessagingMetrics> _metrics = new();

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
        if (metricName.Contains(QueryHandlerPattern))
            metricName = metricName.Replace(QueryHandlerPattern, string.Empty);

        if (metricName.Contains(CommandHandlerPattern))
            metricName = metricName.Replace(CommandHandlerPattern, string.Empty);

        if (metricName.EndsWith(CommandPattern))
            return metricName[..^CommandPattern.Length];

        if (metricName.EndsWith(QueryPattern))
            return metricName[..^QueryPattern.Length];

        return metricName;
    }
}
