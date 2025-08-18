// Copyright (c) ORG_NAME. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;

namespace AppDomain.Cashiers.Contracts.IntegrationEvents;

/// <summary>
///     Published when a new cashier is successfully created in the AppDomain system. This event contains the complete cashier data and
///     partition
///     key information for proper message routing.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant</param>
/// <param name="Cashier">Cashier object containing all cashier data and configuration</param>
/// <remarks>
///     ## When It's Triggered
///
///     This event is published when:
///     - The cashier creation process completes successfully
///     - All validation rules pass for the new cashier data
///     - The cashier has been persisted to the database
///
///     ## Event Usage
///
///     This event can be used by other services to:
///     - Initialize cashier profiles in external systems
///     - Set up authentication and authorization
///     - Configure related business processes
///     - Update reporting and analytics systems
/// </remarks>
[EventTopic<Cashier>]
public record CashierCreated(
    [PartitionKey] Guid TenantId,
    Cashier Cashier
);
