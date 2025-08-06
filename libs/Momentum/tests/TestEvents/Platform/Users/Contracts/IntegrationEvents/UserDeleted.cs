using Momentum.Extensions.Abstractions.Messaging;

namespace Platform.Users.Contracts.IntegrationEvents;

/// <summary>
/// Published when a user account is permanently deleted
/// </summary>
/// <param name="TenantId">Tenant identifier</param>
/// <param name="UserId">User identifier</param>
/// <param name="DeletedAt">Deletion timestamp</param>
[EventTopic<UserDeleted>]
public sealed record UserDeleted(
    [PartitionKey(Order = 0)] Guid TenantId,
    string UserId,
    DateTime DeletedAt
);
