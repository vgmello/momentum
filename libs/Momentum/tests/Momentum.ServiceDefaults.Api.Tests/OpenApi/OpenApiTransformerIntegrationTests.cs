// Copyright (c) Momentum .NET. All rights reserved.

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Momentum.ServiceDefaults.Api.OpenApi.Extensions;

namespace Momentum.ServiceDefaults.Api.Tests.OpenApi;

/// <summary>
///     Integration tests that exercise the OpenAPI transformer lambdas by generating
///     a real OpenAPI document from a minimal API application using a test server.
/// </summary>
public class OpenApiTransformerIntegrationTests
{
    private record TestModelWithNullable(string Name, string? Description, int Age, decimal Price);

    private record TestModelWithDecimal(decimal Amount, decimal? NullableAmount, string Label);

    private record SimpleModel(string Value);

    [Fact]
    public async Task NullableRequiredFix_ShouldRemoveNullablePropertiesFromRequired()
    {
        await using var app = await CreateAppWithDefaults(
            endpoints: app => app.MapGet("/test", () => new TestModelWithNullable("hello", null, 25, 1.23m)));

        var doc = await FetchOpenApiDocument(app);

        // Find the schema for TestModelWithNullable
        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");
        var schemaFound = false;

        foreach (var schema in schemas.EnumerateObject())
        {
            if (!schema.Name.Contains("TestModelWithNullable"))
                continue;

            schemaFound = true;

            // "required" should contain non-nullable properties but NOT "description" (which is nullable)
            if (schema.Value.TryGetProperty("required", out var required))
            {
                var requiredNames = required.EnumerateArray()
                    .Select(e => e.GetString())
                    .ToList();

                requiredNames.ShouldContain("name");
                requiredNames.ShouldContain("age");
                requiredNames.ShouldContain("price");
                requiredNames.ShouldNotContain("description",
                    "Nullable property 'description' should be removed from required array");
            }

            break;
        }

        schemaFound.ShouldBeTrue("TestModelWithNullable schema should exist in the OpenAPI document");
    }

    [Fact]
    public async Task DecimalFormatFix_ShouldSetDecimalFormatOnDecimalProperties()
    {
        await using var app = await CreateAppWithDefaults(
            endpoints: app => app.MapGet("/test", () => new TestModelWithDecimal(1.23m, 4.56m, "test")));

        var doc = await FetchOpenApiDocument(app);

        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");
        var decimalFormatFound = false;

        foreach (var schema in schemas.EnumerateObject())
        {
            if (!schema.Value.TryGetProperty("properties", out var properties))
                continue;

            foreach (var prop in properties.EnumerateObject())
            {
                if (prop.Name is "amount" or "nullableAmount")
                {
                    // The property itself or its inner schema should have format: "decimal"
                    var hasDecimalFormat = HasFormat(prop.Value, "decimal");
                    hasDecimalFormat.ShouldBeTrue(
                        $"Property '{prop.Name}' should have format 'decimal'");
                    decimalFormatFound = true;
                }

                if (prop.Name == "label")
                {
                    // String properties should NOT have decimal format
                    var hasDecimalFormat = HasFormat(prop.Value, "decimal");
                    hasDecimalFormat.ShouldBeFalse(
                        "String property 'label' should not have format 'decimal'");
                }
            }
        }

        decimalFormatFound.ShouldBeTrue("At least one decimal property should have been found");
    }

