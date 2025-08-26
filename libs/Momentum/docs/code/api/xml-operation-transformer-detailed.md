# XML Documentation Operation Transformer

## Transformer Overview

This transformer enhances operation specifications with:

- **Method XML Documentation**: As operation summary and description
- **Parameter Documentation**: Including examples and default values
- **Response Documentation**: From XML response tags
- **Return Type Documentation**: For successful responses
- **Default Response Descriptions**: For common HTTP status codes

## Operation Enhancement Process

### XML Documentation Integration

```csharp
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
```

### Parameter Enhancement

```csharp
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
```

### Parameter Examples and Defaults

```csharp
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
```

## Response Documentation

### XML Response Tags

```csharp
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
```

### Returns Documentation

```csharp
if (xmlDocs.Returns is not null)
{
    var successResponse = operation.Responses.FirstOrDefault(r => r.Key.StartsWith('2'));

    if (successResponse.Key is not null)
        successResponse.Value.Description ??= xmlDocs.Returns;
}
```

## Auto-Produced Response Handling

### Convention Integration

This method checks if the operation has an auto-produced successful response (e.g., 200 OK) added by the `AutoProducesResponseTypeConvention` (only added if no other 2XX already exists) and replaces the status code with the actual documented successful response code from the XML documentation. If there are no documented successful response code, the auto-produced response is removed.

```csharp
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
```

## Default Response Descriptions

### Standard HTTP Status Codes

```csharp
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
```

### Response Completion

```csharp
// Ensure all responses have descriptions
foreach (var (statusCode, response) in operation.Responses.Where(r => r.Value.Description is null))
{
    response.Description = GetDefaultResponseDescription(statusCode);
}
```

## Error Handling and Safety

### Exception Protection

```csharp
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
```

## Type Conversion Integration

### OpenAPI Type Conversion

```csharp
parameter.Example = paramInfo.ParameterType.ConvertToOpenApiType(paramDoc.Example);
```

This utilizes extension methods for proper type conversion between .NET types and OpenAPI representations.

## XML Documentation Structure

### Parameter Documentation

```xml
/// <param name="userId" example="123e4567-e89b-12d3-a456-426614174000">The unique identifier of the user</param>
```

### Response Documentation

```xml
/// <response code="200">User retrieved successfully</response>
/// <response code="404">User not found</response>
```

### Returns Documentation

```xml
/// <returns>The user information</returns>
```

## Integration Benefits

### Enhanced API Documentation

- **Rich Operation Descriptions**: Detailed explanations from XML comments
- **Parameter Guidance**: Examples and default values for developers
- **Response Clarity**: Clear descriptions for all status codes
- **Type Safety**: Proper parameter examples with type conversion

### Developer Experience

- **IntelliSense Integration**: IDE support for XML documentation
- **Automated Generation**: No manual OpenAPI annotation needed
- **Consistency**: Uniform documentation across operations
- **Maintenance**: Documentation updates with code changes