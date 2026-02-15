# Momentum Libraries Hardening Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Harden Momentum libraries with security, reliability, and observability improvements across ServiceDefaults, ServiceDefaults.Api, SourceGenerators, and XmlDocs/EventMarkdownGenerator.

**Architecture:** Each task modifies one focused area. Changes follow existing patterns (extension methods, options binding, IOptions). Tests use xunit.v3, Shouldly, NSubstitute. Source generator tests use the existing `TestHelpers` harness.

**Tech Stack:** .NET 10, Serilog, OpenTelemetry, Asp.Versioning, Roslyn source generators, Dapper, Fluid templates

---

### Task 1: Console sink on bootstrap logger

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/Logging/LoggingSetupExtensions.cs:18-25`

**Step 1: Add `.WriteTo.Console()` to the bootstrap logger**

In `UseInitializationLogger`, add the Console sink so init logs are visible before OpenTelemetry is ready:

```csharp
public static void UseInitializationLogger(this WebApplicationBuilder builder)
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.OpenTelemetry()
        .CreateBootstrapLogger();
}
```

The `Serilog.AspNetCore` package already includes `Serilog.Sinks.Console` transitively, so no new package reference is needed.

**Step 2: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults/Logging/LoggingSetupExtensions.cs
git commit -m "feat(service-defaults): add Console sink to bootstrap logger for init visibility"
```

---

### Task 2: Connection string format validation

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/Messaging/Wolverine/WolverineNpgsqlExtensions.cs:38-39`

**Step 1: Add format validation after null check**

After the null/whitespace check, validate the connection string can be parsed:

```csharp
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException($"The DB string '{ServiceBusOptions.SectionName}' is not set.");

try
{
    _ = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
}
catch (ArgumentException ex)
{
    throw new InvalidOperationException(
        $"The DB connection string '{ServiceBusOptions.SectionName}' has an invalid format: {ex.Message}", ex);
}
```

The `Npgsql` package is already referenced transitively via `Aspire.Npgsql`.

**Step 2: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults/Messaging/Wolverine/WolverineNpgsqlExtensions.cs
git commit -m "fix(service-defaults): validate connection string format at startup"
```

---

### Task 3: Configurable OpenTelemetry sampling rate + resource attributes

**Files:**
- Create: `libs/Momentum/src/Momentum.ServiceDefaults/OpenTelemetry/OpenTelemetryOptions.cs`
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/OpenTelemetry/OpenTelemetrySetupExtensions.cs`

**Step 1: Create OpenTelemetryOptions**

```csharp
// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.ServiceDefaults.OpenTelemetry;

/// <summary>
///     Configuration options for OpenTelemetry instrumentation.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    ///     The configuration section name for OpenTelemetry options.
    /// </summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    ///     Sampling rate for production traces (0.0 to 1.0). Default: 0.1 (10%).
    /// </summary>
    public double ProductionSamplingRate { get; set; } = 0.1;
}
```

**Step 2: Update OpenTelemetrySetupExtensions to use options and add resource attributes**

Replace the hard-coded constant with options-bound value, and add `service.instance.id` + `host.name` resource attributes:

```csharp
// Near top of AddOpenTelemetry method, after existing config reads:
var otelOptions = new OpenTelemetryOptions();
builder.Configuration.GetSection(OpenTelemetryOptions.SectionName).Bind(otelOptions);

// Replace the sampling rate line (line 114-115):
var samplingRate = builder.Environment.IsDevelopment()
    ? DevelopmentSamplingRate
    : otelOptions.ProductionSamplingRate;

// ... .SetSampler(new TraceIdRatioBasedSampler(samplingRate)));

// Update ConfigureResource block (lines 67-71):
.ConfigureResource(resource => resource
    .AddAttributes(new Dictionary<string, object>
    {
        ["env"] = builder.Environment.EnvironmentName,
        ["service.instance.id"] = Environment.MachineName,
        ["host.name"] = Environment.MachineName
    }))
```

Remove the `ProductionSamplingRate` constant (keep `DevelopmentSamplingRate` as it's always 100%).

**Step 3: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults/`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults/OpenTelemetry/
git commit -m "feat(service-defaults): make sampling rate configurable and add resource attributes"
```

---

### Task 4: Request size limits + Kestrel configuration

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/ServiceDefaultsExtensions.cs:73-97`

**Step 1: Add Kestrel request size limit configuration**

In `AddServiceDefaults`, after `UseKestrelHttpsConfiguration()`, add request body size limiting from configuration:

```csharp
builder.WebHost.UseKestrelHttpsConfiguration();

// Configure request size limits from configuration
var maxRequestBodySize = builder.Configuration.GetValue<long?>("Kestrel:Limits:MaxRequestBodySize");
if (maxRequestBodySize.HasValue)
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxRequestBodySize = maxRequestBodySize.Value;
    });
}
```

