# Error Handling in Momentum

Error handling in Momentum follows a structured, predictable approach that distinguishes between different types of failures and provides consistent patterns for handling them. This guide covers the error handling philosophy, patterns, and implementation strategies used throughout Momentum applications.

## Error Handling Philosophy

Momentum embraces these core principles for error handling:

- **Explicit Error States**: Use `Result<T>` pattern to make success and failure states explicit
- **Fail Fast**: Validate inputs early and return meaningful error messages
- **Separation of Concerns**: Distinguish between validation failures and exceptional circumstances  
- **Structured Logging**: Capture errors with context for debugging and monitoring
- **Graceful Degradation**: Handle errors gracefully without exposing internal details
- **Observability**: All errors are logged and tracked for operational insights

## The Result\<T\> Pattern

The `Result<T>` pattern is the foundation of error handling in Momentum. It provides a type-safe way to represent operations that can either succeed with a value or fail with validation errors.

### Basic Usage

```csharp
// Definition in Momentum.Extensions
public partial class Result<T> : OneOfBase<T, List<ValidationFailure>>
{
    // Implicit conversions allow clean syntax
}

// Success case
Result<Cashier> result = cashier; // Implicit conversion from T

// Failure case  
Result<Cashier> result = validationErrors; // Implicit conversion from List<ValidationFailure>
```

### Pattern Matching with Result\<T\>

```csharp
public async Task<IActionResult> GetCashier(Guid tenantId, Guid id)
{
    var query = new GetCashierQuery(tenantId, id);
    var result = await _messageBus.InvokeAsync(query);
    
    return result.Match<IActionResult>(
        cashier => Ok(cashier),           // Success case
        errors => BadRequest(errors)     // Failure case
    );
}
```

### Working with Result\<T\> in Handlers

```csharp
public static async Task<Result<Cashier>> Handle(GetCashierQuery query, AppDomainDb db, CancellationToken cancellationToken)
{
    var cashier = await db.Cashiers
        .FirstOrDefaultAsync(c => c.TenantId == query.TenantId && c.CashierId == query.Id, cancellationToken);

    if (cashier is not null)
    {
        return cashier.ToModel(); // Success - implicit conversion
    }

    // Failure - return validation errors
    return new List<ValidationFailure> { new("Id", "Cashier not found") };
}
```

## Validation vs Exceptions

Momentum makes a clear distinction between validation failures and exceptional circumstances.

### Validation Failures

Use `Result<T>` with `ValidationFailure` for:
- Invalid input data
- Business rule violations  
- Missing required resources
- Logical inconsistencies

```csharp
public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(UpdateCashierCommand command, IMessageBus messaging, CancellationToken cancellationToken)
{
    var dbCommand = new DbCommand(command.TenantId, command.CashierId, command.Name, command.Email, command.Version);
    var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

    if (updatedCashier is null)
    {
        // Validation failure - resource not found
        var failures = new List<ValidationFailure> { new("CashierId", "Cashier not found") };
        return (failures, null);
    }

    var result = updatedCashier.ToModel();
    var updatedEvent = new CashierUpdated(command.TenantId, command.CashierId);

    return (result, updatedEvent);
}
```

### Exceptions

Reserve exceptions for truly exceptional circumstances:
- Infrastructure failures (database connectivity, external service unavailable)
- Programming errors (null reference, invalid operations)
- Security violations
- System-level failures

```csharp
public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(UpdateCashierCommand command, IMessageBus messaging, CancellationToken cancellationToken)
{
    // Example: Simulate an infrastructure failure
    if (command.Name.Contains("error"))
    {
        throw new DivideByZeroException("Forced test unhandled exception to simulate error scenarios");
    }
    
    // Normal flow continues...
}
```

## Validation with FluentValidation

Momentum uses FluentValidation for input validation, integrated with the messaging pipeline.

### Command Validators

```csharp
public class CreateCashierValidator : AbstractValidator<CreateCashierCommand>
{
    public CreateCashierValidator()
    {
        RuleFor(c => c.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");
            
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters");
            
        RuleFor(c => c.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address");
    }
}
```

### Automatic Validation Integration

Wolverine automatically executes validators before handlers:

```csharp
// If validation fails, the handler is never called
// ValidationFailure objects are automatically returned
// This happens transparently through the FluentValidationPolicy
```

### Complex Validation Rules

```csharp
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100).MinimumLength(2);
        
        RuleFor(c => c.Amount)
            .GreaterThan(0)
            .WithMessage("Invoice amount must be greater than 0");
            
        RuleFor(c => c.Currency)
            .MaximumLength(3)
            .When(c => !string.IsNullOrEmpty(c.Currency))
            .WithMessage("Currency code must be 3 characters or less");
            
        RuleFor(c => c.DueDate)
            .GreaterThan(DateTime.UtcNow)
            .When(c => c.DueDate.HasValue)
            .WithMessage("Due date must be in the future");
    }
}
```

