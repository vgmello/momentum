// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;

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
        // Normalize server URLs by removing trailing slashes
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

        var schemes = BindSecuritySchemes(configuration);

        if (schemes.Count > 0)
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                var components = document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
                components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();

                foreach (var (name, scheme) in schemes)
                {
                    components.SecuritySchemes[name] = scheme;
                }

                return Task.CompletedTask;
            });
        }

        return options;
    }

    private static Dictionary<string, Microsoft.OpenApi.OpenApiSecurityScheme> BindSecuritySchemes(
        IConfiguration? configuration)
    {
        var result = new Dictionary<string, Microsoft.OpenApi.OpenApiSecurityScheme>();
        var schemesSection = configuration?.GetSection(SecuritySchemesSectionName);

        if (schemesSection is null || !schemesSection.Exists())
            return result;

        foreach (var child in schemesSection.GetChildren())
        {
            var scheme = new Microsoft.OpenApi.OpenApiSecurityScheme();
            child.Bind(scheme);

            if (scheme.Type is null)
                continue;

            result[child.Key] = scheme;
        }

        return result;
    }
}
