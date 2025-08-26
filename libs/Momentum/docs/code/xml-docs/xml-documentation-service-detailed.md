# XML Documentation Service

## Service Overview

This service parses XML documentation files generated during compilation and provides methods to retrieve documentation for types, methods, properties, and other members. Documentation is cached for performance after initial loading.

## Core Functionality

### Documentation Loading

```csharp
public async Task<bool> LoadDocumentationAsync(string xmlFilePath)
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

        await ParseXmlDocumentationAsync(xmlReader);

        logger.LogInformation("Loaded XML documentation from {FilePath} with {Count} entries",
            xmlFilePath, _documentationCache.Count);

        return true;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to load XML documentation from {FilePath}", xmlFilePath);
        return false;
    }
}
```

### Performance Optimizations

#### Asynchronous File I/O

```csharp
await using var fileStream = new FileStream(xmlFilePath,
    FileMode.Open,
    FileAccess.Read,
    FileShare.Read,
    bufferSize: 8192,
    useAsync: true);
```

#### Efficient XML Reading

```csharp
using var xmlReader = XmlReader.Create(fileStream, new XmlReaderSettings
{
    Async = true,
    IgnoreComments = true,
    IgnoreWhitespace = false,
    ConformanceLevel = ConformanceLevel.Document
});
```

### Documentation Parsing

#### Member Discovery

```csharp
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
```

#### Element Processing

```csharp
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
                // ... other elements
            }
        }
    }

    return hasContent ? docInfo : null;
}
```

## Caching Strategy

### Thread-Safe Caching

```csharp
private readonly ConcurrentDictionary<string, XmlDocumentationInfo> _documentationCache = new();
```

### Cache Operations

```csharp
public XmlDocumentationInfo? GetDocumentation(string memberName) =>
    _documentationCache.GetValueOrDefault(memberName);

public void ClearCache() => _documentationCache.Clear();
```

## Member Name Resolution

### Type Documentation

```csharp
public XmlDocumentationInfo? GetTypeDocumentation(Type type)
{
    return GetDocumentation($"T:{type.FullName}");
}
```

### Method Documentation

```csharp
public XmlDocumentationInfo? GetMethodDocumentation(MethodInfo methodInfo)
{
    var memberName = GetMethodDocumentationName(methodInfo);
    return GetDocumentation(memberName);
}
```

### Property Documentation

```csharp
public XmlDocumentationInfo? GetPropertyDocumentation(PropertyInfo propertyInfo)
{
    return GetDocumentation($"P:{propertyInfo.DeclaringType?.FullName}.{propertyInfo.Name}");
}
```

## Method Name Generation

### Complex Method Signatures

```csharp
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
```

### Generic Type Support

```csharp
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
```

## Content Extraction

### Element Content Reading

```csharp
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
```

## Supported XML Elements

### Core Documentation Elements

- **summary**: Main description of the member
- **remarks**: Additional detailed information
- **returns**: Description of return value
- **example**: Usage examples

### Parameter Documentation

```csharp
case "param":
    var paramName = reader.GetAttribute("name");

    if (!string.IsNullOrEmpty(paramName))
    {
        var paramDoc = await ReadElementContentAsync(reader);
        var paramExample = reader.GetAttribute("example");

        docInfo.Parameters[paramName] = new XmlDocumentationInfo.ParameterInfo(paramDoc, paramExample);
        hasContent = true;
    }
    break;
```

### Response Documentation

```csharp
case "response":
    var responseCode = reader.GetAttribute("code");
    var responseDoc = await ReadElementContentAsync(reader);

    if (!string.IsNullOrEmpty(responseCode))
    {
        docInfo.Responses[responseCode] = responseDoc;
        hasContent = true;
    }
    break;
```

## Error Handling

### File Access Errors

```csharp
if (!File.Exists(xmlFilePath))
{
    logger.LogWarning("XML documentation file not found: {FilePath}", xmlFilePath);
    return false;
}
```

### XML Parsing Errors

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Failed to load XML documentation from {FilePath}", xmlFilePath);
    return false;
}
```

### Graceful Degradation

The service continues operation even when:
- **XML Files Missing**: Returns null for documentation queries
- **Malformed XML**: Skips problematic elements
- **Empty Elements**: Ignores elements without content

## Integration with OpenAPI

### Service Registration

```csharp
services.AddSingleton<IXmlDocumentationService, XmlDocumentationService>();
```

### Usage in Transformers

```csharp
var methodDoc = xmlDocumentationService.GetMethodDocumentation(methodInfo);
if (methodDoc?.Summary is not null)
{
    operation.Summary = methodDoc.Summary;
}
```

## Performance Characteristics

### Memory Usage

- **Efficient Caching**: Documentation cached after first access
- **Memory Bounded**: Cache size limited by assembly size
- **String Interning**: Repeated strings optimized

### I/O Performance

- **Async Operations**: Non-blocking file access
- **Buffered Reading**: 8KB buffer for optimal throughput
- **Single Load**: Documentation loaded once per application lifecycle

### Thread Safety

- **Concurrent Access**: Thread-safe cache operations
- **Read-Heavy Optimization**: Optimized for frequent reads
- **Lazy Loading**: Documentation loaded on demand
