// Copyright (c) Momentum .NET. All rights reserved.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Momentum.ServiceDefaults.HealthChecks;

namespace Momentum.ServiceDefaults.Api.Tests;

public class HealthCheckSetupExtensionsTests
{
    private static async Task<IHost> StartAppAsync(string environment, HealthStatus status)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton<HealthCheckStatusStore>();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddHealthChecks().AddCheck("test", () => new HealthCheckResult(status));

        var app = builder.Build();

        // Make requests look like loopback so RequireHost("localhost") + the LocalhostEndpointFilter pass.
        app.Use(async (context, next) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            await next(context);
        });

        app.MapDefaultHealthCheckEndpoints();

        await app.StartAsync(TestContext.Current.CancellationToken);

        return app;
    }

    [Fact]
    public async Task Status_OnFreshApp_ReturnsCachedHealthyLiveness()
    {
        using var app = await StartAppAsync("Production", HealthStatus.Healthy);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/status", TestContext.Current.CancellationToken);

        // No health check has run yet, so the store returns its default (Healthy).
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("Healthy");
    }

    [Fact]
    public async Task HealthInternal_InDevelopment_WritesDetailedJsonReport()
    {
        using var app = await StartAppAsync("Development", HealthStatus.Healthy);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/health/internal", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.TrimStart().ShouldStartWith("{");
        body.ShouldContain("Healthy");
    }

    [Fact]
    public async Task HealthInternal_InProduction_WritesStatusTextOnly()
    {
        using var app = await StartAppAsync("Production", HealthStatus.Healthy);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/health/internal", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("Healthy");
    }

    [Fact]
    public async Task HealthInternal_WhenUnhealthy_Returns503()
    {
        using var app = await StartAppAsync("Production", HealthStatus.Unhealthy);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/health/internal", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Status_AfterUnhealthyCheckRuns_ReflectsCachedUnhealthy()
    {
        using var app = await StartAppAsync("Production", HealthStatus.Unhealthy);
        var client = app.GetTestClient();

        // Running a failing health check caches Unhealthy in the store...
        await client.GetAsync("/health/internal", TestContext.Current.CancellationToken);

        // ...which /status then reports as a 503.
        var response = await client.GetAsync("/status", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("Unhealthy");
    }
}
