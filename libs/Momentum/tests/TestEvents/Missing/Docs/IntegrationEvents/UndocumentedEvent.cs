// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.Missing.Docs.IntegrationEvents;

[EventTopic<Missing>(Internal = true)]
public sealed record UndocumentedEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    string Data
);

/// <summary>
/// Represents missing documentation scenarios
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Sonar", "S2094", Justification = "Intentionally empty record used as domain marker for test scenarios")]
public record Missing;