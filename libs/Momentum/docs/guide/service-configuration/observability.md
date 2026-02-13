---
title: Observability
description: Comprehensive observability through OpenTelemetry integration, structured logging, and health monitoring for production-ready applications.
date: 2024-01-15
---

# Observability in Momentum

Momentum provides comprehensive observability out-of-the-box through OpenTelemetry integration, structured logging, and health monitoring. This ensures your applications are production-ready with full visibility into their behavior and performance.

## Overview

When you add service defaults to your application, observability is automatically configured:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Automatically includes:
// - Structured logging with Serilog
// - OpenTelemetry tracing, metrics, and logging
// - Health checks
// - Performance counters
builder.AddServiceDefaults();

var app = builder.Build();
await app.RunAsync(args);
```

## Structured Logging with Serilog

### Default Configuration

Momentum uses Serilog for structured logging with sensible defaults:

```csharp
// Automatically configured when calling AddServiceDefaults()
// Logs to:
// - Console (with structured formatting)
// - OpenTelemetry (for centralized collection)
// - Optional: File, database, or other sinks
```

### Using Structured Logging

```csharp
public static class CreateCashierCommandHandler
{
    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierCommand command,
        IMessageBus messaging,
        ILogger<CreateCashierCommandHandler> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating cashier for tenant {TenantId} with name {Name}",
            command.TenantId, command.Name);

        try
        {
            var dbCommand = CreateInsertCommand(command);
            var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

            var result = insertedCashier.ToModel();
            var createdEvent = new CashierCreated(result.TenantId, 0, result);

            logger.LogInformation("Successfully created cashier {CashierId} for tenant {TenantId}",
                result.Id, result.TenantId);

            return (result, createdEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create cashier for tenant {TenantId}", command.TenantId);
            throw;
        }
    }
}
```

### Custom Logging Configuration

```csharp
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### Log Correlation

Momentum automatically adds correlation IDs to track requests across services:

```csharp
public class RequestLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = context.TraceIdentifier,
            ["UserId"] = context.User?.FindFirst("sub")?.Value,
            ["TenantId"] = context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
        });

        await next(context);
    }
}
```

## OpenTelemetry Integration

### Automatic Configuration

OpenTelemetry is pre-configured for tracing, metrics, and logging:

```csharp
// Automatically configured in AddServiceDefaults():
// - HTTP request/response tracing
// - Database operation tracing
// - Custom activity tracing
// - Performance metrics
// - Log forwarding to collectors
```

### Custom Activities

Add custom activities for business operations:

```csharp
public static class CreateCashierCommandHandler
{
    private static readonly ActivitySource ActivitySource = new("AppDomain.Cashiers");

    public static async Task<(Result<Cashier>, CashierCreated?)> Handle(
        CreateCashierCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("CreateCashier");
        activity?.SetTag("tenant.id", command.TenantId.ToString());
        activity?.SetTag("cashier.name", command.Name);

        try
        {
            var dbCommand = CreateInsertCommand(command);
            var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

            var result = insertedCashier.ToModel();
            activity?.SetTag("cashier.id", result.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);

            return (result, new CashierCreated(result.TenantId, 0, result));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}

// Register activity source
builder.Services.ConfigureOpenTelemetryTracingBuilder(tracing =>
{
    tracing.AddSource("AppDomain.Cashiers");
});
```

### Custom Metrics

Create custom metrics for business KPIs:

```csharp
public class CashierMetrics
{
    private readonly Counter<int> _cashierCreatedCounter;
    private readonly Histogram<double> _cashierCreationDuration;
    private readonly Gauge<int> _activeCashiersGauge;

    public CashierMetrics(IMeterProvider meterProvider)
    {
        var meter = meterProvider.GetMeter("AppDomain.Cashiers");

        _cashierCreatedCounter = meter.CreateCounter<int>(
            "cashiers_created_total",
            "Total number of cashiers created");

        _cashierCreationDuration = meter.CreateHistogram<double>(
            "cashier_creation_duration",
            "Duration of cashier creation operations");

        _activeCashiersGauge = meter.CreateGauge<int>(
            "active_cashiers_count",
            "Current number of active cashiers");
    }

    public void RecordCashierCreated(Guid tenantId)
    {
        _cashierCreatedCounter.Add(1, new KeyValuePair<string, object?>("tenant.id", tenantId.ToString()));
    }

    public void RecordCreationDuration(TimeSpan duration, Guid tenantId)
    {
        _cashierCreationDuration.Record(duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("tenant.id", tenantId.ToString()));
    }
}
```

