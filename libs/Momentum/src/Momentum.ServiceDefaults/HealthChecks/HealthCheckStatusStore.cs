// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Momentum.ServiceDefaults.HealthChecks;

/// <summary>
///     Stores the last known health check status for the application.
/// </summary>
/// <remarks>
///     This store is typically used to track the application's health status
///     across multiple health check executions, allowing components to react
///     to health status changes or query the last known state without
///     triggering a new health check.
///     <para>
///         This class is thread-safe and can be accessed concurrently from multiple
///         health check requests without synchronization issues.
///     </para>
/// </remarks>
public class HealthCheckStatusStore
{
    private int _lastHealthStatus = (int)HealthStatus.Healthy;

    /// <summary>
    ///     Gets or sets the last recorded health status.
    /// </summary>
    /// <value>
    ///     The most recent health status from the health check system.
    ///     Defaults to <see cref="HealthStatus.Healthy" />.
    /// </value>
    /// <remarks>
    ///     This property is thread-safe and uses Interlocked operations
    ///     to ensure atomicity and visibility across threads.
    /// </remarks>
    public HealthStatus LastHealthStatus
    {
        get => (HealthStatus)Interlocked.CompareExchange(ref _lastHealthStatus, 0, 0);
        set => Interlocked.Exchange(ref _lastHealthStatus, (int)value);
    }
}