Add `using Microsoft.AspNetCore.Hosting;` and `using Microsoft.Extensions.Configuration;` if not already imported.

**Step 2: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults/ServiceDefaultsExtensions.cs
git commit -m "feat(service-defaults): add configurable Kestrel request size limits"
```

---

### Task 5: Health check cache expiry + HealthCheckStatusStore DI registration

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/HealthChecks/HealthCheckStatusStore.cs`
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/HealthChecks/HealthCheckSetupExtensions.cs:38,45-46`
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/ServiceDefaultsExtensions.cs`

**Step 1: Add timestamp tracking to HealthCheckStatusStore**

```csharp
public class HealthCheckStatusStore
{
    private int _lastHealthStatus = (int)HealthStatus.Healthy;
    private long _lastUpdatedTicks = DateTime.UtcNow.Ticks;

    public HealthStatus LastHealthStatus
    {
        get => (HealthStatus)Volatile.Read(ref _lastHealthStatus);
        set
        {
            Interlocked.Exchange(ref _lastHealthStatus, (int)value);
            Interlocked.Exchange(ref _lastUpdatedTicks, DateTime.UtcNow.Ticks);
        }
    }

    public DateTime LastUpdated => new(Volatile.Read(ref _lastUpdatedTicks), DateTimeKind.Utc);
}
```

**Step 2: Register as singleton in AddServiceDefaults**

In `ServiceDefaultsExtensions.AddServiceDefaults`, before `builder.Services.AddHealthChecks()`:

```csharp
builder.Services.TryAddSingleton<HealthCheckStatusStore>();
builder.Services.AddHealthChecks();
```

Add `using Microsoft.Extensions.DependencyInjection.Extensions;` and `using Momentum.ServiceDefaults.HealthChecks;`.

**Step 3: Update HealthCheckSetupExtensions to use DI and add cache expiry**

Replace line 38:
```csharp
// Old:
var healthCheckStore = app.Services.GetService<HealthCheckStatusStore>() ?? new HealthCheckStatusStore();

// New:
var healthCheckStore = app.Services.GetRequiredService<HealthCheckStatusStore>();
```

Update the `/status` endpoint (lines 43-51) to check staleness:

```csharp
app.MapGet("/status", () =>
    {
        var isCacheStale = DateTime.UtcNow - healthCheckStore.LastUpdated > TimeSpan.FromSeconds(30);

        var statusCode = healthCheckStore.LastHealthStatus is not HealthStatus.Unhealthy && !isCacheStale
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return Results.Text(healthCheckStore.LastHealthStatus.ToString(), statusCode: statusCode);
    })
    .ExcludeFromDescription();
```

**Step 4: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults/`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults/HealthChecks/ libs/Momentum/src/Momentum.ServiceDefaults/ServiceDefaultsExtensions.cs
git commit -m "fix(service-defaults): register HealthCheckStatusStore as singleton, add cache expiry"
```

---

### Task 6: Resilience pipeline configuration

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/ServiceDefaultsExtensions.cs:87-94`

**Step 1: Configure resilience handler with explicit settings**

Replace the bare `AddStandardResilienceHandler()` with configured settings:

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);

        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        options.Retry.BackoffType = DelayBackoffType.Exponential;

        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
    });

    http.AddServiceDiscovery();
});
```

Add `using Polly;` if needed for `DelayBackoffType`.

**Step 2: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults/ServiceDefaultsExtensions.cs
git commit -m "feat(service-defaults): configure resilience with timeout, retry, and circuit breaker"
```

---

### Task 7: Configuration validation at startup

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults/Messaging/MessagingSetupExtensions.cs`

**Step 1: Add ValidateOnStart for ServiceBusOptions**

Find where `ServiceBusOptions` is configured and add validation:

```csharp
// After the Configure<ServiceBusOptions> call, add:
builder.Services.AddOptionsWithValidateOnStart<ServiceBusOptions>();
```

Or, if using `IPostConfigureOptions`, ensure `ValidateOnStart()` is chained.

Read the file first to see the exact configuration pattern.

**Step 2: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults/Messaging/
git commit -m "feat(service-defaults): validate ServiceBusOptions on startup"
```

---

### Task 8: Rate limiting

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs`

**Step 1: Add rate limiting to AddApiServiceDefaults**

In `AddApiServiceDefaults`, add rate limiting with fixed window policy:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 100;
        limiterOptions.QueueLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});
```

Add `using System.Threading.RateLimiting;` and `using Microsoft.AspNetCore.RateLimiting;`.

In `ConfigureApiUsingDefaults`, add `app.UseRateLimiter();` after `UseRouting()`.

**Step 2: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults.Api/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs
git commit -m "feat(api): add rate limiting with fixed window policy"
```