### OpenTelemetry Configuration

```csharp
// appsettings.json
{
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317",
    "ServiceName": "AppDomain.Api",
    "ServiceVersion": "1.0.0",
    "TracingEnabled": true,
    "MetricsEnabled": true,
    "LoggingEnabled": true
  }
}
```

```csharp
// Custom OpenTelemetry configuration
builder.Services.ConfigureOpenTelemetryTracingBuilder(tracing =>
{
    tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddSource("AppDomain.*")
        .AddJaegerExporter();
});

builder.Services.ConfigureOpenTelemetryMeterProviderBuilder(metrics =>
{
    metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("AppDomain.*")
        .AddPrometheusExporter();
});
```

## Health Checks

### Default Health Checks

Basic health checks are included automatically:

```csharp
var app = builder.Build();

// Map health check endpoints
app.MapDefaultHealthCheckEndpoints();

// Available endpoints:
// /status - Liveness probe (cached, no auth)
// /health/internal - Readiness probe (localhost only)
// /health - Public health (requires auth, detailed)
```

### Custom Health Checks

Add checks for your dependencies:

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDomainDb _db;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(AppDomainDb db, ILogger<DatabaseHealthCheck> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = Activity.Current?.Source.StartActivity("HealthCheck.Database");

            await _db.Database.CanConnectAsync(cancellationToken);

            _logger.LogDebug("Database health check passed");
            return HealthCheckResult.Healthy("Database is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}

public class KafkaHealthCheck : IHealthCheck
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaHealthCheck> _logger;

    public KafkaHealthCheck(IProducer<Null, string> producer, ILogger<KafkaHealthCheck> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check Kafka connectivity
            var metadata = _producer.GetMetadata(TimeSpan.FromSeconds(5));

            if (metadata.Brokers.Any())
            {
                _logger.LogDebug("Kafka health check passed - {BrokerCount} brokers available",
                    metadata.Brokers.Count);
                return HealthCheckResult.Healthy($"Kafka is accessible with {metadata.Brokers.Count} brokers");
            }

            return HealthCheckResult.Degraded("Kafka is accessible but no brokers available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka health check failed");
            return HealthCheckResult.Unhealthy("Kafka is not accessible", ex);
        }
    }
}

// Register health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<KafkaHealthCheck>("kafka", tags: ["ready"])
    .AddCheck("memory", () =>
    {
        var gc = GC.GetTotalMemory(false);
        var workingSet = Environment.WorkingSet;

        if (workingSet > 500_000_000) // 500MB
        {
            return HealthCheckResult.Degraded($"High memory usage: {workingSet / 1024 / 1024}MB");
        }

        return HealthCheckResult.Healthy($"Memory usage: {workingSet / 1024 / 1024}MB");
    }, tags: ["live"]);
```

### Advanced Health Check Configuration

```csharp
// Different endpoints for different purposes
app.MapHealthChecks("/health/internal", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live") || check.Tags.Count == 0
});

