# XML Documentation Document Transformer

## Transformer Overview {#transformer-overview}

This transformer enhances the OpenAPI specification with:

- **Controller Documentation**: As tag descriptions
- **Assembly Company Information**: As contact details
- **Assembly Copyright**: As license information  
- **Assembly Version**: As custom metadata

## Document Enhancement Process

### Tag Enrichment

The transformer processes OpenAPI tags by matching them with controller types:

```csharp
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
```

### Controller Documentation Integration

```csharp
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
```

## Assembly Metadata Integration

### Contact Information

```csharp
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
```

### License Information

```csharp
if (document.Info.License is null)
{
    var copyright = GetAssemblyCopyright(assembly);

    if (!string.IsNullOrEmpty(copyright))
    {
        document.Info.License = new OpenApiLicense { Name = copyright };
    }
}
```

### Custom Extensions

```csharp
private static void AddMetadata(Assembly assembly, OpenApiDocument document)
{
    document.Extensions["x-assembly-version"] = new OpenApiString(assembly.GetName().Version?.ToString() ?? "Unknown");
}
```

## Assembly Attribute Extraction

### Company Information

```csharp
private static string? GetAssemblyCompany(Assembly assembly) => 
    assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
```

### Copyright Information

```csharp
private static string? GetAssemblyCopyright(Assembly assembly) => 
    assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
```

## Error Handling and Resilience

### Exception Safety

```csharp
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
```

### Graceful Degradation

The transformer continues processing even if:

- **XML Documentation Missing**: Tags remain with original descriptions
- **Assembly Attributes Missing**: Contact/license information omitted
- **Reflection Failures**: Metadata extraction skipped

## OpenAPI Document Structure

### Enhanced Tags

After transformation, tags include:

```json
{
  "name": "Users",
  "description": "Manages user accounts and profiles.\n\nProvides CRUD operations for user management including registration, authentication, and profile updates."
}
```

### Document Information

```json
{
  "info": {
    "title": "My API",
    "version": "1.0.0",
    "contact": {
      "name": "Acme Corporation"
    },
    "license": {
      "name": "Copyright © 2024 Acme Corporation. All rights reserved."
    }
  },
  "extensions": {
    "x-assembly-version": "1.2.3.0"
  }
}
```

## Integration with OpenAPI Pipeline

### Transformer Registration

```csharp
services.ConfigureOpenApi(opt =>
{
    opt.AddDocumentTransformer<XmlDocumentationDocumentTransformer>();
});
```

### Processing Order

The transformer executes during OpenAPI document generation:

1. **Base Document Creation**: Core OpenAPI structure
2. **Document Transformation**: XML documentation enrichment
3. **Final Serialization**: Complete enhanced document

## XML Documentation Service Integration

### Service Dependency

```csharp
public class XmlDocumentationDocumentTransformer(
    ILogger<XmlDocumentationDocumentTransformer> logger,
    IXmlDocumentationService xmlDocumentationService
) : IOpenApiDocumentTransformer
```

### Type Documentation Retrieval

```csharp
var controllerDoc = xmlDocumentationService.GetTypeDocumentation(controllerActionDescriptor.ControllerTypeInfo);
```

## Benefits and Use Cases

### API Documentation Quality

- **Rich Descriptions**: Detailed tag descriptions from XML comments
- **Professional Appearance**: Proper contact and license information
- **Version Tracking**: Assembly version for API versioning

### Development Workflow

- **Automated Documentation**: No manual tag descriptions needed
- **Consistent Information**: Assembly attributes propagated automatically
- **Maintenance Reduction**: Documentation updates with code changes