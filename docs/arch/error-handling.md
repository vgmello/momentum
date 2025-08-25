---
title: Error Handling Strategy
---

# Error Handling Strategy

This document outlines the error handling approach used in the AppDomain application. The system uses a hybrid approach that combines the Result pattern for business logic errors with traditional exceptions for system errors, following the real-world department metaphor where different types of problems are handled differently.

## Philosophy

The error handling strategy reflects how real-world departments handle problems:

-   **Business errors**: Like "customer not found" or "invalid payment amount" - these are expected outcomes that departments handle routinely
-   **System errors**: Like database connectivity issues or network failures - these are unexpected problems that stop work
-   **Validation errors**: Like missing required fields - these are caught at the reception desk before work begins
-   **Explicit over implicit**: All possible business outcomes are explicit in method signatures
-   **Structured communication**: Error information is consistent and actionable

## The Result Pattern

The system uses a `Result<T>` type that represents either success or failure explicitly in the method signature.

### Result Type Implementation

````csharp
[GenerateOneOf] // SourceGenerated
public partial class Result<T> : OneOfBase<T, List<ValidationFailure>>
{
}
```

### Why Result Over Exceptions

| Aspect               | Result Pattern                      | Exception Pattern              |
| -------------------- | ----------------------------------- | ------------------------------ |
| **Performance**      | Fast - no stack unwinding           | Slow - expensive to throw      |
| **Explicit**         | Errors are part of method signature | Errors are hidden from callers |
| **Handling**         | Must be handled explicitly          | Can be ignored accidentally    |
| **Flow Control**     | Normal program flow                 | Exceptional program flow       |
| **API Integration**  | Maps cleanly to HTTP status codes   | Requires conversion layer      |
| **Testing**          | Easy to test all paths              | Hard to test error scenarios   |
| **Functional Style** | Supports functional composition     | Imperative error handling      |

## Business Error Handling

Business errors represent expected outcomes that departments handle as part of their normal workflow.

### Command Error Handling

Commands return `Result<T>` to indicate success or validation failures:

<<< @/../src/AppDomain/Cashiers/Commands/UpdateCashier.cs{35-48}

### Query Error Handling

Queries use Result pattern for business-level failures:

<<< @/../src/AppDomain/Invoices/Queries/GetInvoice.cs{24-34}

### Creating Validation Failures

<<< @/../src/AppDomain/Invoices/Commands/CancelInvoice.cs{29-35}

## Validation Integration

The system integrates FluentValidation with the Result pattern for comprehensive input validation.

### Command Validation

<<< @/../src/AppDomain/Cashiers/Commands/CreateCashier.cs{20-25}

### Validation Middleware

The system automatically applies validation through middleware:

<<< @/../libs/Momentum/src/Momentum.ServiceDefaults/Messaging/Middlewares/FluentValidationExecutor.cs{19-39}

### Validation Policy

<<< @/../libs/Momentum/src/Momentum.ServiceDefaults/Messaging/Middlewares/FluentValidationPolicy.cs{12-28}

## API Error Handling

Controllers use the `Match` method to convert Result types to HTTP responses:

<<< @/../src/AppDomain.Api/Cashiers/CashiersController.cs{43-61}

### HTTP Status Code Mapping

```csharp
// Success cases
return result.Match(
    success => StatusCode(StatusCodes.Status201Created, success),  // 201 Created
    success => Ok(success),                                        // 200 OK

    // Error cases
    errors => BadRequest(new { Errors = errors })                  // 400 Bad Request
);
```

### Error Response Format

```json
{
    "errors": [
        {
            "propertyName": "Email",
            "errorMessage": "Email address is required",
            "attemptedValue": "",
            "errorCode": "NotEmptyValidator"
        }
    ]
}
```

## gRPC Error Handling

gRPC services convert Result patterns to RPC exceptions:

<<< @/../src/AppDomain.Api/Cashiers/CashierService.cs{27-35}

### gRPC Error Mapping

```csharp
// Map validation failures to appropriate RPC status codes
return result.Match(
    success => success.ToGrpc(),
    errors => throw new RpcException(new Status(
        StatusCode.InvalidArgument,
        string.Join("; ", errors.Select(e => e.ErrorMessage))
    ))
);
```

## Database Error Handling

Database operations use row count checks to detect business-level errors:

```csharp
// Check if operation affected any rows
var rowsAffected = await messaging.InvokeCommandAsync(updateDbCommand, cancellationToken);

