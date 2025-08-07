// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Momentum.Extensions.XmlDocs;
using System.Net.Mime;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Momentum.ServiceDefaults.Api.OpenApi;

/// <summary>
///     Middleware that caches OpenAPI documentation to improve performance.
/// </summary>
/// <remarks>
///     This middleware:
///     <list type="bullet">
///         <item>Intercepts OpenAPI document requests</item>
///         <item>Loads XML documentation on first request</item>
///         <item>Caches generated OpenAPI documents to disk</item>
///         <item>Serves cached responses with proper ETag headers</item>
///         <item>Supports both JSON and YAML formats</item>
///         <item>Handles conditional requests (304 Not Modified)</item>
///     </list>
///     The cache is stored in the system temp directory and persists across application restarts.
/// </remarks>
public class OpenApiCachingMiddleware(
    ILogger<OpenApiCachingMiddleware> logger,
    IXmlDocumentationService xmlDocService,
    RequestDelegate next)
{
    private const int MaxOpenApiRequestPathLenght = 500;
    private const int BufferSize = 8192;

    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly Dictionary<string, bool> _cacheInitialized = [];

    internal static string CacheDirectory { get; } = GetCacheDirectory();

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsOpenApiRequest(context.Request))
        {
            await next(context);

            return;
        }

        try
        {
            await HandleOpenApiRequestAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling OpenAPI request");

            await next(context);
        }
    }

    private async Task HandleOpenApiRequestAsync(HttpContext context)
    {
        var cacheKey = GetCacheKey(context.Request);
        var filePath = GetCacheFilePath(cacheKey);

        if (!_cacheInitialized.ContainsKey(cacheKey))
        {
            await GenerateAndCacheResponseAsync(context, filePath, cacheKey);

            return;
        }

        if (await TryServeCachedResponseAsync(context, filePath))
            return;

        await GenerateAndCacheResponseAsync(context, filePath, cacheKey);
    }

    private async Task GenerateAndCacheResponseAsync(HttpContext context, string filePath, string cacheKey)
    {
        await _fileLock.WaitAsync();

        try
        {
            await xmlDocService.LoadDocumentationAsync(GetXmlDocLocation());

            var originalResponseBodyStream = context.Response.Body;

            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            // Execute OpenAPI request
            await next(context);

            if (context.Response.StatusCode == StatusCodes.Status200OK && memoryStream.Length > 0)
            {
                EnsureCacheDirectoryExists(filePath);

                var responseContent = memoryStream.ToArray();
                await File.WriteAllBytesAsync(filePath, responseContent);

                SetCacheHeaders(context, filePath);

                _cacheInitialized[cacheKey] = true;

                logger.LogInformation("Generated and cached OpenAPI document to disk: {FilePath} ({Size} bytes)",
                    filePath, responseContent.Length);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalResponseBodyStream);
        }
        finally
        {
            xmlDocService.ClearCache();

            _fileLock.Release();
        }
    }

    private async Task<bool> TryServeCachedResponseAsync(HttpContext context, string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        SetCacheHeaders(context, filePath);

        var eTag = context.Response.Headers.ETag;

        if (context.Request.Headers.IfNoneMatch.Contains(eTag.ToString()))
        {
            context.Response.StatusCode = 304;

            return true;
        }

        context.Response.StatusCode = 200;

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        await fileStream.CopyToAsync(context.Response.Body);

        logger.LogDebug("Served OpenAPI document from disk cache: {FilePath}", filePath);

        return true;
    }

    private static void SetCacheHeaders(HttpContext httpContext, string filePath)
    {
        var fileInfo = new FileInfo(filePath);

        var response = httpContext.Response;
        response.ContentType ??= GetContentType(httpContext);
        response.Headers.ETag = GenerateETag(fileInfo);
        response.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");
    }

    private static bool IsOpenApiRequest(HttpRequest request)
    {
        if (!request.Path.HasValue || request.Path.Value.Length > MaxOpenApiRequestPathLenght)
            return false;

        return request.Path.Value.Contains("/openapi", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetContentType(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.ToString();

        if (path.Contains(".yaml", StringComparison.OrdinalIgnoreCase) || path.Contains(".yml", StringComparison.OrdinalIgnoreCase))
        {
            return "application/yaml";
        }

        return MediaTypeNames.Application.Json;
    }

    private static string GetCacheKey(HttpRequest request) => Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Path));

    private static string GenerateETag(FileInfo fileInfo)
    {
        var combined = $"{fileInfo.Length}_{fileInfo.LastWriteTimeUtc.Ticks}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));

        return $"\"{Convert.ToBase64String(hash)[..16]}\"";
    }

    private static string GetCacheFilePath(string cacheKey) => Path.Combine(CacheDirectory, $"{cacheKey}.txt");

    private static string GetCacheDirectory()
    {
        var assemblyDir = Assembly.GetEntryAssembly()?.GetName().Name?.Replace('.', '_') ?? Guid.NewGuid().ToString("N");

        return Path.Combine(Path.GetTempPath(), assemblyDir, "openapi-cache");
    }

    private static void EnsureCacheDirectoryExists(string filePath)
    {
        var cacheDir = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("Cache directory path is invalid.");

        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }
    }

    private static string GetXmlDocLocation()
    {
        var assembly = Assembly.GetEntryAssembly();
        var xmlFileName = Path.GetFileNameWithoutExtension(assembly?.Location) + ".xml";

        return Path.Combine(Path.GetDirectoryName(assembly?.Location) ?? "", xmlFileName);
    }
}
