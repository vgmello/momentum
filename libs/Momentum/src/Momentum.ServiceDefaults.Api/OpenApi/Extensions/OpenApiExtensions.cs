// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

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
                foreach (var openApiServer in document.Servers.OfType<OpenApiServer>())
                {
                    openApiServer.Url = openApiServer.Url?.TrimEnd('/');
                }
            }

            return Task.CompletedTask;
        });

        return options;
    }

}
