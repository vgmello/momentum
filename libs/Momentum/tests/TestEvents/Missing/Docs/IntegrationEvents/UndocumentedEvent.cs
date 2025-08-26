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
public record Missing;