// Copyright (c) ABCDEG. All rights reserved.

using System.Diagnostics.Metrics;

namespace Operations.ServiceDefaults.Messaging.Telemetry;

public class MessagingMetrics(string metricName, Meter meter)
{
    private readonly Counter<long> _messagesReceived = meter.CreateCounter<long>(
        name: $"{metricName}.count",
        unit: "invocations",
        description: "Number of times a command/query has been invoked.");

    private readonly Histogram<double> _messageProcessingTime = meter.CreateHistogram<double>(
        name: $"{metricName}.duration",
        unit: "ms",
        description: "Execution duration of the command.");

    private readonly Counter<long> _exceptionsCount = meter.CreateCounter<long>(
        name: $"{metricName}.exceptions",
        unit: "exceptions",
        description: "Number of times the command processing resulted in an exception.");

    public void MessageReceived() => _messagesReceived.Add(1);

    public void RecordProcessingTime(TimeSpan duration) => _messageProcessingTime.Record(duration.TotalMilliseconds);

    public void ExceptionHappened(Exception exception) => _exceptionsCount.Add(1,
        new KeyValuePair<string, object?>("exception.type", exception.GetType().Name));
}
