# API Extensions

## Service Configuration {#service-configuration}

This method configures the following services:

- **MVC Controllers**: With endpoint API explorer
- **Problem Details**: For standardized error responses
- **OpenAPI**: With XML documentation support
- **HTTP Request/Response Logging**: For debugging and monitoring
- **gRPC Services**: With reflection support
- **Authentication and Authorization**: Services setup
- **Kestrel Server Configuration**: Removes server header for security

### Service Registration Details

#### MVC Controllers with Custom Routing

```csharp
builder.Services.AddControllers(opt =>
{
    opt.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseRoutesTransformer()));
});
```

#### API Documentation Services

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApiWithXmlDocSupport();
builder.Services.AddHttpLogging();
```

#### gRPC Services

```csharp
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
```

#### Security Services

```csharp
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
```

#### Server Configuration

```csharp
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});
```

## Application Configuration {#application-configuration}

This method configures the following middleware and endpoints:

- **HTTP Logging Middleware**: For request/response logging
- **Routing, Authentication, and Authorization**: Core ASP.NET Core middleware
- **gRPC-Web Support**: With default enablement for browser clients
- **HSTS and Exception Handling**: In production environments
- **OpenAPI, Scalar Documentation, and gRPC Reflection**: In development
- **Controller Endpoints**: With optional authorization requirement
- **gRPC Service Endpoints**: For gRPC communication

### Middleware Pipeline Configuration

#### Core Middleware

```csharp
app.UseHttpLogging();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
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
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle($"{app.Environment.ApplicationName} OpenAPI"));
    app.UseMiddleware<OpenApiCachingMiddleware>();

    app.MapGrpcReflectionService();
}
```

#### Endpoint Configuration

```csharp
var controllersEndpointBuilder = app.MapControllers();

if (requireAuth)
    controllersEndpointBuilder.RequireAuthorization();

app.MapGrpcServices();
```

## Route Transformation

### Kebab Case Routing

The extensions configure kebab-case routing transformation:

```csharp
opt.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseRoutesTransformer()));
```

This transforms controller and action names:
- `UserController.GetUser` → `/user/get-user`
- `OrderController.CreateOrder` → `/order/create-order`

## OpenAPI Integration

### XML Documentation Support

```csharp
builder.Services.AddOpenApiWithXmlDocSupport();
```

This extension method configures:
- **XML Documentation Loading**: Automatic discovery of XML files
- **Document Transformers**: For enriching OpenAPI with XML comments
- **Operation Transformers**: For parameter and response documentation

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
- **Caching Middleware**: Performance optimization for docs

### Production Optimizations

Only enabled in production:
- **HSTS**: HTTP Strict Transport Security
- **Exception Handling**: Structured error responses

## Authorization Flexibility

### Optional Authorization

```csharp
public static WebApplication ConfigureApiUsingDefaults(this WebApplication app, bool requireAuth = true)
```

The `requireAuth` parameter allows:
- **Public APIs**: Set to `false` for open endpoints
- **Secure APIs**: Default `true` for protected resources
- **Mixed Scenarios**: Selective authorization per endpoint