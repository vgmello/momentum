// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Momentum.ServiceDefaults.Api.FrontendIntegration;

namespace Momentum.ServiceDefaults.Api.Tests.FrontendIntegration;

public class FrontendIntegrationExtensionsTests
{
    [Fact]
    public void AddFrontendIntegration_ShouldRegisterCorsAndSecurityHeaderServices()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddFrontendIntegration();
        var app = builder.Build();

        var corsService = app.Services.GetService<ICorsService>();
        corsService.ShouldNotBeNull();

        var headerSettings = app.Services.GetService<IOptions<SecurityHeaderSettings>>();
        headerSettings.ShouldNotBeNull();
        headerSettings.Value.ShouldNotBeNull();
    }
}
