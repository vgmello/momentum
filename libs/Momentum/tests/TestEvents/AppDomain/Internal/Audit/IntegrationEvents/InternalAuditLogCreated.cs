// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;

namespace AppDomain.Internal.Audit.IntegrationEvents;

/// <summary>
///     Published internally when audit log entries are created for compliance tracking
/// </summary>
/// <param name="TenantId">Identifier of the tenant for audit isolation</param>
/// <param name="UserId">Identifier of the user who performed the action</param>
/// <param name="Action">Description of the action that was performed</param>
/// <param name="Resource">Resource that was affected by the action</param>
/// <param name="Timestamp">When the audited action occurred</param>
/// <param name="Metadata">Additional metadata about the action in JSON format</param>
/// <remarks>
///     ## When It's Triggered
/// 
///     This internal event is published when:
///     - Sensitive Momentum are performed that require audit logging
///     - Compliance-related actions need to be tracked
///     - Internal system actions require monitoring
/// 
///     ## Security Notice
/// 
///     This is an internal event that should not be exposed to external systems.
///     It contains sensitive audit information that must remain within the system boundary.
/// 
///     ## Processing Requirements
/// 
///     - Event must be processed within the same security context
///     - Audit data must be encrypted at rest
///     - Access requires elevated permissions
/// </remarks>
[EventTopic<AuditLog>(Internal = true)]
public sealed record InternalAuditLogCreated(
    [PartitionKey] Guid TenantId,
    string UserId,
    string Action,
    string Resource,
    DateTime Timestamp,
    string Metadata
);

/// <summary>
///     Represents an audit log entry
/// </summary>
public record AuditLog
{
    /// <summary>Unique identifier for the audit log entry</summary>
    public required Guid Id { get; init; }

    /// <summary>Type of action that was audited</summary>
    public required string ActionType { get; init; }
}
