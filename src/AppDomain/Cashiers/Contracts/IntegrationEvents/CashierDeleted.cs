// Copyright (c) ORG_NAME. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace AppDomain.Cashiers.Contracts.IntegrationEvents;

/// <summary>
///     Published when a cashier is successfully deleted from the AppDomain system.
///     This event contains the deleted cashier identifier and partition key information for proper message routing.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="CashierId">Unique identifier of the deleted cashier</param>
/// <param name="DeletedAt">Date and time when the cashier was deleted (UTC)</param>
/// <remarks>
///     ## When It's Triggered
///
///     This event is published when:
///     - The cashier deletion process completes successfully
///     - The cashier has been removed from the database
///     - All related cleanup operations are complete
///
///     ## Event Usage
///
///     This event can be used by other services to:
///     - Remove cashier from operational systems
///     - Archive historical transaction data
///     - Clean up related authentication records
///     - Notify dependent services of cashier removal
/// </remarks>
[EventTopic<Guid>]
public record CashierDeleted(
    [PartitionKey] Guid TenantId,
    Guid CashierId,
    DateTime DeletedAt
) : IDistributedEvent;
