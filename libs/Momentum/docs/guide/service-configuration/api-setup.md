# API Setup in Momentum

Momentum provides pre-configured patterns for building REST APIs and gRPC services. The API setup includes automatic endpoint discovery, validation, error handling, and OpenAPI documentation.

## Overview

API services in Momentum typically use the slim builder pattern for optimal performance:

```csharp
using Momentum.ServiceDefaults;
using Momentum.ServiceDefaults.Api;

var builder = WebApplication.CreateSlimBuilder(args);

// Core service defaults
builder.AddServiceDefaults();

// API-specific configuration
builder.AddApiServiceDefaults();

var app = builder.Build();

// Configure API middleware and routing
app.ConfigureApiUsingDefaults();

await app.RunAsync(args);
```

## API Service Defaults

The `AddApiServiceDefaults()` method configures:

1. **Controller Support** - MVC controllers and API controllers
2. **OpenAPI/Swagger** - Automatic API documentation
3. **CORS** - Cross-origin resource sharing
4. **JSON Serialization** - System.Text.Json with proper options
5. **Model Validation** - Automatic model state validation
6. **Exception Handling** - Global exception handling middleware
7. **Authentication/Authorization** - JWT bearer token support

### Basic API Configuration

```csharp
// Minimal API setup
var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults();

var app = builder.Build();

app.ConfigureApiUsingDefaults();

// Add your endpoints
app.MapGet("/", () => "Hello World");

await app.RunAsync(args);
```

### API with Authentication

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults();

var app = builder.Build();

// Require authentication
app.ConfigureApiUsingDefaults(requireAuth: true);

// Protected endpoint
app.MapGet("/protected", () => "Secured!")
   .RequireAuthorization();

await app.RunAsync(args);
```

## Controller-Based APIs

Momentum works seamlessly with ASP.NET Core controllers:

### Basic Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class CashiersController : ControllerBase
{
    private readonly IMessageBus _messageBus;

    public CashiersController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cashier>> Get(
        [FromRoute] Guid id, 
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var query = new GetCashierQuery(tenantId, id);
        var result = await _messageBus.InvokeAsync(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<ActionResult<Cashier>> Create(
        [FromBody] CreateCashierRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCashierCommand(
            request.TenantId, 
            request.Name, 
            request.Email);

        var (result, integrationEvent) = await _messageBus.InvokeAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(Get), 
                new { id = result.Value.Id, tenantId = result.Value.TenantId }, 
                result.Value);
        }

        return BadRequest(result.Errors);
    }
}
```

### Request/Response Models

Define clear request and response models:

```csharp
// Request models
public record CreateCashierRequest(Guid TenantId, string Name, string Email);

public record UpdateCashierRequest(string Name, string Email);

// Response models can use domain models directly
public record CashierResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string Email,
    DateTime CreatedDate,
    DateTime UpdatedDate);
```

### Advanced Controller Features

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CashiersController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Cashier>), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<PagedResult<Cashier>>> GetAll(
        [FromQuery] GetCashiersRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetCashiersQuery(
            request.TenantId,
            request.Page ?? 1,
            request.PageSize ?? 10);

        var result = await _messageBus.InvokeAsync(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Cashier), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<Cashier>> Update(
        [FromRoute] Guid id,
        [FromQuery] Guid tenantId,
        [FromBody] UpdateCashierRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCashierCommand(tenantId, id, request.Name, request.Email);
        var (result, integrationEvent) = await _messageBus.InvokeAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        // Check if it's a not found error
        var notFoundError = result.Errors?.FirstOrDefault(e => e.PropertyName == "Id");
        if (notFoundError != null)
        {
            return NotFound(notFoundError.ErrorMessage);
        }

        return BadRequest(result.Errors);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id,
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCashierCommand(tenantId, id);
        var (result, integrationEvent) = await _messageBus.InvokeAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        var notFoundError = result.Errors?.FirstOrDefault(e => e.PropertyName == "Id");
        if (notFoundError != null)
        {
            return NotFound();
        }

        return BadRequest(result.Errors);
    }
}
```

## Minimal APIs

For simpler scenarios, use Minimal APIs:

### Basic Endpoints

```csharp
var app = builder.Build();

app.ConfigureApiUsingDefaults();

// GET endpoint
app.MapGet("/api/cashiers/{id}", async (
    Guid id,
    Guid tenantId,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var query = new GetCashierQuery(tenantId, id);
    var result = await messageBus.InvokeAsync(query, cancellationToken);

    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Errors);
})
.WithName("GetCashier")
.WithOpenApi();

