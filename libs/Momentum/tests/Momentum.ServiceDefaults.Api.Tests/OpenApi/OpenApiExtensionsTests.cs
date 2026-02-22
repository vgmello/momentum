// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Momentum.ServiceDefaults.Api.OpenApi.Extensions;

namespace Momentum.ServiceDefaults.Api.Tests.OpenApi;

public class OpenApiExtensionsTests
{
    [Fact]
    public void ConfigureOpenApiDefaults_WithNullConfiguration_ShouldNotThrow()
    {
        var options = new OpenApiOptions();

        var result = Should.NotThrow(() => options.ConfigureOpenApiDefaults(configuration: null));

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureOpenApiDefaults_WithEmptyConfiguration_ShouldNotThrow()
    {
        var options = new OpenApiOptions();
        var config = new ConfigurationBuilder().Build();

        var result = Should.NotThrow(() => options.ConfigureOpenApiDefaults(config));

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureOpenApiDefaults_ShouldReturnSameOptionsInstance()
    {
        var options = new OpenApiOptions();
        var config = new ConfigurationBuilder().Build();

        var result = options.ConfigureOpenApiDefaults(config);

        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ConfigureOpenApiDefaults_WithSecuritySchemeConfig_ShouldNotThrow()
    {
        var options = new OpenApiOptions();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenApi:SecuritySchemes:Bearer:Type"] = "Http",
                ["OpenApi:SecuritySchemes:Bearer:Scheme"] = "bearer",
                ["OpenApi:SecuritySchemes:Bearer:BearerFormat"] = "JWT"
            })
            .Build();

        var result = Should.NotThrow(() => options.ConfigureOpenApiDefaults(config));

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureOpenApiDefaults_WithNoDefaultConfiguration_ShouldNotThrow()
    {
        var options = new OpenApiOptions();

        var result = Should.NotThrow(() => options.ConfigureOpenApiDefaults());

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureOpenApiDefaults_WithSchemeWithoutType_ShouldNotThrow()
    {
        var options = new OpenApiOptions();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenApi:SecuritySchemes:Invalid:Scheme"] = "bearer",
                ["OpenApi:SecuritySchemes:Invalid:BearerFormat"] = "JWT"
            })
            .Build();

        var result = Should.NotThrow(() => options.ConfigureOpenApiDefaults(config));

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureOpenApiDefaults_WithMultipleSchemes_ShouldNotThrow()
    {
        var options = new OpenApiOptions();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenApi:SecuritySchemes:Bearer:Type"] = "Http",
                ["OpenApi:SecuritySchemes:Bearer:Scheme"] = "bearer",
                ["OpenApi:SecuritySchemes:ApiKey:Type"] = "ApiKey",
                ["OpenApi:SecuritySchemes:ApiKey:Description"] = "API Key authentication"
            })
            .Build();

        var result = Should.NotThrow(() => options.ConfigureOpenApiDefaults(config));

        result.ShouldNotBeNull();
    }
}
