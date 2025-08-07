// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace Missing.Docs;

[EventTopic<UndocumentedEvent>]
public sealed record UndocumentedEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    string Data
);
