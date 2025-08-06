// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Momentum.Extensions.XmlDocs;
using Momentum.ServiceDefaults.Api.OpenApi.Transformers;

namespace Momentum.ServiceDefaults.Api.OpenApi.Extensions;

/// <summary>
///     Provides extension methods for configuring OpenAPI with enhanced XML documentation support.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    ///     Adds OpenAPI services with XML documentation support and automatic response type conventions.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">An optional action to configure additional OpenAPI options.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    /// <remarks>
    ///     This method configures the following:
    ///     <list type="bullet">
    ///         <item>XML documentation service for reading XML comment files</item>
    ///         <item>Document transformer for adding API-level XML documentation</item>
    ///         <item>Operation transformer for adding endpoint-level XML documentation</item>
    ///         <item>Schema transformer for adding model-level XML documentation</item>
    ///         <item>Automatic response type convention for standardized API responses</item>
    ///     </list>
    /// </remarks>
    public static IServiceCollection AddOpenApiWithXmlDocSupport(this IServiceCollection services, Action<OpenApiOptions>? config = null)
    {
        services.AddSingleton<IXmlDocumentationService, XmlDocumentationService>();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<XmlDocumentationDocumentTransformer>();
            options.AddOperationTransformer<XmlDocumentationOperationTransformer>();
            options.AddSchemaTransformer<XmlDocumentationSchemaTransformer>();

            options.AddDocumentTransformer((document, _, _) =>
            {
                foreach (var openApiServer in document.Servers)
                {
                    openApiServer.Url = openApiServer.Url.TrimEnd('/');
                }

                return Task.CompletedTask;
            });

            config?.Invoke(options);
        });

        services.Configure<MvcOptions>(opt => opt.Conventions.Add(new AutoProducesResponseTypeConvention()));

        return services;
    }
}
