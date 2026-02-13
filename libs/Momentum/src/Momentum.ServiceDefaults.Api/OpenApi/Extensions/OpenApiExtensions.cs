// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.OpenApi;

namespace Momentum.ServiceDefaults.Api.OpenApi.Extensions;

/// <summary>
///     Provides extension methods for configuring OpenAPI using the native .NET 10 source generator.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    ///     Configures OpenAPI options with Momentum's default settings.
    /// </summary>
    /// <param name="options">The OpenAPI options to configure.</param>
    /// <remarks>
    ///     This method applies Momentum's standard OpenAPI configuration:
    ///     <list type="bullet">
    ///         <item>Server URL normalization (removes trailing slashes)</item>
    ///         <item>Bearer authentication security scheme</item>
    ///     </list>
    ///     Call this from your project's <c>AddOpenApi()</c> configuration to apply defaults:
    ///     <code>
    ///     builder.Services.AddOpenApi(options =&gt;
    ///     {
    ///         options.ConfigureOpenApiDefaults();
    ///         // Add your custom configuration here
    ///     });
    ///     </code>
    /// </remarks>
    public static OpenApiOptions ConfigureOpenApiDefaults(this OpenApiOptions options)
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

        // Add Bearer authentication security scheme
        options.AddDocumentTransformer((document, _, _) =>
        {
            var components = document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
            components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();

            components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token"
            };

            return Task.CompletedTask;
        });

        return options;
    }
}
