// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.Missing.Docs;

/// <summary>
///     Event with malformed XML in documentation
/// </summary>
/// <remarks>
///     This has unclosed tags and malformed content
/// </remarks>
[EventTopic<MalformedXmlEvent>]
public sealed record MalformedXmlEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    string Data
);
