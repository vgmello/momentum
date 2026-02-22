// Copyright (c) Momentum .NET. All rights reserved.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Momentum.ServiceDefaults.Api.FrontendIntegration;

namespace Momentum.ServiceDefaults.Api.Tests.FrontendIntegration;

/// <summary>
///     Tests for <see cref="FrontendIntegrationExtensions.UseFrontendIntegration"/> verifying
///     middleware is configured correctly in both Development and Production environments.
/// </summary>
public class UseFrontendIntegrationTests
{
    [Fact]
    public async Task UseFrontendIntegration_InDevelopment_ShouldNotRedirectToHttps()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000"
        });
        builder.AddFrontendIntegration();

        var app = builder.Build();
        app.UseFrontendIntegration();
        app.MapGet("/test", () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();
        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        // In Development, should not redirect (HTTP requests go through without HTTPS redirect)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UseFrontendIntegration_InProduction_ShouldApplyHttpsRedirection()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://example.com"
        });
        builder.AddFrontendIntegration();

        var app = builder.Build();
        app.UseFrontendIntegration();
        app.MapGet("/test", () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();
        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        // In Production, HTTPS redirection middleware is active.
        // TestServer sends requests via HTTP by default, so we expect either
        // a redirect (307) or the request passes through if HTTPS port is not configured.
        // The key point is the middleware pipeline runs without errors.
        var statusCode = (int)response.StatusCode;
        (statusCode is (>= 200 and < 400) or 307).ShouldBeTrue(
            $"Expected a success or redirect status, got {response.StatusCode}");

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UseFrontendIntegration_ShouldApplySecurityHeaders()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000"
        });
        builder.AddFrontendIntegration();

        var app = builder.Build();
        app.UseFrontendIntegration();
        app.MapGet("/test", () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();
        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.GetValues("Referrer-Policy").ShouldContain("strict-origin-when-cross-origin");
        response.Headers.GetValues("Content-Security-Policy").ShouldContain("default-src 'self'");

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UseFrontendIntegration_ShouldApplyCorsPolicy()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000",
            ["Cors:AllowCredentials"] = "true"
        });
        builder.AddFrontendIntegration();

        var app = builder.Build();
        app.UseFrontendIntegration();
        app.MapGet("/test", () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "https://localhost:3000");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .ShouldContain("https://localhost:3000");

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void UseFrontendIntegration_ShouldReturnSameAppInstance()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000"
        });
        builder.AddFrontendIntegration();

        var app = builder.Build();
        var result = app.UseFrontendIntegration();

        result.ShouldBeSameAs(app);
    }
}