## Error Handling Middleware

Momentum provides several middleware components for consistent error handling across the messaging pipeline.

### Exception Handling Frame

The `ExceptionHandlingFrame` wraps all message handlers in try-catch blocks:

```csharp
public class ExceptionHandlingFrame : SyncFrame
{
    public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
    {
        writer.Write("BLOCK:try");
        Next?.GenerateCode(method, writer);
        writer.FinishBlock();

        writer.Write("BLOCK:catch (System.Exception ex)");
        writer.Write($"{_envelope?.Usage}.Failure = ex;");
        writer.Write("throw;");
        writer.FinishBlock();
    }
}
```

This ensures that:
- Exceptions are captured in the message envelope for logging
- Exceptions are re-thrown for Wolverine's retry/DLQ handling
- All handlers have consistent exception handling behavior

### FluentValidation Integration

The `FluentValidationPolicy` automatically adds validation to all command handlers:

```csharp
public class FluentValidationPolicy : IHandlerPolicy
{
    public void Apply(IReadOnlyList<HandlerChain> chains, GenerationRules rules, IServiceContainer container)
    {
        foreach (var chain in chains.Where(x => x.MessageType.CanBeCastTo<ICommand>()))
        {
            chain.Middleware.Add(new FluentValidationResultFrame());
        }
    }
}
```

## API Error Handling

### Global Error Handling