---

### Task 9: Custom exception handler with ProblemDetails

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs:123-127`

**Step 1: Add custom exception handler**

In `ConfigureApiUsingDefaults`, replace the bare `app.UseExceptionHandler()` for non-dev with a ProblemDetails-based handler:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseExceptionHandler(exceptionApp =>
    {
        exceptionApp.Run(async context =>
        {
            context.Response.ContentType = "application/problem+json";

            var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var exception = exceptionFeature?.Error;

            var (statusCode, title) = exception switch
            {
                FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            context.Response.StatusCode = statusCode;

            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = app.Environment.IsDevelopment() ? exception?.Message : null,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
        });
    });
}
```

**Step 2: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults.Api/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs
git commit -m "feat(api): add ProblemDetails-based exception handler"
```

---

### Task 10: Frontend integration - response compression + HTTPS redirection

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/FrontendIntegration/FrontendIntegrationExtensions.cs`

**Step 1: Add response compression and HTTPS redirection**

In `AddFrontendIntegration`:

```csharp
public static IHostApplicationBuilder AddFrontendIntegration(this WebApplicationBuilder builder)
{
    builder.AddCorsFromConfiguration();
    builder.Services.Configure<SecurityHeaderSettings>(
        builder.Configuration.GetSection("SecurityHeaders"));

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Fastest;
    });

    return builder;
}
```

In `UseFrontendIntegration`:

```csharp
public static WebApplication UseFrontendIntegration(this WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseResponseCompression();
    app.UseCors(CorsPolicyName);
    app.UseSecurityHeaders();

    return app;
}
```

**Step 2: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults.Api/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults.Api/FrontendIntegration/
git commit -m "feat(api): add response compression and HTTPS redirection to frontend integration"
```

---

### Task 11: API versioning

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/Momentum.ServiceDefaults.Api.csproj`
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs`
- Modify: `/home/vgmello/repos/momentum/Directory.Packages.props`

**Step 1: Add Asp.Versioning.Http package**

Add to `Directory.Packages.props`:
```xml
<PackageVersion Include="Asp.Versioning.Http" Version="8.1.0" />
```

Add to `Momentum.ServiceDefaults.Api.csproj`:
```xml
<PackageReference Include="Asp.Versioning.Http"/>
```

**Step 2: Add versioning extension**

In `AddApiServiceDefaults`, add API versioning:

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
});
```

**Step 3: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults.Api/`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Directory.Packages.props libs/Momentum/src/Momentum.ServiceDefaults.Api/
git commit -m "feat(api): add URL segment API versioning support"
```

---

### Task 12: gRPC message size limits + silent failure warning

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs:74-77`
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/GrpcRegistrationExtensions.cs:64-73`

**Step 1: Make gRPC message size configurable**

Replace the gRPC setup in `AddApiServiceDefaults`:

```csharp
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();

    var maxReceiveSize = builder.Configuration.GetValue<int?>("Grpc:Limits:MaxReceiveMessageSize");
    var maxSendSize = builder.Configuration.GetValue<int?>("Grpc:Limits:MaxSendMessageSize");

    if (maxReceiveSize.HasValue)
        options.MaxReceiveMessageSize = maxReceiveSize.Value;
    if (maxSendSize.HasValue)
        options.MaxSendMessageSize = maxSendSize.Value;
});
```

Add `using Microsoft.Extensions.Configuration;` if not present.

**Step 2: Add warning when no gRPC services found**

In `GrpcRegistrationExtensions.MapGrpcServices(IEndpointRouteBuilder, Assembly)`:

```csharp
public static void MapGrpcServices(this IEndpointRouteBuilder routeBuilder, Assembly assembly)
{
    var grpcServiceTypes = assembly.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false, IsInterface: false, IsGenericType: false } && IsGrpcService(type))
        .ToList();

    if (grpcServiceTypes.Count == 0)
    {
        var logger = routeBuilder.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("GrpcRegistration");
        logger?.LogWarning("No gRPC services found in assembly {AssemblyName}", assembly.GetName().Name);
        return;
    }

    foreach (var grpcServiceType in grpcServiceTypes)
    {
        MapGrpcService(grpcServiceType, routeBuilder);
    }
}
```

Add `using Microsoft.Extensions.DependencyInjection;` and `using Microsoft.Extensions.Logging;`.

**Step 3: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults.Api/`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs libs/Momentum/src/Momentum.ServiceDefaults.Api/GrpcRegistrationExtensions.cs
git commit -m "feat(api): configurable gRPC message sizes and log warning on no services found"
```

---

### Task 13: JSON serialization defaults

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs`

**Step 1: Configure JSON defaults in AddApiServiceDefaults**

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
```

