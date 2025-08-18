# OpenAPI Caching Middleware

## Middleware Overview {#middleware-overview}

This middleware:

- **Intercepts OpenAPI Requests**: Identifies and handles OpenAPI document requests
- **Loads XML Documentation**: Processes XML docs on first request for enrichment
- **Caches Generated Documents**: Stores OpenAPI documents to disk for performance
- **Serves Cached Responses**: Returns cached documents with proper ETag headers
- **Supports Multiple Formats**: Handles both JSON and YAML OpenAPI formats
- **Handles Conditional Requests**: Responds with 304 Not Modified when appropriate

The cache is stored in the system temp directory and persists across application restarts.

## Request Processing Flow

### OpenAPI Request Detection

```csharp
private static bool IsOpenApiRequest(HttpRequest request)
{
    if (!request.Path.HasValue || request.Path.Value.Length > MaxOpenApiRequestPathLenght)
        return false;

    return request.Path.Value.Contains("/openapi", StringComparison.OrdinalIgnoreCase);
}
```

### Cache Key Generation

```csharp
private static string GetCacheKey(HttpRequest request) => 
    Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Path));
```

### Content Type Detection

```csharp
private static string GetContentType(HttpContext httpContext)
{
    var path = httpContext.Request.Path.ToString();

    if (path.Contains(".yaml", StringComparison.OrdinalIgnoreCase) || 
        path.Contains(".yml", StringComparison.OrdinalIgnoreCase))
    {
        return "application/yaml";
    }

    return MediaTypeNames.Application.Json;
}
```

## Caching Strategy

### Cache Directory Structure

```csharp
private static string GetCacheDirectory()
{
    var assemblyDir = Assembly.GetEntryAssembly()?.GetName().Name?.Replace('.', '_') ?? 
                     Guid.NewGuid().ToString("N");

    return Path.Combine(Path.GetTempPath(), assemblyDir, "openapi-cache");
}
```

### Cache File Management

```csharp
private static string GetCacheFilePath(string cacheKey) => 
    Path.Combine(CacheDirectory, $"{cacheKey}.txt");
```

### Initialization Tracking

```csharp
private readonly Dictionary<string, bool> _cacheInitialized = [];
```

## XML Documentation Integration

### Documentation Loading

```csharp
await xmlDocService.LoadDocumentationAsync(GetXmlDocLocation());
```

### Documentation Path Resolution

```csharp
private static string GetXmlDocLocation()
{
    var assembly = Assembly.GetEntryAssembly();
    var xmlFileName = Path.GetFileNameWithoutExtension(assembly?.Location) + ".xml";

    return Path.Combine(Path.GetDirectoryName(assembly?.Location) ?? "", xmlFileName);
}
```

### Cache Cleanup

```csharp
finally
{
    xmlDocService.ClearCache();
    _fileLock.Release();
}
```

## HTTP Caching Implementation

### ETag Generation

```csharp
private static string GenerateETag(FileInfo fileInfo)
{
    var combined = $"{fileInfo.Length}_{fileInfo.LastWriteTimeUtc.Ticks}";
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));

    return $"\"{Convert.ToBase64String(hash)[..16]}\"";
}
```

### Cache Headers

```csharp
private static void SetCacheHeaders(HttpContext httpContext, string filePath)
{
    var fileInfo = new FileInfo(filePath);

    var response = httpContext.Response;
    response.ContentType ??= GetContentType(httpContext);
    response.Headers.ETag = GenerateETag(fileInfo);
    response.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");
}
```

### Conditional Request Handling

```csharp
if (context.Request.Headers.IfNoneMatch.Contains(eTag.ToString()))
{
    context.Response.StatusCode = 304;
    return true;
}
```

## Thread Safety and Concurrency

### File Locking

```csharp
private readonly SemaphoreSlim _fileLock = new(1, 1);

await _fileLock.WaitAsync();
try
{
    // Cache generation logic
}
finally
{
    _fileLock.Release();
}
```

### Concurrent Request Handling

The middleware handles concurrent requests safely:

- **Single Generation**: Only one thread generates cache per key
- **Safe Reading**: Multiple threads can read cached files
- **Initialization Tracking**: Prevents duplicate initialization

## Error Handling and Fallback

### Exception Handling

```csharp
try
{
    await HandleOpenApiRequestAsync(context);
}
catch (Exception ex)
{
    logger.LogError(ex, "Error handling OpenAPI request");
    await next(context);
}
```

### Graceful Degradation

- **Cache Miss**: Falls back to regular OpenAPI generation
- **File Errors**: Continues with non-cached response
- **XML Doc Failures**: Proceeds without documentation enrichment

## Performance Optimizations

### Stream Handling

```csharp
await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, 
    FileShare.Read, BufferSize, useAsync: true);
await fileStream.CopyToAsync(context.Response.Body);
```

### Memory Management

```csharp
using var memoryStream = new MemoryStream();
context.Response.Body = memoryStream;
```

### Async Operations

All I/O operations use async patterns:

- **Async File Operations**: Non-blocking file reading/writing
- **Async XML Processing**: Non-blocking documentation loading
- **Async Stream Copying**: Efficient response streaming

## Configuration Options

### Buffer Size

```csharp
private const int BufferSize = 8192;
```

### Path Length Limits

```csharp
private const int MaxOpenApiRequestPathLenght = 500;
```

### Cache Persistence

Cache files persist across application restarts, improving:

- **Cold Start Performance**: Faster initial OpenAPI responses
- **Development Experience**: Consistent response times
- **Resource Utilization**: Reduced XML processing overhead