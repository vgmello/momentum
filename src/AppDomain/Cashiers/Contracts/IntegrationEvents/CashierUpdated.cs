// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;

namespace AppDomain.Cashiers.Contracts.IntegrationEvents;

/// <summary>
///     Published when an existing cashier's information is successfully updated in the AppDomain system. This event signals that cashier data
///     has been modified and dependent systems should refresh their local copies.
/// </summary>
/// <remarks>
///     ## When It's Triggered
///
///     This event is published when:
///     - Cashier profile information is modified (name, email, permissions, etc.)
///     - Cashier status changes (active/inactive, role updates)
///     - Configuration settings for the cashier are updated
///     - The update operation completes successfully
///
///     ## Data Synchronization
///
///     This event ensures:
///     - All systems maintain consistent cashier information
///     - Cached data is invalidated and refreshed
///     - Downstream services can react to cashier changes appropriately
///     - Audit trails are maintained for compliance purposes
/// </remarks>
/// <param name="TenantId">The unique identifier of the tenant that owns the updated cashier</param>
/// <param name="CashierId">The unique identifier of the cashier that was updated</param>
[EventTopic<Cashier>]
public record CashierUpdated([PartitionKey] Guid TenantId, Guid CashierId);
