// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Momentum.Extensions.XmlDocs;
using System.Reflection;

namespace Momentum.ServiceDefaults.Api.OpenApi.Transformers;

/// <summary>
///     Transforms OpenAPI documents by enriching them with XML documentation and assembly metadata.
/// </summary>
/// <remarks>
///     This transformer enhances the OpenAPI specification with:
///     <list type="bullet">
///         <item>Controller documentation as tag descriptions</item>
///         <item>Assembly company information as contact details</item>
///         <item>Assembly copyright as license information</item>
///         <item>Assembly version as custom metadata</item>
///     </list>
/// </remarks>
public class XmlDocumentationDocumentTransformer(
    ILogger<XmlDocumentationDocumentTransformer> logger,
    IXmlDocumentationService xmlDocumentationService
) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();

            EnrichTags(document, context);
            EnrichDocumentInfo(assembly, document);
            AddMetadata(assembly, document);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to transform OpenAPI document with XML documentation");
        }

        return Task.CompletedTask;
    }

    private void EnrichTags(OpenApiDocument document, OpenApiDocumentTransformerContext context)
    {
        foreach (var tag in document.Tags)
        {
            // try to get the controller type based on the tag name
            var controllerActionDescriptor = context.DescriptionGroups
                .SelectMany(dg => dg.Items)
                .Select(i => i.ActionDescriptor)
                .OfType<ControllerActionDescriptor>()
                .FirstOrDefault(ad => ad.ControllerName == tag.Name);

            if (controllerActionDescriptor is not null)
            {
                var controllerDoc = xmlDocumentationService.GetTypeDocumentation(controllerActionDescriptor.ControllerTypeInfo);

                if (controllerDoc is not null)
                {
                    tag.Description = controllerDoc.Summary;

                    if (controllerDoc.Remarks is not null)
                    {
                        tag.Description += $"\n\n{controllerDoc.Remarks}";
                    }
                }
            }
        }
    }

    private static void EnrichDocumentInfo(Assembly assembly, OpenApiDocument document)
    {
        if (document.Info.Contact is null)
        {
            var company = GetAssemblyCompany(assembly);

            if (!string.IsNullOrEmpty(company))
            {
                document.Info.Contact = new OpenApiContact { Name = company };
            }
        }

        if (document.Info.License is null)
        {
            var copyright = GetAssemblyCopyright(assembly);

            if (!string.IsNullOrEmpty(copyright))
            {
                document.Info.License = new OpenApiLicense { Name = copyright };
            }
        }
    }

    private static void AddMetadata(Assembly assembly, OpenApiDocument document)
    {
        document.Extensions["x-assembly-version"] = new OpenApiString(assembly.GetName().Version?.ToString() ?? "Unknown");
    }

    private static string? GetAssemblyCompany(Assembly assembly) => assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;

    private static string? GetAssemblyCopyright(Assembly assembly) => assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
}