// POST endpoint
app.MapPost("/api/cashiers", async (
    CreateCashierRequest request,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var command = new CreateCashierCommand(request.TenantId, request.Name, request.Email);
    var (result, integrationEvent) = await messageBus.InvokeAsync(command, cancellationToken);

    return result.IsSuccess
        ? Results.CreatedAtRoute("GetCashier", new { id = result.Value.Id, tenantId = result.Value.TenantId }, result.Value)
        : Results.BadRequest(result.Errors);
})
.WithName("CreateCashier")
.WithOpenApi();
```

### Grouped Endpoints

```csharp
var cashierEndpoints = app.MapGroup("/api/cashiers")
    .WithTags("Cashiers")
    .WithOpenApi();

cashierEndpoints.MapGet("/", async (
    Guid tenantId,
    int page,
    int pageSize,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var query = new GetCashiersQuery(tenantId, page, pageSize);
    var result = await messageBus.InvokeAsync(query, cancellationToken);

    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Errors);
});

cashierEndpoints.MapGet("/{id}", async (
    Guid id,
    Guid tenantId,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var query = new GetCashierQuery(tenantId, id);
    var result = await messageBus.InvokeAsync(query, cancellationToken);

    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Errors);
});

cashierEndpoints.MapPost("/", async (
    CreateCashierRequest request,
    IMessageBus messageBus,
    CancellationToken cancellationToken) =>
{
    var command = new CreateCashierCommand(request.TenantId, request.Name, request.Email);
    var (result, integrationEvent) = await messageBus.InvokeAsync(command, cancellationToken);

    return result.IsSuccess
        ? Results.Created($"/api/cashiers/{result.Value.Id}", result.Value)
        : Results.BadRequest(result.Errors);
});
```

## gRPC Services

Momentum supports gRPC services alongside REST APIs:

### gRPC Service Implementation

```csharp
public class CashierService : CashierServiceBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<CashierService> _logger;

    public CashierService(IMessageBus messageBus, ILogger<CashierService> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    public override async Task<GetCashierResponse> GetCashier(
        GetCashierRequest request,
        ServerCallContext context)
    {
        var query = new GetCashierQuery(
            Guid.Parse(request.TenantId),
            Guid.Parse(request.Id));

        var result = await _messageBus.InvokeAsync(query, context.CancellationToken);

        if (result.IsSuccess)
        {
            return new GetCashierResponse
            {
                Id = result.Value.Id.ToString(),
                TenantId = result.Value.TenantId.ToString(),
                Name = result.Value.Name,
                Email = result.Value.Email,
                CreatedDate = Timestamp.FromDateTime(result.Value.CreatedDate.ToUniversalTime()),
                UpdatedDate = Timestamp.FromDateTime(result.Value.UpdatedDate.ToUniversalTime())
            };
        }

        var error = result.Errors?.FirstOrDefault();
        throw new RpcException(new Status(StatusCode.NotFound, error?.ErrorMessage ?? "Cashier not found"));
    }

    public override async Task<CreateCashierResponse> CreateCashier(
        CreateCashierRequest request,
        ServerCallContext context)
    {
        var command = new CreateCashierCommand(
            Guid.Parse(request.TenantId),
            request.Name,
            request.Email);

        var (result, integrationEvent) = await _messageBus.InvokeAsync(command, context.CancellationToken);

        if (result.IsSuccess)
        {
            return new CreateCashierResponse
            {
                Id = result.Value.Id.ToString(),
                TenantId = result.Value.TenantId.ToString(),
                Name = result.Value.Name,
                Email = result.Value.Email
            };
        }

        var validationErrors = string.Join("; ", result.Errors?.Select(e => $"{e.PropertyName}: {e.ErrorMessage}") ?? []);
        throw new RpcException(new Status(StatusCode.InvalidArgument, validationErrors));
    }
}
```

### gRPC Configuration

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults();

// Add gRPC services
builder.Services.AddGrpc();

var app = builder.Build();

app.ConfigureApiUsingDefaults();

// Map gRPC service
app.MapGrpcService<CashierService>();

// Optional: Add gRPC reflection for development
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

await app.RunAsync(args);
```

## Authentication and Authorization

### JWT Bearer Authentication

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireClaim("role", "admin"));
});

var app = builder.Build();

app.ConfigureApiUsingDefaults(requireAuth: true);

// Protected endpoint
app.MapGet("/admin/users", () => "Admin only!")
   .RequireAuthorization("RequireAdmin");

