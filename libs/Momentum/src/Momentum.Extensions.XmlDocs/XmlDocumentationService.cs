// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Momentum.Extensions.XmlDocs;

/// <summary>
///     Provides services for loading and retrieving XML documentation comments from compiled assemblies.
/// </summary>
/// <remarks>
///     <!--@include: @code/xml-docs/xml-documentation-service-detailed.md#service-overview -->
/// </remarks>
public class XmlDocumentationService(ILogger<XmlDocumentationService> logger) : IXmlDocumentationService
{
    private readonly ConcurrentDictionary<string, XmlDocumentationInfo> _documentationCache = new();

    public async Task<bool> LoadDocumentationAsync(string xmlFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(xmlFilePath))
        {
            logger.LogWarning("XML documentation file not found: {FilePath}", xmlFilePath);

            return false;
        }

        try
        {
            await using var fileStream = new FileStream(xmlFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 8192,
                useAsync: true);

            using var xmlReader = XmlReader.Create(fileStream, new XmlReaderSettings
            {
                Async = true,
                IgnoreComments = true,
                IgnoreWhitespace = false,
                ConformanceLevel = ConformanceLevel.Document
            });

            await ParseXmlDocumentationAsync(xmlReader, cancellationToken).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Loaded XML documentation from {FilePath} with {Count} entries", xmlFilePath, _documentationCache.Count);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load XML documentation from {FilePath}", xmlFilePath);

            return false;
        }
    }

    public XmlDocumentationInfo? GetDocumentation(string memberName) => _documentationCache.GetValueOrDefault(memberName);

    public XmlDocumentationInfo? GetTypeDocumentation(Type type)
    {
        return GetDocumentation($"T:{type.FullName}");
    }

    public XmlDocumentationInfo? GetMethodDocumentation(MethodInfo methodInfo)
    {
        var memberName = GetMethodDocumentationName(methodInfo);

        return GetDocumentation(memberName);
    }

    public XmlDocumentationInfo? GetPropertyDocumentation(PropertyInfo propertyInfo)
    {
        return GetDocumentation($"P:{propertyInfo.DeclaringType?.FullName}.{propertyInfo.Name}");
    }

    public void ClearCache() => _documentationCache.Clear();

    private async Task ParseXmlDocumentationAsync(XmlReader reader, CancellationToken cancellationToken)
    {
        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader is not { NodeType: XmlNodeType.Element, Name: "member" })
                continue;

            var nameAttribute = reader.GetAttribute("name");

            if (!string.IsNullOrEmpty(nameAttribute))
            {
                var docInfo = await ParseMemberDocumentationAsync(reader);

                if (docInfo is not null)
                {
                    _documentationCache.TryAdd(nameAttribute, docInfo);
                }
            }
        }
    }

    [SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high")]
    private static async Task<XmlDocumentationInfo?> ParseMemberDocumentationAsync(XmlReader reader)
    {
        var docInfo = new XmlDocumentationInfo();
        var hasContent = false;

        if (reader.IsEmptyElement)
            return null;

        while (await reader.ReadAsync())
        {
            if (reader is { NodeType: XmlNodeType.EndElement, Name: "member" })
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name.ToLowerInvariant())
            {
                case "summary":
                    docInfo.Summary = await ReadElementContentAsync(reader);
                    hasContent = true;

                    break;
                case "remarks":
                    docInfo.Remarks = await ReadElementContentAsync(reader);
                    hasContent = true;

                    break;
                case "returns":
                    docInfo.Returns = await ReadElementContentAsync(reader);
                    hasContent = true;

                    break;
                case "param":
                    var paramName = reader.GetAttribute("name");

                    if (!string.IsNullOrEmpty(paramName))
                    {
                        var paramExample = reader.GetAttribute("example");
                        var paramDoc = await ReadElementContentAsync(reader);

                        docInfo.Parameters[paramName] = new XmlDocumentationInfo.ParameterInfo(paramDoc, paramExample);
                        hasContent = true;
                    }

                    break;
                case "response":
                    var responseCode = reader.GetAttribute("code");
                    var responseDoc = await ReadElementContentAsync(reader);

                    if (!string.IsNullOrEmpty(responseCode))
                    {
                        docInfo.Responses[responseCode] = responseDoc;
                        hasContent = true;
                    }

                    break;
                case "example":
                    docInfo.Example = await ReadElementContentAsync(reader);
                    hasContent = true;

                    break;
                default:
                    // Unknown element
                    if (!reader.IsEmptyElement)
                    {
                        await reader.ReadAsync();
                    }

                    break;
            }
        }

        return hasContent ? docInfo : null;
    }

    private static async Task<string?> ReadElementContentAsync(XmlReader reader)
    {
        if (reader.IsEmptyElement)
            return null;

        var content = new StringBuilder();

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
                break;

            if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
            {
                content.Append(reader.Value);
            }
        }

        var result = content.ToString();

        // Preserve internal newlines but trim leading/trailing whitespace
        return result.Trim();
    }

    private static string GetMethodDocumentationName(MethodInfo methodInfo)
    {
        var sb = new StringBuilder();

        sb.Append("M:");
        sb.Append(methodInfo.DeclaringType?.FullName);
        sb.Append('.');
        sb.Append(methodInfo.Name);

        if (methodInfo.GetParameters().Length > 0)
        {
            sb.Append('(');

            var parameters = methodInfo.GetParameters();

            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(GetTypeName(parameters[i].ParameterType));
            }

            sb.Append(')');
        }

        return sb.ToString();
    }

    private static string GetTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var genericTypeName = type.GetGenericTypeDefinition().FullName;
        var genericArgs = type.GetGenericArguments();

        var sb = new StringBuilder();

        sb.Append(genericTypeName?[..genericTypeName.IndexOf('`')]);
        sb.Append('{');

        for (var i = 0; i < genericArgs.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(GetTypeName(genericArgs[i]));
        }

        sb.Append('}');

        return sb.ToString();
    }
}
