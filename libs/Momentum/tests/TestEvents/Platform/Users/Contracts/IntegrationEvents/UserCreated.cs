// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace Platform.Users.Contracts.IntegrationEvents;

/// <summary>
///     Published when a new user account is created
/// </summary>
/// <param name="TenantId">Tenant identifier</param>
/// <param name="UserId">User identifier</param>
/// <param name="Email">User email address</param>
[EventTopic<UserCreated>]
public sealed record UserCreated(
    [PartitionKey(Order = 0)] Guid TenantId,
    string UserId,
    string Email
);