**Step 2: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults.Api/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs
git commit -m "feat(api): configure JSON serialization defaults"
```

---

### Task 14: OpenAPI security scheme

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/OpenApi/Extensions/OpenApiExtensions.cs`

**Step 1: Add Bearer security scheme document transformer**

```csharp
public static OpenApiOptions ConfigureOpenApiDefaults(this OpenApiOptions options)
{
    // Normalize server URLs by removing trailing slashes
    options.AddDocumentTransformer((document, _, _) =>
    {
        if (document.Servers is not null)
        {
            foreach (var server in document.Servers)
            {
                server.Url = server.Url?.TrimEnd('/');
            }
        }

        return Task.CompletedTask;
    });

    // Add Bearer authentication security scheme
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new Microsoft.OpenApi.Models.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token"
        };

        return Task.CompletedTask;
    });

    return options;
}
```

**Step 2: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults.Api/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults.Api/OpenApi/
git commit -m "feat(api): add Bearer security scheme to OpenAPI spec"
```

---

### Task 15: HTTP logging environment check

**Files:**
- Modify: `libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs:65-66,110`

**Step 1: Conditionally add and use HTTP logging**

In `AddApiServiceDefaults`, wrap with environment check:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging();
}
```

In `ConfigureApiUsingDefaults`, wrap:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
}
app.UseRouting();
```

**Step 2: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.ServiceDefaults.Api/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.ServiceDefaults.Api/ApiExtensions.cs
git commit -m "fix(api): only enable HTTP logging in development environment"
```

---

### Task 16: Source generator exception handling

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/DbCommandSourceGenerator.cs:41-56`
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/DbCommandAnalyzers.cs`

**Step 1: Add MMT004 diagnostic descriptor to DbCommandAnalyzers**

```csharp
internal static readonly DiagnosticDescriptor UnexpectedGeneratorError = new(
    id: "MMT004",
    title: "Unexpected error during source generation",
    messageFormat: "An unexpected error occurred while generating code for '{0}': {1}",
    category: "DbCommandSourceGenerator",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);
```

**Step 2: Wrap RegisterSourceOutput in try-catch**

In `DbCommandSourceGenerator.Initialize`:

```csharp
context.RegisterSourceOutput(commandTypes, static (spc, dbCommandTypeInfo) =>
{
    try
    {
        foreach (var diagnostic in dbCommandTypeInfo.DiagnosticsToReport)
        {
            spc.ReportDiagnostic(diagnostic);
        }

        if (dbCommandTypeInfo.HasErrors)
        {
            return;
        }

        GenerateDbExtensionsPart(spc, dbCommandTypeInfo);
        GenerateHandlerPart(spc, dbCommandTypeInfo);
    }
    catch (Exception ex)
    {
        var diagnostic = Diagnostic.Create(
            DbCommandAnalyzers.UnexpectedGeneratorError,
            Location.None,
            dbCommandTypeInfo.TypeName,
            ex.Message);
        spc.ReportDiagnostic(diagnostic);
    }
});
```

**Step 3: Build to verify compilation**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.SourceGenerators/`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/
git commit -m "fix(source-gen): add exception handling in generator pipeline with MMT004 diagnostic"
```

---

### Task 17: SQL identifier validation for fn parameter

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/DbCommandAnalyzers.cs`
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/DbCommandTypeInfo.SourceGen.cs`

**Step 1: Add MMT005 diagnostic and validator to DbCommandAnalyzers**

```csharp
private static readonly DiagnosticDescriptor InvalidFunctionNameError = new(
    id: "MMT005",
    title: "Invalid SQL function name in DbCommandAttribute",
    messageFormat: "Class '{0}' has an invalid function name '{1}' in DbCommandAttribute. " +
                   "Function names must be valid SQL identifiers (letters, digits, underscores) " +
                   "optionally schema-qualified (schema.name) or bracket/quote-delimited ([name] or \"name\").",
    category: "DbCommandSourceGenerator",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

// SQL identifier: plain, [bracketed], or "quoted", with optional schema qualification
private static readonly System.Text.RegularExpressions.Regex ValidSqlIdentifierPattern = new(
    @"^(\$)?(\""?[a-zA-Z_]\w*\""?|\[[a-zA-Z_]\w*\])(\.\s*(\""?[a-zA-Z_]\w*\""?|\[[a-zA-Z_]\w*\]))*$",
    System.Text.RegularExpressions.RegexOptions.Compiled);

public static void ExecuteInvalidFunctionNameAnalyzer(INamedTypeSymbol typeSymbol,
    DbCommandAttribute dbCommandAttribute, ImmutableArray<Diagnostic>.Builder diagnostics)
{
    if (string.IsNullOrWhiteSpace(dbCommandAttribute.Fn))
        return;

    var fnName = dbCommandAttribute.Fn;
    // Strip the $ prefix for validation (it's a valid Momentum convention)
    var nameToValidate = fnName.StartsWith("$") ? fnName[1..] : fnName;

    if (!ValidSqlIdentifierPattern.IsMatch(nameToValidate))
    {
        var typeLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
        var diagnostic = Diagnostic.Create(InvalidFunctionNameError, typeLocation, typeSymbol.Name, fnName);
        diagnostics.Add(diagnostic);
    }
}
```

