# Health Check Setup Extensions

## Default Health Check Endpoints

This method sets up the following health check endpoints:

| Endpoint | Description |
|----------|-------------|
| `/status` | A lightweight endpoint returning the status string of the last health check. Used for liveness probes. **Note**: This endpoint does not actually execute any health checks |
| `/health/internal` | A container-only endpoint that returns simplified version health status information. Used for readiness probes in cloud environments. Locally, this endpoint will return the same information as the `/health` endpoint. |
| `/health` | A public endpoint requiring authorization that returns detailed health status information. |

## Endpoint Implementation Details

### Liveness Probe - `/status`

```csharp
app.MapGet("/status", () => healthCheckStore.LastHealthStatus.ToString())
    .ExcludeFromDescription();
```

**Characteristics:**
- **Lightweight**: Returns cached status without executing checks
- **Fast Response**: No I/O operations or health check execution
- **Kubernetes Compatible**: Standard liveness probe format
- **Hidden from OpenAPI**: Excluded from documentation

### Readiness Probe - `/health/internal`

```csharp
var isDevelopment = app.Environment.IsDevelopment();
app.MapHealthChecks("/health/internal",
        new HealthCheckOptions
        {
            ResponseWriter = (ctx, report) =>
                ProcessHealthCheckResult(ctx, logger, healthCheckStore, report, outputResult: isDevelopment)
        })
    .RequireHost("localhost")
    .AddEndpointFilter(new LocalhostEndpointFilter(logger));
```

**Characteristics:**
- **Environment Aware**: Different responses for dev vs production
- **Localhost Only**: Restricted to internal container access
- **Simplified Output**: Production returns status string only
- **Detailed Development**: Full report in development environment

### Public Health Check - `/health`

```csharp
app.MapHealthChecks("/health",
        new HealthCheckOptions
        {
            ResponseWriter = (ctx, report) =>
                ProcessHealthCheckResult(ctx, logger, healthCheckStore, report, outputResult: true)
        })
    .RequireAuthorization();
```

**Characteristics:**
- **Authorized Access**: Requires authentication
- **Detailed Output**: Full health check report
- **Public Facing**: External monitoring system compatible
- **Comprehensive**: Includes all check details and timing

## Health Check Store Integration

### Automatic Registration

```csharp
var healthCheckStore = app.Services.GetService<HealthCheckStatusStore>() ?? new HealthCheckStatusStore();
```

This approach:
- **Simplifies Registration**: Works without explicit DI registration
- **Enables Querying**: Allows applications to check health status
- **Provides Caching**: Stores last health check result

### Status Tracking

```csharp
healthCheckStore.LastHealthStatus = report.Status;
```

Benefits:
- **Cached Results**: Liveness probe returns without execution
- **Performance**: Eliminates redundant health check runs
- **Consistency**: Same status across multiple endpoints

## Response Processing

### Conditional Output

```csharp
private static Task ProcessHealthCheckResult(
    HttpContext httpContext,
    ILogger logger,
    HealthCheckStatusStore healthCheckStore,
    HealthReport report,
    bool outputResult)
{
    LogHealthCheckResponse(logger, report);
    healthCheckStore.LastHealthStatus = report.Status;

    return outputResult
        ? WriteReportObject(httpContext, report)
        : httpContext.Response.WriteAsync(report.Status.ToString());
}
```

### Detailed Report Format

```csharp
private static Task WriteReportObject(HttpContext context, HealthReport report)
{
    var response = new
    {
        Status = report.Status.ToString(),
        Duration = report.TotalDuration,
        Info = report.Entries
            .Select(e =>
                new
                {
                    e.Key,
                    e.Value.Description,
                    e.Value.Duration,
                    Status = Enum.GetName(e.Value.Status),
                    Error = e.Value.Exception?.Message,
                    e.Value.Data
                })
            .ToList()
    };

    return context.Response.WriteAsJsonAsync(response, options: JsonSerializerOptions);
}
```

## Logging Integration

### Structured Logging

```csharp
[LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Health check response: {@HealthReport}")]
private static partial void LogSuccessfulHealthCheck(ILogger logger, HealthReport healthReport);

[LoggerMessage(EventId = 2, Message = "Health check failed: {FailedHealthReport}")]
private static partial void LogFailedHealthCheck(ILogger logger, LogLevel level, object failedHealthReport);
```

### Health Status Logging

```csharp
private static void LogHealthCheckResponse(ILogger logger, HealthReport report)
{
    if (report.Status is HealthStatus.Healthy)
    {
        LogSuccessfulHealthCheck(logger, report);
        return;
    }

    var logLevel = report.Status == HealthStatus.Unhealthy ? LogLevel.Error : LogLevel.Warning;

    var failedHealthReport = report.Entries.Select(e =>
        new { e.Key, e.Value.Status, e.Value.Duration, Error = e.Value.Exception?.Message });

    LogFailedHealthCheck(logger, logLevel, failedHealthReport);
}
```

## JSON Serialization Configuration

### Optimized Settings

```csharp
private static readonly JsonSerializerOptions JsonSerializerOptions = new()
{
    WriteIndented = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

**Benefits:**
- **Compact Output**: No indentation for efficiency
- **Consistent Naming**: camelCase for web standards
- **Clean Response**: Omits null values

## Security Considerations

### Localhost Restriction

```csharp
.RequireHost("localhost")
.AddEndpointFilter(new LocalhostEndpointFilter(logger))
```

**Purpose:**
- **Container Security**: Prevents external access to internal endpoint
- **Network Isolation**: Restricts to container networking
- **Attack Surface Reduction**: Limits health check exposure

### Authorization Requirement

```csharp
.RequireAuthorization()
```

**Benefits:**
- **Access Control**: Prevents unauthorized health monitoring
- **Information Security**: Protects system status information
- **Compliance**: Meets security audit requirements

## Cloud Platform Integration

### Kubernetes Health Checks

The endpoints integrate with Kubernetes probes:

```yaml
livenessProbe:
  httpGet:
    path: /status
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/internal
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

### Docker Health Checks

```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/status || exit 1
```

## Performance Characteristics

### Liveness Probe Performance
- **Sub-millisecond**: Cached status lookup
- **No I/O**: No database or external service calls
- **Minimal Allocation**: String return only

### Readiness Probe Performance
- **Environment Dependent**: Fast in production, detailed in development
- **I/O Bound**: Executes actual health checks
- **Comprehensive**: Tests all registered health checks

### Monitoring Integration
- **Structured Logs**: Machine-readable health status
- **Metrics Compatible**: Status codes for monitoring systems
- **Alert Friendly**: Clear success/failure indicators