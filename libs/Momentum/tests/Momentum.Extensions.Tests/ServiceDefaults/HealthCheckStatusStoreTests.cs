// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Momentum.ServiceDefaults.HealthChecks;

namespace Momentum.Extensions.Tests.ServiceDefaults;

public class HealthCheckStatusStoreTests
{
    [Fact]
    public void DefaultStatus_ShouldBeHealthy()
    {
        var store = new HealthCheckStatusStore();

        store.LastHealthStatus.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void SetAndGet_ShouldReturnSetValue()
    {
        var store = new HealthCheckStatusStore();

        store.LastHealthStatus = HealthStatus.Degraded;

        store.LastHealthStatus.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public void SetToUnhealthy_ShouldReturnUnhealthy()
    {
        var store = new HealthCheckStatusStore();

        store.LastHealthStatus = HealthStatus.Unhealthy;

        store.LastHealthStatus.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public void SetToDegraded_ThenBackToHealthy_ShouldReturnHealthy()
    {
        var store = new HealthCheckStatusStore();

        store.LastHealthStatus = HealthStatus.Degraded;
        store.LastHealthStatus.ShouldBe(HealthStatus.Degraded);

        store.LastHealthStatus = HealthStatus.Healthy;
        store.LastHealthStatus.ShouldBe(HealthStatus.Healthy);
    }
}