**Step 2: Wire analyzer into ExecuteAnalyzers in DbCommandTypeInfo.SourceGen.cs**

In the `ExecuteAnalyzers` method:

```csharp
private ImmutableArray<Diagnostic> ExecuteAnalyzers(INamedTypeSymbol typeSymbol)
{
    var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    DbCommandAnalyzers.ExecuteMissingInterfaceAnalyzer(typeSymbol, ResultType, DbCommandAttribute, diagnostics);
    DbCommandAnalyzers.ExecuteNonQueryWithNonIntegralResultAnalyzer(typeSymbol, ResultType, DbCommandAttribute, diagnostics);
    DbCommandAnalyzers.ExecuteMutuallyExclusivePropertiesAnalyzer(typeSymbol, DbCommandAttribute, diagnostics);
    DbCommandAnalyzers.ExecuteInvalidFunctionNameAnalyzer(typeSymbol, DbCommandAttribute, diagnostics);

    return diagnostics.ToImmutable();
}
```

**Step 3: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.SourceGenerators/`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/
git commit -m "feat(source-gen): add SQL identifier validation for fn parameter with MMT005 diagnostic"
```

---

### Task 18: NonQuery analyzer fix - use IsIntegralType

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/DbCommandAnalyzers.cs:60-71`

**Step 1: Fix the analyzer to use IsIntegralType flag**

```csharp
public static void ExecuteNonQueryWithNonIntegralResultAnalyzer(INamedTypeSymbol typeSymbol,
    DbCommandTypeInfo.ResultTypeInfo? resultTypeInfo, DbCommandAttribute dbCommandAttribute, ImmutableArray<Diagnostic>.Builder diagnostics)
{
    if (dbCommandAttribute.NonQuery && resultTypeInfo is { IsIntegralType: false })
    {
        var typeLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
        var diagnostic = Diagnostic.Create(NonQueryWithGenericResultWarning,
            typeLocation, typeSymbol.Name, resultTypeInfo.TypeName);

        diagnostics.Add(diagnostic);
    }
}
```

This now properly checks all integral types (Int16, Int32, Int64, etc.) instead of only checking `"Int32"` by string comparison.

**Step 2: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.SourceGenerators/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/DbCommandAnalyzers.cs
git commit -m "fix(source-gen): use IsIntegralType flag instead of string comparison in NonQuery analyzer"
```

---

### Task 19: GeneratedCode attribute + StringBuilder capacity + XML docs on generated code

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/SourceGenBaseWriter.cs`
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/Writers/DbCommandHandlerSourceGenWriter.cs`
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/Writers/DbCommandDbParamsSourceGenWriter.cs`

**Step 1: Add GeneratedCode attribute helper to SourceGenBaseWriter**

```csharp
protected static void AppendFileHeader(StringBuilder sb)
{
    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
}

protected const string GeneratedCodeAttribute =
    "[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"Momentum.SourceGenerators\", \"1.0\")]";
```

**Step 2: Add GeneratedCode attribute and inheritdoc to handler writer**

In `DbCommandHandlerSourceGenWriter.Write`, add capacity and attribute:

```csharp
var sourceBuilder = new StringBuilder(512);

// ... after class declaration line:
if (!dbCommandTypeInfo.IsNestedType)
{
    var handlerClassName = $"{dbCommandTypeInfo.TypeName}Handler";
    sourceBuilder.AppendLine(GeneratedCodeAttribute);
    sourceBuilder.AppendLine($"public static class {handlerClassName}");
    sourceBuilder.AppendLine("{");
}

// Before the HandleAsync declaration:
sourceBuilder.AppendLine("    /// <inheritdoc />");
sourceBuilder.AppendLine(
    $"    public static async {returnTypeDeclaration} HandleAsync(...");
```

**Step 3: Add capacity and GeneratedCode to DbParams writer**

In `DbCommandDbParamsSourceGenWriter.Write`:

```csharp
var sourceBuilder = new StringBuilder(256);
```

**Step 4: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.SourceGenerators/`
Expected: Build succeeded

**Step 5: Run existing tests**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.SourceGenerators.Tests/`
Expected: Tests pass (some verified output strings may need updating due to added attributes)

**Step 6: Update test expectations if needed**

The existing snapshot tests compare generated output. If they fail, update the expected strings to include the `[GeneratedCode]` attribute and `/// <inheritdoc />`.

