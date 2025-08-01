// Copyright (c) ABCDEG. All rights reserved.

using AppDomain.Cashiers.Contracts.Models;

namespace AppDomain.Cashiers.Contracts.IntegrationEvents;

/// <summary>
///     Published when a cashier is successfully deleted from the AppDomain system. This event contains the essential identifiers for the deleted
///     cashier and ensures proper cleanup and audit trail.
/// </summary>
/// <remarks>
///     ## When It's Triggered
///
///     This event is published when:
///     - A cashier is permanently removed from the system
///     - The deletion process completes successfully
///     - All associated cleanup operations are finished
///
///     ## Impact and Considerations
///
///     When this event is published:
///     - All dependent systems should remove references to this cashier
///     - Any cached data related to this cashier should be invalidated
///     - Audit logs should record the deletion for compliance purposes
/// </remarks>
/// <param name="TenantId">The unique identifier of the tenant that owned the deleted cashier</param>
/// <param name="CashierId">The unique identifier of the cashier that was deleted</param>
[EventTopic<Cashier>]
public record CashierDeleted([PartitionKey] Guid TenantId, Guid CashierId);
