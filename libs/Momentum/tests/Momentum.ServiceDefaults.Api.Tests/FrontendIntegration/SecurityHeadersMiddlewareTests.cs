// Copyright (c) Momentum .NET. All rights reserved.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Momentum.ServiceDefaults.Api.FrontendIntegration;

namespace Momentum.ServiceDefaults.Api.Tests.FrontendIntegration;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task UseSecurityHeaders_ShouldAddAllDefaultHeaders()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.Configure<SecurityHeaderSettings>(_ => { });
        var app = builder.Build();
        app.UseSecurityHeaders();
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
    public async Task UseSecurityHeaders_ShouldUseConfiguredValues()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.Configure<SecurityHeaderSettings>(s =>
        {
            s.XFrameOptions = "SAMEORIGIN";
            s.ContentSecurityPolicy = "default-src 'self'; script-src 'self'";
        });
        var app = builder.Build();
        app.UseSecurityHeaders();
        app.MapGet("/test", () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();
        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        response.Headers.GetValues("X-Frame-Options").ShouldContain("SAMEORIGIN");
        response.Headers.GetValues("Content-Security-Policy").ShouldContain("default-src 'self'; script-src 'self'");

        await app.StopAsync(TestContext.Current.CancellationToken);
    }
}