**Step 7: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.SourceGenerators/
git commit -m "feat(source-gen): add GeneratedCode attribute, XML docs, and StringBuilder capacity"
```

---

### Task 20: DbCommandIgnore attribute for property exclusion

**Files:**
- Create: `libs/Momentum/src/Momentum.Extensions.Abstractions/Dapper/DbCommandIgnoreAttribute.cs`
- Modify: `libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/DbCommandTypeInfo.SourceGen.cs:297-301`

**Step 1: Create the DbCommandIgnoreAttribute**

```csharp
// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.Abstractions.Dapper;

/// <summary>
///     Marks a property to be excluded from database parameter generation by the DbCommand source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class DbCommandIgnoreAttribute : Attribute;
```

**Step 2: Filter ignored properties in DbCommandTypeInfo.SourceGen.cs**

Update the `GetDbCommandObjProperties` method to filter out properties with `DbCommandIgnoreAttribute`:

```csharp
private static readonly string DbCommandIgnoreAttributeName = typeof(DbCommandIgnoreAttribute).FullName!;

// In GetDbCommandObjProperties, update the primary constructor params:
if (typeSymbol.IsRecord)
{
    var primaryConstructor = typeSymbol.Constructors.FirstOrDefault(c => c.IsPrimaryConstructor());
    primaryProperties = primaryConstructor?.Parameters
        .Where(p => p.GetAttribute(DbCommandIgnoreAttributeName) is null)
        .Select(p => new PropertyInfo(p.Name, GetParameterName(p, paramsCase, settings)))
        .ToDictionary(p => p.PropertyName, p => p) ?? [];
}

// Update normal property filtering:
var normalProps = typeSymbol.GetMembers()
    .OfType<IPropertySymbol>()
    .Where(p => !primaryProperties.ContainsKey(p.Name))
    .Where(p => p is { DeclaredAccessibility: Accessibility.Public, IsStatic: false, GetMethod: not null })
    .Where(p => p.GetAttributes().All(a => a.AttributeClass?.ToDisplayString() != DbCommandIgnoreAttributeName))
    .Select(p => new PropertyInfo(p.Name, GetParameterName(p, paramsCase, settings)));
```

**Step 3: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.SourceGenerators/ && dotnet build libs/Momentum/src/Momentum.Extensions.Abstractions/`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.Abstractions/Dapper/DbCommandIgnoreAttribute.cs libs/Momentum/src/Momentum.Extensions.SourceGenerators/DbCommand/DbCommandTypeInfo.SourceGen.cs
git commit -m "feat(source-gen): add DbCommandIgnore attribute to exclude properties from parameter generation"
```

---

### Task 21: Source generator edge case tests

**Files:**
- Modify: `libs/Momentum/tests/Momentum.Extensions.SourceGenerators.Tests/`

**Step 1: Read existing test patterns**

Read `DbCommandSourceGen.HandlerTests.cs` and `TestHelpers.cs` to understand the test harness.

**Step 2: Add nested type test**

Add a test for nested types to the handler tests file:

```csharp
[Fact]
public Task Handler_NestedType_ShouldGenerateCorrectly()
{
    var source = """
        using Momentum.Extensions.Abstractions.Dapper;
        using Momentum.Extensions.Abstractions.Messaging;

        namespace TestApp;

        public static partial class Customers
        {
            [DbCommand(sp: "get_customer")]
            public partial record GetCustomer(int Id) : IQuery<Customer>;
        }

        public record Customer(int Id, string Name);
        """;

    return TestHelpers.VerifyHandlerGeneration(source);
}
```

**Step 3: Add nullable parameter test**

```csharp
[Fact]
public Task Handler_NullableParameters_ShouldGenerateCorrectly()
{
    var source = """
        using Momentum.Extensions.Abstractions.Dapper;
        using Momentum.Extensions.Abstractions.Messaging;

        namespace TestApp;

        [DbCommand(sp: "update_customer")]
        public partial record UpdateCustomer(int Id, string? Name, int? Age) : ICommand<int>;
        """;

    return TestHelpers.VerifyHandlerGeneration(source);
}
```

**Step 4: Add DbCommandIgnore test**

```csharp
[Fact]
public Task DbParams_IgnoredProperty_ShouldBeExcluded()
{
    var source = """
        using Momentum.Extensions.Abstractions.Dapper;
        using Momentum.Extensions.Abstractions.Messaging;

        namespace TestApp;

        [DbCommand]
        public partial record CreateOrder(string Name, [DbCommandIgnore] string InternalTag) : ICommand<int>;
        """;

    return TestHelpers.VerifyDbParamsGeneration(source);
}
```

**Step 5: Add SQL injection validation test**

```csharp
[Fact]
public Task Handler_InvalidFunctionName_ShouldReportDiagnostic()
{
    var source = """
        using Momentum.Extensions.Abstractions.Dapper;
        using Momentum.Extensions.Abstractions.Messaging;

        namespace TestApp;

        [DbCommand(fn: "users); DROP TABLE users; --")]
        public partial record MaliciousQuery(int Id) : IQuery<int>;
        """;

    return TestHelpers.VerifyDiagnostic(source, "MMT005");
}
```

**Step 6: Run tests**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.SourceGenerators.Tests/`
Expected: New tests pass (some may need snapshot updates)

