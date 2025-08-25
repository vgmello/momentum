---
title: Error Handling in Momentum
description: Master structured error handling with the Result pattern, FluentValidation integration, exception management, and consistent API responses.
date: 2024-01-15
---

# Error Handling in Momentum

Error handling in Momentum follows a **structured, predictable approach** that distinguishes between different types of failures and provides consistent patterns for handling them. This comprehensive guide covers error handling philosophy, implementation patterns, and production-ready strategies.

> **Prerequisites**: Understanding of [Commands and Queries](./cqrs/) and [Handlers](./cqrs/handlers). New to Momentum? Start with our [Getting Started Guide](./getting-started).

## Error Handling Philosophy

Momentum's error handling approach is built on these **core principles**:

### Design Principles

| Principle                  | Description                                     | Implementation                                            |
| -------------------------- | ----------------------------------------------- | --------------------------------------------------------- |
| **Explicit Error States**  | Make success/failure states clear in code       | `Result<T>` pattern                                       |
| **Fail Fast**              | Validate inputs early, return meaningful errors | FluentValidation integration                              |
| **Separation of Concerns** | Distinguish validation failures from exceptions | Result for business errors, exceptions for infrastructure |
| **Structured Logging**     | Capture errors with context for debugging       | Structured logs with correlation IDs                      |
| **Graceful Degradation**   | Handle errors without exposing internal details | User-friendly error messages                              |
| **Observability**          | All errors logged and tracked                   | OpenTelemetry integration                                 |

### Error Classification

Momentum classifies errors into distinct categories:

```mermaid
graph TD
    A["Error Types"] -/-> B["Validation Failures"]
    A -/-> C["Business Rule Violations"]
    A -/-> D["Infrastructure Exceptions"]

    B -/-> B1["Input validation"]
    B -/-> B2["Missing data"]
    B -/-> B3["Format errors"]

    C -/-> C1["Business logic violations"]
    C -/-> C2["State inconsistencies"]
    C -/-> C3["Authorization failures"]

    D -/-> D1["Database connectivity"]
    D -/-> D2["External service failures"]
    D -/-> D3["System exceptions"]

    style B fill:#e8f5e8
    style C fill:#fff3e0
    style D fill:#ffebee
```

## The Result\<T\> Pattern

The `Result<T>` pattern is the **foundation** of error handling in Momentum, providing type-safe representation of operations that can succeed or fail.

### Why Result\<T\>?

| Benefit           | Description                              | Alternative Problems           |
| ----------------- | ---------------------------------------- | ------------------------------ |
| **Type Safety**   | Compile-time guarantee of error handling | Exceptions can be ignored      |
| **Explicit Flow** | Success/failure paths are clear          | Hidden exception propagation   |
| **Composability** | Results can be chained and transformed   | Exception handling breaks flow |
| **Performance**   | No exception throwing overhead           | Exceptions are expensive       |
| **Testability**   | Easy to test both success and failure    | Exception testing is complex   |

### Result\<T\> Structure

The `Result<T>` type uses OneOf pattern for type-safe success/failure representation:

