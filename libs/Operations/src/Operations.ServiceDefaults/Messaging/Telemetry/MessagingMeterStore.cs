// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Operations.Extensions.Abstractions.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Operations.ServiceDefaults.Messaging.Telemetry;

public class MessagingMeterStore([FromKeyedServices(MessagingMeterStore.MessagingMeterKey)] Meter meter)
{
    public const string MessagingMeterKey = "App.Messaging.Meter";

    private readonly ConcurrentDictionary<string, MessagingMetrics> _metrics = new();

    public MessagingMetrics GetOrCreateMetrics(string messageType)
    {
        return _metrics.GetOrAdd(messageType, static (key, m) => CreateMessagingMetrics(key, m), meter);
    }

    private static MessagingMetrics CreateMessagingMetrics(string messageType, Meter meter)
    {
        var metricName = string.Join('.', messageType.Split('.').Select(s => s.ToSnakeCase()));

        metricName = metricName switch
        {
            _ when metricName.EndsWith("_command") => metricName[..^8],
            _ when metricName.EndsWith("_query") => metricName[..^6],
            _ => metricName
        };

        return new MessagingMetrics(metricName, meter);
    }
}
