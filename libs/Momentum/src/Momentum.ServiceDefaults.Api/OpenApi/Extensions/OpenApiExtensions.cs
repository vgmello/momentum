// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;

namespace Momentum.ServiceDefaults.Api.OpenApi.Extensions;

/// <summary>
///     Provides extension methods for configuring OpenAPI using the native .NET 10 source generator.
/// </summary>
public static class OpenApiExtensions
{
    private const string SecuritySchemesSectionName = "OpenApi:SecuritySchemes";

    /// <summary>
    ///     Configures OpenAPI options with Momentum's default settings.
    /// </summary>
    /// <param name="options">The OpenAPI options to configure.</param>
    /// <param name="configuration">
    ///     Optional application configuration. When provided, security schemes are bound from
    ///     <c>OpenApi:SecuritySchemes</c>. Each key becomes a scheme name with properties
    ///     <c>Type</c>, <c>Scheme</c>, <c>BearerFormat</c>, and <c>Description</c>.
    ///     When <c>null</c> or when the section is absent, no security schemes are added.
    /// </param>
    /// <remarks>
    ///     This method applies Momentum's standard OpenAPI configuration:
    ///     <list type="bullet">
    ///         <item>Server URL normalization (removes trailing slashes)</item>
    ///         <item>Config-driven security schemes via <c>OpenApi:SecuritySchemes</c></item>
    ///         <item>Schema fixes: removes nullable properties from <c>required</c> arrays, sets <c>decimal</c> format</item>
    ///     </list>
    ///     Call this from your project's <c>AddOpenApi()</c> configuration to apply defaults:
    ///     <code>
    ///     builder.Services.AddOpenApi(options =&gt;
    ///     {
    ///         options.ConfigureOpenApiDefaults(builder.Configuration);
    ///         // Add your custom configuration here
    ///     });
    ///     </code>
    /// </remarks>
    public static OpenApiOptions ConfigureOpenApiDefaults(this OpenApiOptions options, IConfiguration? configuration = null)
    {
        AddNullableRequiredFix(options);
        AddDecimalFormatFix(options);
        AddServerUrlNormalization(options);
        AddSecuritySchemes(options, configuration);

        return options;
    }

    /// <summary>
    ///     Removes nullable properties from <c>required</c> arrays in OpenAPI schemas.
    ///     .NET's OpenAPI generator marks all positional record parameters as required, even nullable ones.
    ///     When the API uses <c>WhenWritingNull</c> JSON serialization, null properties are omitted from responses,
    ///     causing client deserialization failures for fields marked as both required and nullable.
    /// </summary>
    private static void AddNullableRequiredFix(OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, _, _) =>
        {
            if (schema.Required is not { Count: > 0 } || schema.Properties is not { Count: > 0 })
                return Task.CompletedTask;

            var nullableRequired = schema.Required
                .Where(name => schema.Properties.TryGetValue(name, out var prop)
                               && prop.Type.HasValue
                               && prop.Type.Value.HasFlag(JsonSchemaType.Null))
                .ToList();

            foreach (var name in nullableRequired)
            {
                schema.Required.Remove(name);
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    ///     Sets the format of <c>decimal</c> types to <c>"decimal"</c> instead of the default <c>"double"</c>,
    ///     ensuring NSwag generates <c>decimal</c> in C# client code.
    /// </summary>
    private static void AddDecimalFormatFix(OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type == typeof(decimal) || context.JsonTypeInfo.Type == typeof(decimal?))
            {
                schema.Format = "decimal";
            }

            return Task.CompletedTask;
        });
    }

    private static void AddServerUrlNormalization(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            if (document.Servers is not null)
            {
                foreach (var server in document.Servers)
                {
                    server.Url = server.Url?.TrimEnd('/');
                }
            }

            return Task.CompletedTask;
        });
    }

    private static void AddSecuritySchemes(OpenApiOptions options, IConfiguration? configuration)
    {
        var schemes = BindSecuritySchemes(configuration);

        if (schemes.Count > 0)
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                var components = document.Components ??= new OpenApiComponents();
                components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                foreach (var (name, scheme) in schemes)
                {
                    components.SecuritySchemes[name] = scheme;
                }

                return Task.CompletedTask;
            });
        }
    }

    private static Dictionary<string, OpenApiSecurityScheme> BindSecuritySchemes(
        IConfiguration? configuration)
    {
        var result = new Dictionary<string, OpenApiSecurityScheme>();
        var schemesSection = configuration?.GetSection(SecuritySchemesSectionName);

        if (schemesSection is null || !schemesSection.Exists())
            return result;

        foreach (var child in schemesSection.GetChildren())
        {
            var scheme = new OpenApiSecurityScheme();
            child.Bind(scheme);

            if (scheme.Type is null)
                continue;

            result[child.Key] = scheme;
        }

        return result;
    }
}
