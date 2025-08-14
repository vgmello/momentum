// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;

namespace AppDomain.Cashiers.Contracts.IntegrationEvents;

/// <summary>
/// Published when a cashier is successfully updated in the AppDomain system.
/// This event contains the updated cashier data and partition key information for proper message routing.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="PartitionKeyTest">Additional partition key for message routing</param>
/// <param name="Cashier">Updated cashier object containing all current cashier data</param>
/// <remarks>
/// ## When It's Triggered
///
/// This event is published when:
/// - The cashier update process completes successfully
/// - All validation rules pass for the updated data
/// - The updated cashier data has been persisted to the database
///
/// ## Event Usage
///
/// This event can be used by other services to:
/// - Update cached cashier information
/// - Synchronize cashier data across systems
/// - Notify dependent services of cashier changes
/// </remarks>
[EventTopic<Cashier>]
public record CashierUpdated(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] int PartitionKeyTest,
    Cashier Cashier
);