if (rowsAffected == 0)
{
    var failures = new List<ValidationFailure>
    {
        new("CashierId", "Cashier not found or cannot be updated")
    };
    return (failures, null);
}
```

### Database Connection Errors

System-level database errors (connection failures, timeouts) are handled as exceptions:

```csharp
try
{
    await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
    // Database operations
}
catch (NpgsqlException ex)
{
    // System error - let exception bubble up
    throw;
}
```

## System Error Handling

System errors represent unexpected problems that stop normal operation.

### Exception Handling Middleware

<<< @/../libs/Momentum/src/Momentum.ServiceDefaults/Messaging/Middlewares/ExceptionHandlingFrame.cs{17-33}

### Exception Handling Policy

<<< @/../libs/Momentum/src/Momentum.ServiceDefaults/Messaging/Middlewares/ExceptionHandlingPolicy.cs{13-25}

## Testing Error Scenarios

### Testing Result Pattern

<<< @/../tests/AppDomain.Tests/Unit/Cashier/UpdateCashierCommandHandlerTests.cs{47-62}

### Testing Exception Scenarios

```csharp
[Fact]
public async Task Handle_DatabaseConnectionFails_ThrowsException()
{
    // Arrange
    var command = new GetCashierQuery(Guid.NewGuid());
    var dataSourceMock = Substitute.For<NpgsqlDataSource>();
    dataSourceMock.OpenConnectionAsync(Arg.Any<CancellationToken>())
              .Returns(Task.FromException<NpgsqlConnection>(new NpgsqlException()));

    // Act & Assert
    await Should.ThrowAsync<NpgsqlException>(async () =>
        await GetCashierQueryHandler.Handle(command, dataSourceMock, CancellationToken.None));
}
```

## Error Categories

### 1. Validation Errors (400 Bad Request)

-   Missing required fields
-   Invalid data formats
-   Business rule violations
-   **Handling**: Result pattern with ValidationFailure

### 2. Business Logic Errors (400 Bad Request)

-   Resource not found
-   Invalid state transitions
-   Duplicate entries
-   **Handling**: Result pattern with ValidationFailure

### 3. Authorization Errors (401/403)

-   Invalid authentication
-   Insufficient permissions
-   **Handling**: Exceptions (handled by framework)

### 4. System Errors (500 Internal Server Error)

-   Database connection failures
-   Network timeouts
-   Out of memory conditions
-   **Handling**: Exceptions

## Best Practices

### ✅ Do This

```csharp
// Use Result pattern for business errors
public static async Task<Result<Cashier>> Handle(CreateCashierCommand command, ...)
{
    if (await EmailAlreadyExists(command.Email))
    {
        return Result.Failure<Cashier>(new ValidationFailure("Email", "Email already exists"));
    }

    // Success case
    return Result.Success(cashier);
}

// Use Match for handling Results
return result.Match(
    success => Ok(success),
    errors => BadRequest(new { Errors = errors })
);

// Create meaningful error messages
new ValidationFailure("DueDate", "Due date must be in the future")
```

### ❌ Don't Do This

```csharp
// Don't throw exceptions for business logic
public static async Task<Cashier> Handle(CreateCashierCommand command, ...)
{
    if (await EmailAlreadyExists(command.Email))
    {
        throw new ArgumentException("Email already exists");  // Don't throw!
    }
}

