# API Extensions

## Service Configuration

This method configures core service defaults (via `AddServiceDefaults()`) plus the following API-specific services:

- **Core Service Defaults**: Logging, OpenTelemetry, validators, health checks, service discovery, HTTP resilience
- **Endpoints API Explorer**: For OpenAPI endpoint discovery
- **Problem Details**: For standardized error responses
- **HTTP Request/Response Logging**: For debugging and monitoring (when enabled)
- **Output Caching**: For OpenAPI document caching
- **gRPC Services**: With reflection support
- **OpenTelemetry**: gRPC instrumentation
- **Authentication and Authorization**: Conditional on `requireAuth` flag
- **Rate Limiting**: Via `AddRateLimiting()` (when enabled in configuration)
- **JSON Serialization**: With enum string conversion
- **API Versioning**: URL segment versioning (v1.0 default)
- **Kestrel Server Configuration**: HTTPS and server header removal

### Service Registration Details

#### API Documentation Services

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
```

#### gRPC Services

```csharp
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
```

#### Security Services

```csharp
// Only when requireAuth is true:
builder.Services.AddAuthentication();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

#### Rate Limiting

```csharp
// Called automatically, but also available standalone:
builder.AddRateLimiting();
```

Rate limiting is configured via the `RateLimiting` configuration section:
- `RateLimiting:Enabled` - enables rate limiting
- `RateLimiting:Fixed` - configures a fixed window limiter

#### Server Configuration

```csharp
builder.WebHost.UseKestrelHttpsConfiguration();
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});
```

## Application Configuration

This method configures the full API middleware pipeline and maps all endpoints:

- **HTTP Logging Middleware**: For request/response logging (when enabled)
- **Routing, Rate Limiting, Authentication, and Authorization**: Core middleware
- **gRPC-Web Support**: With default enablement for browser clients
- **HSTS and Exception Handling**: In production environments
- **Output Caching**: For OpenAPI document performance
- **OpenAPI, Scalar Documentation, and gRPC Reflection**: In development (anonymous)
- **gRPC Service Endpoints**: Auto-discovered via `MapGrpcServices()`
- **REST Endpoints**: Auto-discovered via `MapEndpoints()` from `IEndpointDefinition` implementations
- **Health Check Endpoints**: Via `MapDefaultHealthCheckEndpoints()`

### Middleware Pipeline Configuration

#### Core Middleware

```csharp
app.UseHttpLogging();    // when enabled
app.UseRouting();
app.UseRateLimiter();    // when enabled
app.UseAuthentication(); // when requireAuth
app.UseAuthorization();  // when requireAuth
```

#### gRPC-Web Support

```csharp
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
```

#### Production Middleware

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseExceptionHandler();
}
```

#### Development Tools

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().CacheOutput("OpenApi").AllowAnonymous();
    app.MapScalarApiReference(options => options.WithTitle($"{app.Environment.ApplicationName} OpenAPI"))
        .AllowAnonymous();
    app.MapGrpcReflectionService().AllowAnonymous();
}
```

#### Endpoint Configuration

```csharp
app.MapGrpcServices();                  // Auto-discovers gRPC services
app.MapEndpoints();                     // Auto-discovers IEndpointDefinition implementations
app.MapDefaultHealthCheckEndpoints();   // Maps /status, /health/internal, /health
```

## Endpoint Auto-Discovery

### IEndpointDefinition Interface

Implement `IEndpointDefinition` on endpoint classes for automatic registration:

```csharp
public class CustomerEndpoints : IEndpointDefinition
{
    public static void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("customers").WithTags("Customers");
        group.MapGet("/", GetCustomers);
        group.MapPost("/", CreateCustomer);
    }

    private static async Task<IResult> GetCustomers(...) { ... }
    private static async Task<IResult> CreateCustomer(...) { ... }
}
```

Endpoints are discovered from the entry assembly by default. To scan a specific assembly:

```csharp
app.MapEndpoints(typeof(MyEndpoints).Assembly);
```

## OpenAPI Integration

### XML Documentation Support

```csharp
// Call AddOpenApi() directly in your API project for XML documentation support
builder.Services.AddOpenApi(options => options.ConfigureOpenApiDefaults());
```

The .NET 10 source generator automatically enriches OpenAPI with XML comments when:
- `GenerateDocumentationFile` is set to `true` in your project
- `AddOpenApi()` is called directly in your API project (not from a library)
- The `InterceptorsNamespaces` includes `Microsoft.AspNetCore.OpenApi.Generated`

### Scalar Documentation

```csharp
app.MapScalarApiReference(options => options.WithTitle($"{app.Environment.ApplicationName} OpenAPI"));
```

Provides modern API documentation interface with:
- **Interactive Testing**: Built-in API explorer
- **Code Generation**: Client SDK examples
- **Schema Visualization**: Type definitions and relationships

## gRPC Configuration

### Service Registration

```csharp
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
```

### Web Support

```csharp
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
```

Enables:
- **Browser Compatibility**: gRPC calls from web browsers
- **Streaming Support**: Bidirectional streaming over HTTP/1.1
- **Protocol Translation**: gRPC-Web to gRPC protocol bridging

### Development Reflection

```csharp
app.MapGrpcReflectionService();
```

Provides:
- **Service Discovery**: Runtime service enumeration
- **Tool Integration**: Support for gRPC clients and testing tools
- **Development Experience**: Service exploration and debugging

## Security Considerations

### Server Header Removal

```csharp
serverOptions.AddServerHeader = false;
```

Removes the `Server` header to:
- **Reduce Information Disclosure**: Hide implementation details
- **Security Best Practice**: Minimize attack surface
- **Compliance**: Meet security scanning requirements

### HSTS Configuration

```csharp
app.UseHsts();
```

Enforces HTTPS in production:
- **Browser Security**: Prevents protocol downgrade attacks
- **Certificate Pinning**: Validates server certificates
- **Compliance**: Meets modern security standards

## Environment-Specific Configuration

### Development Features

Only enabled in development:
- **OpenAPI Documentation**: Interactive API docs
- **gRPC Reflection**: Service discovery
- **Output Caching**: Performance optimization for docs

### Production Optimizations

Only enabled in production:
- **HSTS**: HTTP Strict Transport Security
- **Exception Handling**: Structured error responses

## Authorization Flexibility

### Optional Authorization

```csharp
builder.AddApiServiceDefaults(requireAuth: false);
```

The `requireAuth` parameter allows:
- **Public APIs**: Set to `false` for open endpoints
- **Secure APIs**: Default `true` for protected resources
- **Mixed Scenarios**: Individual endpoints can use `.AllowAnonymous()` or `[AllowAnonymous]`

## Minimal API Program.cs Example

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

// Includes AddServiceDefaults() internally - no need to call it separately
builder.AddApiServiceDefaults(requireAuth: false);
builder.AddServiceBus(bus => bus.UseWolverine());

// OpenAPI must be called directly for XML doc source generation
builder.Services.AddOpenApi(options => options.ConfigureOpenApiDefaults(builder.Configuration));

var app = builder.Build();

// Configures middleware, maps endpoints (IEndpointDefinition), gRPC services, and health checks
app.ConfigureApiUsingDefaults();

await app.RunAsync(args);
```
