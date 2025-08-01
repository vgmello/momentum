using Operations.Extensions.Abstractions.Messaging;

namespace Missing.Docs;

[EventTopic<UndocumentedEvent>]
public sealed record UndocumentedEvent(
    [PartitionKey(Order = 0)] Guid TenantId,
    string Data
);