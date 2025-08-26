// Copyright (c) Momentum .NET. All rights reserved.

namespace TestEvents.AppDomain.Types.Contracts.Enums;

/// <summary>
///     Enumeration of possible payment status values
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    ///     Payment is awaiting processing
    /// </summary>
    Pending,

    /// <summary>
    ///     Payment is currently being processed
    /// </summary>
    Processing,

    /// <summary>
    ///     Payment processing completed successfully
    /// </summary>
    Completed,

    /// <summary>
    ///     Payment processing failed
    /// </summary>
    Failed,

    /// <summary>
    ///     Payment was cancelled before processing
    /// </summary>
    Cancelled
}
