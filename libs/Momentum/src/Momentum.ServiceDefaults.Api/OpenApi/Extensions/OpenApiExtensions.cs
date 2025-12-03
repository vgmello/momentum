// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    ///     Registers the auto-produces response type convention for controllers.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    /// <remarks>
    ///     This method adds a convention that automatically infers response types
    ///     for controller actions, improving OpenAPI schema generation.
    /// </remarks>
    public static IServiceCollection AddAutoProducesConvention(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(opt => opt.Conventions.Add(new AutoProducesResponseTypeConvention()));
        return services;
    }

}