**Step 7: Commit**

```bash
git add libs/Momentum/tests/Momentum.Extensions.SourceGenerators.Tests/
git commit -m "test(source-gen): add edge case tests for nested types, nullable params, ignore attribute"
```

---

### Task 22: Assembly loading timeout in EventMarkdownGenerator

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/GenerateCommand.cs:130-160,257-261`

**Step 1: Add timeout to assembly loading**

Wrap the assembly loading in a timeout:

```csharp
foreach (var assemblyPath in options.AssemblyPaths)
{
    IsolatedAssemblyLoadContext? loadContext = null;
    try
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var assembly = await Task.Run(
            () => LoadAssemblyWithDependencyResolution(assemblyPath, out loadContext),
            cts.Token);

        var events = AssemblyEventDiscovery.DiscoverEvents(assembly, xmlParser);
        // ... rest unchanged
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Timed out loading assembly {assemblyPath} (30s limit)");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to process assembly {assemblyPath}: {ex.Message.EscapeMarkup()}");
    }
    finally
    {
        loadContext?.Dispose();
    }
}
```

**Step 2: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/GenerateCommand.cs
git commit -m "fix(event-markdown): add 30s timeout for assembly loading"
```

---

### Task 23: Async template loading in FluidMarkdownGenerator

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/FluidMarkdownGenerator.cs:142-161`

**Step 1: Make GetTemplate and GetEmbeddedTemplate async**

Since the constructor calls `GetTemplate` synchronously and `FluidParser.Parse` is sync, the best approach is to change `File.ReadAllText` to `File.ReadAllTextAsync` and use a factory method pattern:

Actually, since the constructor is sync and Fluid parsing is sync, and these templates are loaded once at init, the pragmatic fix is to use `File.ReadAllTextAsync` with `.GetAwaiter().GetResult()` or keep it sync (it's initialization-time). The simpler fix is to leave the init sync but add a static async factory:

```csharp
private static async Task<string> GetTemplateAsync(string templateName, string? customTemplatesDirectory)
{
    try
    {
        if (!string.IsNullOrEmpty(customTemplatesDirectory))
        {
            var customTemplatePath = Path.Combine(customTemplatesDirectory, templateName);

            if (File.Exists(customTemplatePath))
                return await File.ReadAllTextAsync(customTemplatePath);
        }

        return await GetEmbeddedTemplateAsync(templateName);
    }
    catch (Exception ex) when (ex is not FileNotFoundException)
    {
        throw new InvalidOperationException($"Failed to load template '{templateName}': {ex.Message}", ex);
    }
}

private static async Task<string> GetEmbeddedTemplateAsync(string templateName)
{
    try
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;

        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);

            if (!string.IsNullOrEmpty(assemblyDir))
            {
                var templatePath = Path.Combine(assemblyDir, "Templates", Path.GetFileName(templateName));

                if (File.Exists(templatePath))
                    return await File.ReadAllTextAsync(templatePath);
            }
        }

        var resourceName = $"Momentum.Extensions.EventMarkdownGenerator.Templates.{templateName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Template '{templateName}' not found as content file or embedded resource. Expected location: Templates/{templateName}");
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
    catch (Exception ex) when (ex is not FileNotFoundException)
    {
        throw new InvalidOperationException($"Failed to load template '{templateName}': {ex.Message}", ex);
    }
}
```

Add a static factory method:

```csharp
public static async Task<FluidMarkdownGenerator> CreateAsync(string? customTemplatesDirectory = null)
{
    var eventTemplateSource = await GetTemplateAsync("event.liquid", customTemplatesDirectory);
    var schemaTemplateSource = await GetTemplateAsync("schema.liquid", customTemplatesDirectory);

    return new FluidMarkdownGenerator(eventTemplateSource, schemaTemplateSource);
}

private FluidMarkdownGenerator(string eventTemplateSource, string schemaTemplateSource)
{
    _eventTemplate = Parser.Parse(eventTemplateSource);
    _schemaTemplate = Parser.Parse(schemaTemplateSource);
}
```

Keep the existing sync constructor for backward compatibility but mark it as using the sync methods.

**Step 2: Update GenerateCommand to use the async factory**

In `GenerateDocumentationAsync`:

```csharp
var markdownGenerator = await FluidMarkdownGenerator.CreateAsync(options.TemplatesDirectory);
```

**Step 3: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/
git commit -m "refactor(event-markdown): add async template loading via factory method"
```

