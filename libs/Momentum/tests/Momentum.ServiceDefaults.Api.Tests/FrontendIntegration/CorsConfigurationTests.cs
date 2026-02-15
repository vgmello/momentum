// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Momentum.ServiceDefaults.Api.FrontendIntegration;

namespace Momentum.ServiceDefaults.Api.Tests.FrontendIntegration;

public class CorsConfigurationTests
{
    [Fact]
    public void AddCorsFromConfiguration_ShouldRegisterCorsPolicy()
    {
        var config = new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000",
            ["Cors:AllowedOrigins:1"] = "https://example.com",
            ["Cors:AllowCredentials"] = "true"
        };

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(config);

        builder.AddCorsFromConfiguration();
        var app = builder.Build();

        var corsService = app.Services.GetRequiredService<ICorsService>();
        corsService.ShouldNotBeNull();

        var policyProvider = app.Services.GetRequiredService<ICorsPolicyProvider>();
        policyProvider.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddCorsFromConfiguration_ShouldApplyConfiguredOrigins()
    {
        var config = new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000",
            ["Cors:AllowCredentials"] = "true"
        };

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(config);
        builder.WebHost.UseTestServer();
        builder.AddCorsFromConfiguration();

        var app = builder.Build();
        app.UseCors(FrontendIntegrationExtensions.CorsPolicyName);
        app.MapGet("/test", () => "ok");

        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "https://localhost:3000");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldContain("https://localhost:3000");
        response.Headers.GetValues("Access-Control-Allow-Credentials").ShouldContain("true");

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddCorsFromConfiguration_ShouldRejectDisallowedOrigin()
    {
        var config = new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000",
            ["Cors:AllowCredentials"] = "true"
        };

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(config);
        builder.WebHost.UseTestServer();
        builder.AddCorsFromConfiguration();

        var app = builder.Build();
        app.UseCors(FrontendIntegrationExtensions.CorsPolicyName);
        app.MapGet("/test", () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "https://evil.com");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();

        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void AddCorsFromConfiguration_ShouldThrowWhenCredentialsWithoutOrigins()
    {
        var config = new Dictionary<string, string?>
        {
            ["Cors:AllowCredentials"] = "true"
        };

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(config);

        Should.Throw<InvalidOperationException>(() => builder.AddCorsFromConfiguration());
    }
}
