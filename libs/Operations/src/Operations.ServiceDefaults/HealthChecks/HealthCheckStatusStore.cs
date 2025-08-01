// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Operations.ServiceDefaults.HealthChecks;

/// <summary>
///     Stores the last known health check status for the application.
/// </summary>
/// <remarks>
///     This store is typically used to track the application's health status
///     across multiple health check executions, allowing components to react
///     to health status changes or query the last known state without
///     triggering a new health check.
/// </remarks>
public class HealthCheckStatusStore
{
    /// <summary>
    ///     Gets or sets the last recorded health status.
    /// </summary>
    /// <value>
    ///     The most recent health status from the health check system.
    ///     Defaults to <see cref="HealthStatus.Healthy" />.
    /// </value>
    public HealthStatus LastHealthStatus { get; set; } = HealthStatus.Healthy;
}
