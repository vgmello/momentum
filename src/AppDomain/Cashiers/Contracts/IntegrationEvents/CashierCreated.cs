// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;

namespace AppDomain.Cashiers.Contracts.IntegrationEvents;

/// <summary>
///     Published when a new cashier is successfully created in the AppDomain system. This event contains the complete cashier data and partition
///     key information for proper message routing.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="PartitionKeyTest">Additional partition key for message routing</param>
/// <param name="Cashier">Cashier object containing all cashier data and configuration</param>
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
public record CashierCreated(
    [PartitionKey(Order = 0)] Guid TenantId,
    [PartitionKey(Order = 1)] int PartitionKeyTest,
    Cashier Cashier
);