```csharp
// Definition in Momentum.Extensions
public partial class Result<T> : OneOfBase<T, List<ValidationFailure>>
{
    // Success case - implicit conversion from T
    public static implicit operator Result<T>(T value) => new(value);

    // Failure case - implicit conversion from validation errors
    public static implicit operator Result<T>(List<ValidationFailure> errors) => new(errors);

    // Helper properties
    public bool IsSuccess => IsT0;
    public bool IsFailure => IsT1;
    public T Value => AsT0;
    public List<ValidationFailure> Errors => AsT1;
}

// Usage examples
Result<Cashier> success = cashier;              // Success case
Result<Cashier> failure = validationErrors;     // Failure case

// Factory methods for clarity
Result<Cashier> success = Result<Cashier>.Success(cashier);
Result<Cashier> failure = Result<Cashier>.Failure("Error message");
Result<Cashier> failure = Result<Cashier>.Failure(validationErrors);
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

## Validation vs Exceptions: The Clear Distinction

Momentum makes a **clear distinction** between different types of failures to ensure appropriate handling:

### Decision Matrix

| Scenario                     | Use Result\<T\> | Use Exception | Rationale                      |
| ---------------------------- | --------------- | ------------- | ------------------------------ |
| Invalid user input           | ✅              | ❌            | Expected, recoverable          |
| Business rule violation      | ✅              | ❌            | Expected, part of domain logic |
| Resource not found           | ✅              | ❌            | Expected in normal operation   |
| Database connection failure  | ❌              | ✅            | Infrastructure, unexpected     |
| Programming error (null ref) | ❌              | ✅            | Bug, needs immediate attention |
| External service timeout     | ❌              | ✅            | Infrastructure, retryable      |

### When to Use Result\<T\> (Validation Failures)

Use the Result pattern for **expected failures** that are part of normal business operation:

#### Categories

```csharp
// ✅ Input Validation Errors
public static Result<User> ValidateUser(CreateUserCommand command)
{
    var errors = new List<ValidationFailure>();

    if (string.IsNullOrEmpty(command.Email))
        errors.Add(new ValidationFailure("Email", "Email is required"));

    if (!IsValidEmail(command.Email))
        errors.Add(new ValidationFailure("Email", "Please provide a valid email address"));

    return errors.Any() ? errors : Result<User>.Success(user);
}

// ✅ Business Rule Violations
public static Result<Invoice> ProcessPayment(ProcessPaymentCommand command)
{
    if (invoice.Status == InvoiceStatus.Paid)
        return Result<Invoice>.Failure("Invoice is already paid");

    if (invoice.Amount != command.PaymentAmount)
        return Result<Invoice>.Failure("Payment amount must match invoice amount");

    // Continue processing...
}

// ✅ Resource Not Found (Expected)
public static async Task<Result<Cashier>> Handle(
    GetCashierQuery query,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    var cashier = await db.Cashiers
        .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

    return cashier?.ToModel() ??
           new List<ValidationFailure> { new("Id", "Cashier not found") };
}

// ✅ State Inconsistencies
public static Result<Order> CancelOrder(CancelOrderCommand command, Order order)
{
    if (order.Status == OrderStatus.Completed)
        return Result<Order>.Failure("Cannot cancel completed order");

    if (order.Status == OrderStatus.Cancelled)
        return Result<Order>.Failure("Order is already cancelled");

    // Continue with cancellation...
}
```

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

### When to Use Exceptions (Exceptional Circumstances)

Reserve exceptions for **unexpected failures** that indicate infrastructure problems or programming errors:

#### Categories

```csharp
// ✅ Infrastructure Failures
public static async Task<Data.Entities.Cashier> Handle(
    DbCommand command,
    AppDomainDb db,
    CancellationToken cancellationToken)
{
    try
    {
        return await db.Cashiers.InsertWithOutputAsync(command.Cashier, token: cancellationToken);
    }
    catch (NpgsqlException ex) when (ex.IsTransient)
    {
        // Let transient database errors bubble up for retry
        throw;
    }
    catch (NpgsqlException ex) when (ex.SqlState == "23505")
    {
        // Convert known database errors to business failures
        throw new BusinessException("A cashier with this email already exists");
    }
}

// ✅ Programming Errors (Should not happen in production)
public static class ArgumentValidation
{
    public static T NotNull<T>(T value, string paramName) where T : class
    {
        return value ?? throw new ArgumentNullException(paramName);
    }

    public static void ValidateRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName,
                $"Value must be between {min} and {max}");
    }
}

// ✅ Security Violations
public static async Task<Result<SecretData>> GetSecretData(
    GetSecretDataQuery query,
    ICurrentUser currentUser,
    CancellationToken cancellationToken)
{
    if (!currentUser.IsInRole("Admin"))
        throw new UnauthorizedAccessException("Admin role required for secret data access");

    // Continue with authorized access...
}