    [Fact]
    public async Task ServerUrlNormalization_ShouldNotCrashWhenNoServersConfigured()
    {
        await using var app = await CreateAppWithDefaults(
            endpoints: app => app.MapGet("/test", () => new SimpleModel("ok")));

        var doc = await FetchOpenApiDocument(app);

        // The document should be valid even without explicit server configuration
        doc.RootElement.GetProperty("openapi").GetString().ShouldNotBeNullOrEmpty();
        doc.RootElement.GetProperty("info").GetProperty("title").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ServerUrlNormalization_ShouldTrimTrailingSlashFromServerUrls()
    {
        await using var app = await CreateAppWithDefaults(
            endpoints: app => app.MapGet("/test", () => new SimpleModel("ok")));

        var doc = await FetchOpenApiDocument(app);

        // If servers are present, none should have trailing slashes
        if (doc.RootElement.TryGetProperty("servers", out var servers))
        {
            foreach (var server in servers.EnumerateArray())
            {
                var url = server.GetProperty("url").GetString();
                if (url is not null)
                {
                    url.ShouldNotEndWith("/",
                        $"Server URL '{url}' should not have a trailing slash");
                }
            }
        }
    }

    [Fact]
    public async Task SecuritySchemes_ShouldAppearInDocumentWhenConfigured()
    {
        var config = new Dictionary<string, string?>
        {
            ["OpenApi:SecuritySchemes:Bearer:Type"] = "Http",
            ["OpenApi:SecuritySchemes:Bearer:Scheme"] = "bearer",
            ["OpenApi:SecuritySchemes:Bearer:BearerFormat"] = "JWT",
            ["OpenApi:SecuritySchemes:Bearer:Description"] = "JWT Bearer token"
        };

        await using var app = await CreateAppWithDefaults(
            configValues: config,
            endpoints: app => app.MapGet("/test", () => new SimpleModel("ok")));

        var doc = await FetchOpenApiDocument(app);

        var components = doc.RootElement.GetProperty("components");
        components.TryGetProperty("securitySchemes", out var securitySchemes).ShouldBeTrue(
            "Security schemes should be present in the document components");

        securitySchemes.TryGetProperty("Bearer", out var bearerScheme).ShouldBeTrue(
            "Bearer scheme should be present");

        bearerScheme.GetProperty("scheme").GetString().ShouldBe("bearer");
    }

    [Fact]
    public async Task SecuritySchemes_WithMultipleSchemes_ShouldAddAllValidSchemes()
    {
        var config = new Dictionary<string, string?>
        {
            ["OpenApi:SecuritySchemes:Bearer:Type"] = "Http",
            ["OpenApi:SecuritySchemes:Bearer:Scheme"] = "bearer",
            ["OpenApi:SecuritySchemes:ApiKey:Type"] = "ApiKey",
            ["OpenApi:SecuritySchemes:ApiKey:Description"] = "API Key"
        };

        await using var app = await CreateAppWithDefaults(
            configValues: config,
            endpoints: app => app.MapGet("/test", () => new SimpleModel("ok")));

        var doc = await FetchOpenApiDocument(app);

        var securitySchemes = doc.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes");

        securitySchemes.TryGetProperty("Bearer", out _).ShouldBeTrue("Bearer scheme should be present");
        securitySchemes.TryGetProperty("ApiKey", out _).ShouldBeTrue("ApiKey scheme should be present");
    }

    [Fact]
    public async Task SecuritySchemes_WithNoConfiguration_ShouldNotAddSecuritySchemes()
    {
        await using var app = await CreateAppWithDefaults(
            endpoints: app => app.MapGet("/test", () => new SimpleModel("ok")));

        var doc = await FetchOpenApiDocument(app);

        if (doc.RootElement.TryGetProperty("components", out var components)
            && components.TryGetProperty("securitySchemes", out var schemes))
        {
            // If securitySchemes exists, it should be empty
            schemes.EnumerateObject().Count().ShouldBe(0,
                "No security schemes should be present when no configuration is provided");
        }
    }

    [Fact]
    public async Task SecuritySchemes_WithSchemeWithoutType_ShouldBeSkipped()
    {
        var config = new Dictionary<string, string?>
        {
            ["OpenApi:SecuritySchemes:Invalid:Scheme"] = "bearer",
            ["OpenApi:SecuritySchemes:Invalid:BearerFormat"] = "JWT"
            // No Type specified - should be skipped
        };

        await using var app = await CreateAppWithDefaults(
            configValues: config,
            endpoints: app => app.MapGet("/test", () => new SimpleModel("ok")));

        var doc = await FetchOpenApiDocument(app);

        if (doc.RootElement.TryGetProperty("components", out var components)
            && components.TryGetProperty("securitySchemes", out var schemes))
        {
            schemes.TryGetProperty("Invalid", out _).ShouldBeFalse(
                "Scheme without Type should not be added to the document");
        }
    }

    [Fact]
    public async Task ConfigureOpenApiDefaults_ShouldApplyAllTransformersToDocument()
    {
        var config = new Dictionary<string, string?>
        {
            ["OpenApi:SecuritySchemes:Bearer:Type"] = "Http",
            ["OpenApi:SecuritySchemes:Bearer:Scheme"] = "bearer"
        };

        await using var app = await CreateAppWithDefaults(
            configValues: config,
            endpoints: app => app.MapGet("/test",
                () => new TestModelWithNullable("hello", null, 25, 9.99m)));

        var doc = await FetchOpenApiDocument(app);

        // Verify the document is well-formed
        doc.RootElement.GetProperty("openapi").GetString().ShouldNotBeNullOrEmpty();
        doc.RootElement.GetProperty("paths").TryGetProperty("/test", out _).ShouldBeTrue(
            "The /test endpoint should be present in the document");

        // Verify security schemes were added
        doc.RootElement.GetProperty("components")
            .TryGetProperty("securitySchemes", out var schemes).ShouldBeTrue();
        schemes.TryGetProperty("Bearer", out _).ShouldBeTrue();

        // Verify nullable fix was applied (description should NOT be in required)
        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var schema in schemas.EnumerateObject())
        {
            if (!schema.Name.Contains("TestModelWithNullable"))
                continue;

            if (schema.Value.TryGetProperty("required", out var required))
            {
                var requiredNames = required.EnumerateArray()
                    .Select(e => e.GetString())
                    .ToList();

                requiredNames.ShouldNotContain("description");
            }

            break;
        }
    }

    /// <summary>
    ///     Creates a minimal API application with OpenAPI defaults configured and starts the test server.
    /// </summary>
    private static async Task<WebApplication> CreateAppWithDefaults(
        Action<WebApplication> endpoints,
        Dictionary<string, string?>? configValues = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        if (configValues is not null)
        {
            builder.Configuration.AddInMemoryCollection(configValues);
        }

        var configuration = builder.Configuration as IConfiguration;
        builder.Services.AddOpenApi(options => options.ConfigureOpenApiDefaults(configuration));

        var app = builder.Build();

        endpoints(app);
        app.MapOpenApi();

        await app.StartAsync(TestContext.Current.CancellationToken);

        return app;
    }

    /// <summary>
    ///     Fetches and parses the OpenAPI document from the test server.
    /// </summary>
    private static async Task<JsonDocument> FetchOpenApiDocument(WebApplication app)
    {
        var client = app.GetTestClient();
        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(content);
    }

    /// <summary>
    ///     Checks if a JSON element (property schema) has a given format, handling both
    ///     direct format fields and anyOf/oneOf composite schemas.
    /// </summary>
    private static bool HasFormat(JsonElement element, string expectedFormat)
    {
        if (element.TryGetProperty("format", out var format) && format.GetString() == expectedFormat)
            return true;

        // Handle nullable types which may use anyOf
        if (element.TryGetProperty("anyOf", out var anyOf))
        {
            foreach (var item in anyOf.EnumerateArray())
            {
                if (item.TryGetProperty("format", out var innerFormat) && innerFormat.GetString() == expectedFormat)
                    return true;
            }
        }

        return false;
    }
}