await app.RunAsync(args);
```

### Custom Authorization

```csharp
public class TenantAuthorizationHandler : AuthorizationHandler<TenantRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequirement requirement)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;
        
        if (tenantId == requirement.TenantId.ToString())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Register handler
builder.Services.AddScoped<IAuthorizationHandler, TenantAuthorizationHandler>();
```

## Error Handling

### Global Exception Handling

Momentum includes built-in exception handling:

```csharp
app.ConfigureApiUsingDefaults(); // Includes global exception handling

// Exceptions are automatically converted to appropriate HTTP responses:
// - ValidationException -> 400 Bad Request
// - UnauthorizedAccessException -> 401 Unauthorized
// - NotFoundException -> 404 Not Found
// - Other exceptions -> 500 Internal Server Error
```

### Custom Error Handling

```csharp
public class CustomExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomExceptionHandlingMiddleware> _logger;

    public CustomExceptionHandlingMiddleware(RequestDelegate next, ILogger<CustomExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var result = exception switch
        {
            BusinessException => new { error = exception.Message, statusCode = 400 },
            UnauthorizedAccessException => new { error = "Unauthorized", statusCode = 401 },
            NotFoundException => new { error = "Not found", statusCode = 404 },
            _ => new { error = "Internal server error", statusCode = 500 }
        };

        response.StatusCode = result.statusCode;
        await response.WriteAsync(JsonSerializer.Serialize(result));
    }
}

// Register middleware
app.UseMiddleware<CustomExceptionHandlingMiddleware>();
```

## OpenAPI/Swagger Configuration

### Basic Swagger Setup

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();
builder.AddApiServiceDefaults(); // Includes Swagger

var app = builder.Build();

app.ConfigureApiUsingDefaults();

// Swagger is automatically available at:
// - /swagger (Swagger UI)
// - /swagger/v1/swagger.json (OpenAPI spec)

await app.RunAsync(args);
```

### Custom Swagger Configuration

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "My API", 
        Version = "v1",
        Description = "API for managing cashiers and invoices"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
```

## CORS Configuration

### Basic CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.ConfigureApiUsingDefaults();
```

### Environment-Specific CORS

```csharp
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
    else
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("https://myapp.com", "https://api.myapp.com")
                  .WithMethods("GET", "POST", "PUT", "DELETE")
                  .WithHeaders("Content-Type", "Authorization");
        });
    }
});
```

## Health Checks

### API Health Endpoints

```csharp
var app = builder.Build();

app.ConfigureApiUsingDefaults();

// Map default health check endpoints
app.MapDefaultHealthCheckEndpoints();

// Custom health check endpoint
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync(args);
```

### Custom Health Checks

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDomainDb _db;

    public DatabaseHealthCheck(AppDomainDb db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}

// Register health check
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");
```

## Testing API Endpoints

### Integration Testing Setup

```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCashier_ValidId_ReturnsOk()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/cashiers/{cashierId}?tenantId={tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cashier = await response.Content.ReadFromJsonAsync<Cashier>();
        cashier.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCashier_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateCashierRequest(
            Guid.NewGuid(),
            "John Doe",
            "john@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/cashiers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var cashier = await response.Content.ReadFromJsonAsync<Cashier>();
        cashier.Should().NotBeNull();
        cashier!.Name.Should().Be(request.Name);
    }
}
```

## Best Practices

### API Design
1. **Use consistent naming**: Follow REST conventions for endpoints
2. **Version your APIs**: Plan for API evolution with versioning
3. **Document everything**: Use OpenAPI/Swagger for documentation
4. **Handle errors gracefully**: Provide meaningful error messages

### Performance
1. **Use async/await**: All endpoints should be asynchronous
2. **Implement pagination**: For endpoints that return collections
3. **Use appropriate HTTP status codes**: Follow HTTP semantics
4. **Enable compression**: Use response compression for better performance

### Security
1. **Always validate input**: Use model validation and FluentValidation
2. **Implement authentication**: Protect sensitive endpoints
3. **Use HTTPS**: Always use HTTPS in production
4. **Implement rate limiting**: Protect against abuse

### Monitoring
1. **Add health checks**: Monitor API health and dependencies
2. **Use structured logging**: Include request/response information
3. **Implement metrics**: Track API performance and usage
4. **Set up alerts**: Monitor for errors and performance issues

## Next Steps

- Learn about [Service Defaults](./service-defaults) for comprehensive service configuration
- Understand [Observability](./observability) for monitoring and telemetry
- Explore [CQRS](../cqrs/) patterns for commands and queries
- See [Testing](../testing/) for comprehensive testing strategies