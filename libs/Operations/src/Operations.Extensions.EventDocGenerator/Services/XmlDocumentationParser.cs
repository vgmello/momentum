using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Xml;
using Operations.Extensions.EventDocGenerator.Models;

namespace Operations.Extensions.EventDocGenerator.Services;

public class XmlDocumentationParser
{
    private readonly ConcurrentDictionary<string, ParsedXmlDocumentation> _documentationCache = new();

    public async Task<bool> LoadDocumentationAsync(string? xmlFilePath)
    {
        if (string.IsNullOrEmpty(xmlFilePath) || !File.Exists(xmlFilePath))
        {
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
                IgnoreWhitespace = true,
                ConformanceLevel = ConformanceLevel.Document
            });

            await ParseXmlDocumentationAsync(xmlReader);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public EventDocumentation GetEventDocumentation(Type eventType)
    {
        var typeDocumentation = GetTypeDocumentation(eventType);
        var propertyDescriptions = GetPropertyDescriptions(eventType);

        return new EventDocumentation
        {
            Summary = typeDocumentation?.Summary ?? "No documentation available",
            Remarks = typeDocumentation?.Remarks,
            Example = typeDocumentation?.Example,
            PropertyDescriptions = propertyDescriptions
        };
    }

    private ParsedXmlDocumentation? GetTypeDocumentation(Type type)
    {
        var memberName = $"T:{type.FullName}";
        return _documentationCache.GetValueOrDefault(memberName);
    }

    private Dictionary<string, string> GetPropertyDescriptions(Type eventType)
    {
        var descriptions = new Dictionary<string, string>();

        foreach (var property in eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var memberName = $"P:{property.DeclaringType?.FullName}.{property.Name}";
            var documentation = _documentationCache.GetValueOrDefault(memberName);
            
            if (!string.IsNullOrEmpty(documentation?.Summary))
            {
                descriptions[property.Name] = documentation.Summary;
            }
        }

        return descriptions;
    }

    private async Task ParseXmlDocumentationAsync(XmlReader reader)
    {
        while (await reader.ReadAsync())
        {
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

    private static async Task<ParsedXmlDocumentation?> ParseMemberDocumentationAsync(XmlReader reader)
    {
        var docInfo = new ParsedXmlDocumentation();
        var hasContent = false;

        if (reader.IsEmptyElement)
            return null;

        while (await reader.ReadAsync())
        {
            if (reader is { NodeType: XmlNodeType.EndElement, Name: "member" })
                break;

            if (reader.NodeType == XmlNodeType.Element)
            {
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
                    case "example":
                        docInfo.Example = await ReadElementContentAsync(reader);
                        hasContent = true;
                        break;
                    default:
                        // Skip unknown elements
                        if (!reader.IsEmptyElement)
                        {
                            await reader.ReadAsync();
                        }
                        break;
                }
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
                content.Append(reader.Value.Trim());
            }
        }

        var result = content.ToString();
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public void ClearCache() => _documentationCache.Clear();
}

internal class ParsedXmlDocumentation
{
    public string? Summary { get; set; }
    public string? Remarks { get; set; }
    public string? Example { get; set; }
}