---

### Task 24: CancellationToken in XmlDocumentationService.LoadDocumentationAsync

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.XmlDocs/IXmlDocumentationService.cs:20`
- Modify: `libs/Momentum/src/Momentum.Extensions.XmlDocs/XmlDocumentationService.cs:22`

**Step 1: Add CancellationToken parameter to interface**

```csharp
Task<bool> LoadDocumentationAsync(string xmlFilePath, CancellationToken cancellationToken = default);
```

**Step 2: Update implementation**

```csharp
public async Task<bool> LoadDocumentationAsync(string xmlFilePath, CancellationToken cancellationToken = default)
{
    // ... existing null check ...

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
        // ...
    }
}
```

Pass `cancellationToken` through to `ParseXmlDocumentationAsync` and the inner `ReadAsync` calls. Add `cancellationToken.ThrowIfCancellationRequested()` in the parse loop.

**Step 3: Fix any callers**

Search for and update any callers that don't pass the token.

**Step 4: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.XmlDocs/`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.XmlDocs/
git commit -m "feat(xml-docs): add CancellationToken support to LoadDocumentationAsync"
```

---

### Task 25: Fix integration test skip attributes

**Files:**
- Modify: `libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/IntegrationTests.cs:45,193`

**Step 1: Remove Skip attributes and fix path resolution**

Remove `Skip = "Debug test - only run when debugging path issues"` from both tests.

The path resolution issue is in `FindReferenceMarkdownPath` - it returns a dummy path if the reference file doesn't exist. The test at line 45 gracefully handles missing reference files (line 86: `if (File.Exists(ReferenceMarkdownPath))`), so it should work without skip.

For the sidebar test (line 193), it doesn't depend on reference files at all.

```csharp
// Line 45: Remove Skip
[Fact]
public async Task GenerateMarkdown_ShouldMatchReferenceFormat()

// Line 193: Remove Skip
[Fact]
public void JsonSidebarGenerator_ShouldGenerateCorrectStructure()
```

**Step 2: Run the tests**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/ --filter "GenerateMarkdown_ShouldMatchReferenceFormat|JsonSidebarGenerator_ShouldGenerateCorrectStructure"`
Expected: Tests pass

**Step 3: If tests fail, investigate and fix path issues**

The reference markdown path and debug output paths may need adjustment.

**Step 4: Commit**

```bash
git add libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/IntegrationTests.cs
git commit -m "test(event-markdown): enable previously skipped integration tests"
```

---

### Task 26: UTF-32 comment fix in PayloadSizeCalculator

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/PayloadSizeCalculator.cs:66-80`

**Step 1: Fix the comment**

```csharp
/// <summary>
///     Calculates string payload size using worst-case encoding (4 bytes per character).
///     This is the maximum for both UTF-8 (4 bytes for supplementary characters) and UTF-32,
///     providing a conservative upper bound for JSON serialization.
///     Uses MaxLength or StringLength attributes when available for accurate estimation.
/// </summary>
private static PayloadSizeResult CalculateStringSize(PropertyInfo property)
{
    var constraints = GetDataAnnotationConstraints(property);

    if (constraints.MaxLength.HasValue)
    {
        // 4 bytes per character is the worst case for both UTF-8 and UTF-32
        return new PayloadSizeResult
        {
            SizeBytes = constraints.MaxLength.Value * 4,
            IsAccurate = true,
            Warning = null
        };
    }
    // ...
```

**Step 2: Build to verify**

Run: `dotnet build libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/PayloadSizeCalculator.cs
git commit -m "docs(event-markdown): fix UTF encoding comment to clarify 4 bytes/char applies to both UTF-8 and UTF-32"
```

---

### Task 27: Build all libraries and run all tests

**Step 1: Build all libraries**

Run: `dotnet build libs/Momentum/Momentum.slnx`
Expected: Build succeeded

**Step 2: Run all library tests**

Run: `dotnet test libs/Momentum/Momentum.slnx`
Expected: All tests pass (note: 2 previously skipped MarkInvoiceAsPaid tests remain skipped)

**Step 3: Fix any failures**

Address any compilation errors or test failures introduced by the changes.

**Step 4: Final commit**

```bash
git commit -m "chore: fix any remaining issues from hardening pass"
```

---

### Task 28: Build AppDomain to verify no regressions

**Step 1: Build the sample application**

Run: `dotnet build AppDomain.slnx`
Expected: Build succeeded

**Step 2: Run AppDomain tests**

Run: `dotnet test AppDomain.slnx`
Expected: All tests pass

**Step 3: Fix any regressions**

Address any issues caused by the library changes.
