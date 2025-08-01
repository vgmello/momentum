// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using Operations.Extensions.EventMarkdownGenerator.Models;
using Operations.Extensions.XmlDocs;

namespace Operations.Extensions.EventMarkdownGenerator.Services;

public class XmlDocumentationParser
{
    private readonly XmlDocumentationService _xmlService = new(new NullLogger<XmlDocumentationService>());

    public EventDocumentation GetEventDocumentation(Type eventType)
    {
        var typeDocumentation = _xmlService.GetTypeDocumentation(eventType);
        var propertyDescriptions = GetPropertyDescriptions(eventType);

        return new EventDocumentation
        {
            Summary = CleanupXmlText(typeDocumentation?.Summary) ?? "No documentation available",
            Remarks = CleanupXmlText(typeDocumentation?.Remarks),
            Example = typeDocumentation?.Example,
            PropertyDescriptions = propertyDescriptions
        };
    }

    public async Task<bool> LoadMultipleDocumentationAsync(IEnumerable<string>? xmlFilePaths)
    {
        if (xmlFilePaths == null)
            return false;

        var loadResults = await Task.WhenAll(
            xmlFilePaths.Select(_xmlService.LoadDocumentationAsync)
        );

        return loadResults.Any(result => result);
    }

    private Dictionary<string, string> GetPropertyDescriptions(Type eventType)
    {
        var descriptions = new Dictionary<string, string>();
        var constructorDescriptions = GetConstructorParameterDescriptions(eventType);

        foreach (var property in eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var documentation = _xmlService.GetPropertyDocumentation(property);

            if (!string.IsNullOrEmpty(documentation?.Summary))
            {
                descriptions[property.Name] = documentation.Summary;
            }
            // If no property documentation, try constructor parameter documentation
            else if (constructorDescriptions.TryGetValue(property.Name, out var constructorDoc))
            {
                descriptions[property.Name] = constructorDoc;
            }
            else
            {
                descriptions[property.Name] = "No description available";
            }
        }

        return descriptions;
    }

    private Dictionary<string, string> GetConstructorParameterDescriptions(Type eventType)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Look for constructor documentation
        var constructors = eventType.GetConstructors();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();

            if (parameters.Length == 0) continue;

            // Build constructor member name - format: M:TypeName.#ctor(ParameterTypes)
            var parameterTypes = parameters.Select(p => GetXmlTypeName(p.ParameterType));
            var memberName = parameters.Length > 0
                ? $"M:{eventType.FullName}.#ctor({string.Join(",", parameterTypes)})"
                : $"M:{eventType.FullName}.#ctor";

            var documentation = _xmlService.GetDocumentation(memberName);

            if (documentation?.Parameters != null)
            {
                foreach (var paramDoc in documentation.Parameters)
                {
                    descriptions[paramDoc.Key] = paramDoc.Value.Description ?? "No description available";
                }
            }
        }

        return descriptions;
    }

    private static string GetXmlTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            try
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                var genericTypeName = genericTypeDefinition.FullName?.Replace("`1", "");

                if (string.IsNullOrEmpty(genericTypeName))
                {
                    genericTypeName = genericTypeDefinition.Name.Replace("`1", "");
                }

                var genericArgs = type.GetGenericArguments().Select(GetXmlTypeName);

                return $"{genericTypeName}{{{string.Join(",", genericArgs)}}}";
            }
            catch (Exception)
            {
                return type.Name;
            }
        }

        return type.FullName ?? type.Name;
    }

    private static string? CleanupXmlText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var lines = text.Split(['\r', '\n'], StringSplitOptions.None)
            .Select(line => line.TrimStart(' ', '\t'))
            .ToList();

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join("\n", lines);
    }
}
