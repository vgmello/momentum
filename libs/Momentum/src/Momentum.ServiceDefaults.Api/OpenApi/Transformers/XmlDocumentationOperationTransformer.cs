// Copyright (c) Momentum .NET. All rights reserved.

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
///     <!--@include: @code/api/xml-operation-transformer-detailed.md#transformer-overview -->
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
            EnrichParameterWithDocumentation(parameter, xmlDocs);
            EnrichParameterWithReflectionInfo(parameter, parametersByName, xmlDocs);
        }
    }

    private static void EnrichParameterWithDocumentation(OpenApiParameter parameter, XmlDocumentationInfo xmlDocs)
    {
        if (xmlDocs.Parameters.TryGetValue(parameter.Name, out var paramDoc))
        {
            parameter.Description = paramDoc.Description;
        }
    }

    private static void EnrichParameterWithReflectionInfo(OpenApiParameter parameter, Dictionary<string, ParameterInfo> parametersByName,
        XmlDocumentationInfo xmlDocs)
    {
        if (!parametersByName.TryGetValue(parameter.Name, out var paramInfo))
            return;

        SetParameterExample(parameter, xmlDocs, paramInfo);
        SetParameterDefaultValue(parameter, paramInfo);
    }

    private static void SetParameterExample(OpenApiParameter parameter, XmlDocumentationInfo xmlDocs, ParameterInfo paramInfo)
    {
        if (xmlDocs.Parameters.TryGetValue(parameter.Name, out var paramDoc) && paramDoc.Example is not null)
        {
            parameter.Example = paramInfo.ParameterType.ConvertToOpenApiType(paramDoc.Example);
        }
    }

    private static void SetParameterDefaultValue(OpenApiParameter parameter, ParameterInfo paramInfo)
    {
        if (!paramInfo.HasDefaultValue)
            return;

        var defaultValue = paramInfo.DefaultValue?.ToString();

        if (string.IsNullOrEmpty(defaultValue))
            return;

        parameter.Description = string.IsNullOrEmpty(parameter.Description)
            ? $"Default value: {defaultValue}"
            : $"{parameter.Description} (Default: {defaultValue})";
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
    ///     <!--@include: @code/api/xml-operation-transformer-detailed.md#auto-produced-response -->
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
            else
            {
                operation.Responses["200"] = autoProducedResponse;
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