Configure global exception handling in your API:

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception occurred during request processing");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = exception switch
        {
            ValidationException => 400,
            UnauthorizedAccessException => 401,
            ArgumentException => 400,
            _ => 500
        };

        var response = new
        {
            error = "An error occurred processing your request",
            statusCode = context.Response.StatusCode,
            // Include details in development
            details = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true 
                ? exception.Message 
                : null
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### Controller Error Responses

Use consistent patterns for returning error responses:

```csharp
[ApiController]
[Route("api/[controller]")]
public class CashiersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCashier([FromQuery] Guid tenantId, Guid id)
    {
        var query = new GetCashierQuery(tenantId, id);
        var result = await _messageBus.InvokeAsync(query);

        return result.Match<IActionResult>(
            cashier => Ok(cashier),
            errors => BadRequest(new { 
                message = "Validation failed", 
                errors = errors.Select(e => new { 
                    field = e.PropertyName, 
                    message = e.ErrorMessage 
                }) 
            })
        );
    }

    [HttpPost]
    public async Task<IActionResult> CreateCashier(CreateCashierRequest request)
    {
        var command = new CreateCashierCommand(request.TenantId, request.Name, request.Email);
        
        try 
        {
            var (result, integrationEvent) = await _messageBus.InvokeAsync(command);
            
            return result.Match<IActionResult>(
                cashier => CreatedAtAction(
                    nameof(GetCashier), 
                    new { tenantId = cashier.TenantId, id = cashier.Id }, 
                    cashier),
                errors => BadRequest(new { 
                    message = "Validation failed", 
                    errors = errors.Select(e => new { 
                        field = e.PropertyName, 
                        message = e.ErrorMessage 
                    }) 
                })
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cashier for tenant {TenantId}", request.TenantId);
            return StatusCode(500, new { message = "An error occurred processing your request" });
        }
    }
}
```

### HTTP Status Code Mapping

Follow RESTful conventions for error status codes:

```csharp
public static class ErrorResponseExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        return result.Match<IActionResult>(
            success => new OkObjectResult(success),
            errors => MapToErrorResponse(errors)
        );
    }

    private static IActionResult MapToErrorResponse(List<ValidationFailure> errors)
    {
        // Map different error types to appropriate status codes
        if (errors.Any(e => e.PropertyName == "Id" && e.ErrorMessage.Contains("not found")))
        {
            return new NotFoundObjectResult(new { message = "Resource not found", errors });
        }

        if (errors.Any(e => e.ErrorMessage.Contains("unauthorized")))
        {
            return new UnauthorizedObjectResult(new { message = "Unauthorized access" });
        }

        if (errors.Any(e => e.ErrorMessage.Contains("conflict")))
        {
            return new ConflictObjectResult(new { message = "Resource conflict", errors });
        }

        // Default to bad request for validation errors
        return new BadRequestObjectResult(new { message = "Validation failed", errors });
    }
}
```

## Error Handling in Integration Events

### Event Publishing Errors

Handle failures during event publishing gracefully:

```csharp
public static async Task<(Result<Cashier>, CashierCreated?)> Handle(CreateCashierCommand command, IMessageBus messaging, CancellationToken cancellationToken)
{
    try
    {
        var dbCommand = CreateInsertCommand(command);
        var insertedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedCashier.ToModel();
        var createdEvent = new CashierCreated(result.TenantId, PartitionKeyTest: 0, result);

        return (result, createdEvent);
    }
    catch (DbException ex)
    {
        _logger.LogError(ex, "Database error creating cashier for tenant {TenantId}", command.TenantId);
        
        // Convert database errors to validation failures
        if (ex.Message.Contains("unique constraint"))
        {
            var failures = new List<ValidationFailure> { new("Email", "Email address already exists") };
            return (failures, null);
        }
        
        throw; // Re-throw for infrastructure errors
    }
}
```

### Event Handler Errors

Event handlers should be resilient and handle errors appropriately:

```csharp
public class CashierCreatedHandler
{
    private readonly IEmailService _emailService;
    private readonly ILogger<CashierCreatedHandler> _logger;

    public async Task Handle(CashierCreated integrationEvent, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendWelcomeEmailAsync(
                integrationEvent.Cashier.Email,
                integrationEvent.Cashier.Name,
                cancellationToken);
                
            _logger.LogInformation("Welcome email sent to {Email} for cashier {CashierId}",
                integrationEvent.Cashier.Email, integrationEvent.Cashier.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email} for cashier {CashierId}",
                integrationEvent.Cashier.Email, integrationEvent.Cashier.Id);
                
            // Don't re-throw - email failure shouldn't fail the entire event processing
            // Consider publishing a compensation event or storing for retry
        }
    }
}
```

## Retry and Compensation Patterns

### Wolverine Retry Policies

Configure automatic retries for transient failures:

```csharp
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    builder.Services.AddWolverine(options =>
    {
        // Configure retry policy for database-related exceptions
        options.Policies.OnException<NpgsqlException>()
            .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
            
        // Configure retry policy for HTTP exceptions
        options.Policies.OnException<HttpRequestException>()
            .RetryWithCooldown(1.Seconds(), 2.Seconds(), 5.Seconds());
    });
    
    return builder;
}
```

### Dead Letter Queues

Configure DLQ for messages that repeatedly fail:

```csharp
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    builder.Services.AddWolverine(options =>
    {
        // After 3 retries, move to dead letter queue
        options.Policies.OnException<Exception>()
            .MoveToErrorQueue();
            
        // Configure specific handling for different message types
        options.Policies.ForMessagesOfType<CashierCreated>()
            .OnException<EmailException>()
            .RetryWithCooldown(30.Seconds(), 60.Seconds())
            .Then.MoveToErrorQueue();
    });
    
    return builder;
}
```

### Compensation Events

Use compensation events for rollback scenarios:

```csharp
public class PaymentFailedHandler
{
    public async Task Handle(PaymentFailed integrationEvent, IMessageBus messageBus, CancellationToken cancellationToken)
    {
        // Compensate for the failed payment by canceling the invoice
        var compensationCommand = new CancelInvoiceCommand(
            integrationEvent.TenantId,
            integrationEvent.InvoiceId,
            "Payment failed - automatically canceled");
            
        await messageBus.InvokeAsync(compensationCommand, cancellationToken);
        
        // Publish compensation event
        var compensationEvent = new InvoiceCanceled(
            integrationEvent.TenantId,
            integrationEvent.InvoiceId,
            "Payment failure compensation");
            
        await messageBus.PublishAsync(compensationEvent, cancellationToken);
    }
}
```

## Structured Logging for Errors

### Error Logging with Context

Use structured logging to capture errors with relevant context:

```csharp
public static async Task<(Result<Cashier>, CashierUpdated?)> Handle(UpdateCashierCommand command, IMessageBus messaging, ILogger<UpdateCashierCommandHandler> logger, CancellationToken cancellationToken)
{
    logger.LogInformation("Updating cashier {CashierId} for tenant {TenantId}",
        command.CashierId, command.TenantId);

    try
    {
        var dbCommand = new DbCommand(command.TenantId, command.CashierId, command.Name, command.Email, command.Version);
        var updatedCashier = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        if (updatedCashier is null)
        {
            logger.LogWarning("Cashier {CashierId} not found for tenant {TenantId} during update",
                command.CashierId, command.TenantId);
                
            var failures = new List<ValidationFailure> { new("CashierId", "Cashier not found") };
            return (failures, null);
        }

        logger.LogInformation("Successfully updated cashier {CashierId} for tenant {TenantId}",
            command.CashierId, command.TenantId);

        var result = updatedCashier.ToModel();
        var updatedEvent = new CashierUpdated(command.TenantId, command.CashierId);

        return (result, updatedEvent);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating cashier {CashierId} for tenant {TenantId}",
            command.CashierId, command.TenantId);
        throw;
    }
}
```

### Performance Logging

Log performance metrics for monitoring:

```csharp
public class RequestPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestPerformanceMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 1000) // Log slow requests
            {
                _logger.LogWarning("Slow request: {Method} {Path} took {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

## OpenTelemetry Integration

### Automatic Error Tracking

Momentum automatically tracks errors through OpenTelemetry:

```csharp
// Errors are automatically captured in traces
using var activity = Activity.StartActivity("ProcessCommand");
activity?.SetTag("command.type", command.GetType().Name);
activity?.SetTag("tenant.id", command.TenantId.ToString());

try
{
    // Process command
    var result = await handler.Handle(command);
    activity?.SetTag("result.success", result.IsSuccess);
    return result;
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
```

### Custom Error Metrics

Track application-specific error metrics:

```csharp
public class ErrorMetrics
{
    private static readonly Counter<int> ErrorCounter = MeterProvider.Meter.CreateCounter<int>("app.errors");
    private static readonly Histogram<double> ErrorDuration = MeterProvider.Meter.CreateHistogram<double>("app.error_duration");

    public static void RecordError(string errorType, string operation, double durationMs)
    {
        ErrorCounter.Add(1, 
            new KeyValuePair<string, object?>("error.type", errorType),
            new KeyValuePair<string, object?>("operation", operation));
            
        ErrorDuration.Record(durationMs,
            new KeyValuePair<string, object?>("error.type", errorType),
            new KeyValuePair<string, object?>("operation", operation));
    }
}
```

## Testing Error Scenarios

### Unit Testing Error Cases

Always test both success and failure scenarios:

```csharp
[TestFixture]
public class UpdateCashierCommandHandlerTests
{
    [Test]
    public async Task Handle_CashierNotFound_ReturnsFailureResult()
    {
        // Arrange
        var command = new UpdateCashierCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated Name", "updated@example.com");
        var mockMessaging = new Mock<IMessageBus>();
        
        mockMessaging.Setup(m => m.InvokeCommandAsync(It.IsAny<UpdateCashierCommandHandler.DbCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Data.Entities.Cashier?)null);

        // Act
        var (result, integrationEvent) = await UpdateCashierCommandHandler.Handle(command, mockMessaging.Object, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Cashier not found");
        integrationEvent.Should().BeNull();
    }

    [Test]
    public async Task Handle_DatabaseException_ThrowsException()
    {
        // Arrange
        var command = new UpdateCashierCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated Name", "updated@example.com");
        var mockMessaging = new Mock<IMessageBus>();
        
        mockMessaging.Setup(m => m.InvokeCommandAsync(It.IsAny<UpdateCashierCommandHandler.DbCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UpdateCashierCommandHandler.Handle(command, mockMessaging.Object, CancellationToken.None));
    }
}
```

### Integration Testing Error Scenarios

Test error handling with real infrastructure:

```csharp
[Test]
public async Task CreateCashier_DuplicateEmail_ReturnsBadRequest()
{
    // Arrange
    var tenantId = Guid.NewGuid();
    var email = "duplicate@example.com";
    
    // Create first cashier
    var firstRequest = new CreateCashierRequest(tenantId, "First User", email);
    await _client.PostAsJsonAsync("/api/cashiers", firstRequest);
    
    // Act - try to create duplicate
    var duplicateRequest = new CreateCashierRequest(tenantId, "Second User", email);
    var response = await _client.PostAsJsonAsync("/api/cashiers", duplicateRequest);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("already exists");
}
```

## Best Practices Summary

### Do's
- **Use Result\<T\> for business logic failures**: Make success/failure explicit
- **Reserve exceptions for exceptional circumstances**: Infrastructure failures, programming errors
- **Validate early**: Use FluentValidation to catch issues before processing
- **Log with context**: Include relevant identifiers and state information
- **Handle errors gracefully**: Don't expose internal details to clients
- **Test error scenarios**: Both success and failure paths should be tested
- **Use structured logging**: Enable better observability and debugging

### Don'ts
- **Don't use exceptions for control flow**: Use Result\<T\> for expected failures
- **Don't ignore errors**: Every error should be logged or handled appropriately
- **Don't leak implementation details**: Return generic error messages to clients
- **Don't block on error handling**: Keep error handling asynchronous
- **Don't create chatty logs**: Log at appropriate levels with meaningful messages

## Next Steps

- Review [CQRS Patterns](./cqrs/) for command and query error handling specifics
- Explore [Messaging Error Handling](./messaging/) for event-driven error scenarios  
- See [Testing Strategies](./testing/) for comprehensive error testing approaches
- Check [Troubleshooting](./troubleshooting) for common error handling issues