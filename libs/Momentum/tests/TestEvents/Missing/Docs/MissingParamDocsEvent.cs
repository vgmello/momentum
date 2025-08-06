using Momentum.Extensions.Abstractions.Messaging;

namespace Missing.Docs;

/// <summary>
/// Event with missing parameter documentation
/// </summary>
[EventTopic<MissingParamDocsEvent>]
public sealed record MissingParamDocsEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    string Data
);
