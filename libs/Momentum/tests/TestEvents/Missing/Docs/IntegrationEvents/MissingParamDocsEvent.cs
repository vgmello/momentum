// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.Missing.Docs.IntegrationEvents;

/// <summary>
///     Event with missing parameter documentation
/// </summary>
[EventTopic<MissingParamDocsEvent>]
public sealed record MissingParamDocsEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    string Data
);
