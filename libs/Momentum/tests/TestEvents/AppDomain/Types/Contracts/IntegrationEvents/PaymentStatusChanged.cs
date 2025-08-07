// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using AppDomain.Types.Contracts.Enums;

namespace AppDomain.Types.Contracts.IntegrationEvents;

/// <summary>
///     Published when payment status transitions occur with optional metadata and enum values
/// </summary>
/// <param name="TenantId">Identifier of the tenant that owns the payment</param>
/// <param name="PaymentId">Unique identifier for the payment being updated</param>
/// <param name="PreviousStatus">The payment status before this change occurred</param>
/// <param name="NewStatus">The new payment status after the change</param>
/// <param name="ProcessedAt">Optional timestamp when payment processing completed (null for pending statuses)</param>
/// <param name="Notes">Optional notes about the status change (null if no additional information)</param>
/// <param name="Amount">Optional payment amount (null for status changes that don't involve amounts)</param>
/// <param name="PaymentMethod">The method used for payment processing</param>
/// <param name="FailureReason">Optional reason for payment failure (null for successful payments)</param>
/// <remarks>
///     ## When It's Triggered
/// 
///     This event is published when:
///     - Payment status changes from one state to another
///     - Optional payment metadata is updated
///     - Nullable fields are conditionally populated
/// 
///     ## Type System Features
/// 
///     This event demonstrates:
///     - Nullable reference types with proper nullability annotations
///     - Enum types for type-safe status representation
///     - Optional nullable value types for conditional data
///     - Mixed required and optional fields
/// </remarks>
[EventTopic<PaymentStatusChanged>]
public sealed record PaymentStatusChanged(
    [PartitionKey(Order = 0)] Guid TenantId,
    string PaymentId,
    PaymentStatus PreviousStatus,
    PaymentStatus NewStatus,
    DateTime? ProcessedAt,
    string? Notes,
    decimal? Amount,
    PaymentMethod PaymentMethod,
    FailureReason? FailureReason
);