// Don't ignore Result values
var result = await handler.Handle(command, cancellationToken);
// Missing: check if result is success or failure

// Don't use generic error messages
new ValidationFailure("Field", "Invalid")  // Too vague
```

## Error Logging and Monitoring

### Structured Logging

```csharp
// Log business errors at appropriate levels
logger.LogWarning("Cashier not found: {CashierId}", command.CashierId);

// Log system errors as errors
logger.LogError(ex, "Database connection failed");
```

### Metrics and Monitoring

```csharp
// Track error rates
_meter.CreateCounter<int>("AppDomain.errors.business")
      .Add(1, new("operation", "create_cashier"));

_meter.CreateCounter<int>("AppDomain.errors.system")
      .Add(1, new("operation", "database_connection"));
```

## Integration with Wolverine Framework

The error handling strategy integrates seamlessly with Wolverine's messaging system:

### Automatic Error Handling

```csharp
// Wolverine automatically handles Result types
public static async Task<Result<Cashier>> Handle(CreateCashierCommand command, ...)
{
    // Return Result - Wolverine converts to appropriate response
    return Result.Success(cashier);
}
```

### Transaction Rollback

```csharp
// If any operation in a handler fails, the entire transaction is rolled back
public static async Task<Result<ComplexOperation>> Handle(ComplexCommand command, ...)
{
    await messaging.InvokeCommandAsync(new CreateCashierCommand(...), cancellationToken);

    // If this fails, the cashier creation above is rolled back
    var result = await messaging.InvokeCommandAsync(new CreateInvoiceCommand(...), cancellationToken);

    return result.Match(
        success => Result.Success(new ComplexOperation()),
        errors => Result.Failure<ComplexOperation>(errors)
    );
}
```

## Migration Strategy

For teams transitioning from exception-based to Result-based error handling:

### Phase 1: New Code

-   All new commands and queries use Result pattern
-   New API endpoints handle Result types
-   New tests verify both success and failure cases

### Phase 2: High-Traffic Paths

-   Convert frequently used endpoints to Result pattern
-   Update error handling middleware
-   Add comprehensive logging

### Phase 3: Complete Migration

-   Convert remaining exception-based code
-   Update all tests to use Result pattern
-   Remove exception-based error handling

## Troubleshooting Common Issues

### Issue: Unhandled Result Values

```csharp
// Problem: Result not handled
var result = await handler.Handle(command, cancellationToken);
// Solution: Always handle Result with Match
return result.Match(
    success => Ok(success),
    errors => BadRequest(new { Errors = errors })
);
```

### Issue: Converting Exceptions to Results

```csharp
// Problem: Exception thrown in business logic
if (cashier == null)
    throw new ArgumentException("Cashier not found");

// Solution: Return Result
if (cashier == null)
    return Result.Failure<Cashier>(new ValidationFailure("Id", "Cashier not found"));
```

### Issue: Lost Error Information

```csharp
// Problem: Generic error handling
catch (Exception ex)
{
    return BadRequest("An error occurred");
}

// Solution: Preserve error details
return result.Match(
    success => Ok(success),
    errors => BadRequest(new {
        Errors = errors.Select(e => new {
            Field = e.PropertyName,
            Message = e.ErrorMessage
        })
    })
);
```

## Summary

The error handling strategy in this AppDomain application provides:

-   **Explicit error handling** through the Result pattern
-   **Structured error information** with ValidationFailure
-   **Performance benefits** by avoiding exceptions for business logic
-   **API-friendly** error responses
-   **Comprehensive testing** support
-   **Framework integration** with Wolverine messaging

This approach mirrors real-world department operations where different types of problems are handled appropriately - routine business issues are processed normally, while system failures require escalation and intervention.

The Result pattern makes error handling a first-class citizen in the application, ensuring that all possible outcomes are explicitly handled and communicated to users in a consistent, actionable manner.