// ✅ System-Level Failures
public static async Task ProcessLargeFile(ProcessFileCommand command)
{
    try
    {
        // File processing logic
        await ProcessFileInternal(command.FilePath);
    }
    catch (OutOfMemoryException)
    {
        // System resource exhaustion - let it bubble up
        throw;
    }
    catch (IOException ex) when (ex.Message.Contains("disk full"))
    {
        // System resource issue - let it bubble up
        throw;
    }
}
```

### Conversion Patterns

Sometimes you need to **convert between exceptions and Results**:

```csharp
// Converting exceptions to Results for external service calls
public static async Task<Result<PaymentResult>> ProcessPayment(
    PaymentRequest request,
    IExternalPaymentService paymentService)
{
    try
    {
        var result = await paymentService.ProcessAsync(request);
        return Result<PaymentResult>.Success(result);
    }
    catch (PaymentDeclinedException ex)
    {
        // Business failure - convert to Result
        return Result<PaymentResult>.Failure($"Payment declined: {ex.Reason}");
    }
    catch (PaymentServiceUnavailableException)
    {
        // Infrastructure failure - let exception bubble up for retry
        throw;
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("timeout"))
    {
        // Transient infrastructure failure - let it bubble up
        throw;
    }
}

// Converting Results to exceptions when needed
public static async Task<User> GetUserOrThrow(Guid userId, IUserRepository repository)
{
    var result = await repository.GetByIdAsync(userId);

    return result.Match(
        user => user,
        errors => throw new InvalidOperationException(
            $"User not found: {string.Join(", ", errors.Select(e => e.ErrorMessage))}"));
}
```

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

-   Exceptions are captured in the message envelope for logging
-   Exceptions are re-thrown for Wolverine's retry/DLQ handling
-   All handlers have consistent exception handling behavior

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

-   **Use Result\<T\> for business logic failures**: Make success/failure explicit
-   **Reserve exceptions for exceptional circumstances**: Infrastructure failures, programming errors
-   **Validate early**: Use FluentValidation to catch issues before processing
-   **Log with context**: Include relevant identifiers and state information
-   **Handle errors gracefully**: Don't expose internal details to clients
-   **Test error scenarios**: Both success and failure paths should be tested
-   **Use structured logging**: Enable better observability and debugging

### Don'ts

-   **Don't use exceptions for control flow**: Use Result\<T\> for expected failures
-   **Don't ignore errors**: Every error should be logged or handled appropriately
-   **Don't leak implementation details**: Return generic error messages to clients
-   **Don't block on error handling**: Keep error handling asynchronous
-   **Don't create chatty logs**: Log at appropriate levels with meaningful messages

## Next Steps

Now that you understand error handling fundamentals, explore these related topics:

### Core Implementation

1. **[CQRS Error Patterns](./cqrs/commands#error-handling)** - Error handling in commands and queries
2. **[Handler Error Management](./cqrs/handlers#error-handling-patterns)** - Structured error handling in handlers
3. **[Validation Integration](./cqrs/validation)** - FluentValidation and Result pattern integration

### Advanced Scenarios

4. **[Database Error Handling](./database/dbcommand#error-handling-and-resilience)** - Database-specific error patterns
5. **[Messaging Error Patterns](./messaging/kafka#error-handling)** - Event processing error handling
6. **[API Error Responses](./best-practices#api-error-handling)** - Consistent HTTP error responses

### Testing and Operations

7. **[Testing Error Scenarios](./testing/unit-tests#testing-error-cases)** - Comprehensive error testing strategies
8. **[Troubleshooting Guide](./troubleshooting#error-handling-issues)** - Common error handling problems
9. **[Observability](./service-configuration/observability#error-monitoring)** - Error monitoring and alerting

### Production Readiness

10. **[Best Practices](./best-practices#error-handling-guidelines)** - Production error handling guidelines
11. **[Performance Considerations](./best-practices#error-handling-performance)** - Error handling performance optimization
