// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Momentum.Extensions.XmlDocs;
using Momentum.ServiceDefaults.Api.OpenApi.Extensions;
using System.Reflection;

namespace Momentum.ServiceDefaults.Api.OpenApi.Transformers;

/// <summary>
///     Transforms OpenAPI Momentum by enriching them with XML documentation from action methods.
/// </summary>
/// <remarks>
///     This transformer enhances operation specifications with:
///     <list type="bullet">
///         <item>Method XML documentation as operation summary and description</item>
///         <item>Parameter documentation including examples and default values</item>
///         <item>Response documentation from XML response tags</item>
///         <item>Return type documentation for successful responses</item>
///         <item>Default response descriptions for common HTTP status codes</item>
///     </list>
/// </remarks>
public class XmlDocumentationOperationTransformer(
    ILogger<XmlDocumentationOperationTransformer> logger,
    IXmlDocumentationService xmlDocumentationService
) : IOpenApiOperationTransformer
{
    private static readonly string AutoProducesStatusCode = AutoProducesResponseTypeConvention.StatusCode.ToString();

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (context.Description.ActionDescriptor is ControllerActionDescriptor controllerDescriptor)
            {
                EnrichOperation(operation, controllerDescriptor.MethodInfo);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to transform operation with XML documentation");
        }

        return Task.CompletedTask;
    }

    private void EnrichOperation(OpenApiOperation operation, MethodInfo methodInfo)
    {
        operation.OperationId = methodInfo.Name;

        var xmlDocs = xmlDocumentationService.GetMethodDocumentation(methodInfo);

        if (xmlDocs is null)
            return;

        if (xmlDocs.Summary is not null)
        {
            operation.Summary = xmlDocs.Summary;
            operation.Description = xmlDocs.Summary;
        }

        if (xmlDocs.Remarks is not null)
        {
            operation.Description += $"\n\n{xmlDocs.Remarks}";
        }

        EnrichParameters(operation, xmlDocs, methodInfo);
        EnrichResponses(operation, xmlDocs);
    }

    private static void EnrichParameters(OpenApiOperation operation, XmlDocumentationInfo xmlDocs, MethodInfo methodInfo)
    {
        if (operation.Parameters is null)
            return;

        var parametersByName = methodInfo.GetParameters().ToDictionary(p => p.Name!, p => p);

        foreach (var parameter in operation.Parameters)
        {
            if (xmlDocs.Parameters.TryGetValue(parameter.Name, out var paramDoc))
            {
                parameter.Description = paramDoc.Description;
            }

            if (parametersByName.TryGetValue(parameter.Name, out var paramInfo))
            {
                if (paramDoc?.Example is not null)
                {
                    parameter.Example = paramInfo.ParameterType.ConvertToOpenApiType(paramDoc.Example);
                }

                if (paramInfo.HasDefaultValue)
                {
                    var defaultValue = paramInfo.DefaultValue?.ToString();

                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        parameter.Description = string.IsNullOrEmpty(parameter.Description)
                            ? $"Default value: {defaultValue}"
                            : $"{parameter.Description} (Default: {defaultValue})";
                    }
                }
            }
        }
    }

    private static void EnrichResponses(OpenApiOperation operation, XmlDocumentationInfo xmlDocs)
    {
        ReplaceAutoProducedResponseToOperation(operation, xmlDocs);

        foreach (var (responseCode, responseDoc) in xmlDocs.Responses)
        {
            if (!operation.Responses.TryGetValue(responseCode, out var response))
            {
                response = new OpenApiResponse();
                operation.Responses[responseCode] = response;
            }

            response.Description = responseDoc;
        }

        if (xmlDocs.Returns is not null)
        {
            var successResponse = operation.Responses.FirstOrDefault(r => r.Key.StartsWith('2'));

            if (successResponse.Key is not null)
                successResponse.Value.Description ??= xmlDocs.Returns;
        }

        // Ensure all responses have descriptions
        foreach (var (statusCode, response) in operation.Responses.Where(r => r.Value.Description is null))
        {
            response.Description = GetDefaultResponseDescription(statusCode);
        }
    }

    /// <summary>
    ///     This method checks if the operation has an auto-produced successful response (e.g., 200 OK) added by the
    ///     <see cref="AutoProducesResponseTypeConvention" /> (only added if no other 2XX already exists) and replaces
    ///     the status code with the actual documented successful response code from the XML documentation.
    ///     If there are no documented successful response code, the auto-produced response is removed.
    /// </summary>
    private static void ReplaceAutoProducedResponseToOperation(OpenApiOperation operation, XmlDocumentationInfo xmlDocs)
    {
        if (operation.Responses.TryGetValue(AutoProducesStatusCode, out var autoProducedResponse))
        {
            var successXmlResponse = xmlDocs.Responses.FirstOrDefault(r => r.Key.StartsWith('2'));

            if (successXmlResponse.Key is not null)
            {
                operation.Responses[successXmlResponse.Key] = autoProducedResponse;
            }

            operation.Responses.Remove(AutoProducesStatusCode);
        }
    }

    private static string GetDefaultResponseDescription(string statusCode) =>
        statusCode switch
        {
            "200" => "Success",
            "201" => "Created",
            "202" => "Accepted",
            "204" => "No Content",
            "400" => "Bad Request",
            "401" => "Unauthorized",
            "403" => "Forbidden",
            "404" => "Not Found",
            "409" => "Conflict",
            "500" => "Internal Server Error",
            "503" => "Service Unavailable",
            _ => "Response"
        };
}
