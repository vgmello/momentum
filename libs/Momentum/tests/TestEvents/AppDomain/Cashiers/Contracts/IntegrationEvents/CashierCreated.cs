// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace TestEvents.AppDomain.Cashiers.Contracts.IntegrationEvents;

/// <summary>
///     Published when a new cashier is successfully created in the AppDomain system. This event contains the complete cashier data and
///     partition
///     key information for proper message routing.
/// </summary>
/// <param name="TenantId">Identifier of the tenant that owns the cashier</param>
/// <param name="PartitionKeyTest">Additional partition key for message routing</param>
/// <param name="Cashier">Complete cashier object containing all cashier data and configuration</param>
/// <remarks>
///     ## When It's Triggered
/// 
///     This event is published when:
///     -   The cashier creation process completes successfully
/// 
///     ## Some other event data
/// 
///     Some other event data text
/// </remarks>
[EventTopic<Cashier>]
public sealed record CashierCreated(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] int PartitionKeyTest,
    Cashier Cashier
);

/// <summary>
///     Represents a cashier in the AppDomain system
/// </summary>
public record Cashier
{
    /// <summary>Unique identifier for the cashier</summary>
    public required Guid Id { get; init; }

    /// <summary>Display name of the cashier</summary>
    public required string Name { get; init; }

    /// <summary>Email address for the cashier</summary>
    public string? Email { get; init; }

    /// <summary>Whether the cashier is currently active</summary>
    public bool IsActive { get; init; } = true;
}