// Detailed health information
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                exception = entry.Value.Exception?.Message,
                duration = entry.Value.Duration
            }),
            totalDuration = report.TotalDuration
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});
```

## Application Performance Monitoring

### Request Tracking

Track request performance and errors:

```csharp
public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = Activity.Current?.Source.StartActivity("HTTP Request");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000) // Log slow requests
            {
                _logger.LogWarning("Slow request: {Method} {Path} took {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Request failed: {Method} {Path} after {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        finally
        {
            activity?.SetTag("http.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("http.status_code", context.Response.StatusCode);
        }
    }
}
```

### Command/Query Performance

Track CQRS operation performance:

```csharp
public class CommandPerformanceWrapper<TCommand, TResult>
{
    private readonly ILogger<CommandPerformanceWrapper<TCommand, TResult>> _logger;
    private readonly IMeter _meter;
    private readonly Histogram<double> _duration;

    public CommandPerformanceWrapper(ILogger<CommandPerformanceWrapper<TCommand, TResult>> logger, IMeterProvider meterProvider)
    {
        _logger = logger;
        _meter = meterProvider.GetMeter("AppDomain.Commands");
        _duration = _meter.CreateHistogram<double>(
            "command_duration",
            "Duration of command execution");
    }

    public async Task<TResult> ExecuteAsync<THandler>(
        TCommand command,
        Func<TCommand, Task<TResult>> handler,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        using var activity = Activity.Current?.Source.StartActivity($"Command.{commandName}");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing command {CommandName}", commandName);

            var result = await handler(command);

            stopwatch.Stop();
            _duration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("command.name", commandName),
                new KeyValuePair<string, object?>("command.success", true));

            _logger.LogInformation("Command {CommandName} completed in {Duration}ms",
                commandName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _duration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("command.name", commandName),
                new KeyValuePair<string, object?>("command.success", false));

            _logger.LogError(ex, "Command {CommandName} failed after {Duration}ms",
                commandName, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

## Error Tracking and Alerting

### Error Aggregation

Track and categorize errors:

```csharp
public class ErrorTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorTrackingMiddleware> _logger;
    private readonly Counter<int> _errorCounter;

    public ErrorTrackingMiddleware(RequestDelegate next, ILogger<ErrorTrackingMiddleware> logger, IMeterProvider meterProvider)
    {
        _next = next;
        _logger = logger;
        var meter = meterProvider.GetMeter("AppDomain.Errors");
        _errorCounter = meter.CreateCounter<int>("errors_total", "Total number of errors");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var errorType = ex.GetType().Name;
            var endpoint = context.Request.Path.Value ?? "unknown";

            _errorCounter.Add(1,
                new KeyValuePair<string, object?>("error.type", errorType),
                new KeyValuePair<string, object?>("endpoint", endpoint),
                new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));

            _logger.LogError(ex, "Unhandled exception in {Endpoint}: {ErrorType}",
                endpoint, errorType);

            throw;
        }
    }
}
```

## Monitoring Dashboard Integration

### Grafana Dashboard Configuration

Example dashboard configuration for key metrics:

```json
{
  "dashboard": {
    "title": "AppDomain API Monitoring",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total[5m])",
            "legendFormat": "{{method}} {{endpoint}}"
          }
        ]
      },
      {
        "title": "Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "title": "Error Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(errors_total[5m])",
            "legendFormat": "{{error_type}}"
          }
        ]
      },
      {
        "title": "Active Cashiers",
        "type": "singlestat",
        "targets": [
          {
            "expr": "active_cashiers_count",
            "legendFormat": "Active Cashiers"
          }
        ]
      }
    ]
  }
}
```

### Application Insights Integration

```csharp
// appsettings.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key"
  }
}

// Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

## Production Observability Checklist

### Logging
- [ ] Structured logging with correlation IDs
- [ ] Appropriate log levels configured
- [ ] Sensitive data excluded from logs
- [ ] Log aggregation configured (ELK, Splunk, etc.)
- [ ] Log retention policies defined

### Metrics
- [ ] Business metrics defined and tracked
- [ ] Infrastructure metrics collected
- [ ] Custom dashboards created
- [ ] Alerting rules configured
- [ ] SLA/SLO metrics tracked

### Tracing
- [ ] Distributed tracing enabled
- [ ] Critical paths instrumented
- [ ] External dependencies traced
- [ ] Performance baselines established
- [ ] Trace sampling configured

### Health Checks
- [ ] All dependencies health checked
- [ ] Different probe types configured (liveness/readiness)
- [ ] Health check endpoints secured
- [ ] Monitoring integrated with orchestration platform
- [ ] Graceful degradation handled

### Alerting
- [ ] Error rate alerts configured
- [ ] Performance degradation alerts set
- [ ] Infrastructure alerts integrated
- [ ] On-call procedures documented
- [ ] Alert fatigue minimized

## Best Practices

### Logging
1. **Use structured logging**: Always use structured logging with meaningful properties
2. **Include correlation IDs**: Track requests across service boundaries
3. **Log at appropriate levels**: Debug < Information < Warning < Error < Critical
4. **Avoid logging sensitive data**: Never log passwords, tokens, or PII

### Metrics
1. **Focus on business metrics**: Track what matters to your business
2. **Use consistent naming**: Follow naming conventions for metrics
3. **Add appropriate dimensions**: Enable filtering and grouping
4. **Monitor trends**: Look at rates of change, not just absolute values

### Tracing
1. **Instrument critical paths**: Focus on user-facing operations
2. **Add meaningful tags**: Include business context in traces
3. **Consider sampling**: Balance detail with performance
4. **Connect traces to logs**: Use correlation for debugging

### Performance
1. **Set baselines**: Establish performance expectations
2. **Monitor continuously**: Don't just check during incidents
3. **Alert proactively**: Catch issues before users notice
4. **Optimize based on data**: Use metrics to guide optimization

## Next Steps

- Learn about [Service Defaults](./service-defaults) for comprehensive service configuration
- Understand [API Setup](./api-setup) for REST and gRPC configuration
- Explore [Testing](../testing/) strategies for observability
- See [Troubleshooting](../troubleshooting) for common observability issues
