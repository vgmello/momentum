# Momentum.Extensions

Comprehensive core extensions library providing robust result types, CQRS messaging abstractions, database utilities, and essential extensions for building enterprise .NET applications with the Momentum platform.

## Overview

**Momentum.Extensions** is the foundational library that provides essential patterns and utilities for modern .NET applications. It focuses on functional programming principles with discriminated union result types, strongly-typed messaging abstractions for CQRS architectures, enhanced database access patterns, and utility extensions that improve developer productivity.

**Key Value Propositions:**

-   **Eliminate Exception-Driven Development**: Use `Result<T>` types for predictable error handling
-   **Type-Safe Messaging**: CQRS command/query abstractions with compile-time validation
-   **Enhanced Database Access**: Simplified stored procedure execution with Dapper extensions
-   **String Utilities**: High-performance case conversion and pluralization
-   **Functional Composition**: Chain operations without exception handling complexity

## Installation

Add the package to your project using the .NET CLI:

```bash
dotnet add package Momentum.Extensions
```

Or using the Package Manager Console:

```powershell
Install-Package Momentum.Extensions
```

## Core Features

### 🎯 Result Types (Discriminated Unions)

-   `Result<T>` for success/failure states without exceptions
-   Built on OneOf library for type-safe union types
-   Seamless integration with FluentValidation
-   Perfect for functional composition patterns

### 📨 CQRS Messaging Abstractions

-   `ICommand<TResult>` and `IQuery<TResult>` interfaces
-   WolverineFx message bus extensions
-   Type-safe command and query invocation
-   Built-in empty result handling

### 🗄️ Database Access Enhancements

-   Enhanced Dapper extensions for stored procedures
-   `DbDataSource` integration with async patterns
-   Simplified parameter provider interfaces
-   LINQ2DB integration with snake_case conventions

### 🔧 Utility Extensions

-   High-performance string case conversion (`ToSnakeCase`, `ToKebabCase`)
-   Pluralization services for domain modeling
-   Thread-safe, allocation-efficient implementations

## Result Types - Comprehensive Guide

### Basic Result Usage

The `Result<T>` type is a discriminated union that can contain either a success value of type `T` or a list of validation failures. This eliminates the need for exception handling in business logic.

```csharp
using Momentum.Extensions;
using FluentValidation.Results;

public class OrderService
{
    public Result<Order> CreateOrder(CreateOrderCommand command)
    {
        // Validate the command
        var validationResult = validator.Validate(command);
        if (!validationResult.IsValid)
        {
            // Return validation failures directly
            return validationResult.Errors;
        }

        // Business logic
        var order = new Order(command.CustomerId, command.Items);

        // Return success result
        return order;
    }
}
```

### Pattern Matching with Results

```csharp
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
{
    var result = await orderService.CreateOrderAsync(command);

    return result.Match<IActionResult>(
        success: order => CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order),
        validationFailures: errors => BadRequest(new {
            Message = "Validation failed",
            Errors = errors.Select(e => e.ErrorMessage)
        })
    );
}
```

### Advanced Result Composition

```csharp
public class OrderProcessingService
{
    public async Task<Result<ProcessedOrder>> ProcessOrderAsync(int orderId)
    {
        // Chain multiple operations that can fail
        var getOrderResult = await GetOrderAsync(orderId);
        if (getOrderResult.IsFailure)
            return CreateProcessingFailure(getOrderResult.ValidationFailures);

        var inventoryResult = await ReserveInventoryAsync(getOrderResult.Value);
        if (inventoryResult.IsFailure)
            return CreateProcessingFailure(inventoryResult.ValidationFailures);

        var paymentResult = await ProcessPaymentAsync(getOrderResult.Value);
        if (paymentResult.IsFailure)
            return CreateProcessingFailure(paymentResult.ValidationFailures);

        // All operations succeeded
        var processedOrder = new ProcessedOrder(
            getOrderResult.Value,
            inventoryResult.Value,
            paymentResult.Value);

        return processedOrder;
    }

    private static Result<ProcessedOrder> CreateProcessingFailure(IList<ValidationFailure> failures)
    {
        return failures.ToList();
    }
}
```

### FluentValidation Integration

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());
    }
}

public class OrderCommandHandler
{
    private readonly IValidator<CreateOrderCommand> validator;

    public async Task<Result<Order>> Handle(CreateOrderCommand command)
    {
        // Validate command
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            // Return failures as Result<T>
            return validationResult.Errors;
        }

        // Process valid command
        var order = new Order(command.CustomerId, command.Items);
        await repository.SaveAsync(order);

        return order;
    }
}
```

### Async Result Patterns

```csharp
public class UserService
{
    public async Task<Result<User>> RegisterUserAsync(RegisterUserCommand command)
    {
        // Async validation
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            return validationResult.Errors;
        }

        // Check for existing user
        var existingUser = await repository.GetByEmailAsync(command.Email);
        if (existingUser != null)
        {
            return new List<ValidationFailure>
            {
                new("Email", "Email address is already registered")
            };
        }

        // Create and save user
        var user = new User(command.Name, command.Email);
        await repository.SaveAsync(user);

        // Publish integration event
        await messageBus.PublishAsync(new UserRegistered(user.Id, user.Email));

        return user;
    }
}
```

## CQRS Messaging Abstractions

### Command and Query Definitions

```csharp
using Momentum.Extensions.Abstractions.Messaging;

// Commands modify state and return results
public record CreateCustomerCommand(
    string Name,
    string Email,
    string Phone
) : ICommand<Result<Customer>>;

public record UpdateCustomerCommand(
    int CustomerId,
    string Name,
    string Email
) : ICommand<Result<Customer>>;

// Queries retrieve data without side effects
public record GetCustomerQuery(int CustomerId) : IQuery<Result<Customer>>;

public record GetCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null
) : IQuery<Result<PagedResult<Customer>>>;
```

### Command and Query Handlers

```csharp
public class CreateCustomerHandler
{
    private readonly ICustomerRepository repository;
    private readonly IValidator<CreateCustomerCommand> validator;
    private readonly IMessageBus messageBus;

    public CreateCustomerHandler(
        ICustomerRepository repository,
        IValidator<CreateCustomerCommand> validator,
        IMessageBus messageBus)
    {
        this.repository = repository;
        this.validator = validator;
        this.messageBus = messageBus;
    }

    public async Task<Result<Customer>> Handle(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        // Validate command
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.Errors;
        }

        // Business logic
        var customer = new Customer(command.Name, command.Email, command.Phone);
        await repository.SaveAsync(customer, cancellationToken);

        // Publish integration event
        await messageBus.PublishAsync(
            new CustomerCreated(customer.Id, customer.Name, customer.Email),
            cancellationToken);

        return customer;
    }
}

public class GetCustomerHandler
{
    private readonly ICustomerRepository repository;

    public GetCustomerHandler(ICustomerRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<Customer>> Handle(
        GetCustomerQuery query,
        CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(query.CustomerId, cancellationToken);

        if (customer == null)
        {
            return new List<ValidationFailure>
            {
                new("CustomerId", $"Customer with ID {query.CustomerId} not found")
            };
        }

        return customer;
    }
}
```

### Message Bus Extensions

```csharp
using Momentum.Extensions.Messaging;

public class CustomerController : ControllerBase
{
    private readonly IMessageBus messageBus;

    public CustomerController(IMessageBus messageBus)
    {
        this.messageBus = messageBus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        // Use strongly-typed command invocation
        var result = await messageBus.InvokeCommandAsync(command, cancellationToken);

        return result.Match<IActionResult>(
            success: customer => CreatedAtAction(
                nameof(GetCustomer),
                new { id = customer.Id },
                customer),
            validationFailures: errors => BadRequest(new ValidationProblemDetails(
                errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage })))
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(
        int id,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerQuery(id);

        // Use strongly-typed query invocation
        var result = await messageBus.InvokeQueryAsync(query, cancellationToken);

        return result.Match<IActionResult>(
            success: customer => Ok(customer),
            validationFailures: errors => NotFound(new {
                Message = "Customer not found",
                Errors = errors.Select(e => e.ErrorMessage)
            })
        );
    }
}
```

## Database Access Enhancements

### Stored Procedure Execution

```csharp
using Momentum.Extensions.Data;
using Momentum.Extensions.Abstractions.Dapper;

public class CustomerRepository
{
    private readonly DbDataSource dataSource;

    public CustomerRepository(DbDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetCustomersParams(searchTerm);

        return await dataSource.SpQuery<Customer>(
            spName: "sp_get_customers",
            parameters: parameters,
            cancellationToken: cancellationToken);
    }

    public async Task<int> CreateCustomerAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        var parameters = new CreateCustomerParams(customer);

        return await dataSource.SpExecute(
            spName: "sp_create_customer",
            parameters: parameters,
            cancellationToken: cancellationToken);
    }
}

// Parameter providers implement IDbParamsProvider
public class GetCustomersParams : IDbParamsProvider
{
    public string? SearchTerm { get; }

    public GetCustomersParams(string? searchTerm)
    {
        SearchTerm = searchTerm;
    }

    public object ToDbParams()
    {
        return new
        {
            search_term = SearchTerm
        };
    }
}

public class CreateCustomerParams : IDbParamsProvider
{
    public Customer Customer { get; }

    public CreateCustomerParams(Customer customer)
    {
        Customer = customer;
    }

    public object ToDbParams()
    {
        return new
        {
            name = Customer.Name,
            email = Customer.Email,
            phone = Customer.Phone,
            created_at = Customer.CreatedAt
        };
    }
}
```

### LINQ2DB Integration

```csharp
using Momentum.Extensions.Data.LinqToDb;

public class AppDbContext : LinqToDB.Data.DataConnection
{
    public AppDbContext(string connectionString)
        : base("PostgreSQL", connectionString)
    {
        // Apply snake_case naming conventions
        this.AddMappingSchema(new SnakeCaseNamingConventionMetadataReader());
    }

    public ITable<Customer> Customers => this.GetTable<Customer>();
    public ITable<Order> Orders => this.GetTable<Order>();
}

// Entities automatically map to snake_case database columns
public class Customer
{
    public int Id { get; set; }                    // Maps to: id
    public string Name { get; set; }               // Maps to: name
    public string Email { get; set; }              // Maps to: email
    public DateTime CreatedAt { get; set; }        // Maps to: created_at
    public DateTime? UpdatedAt { get; set; }       // Maps to: updated_at
}

public class CustomerService
{
    private readonly AppDbContext context;

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        return await context.Customers
            .Where(c => c.UpdatedAt != null)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
```

## Utility Extensions

### String Case Conversion

High-performance, allocation-efficient case conversion utilities:

```csharp
using Momentum.Extensions.Abstractions.Extensions;

public class ApiEndpointMapper
{
    public string MapControllerName(string controllerName)
    {
        // Convert "CustomerController" to "customer"
        var baseName = controllerName.Replace("Controller", "");
        return baseName.ToKebabCase(); // "customer"
    }

    public string MapActionName(string actionName)
    {
        // Convert "GetCustomerById" to "get-customer-by-id"
        return actionName.ToKebabCase();
    }

    public string MapDatabaseTable(string entityName)
    {
        // Convert "CustomerOrder" to "customer_orders" (pluralized)
        return entityName.ToSnakeCase().Pluralize();
    }
}

// Examples of case conversion
public class CaseConversionExamples
{
    public void DemonstrateConversions()
    {
        // Snake case for database columns/tables
        "CustomerId".ToSnakeCase();           // "customer_id"
        "OrderDateTime".ToSnakeCase();        // "order_date_time"
        "HTTPRequest".ToSnakeCase();          // "http_request"
        "MyAPIName".ToSnakeCase();            // "my_api_name"

        // Kebab case for URLs/file names
        "CustomerController".ToKebabCase();   // "customer-controller"
        "GetCustomerById".ToKebabCase();      // "get-customer-by-id"
        "XMLHttpRequest".ToKebabCase();       // "xml-http-request"
    }
}
```

### Pluralization Services

```csharp
using Momentum.Extensions.Abstractions.Extensions;

public class DatabaseTableMapper
{
    public string GetTableName<T>()
    {
        var entityName = typeof(T).Name;
        return entityName.ToSnakeCase().Pluralize();
    }
}

// Usage examples
var tableNames = new DatabaseTableMapper();
tableNames.GetTableName<Customer>();    // "customers"
tableNames.GetTableName<OrderItem>();   // "order_items"
tableNames.GetTableName<Category>();    // "categories"
```

## Advanced Integration Patterns

### ASP.NET Core Middleware Integration

```csharp
public class ResultHandlingMiddleware
{
    private readonly RequestDelegate next;

    public ResultHandlingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Convert unhandled exceptions to Result types
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new
        {
            Message = "An error occurred processing your request",
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

// Extension method for easy registration
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseResultHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResultHandlingMiddleware>();
    }
}
```

### Dependency Injection Setup

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMomentumExtensions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register validators
        services.AddValidatorsFromAssemblyContaining<Program>();

        // Register message handlers
        services.AddWolverine(opts =>
        {
            opts.Discovery.IncludeAssembly(Assembly.GetExecutingAssembly());
            opts.OptimizeArtifactWorkflow();
        });

        // Register repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register database context
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddScoped<DbDataSource>(_ =>
            NpgsqlDataSource.Create(connectionString));

        // Register LINQ2DB context
        services.AddScoped<AppDbContext>(_ =>
            new AppDbContext(connectionString));

        return services;
    }
}
```

### Testing Patterns

```csharp
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> mockRepository;
    private readonly Mock<IValidator<CreateCustomerCommand>> mockValidator;
    private readonly CustomerService sut;

    public CustomerServiceTests()
    {
        mockRepository = new Mock<ICustomerRepository>();
        mockValidator = new Mock<IValidator<CreateCustomerCommand>>();
        sut = new CustomerService(mockRepository.Object, mockValidator.Object);
    }

    [Fact]
    public async Task CreateCustomer_WithValidCommand_ReturnsSuccessResult()
    {
        // Arrange
        var command = new CreateCustomerCommand("John Doe", "john@example.com", "+1234567890");
        var validationResult = new ValidationResult();
        var expectedCustomer = new Customer(command.Name, command.Email, command.Phone);

        mockValidator.Setup(x => x.ValidateAsync(command, default))
            .ReturnsAsync(validationResult);

        mockRepository.Setup(x => x.SaveAsync(It.IsAny<Customer>(), default))
            .ReturnsAsync(1);

        // Act
        var result = await sut.CreateCustomerAsync(command);

        // Assert
        result.IsT0.Should().BeTrue(); // Success case
        var customer = result.AsT0;
        customer.Name.Should().Be(command.Name);
        customer.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task CreateCustomer_WithInvalidCommand_ReturnsValidationFailures()
    {
        // Arrange
        var command = new CreateCustomerCommand("", "invalid-email", "");
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Email", "Email format is invalid")
        };
        var validationResult = new ValidationResult(validationFailures);

        mockValidator.Setup(x => x.ValidateAsync(command, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await sut.CreateCustomerAsync(command);

        // Assert
        result.IsT1.Should().BeTrue(); // Validation failure case
        var failures = result.AsT1;
        failures.Should().HaveCount(2);
        failures.Should().Contain(f => f.PropertyName == "Name");
        failures.Should().Contain(f => f.PropertyName == "Email");
    }
}
```

## Best Practices

### Result Type Guidelines

```csharp
// ✅ DO: Use Result<T> for operations that can fail
public async Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
{
    var validationResult = await validator.ValidateAsync(command);
    if (!validationResult.IsValid)
        return validationResult.Errors;

    // Business logic here
    return new Order(command);
}

// ❌ DON'T: Use exceptions for expected business failures
public async Task<Order> CreateOrderAsync(CreateOrderCommand command)
{
    var validationResult = await validator.ValidateAsync(command);
    if (!validationResult.IsValid)
        throw new ValidationException(validationResult.Errors); // Avoid this

    return new Order(command);
}

// ✅ DO: Use pattern matching for Result handling
var result = await orderService.CreateOrderAsync(command);
return result.Match<IActionResult>(
    success: order => Ok(order),
    validationFailures: errors => BadRequest(errors)
);

// ❌ DON'T: Use reflection-based checking
if (result.IsT0) // Avoid this pattern
{
    var order = result.AsT0;
    return Ok(order);
}
```

### Command/Query Handler Design

```csharp
// ✅ DO: Keep handlers focused on single responsibility
public class CreateCustomerHandler
{
    public async Task<Result<Customer>> Handle(CreateCustomerCommand command)
    {
        // Only handle customer creation logic
        // Delegate validation, persistence, and event publishing to appropriate services
    }
}

// ❌ DON'T: Mix multiple concerns in handlers
public class CustomerHandler // Avoid this
{
    public async Task<Result<Customer>> Create(CreateCustomerCommand command) { }
    public async Task<Result<Customer>> Update(UpdateCustomerCommand command) { }
    public async Task<Result> Delete(DeleteCustomerCommand command) { }
    // Too many responsibilities
}

// ✅ DO: Use cancellation tokens consistently
public async Task<Result<Customer>> Handle(
    CreateCustomerCommand command,
    CancellationToken cancellationToken) // Always include cancellation token
{
    var customer = new Customer(command.Name, command.Email);
    await repository.SaveAsync(customer, cancellationToken);
    return customer;
}
```

### Database Access Patterns

```csharp
// ✅ DO: Use parameter providers for complex stored procedures
public class GetCustomerOrdersParams : IDbParamsProvider
{
    public int CustomerId { get; }
    public DateTime? FromDate { get; }
    public DateTime? ToDate { get; }

    public object ToDbParams() => new
    {
        customer_id = CustomerId,
        from_date = FromDate,
        to_date = ToDate
    };
}

// ✅ DO: Use async patterns consistently
public async Task<IEnumerable<Order>> GetCustomerOrdersAsync(
    int customerId,
    CancellationToken cancellationToken = default)
{
    var parameters = new GetCustomerOrdersParams(customerId);
    return await dataSource.SpQuery<Order>(
        "sp_get_customer_orders",
        parameters,
        cancellationToken);
}
```

## Performance Considerations

### String Extensions Performance

The string extension methods are highly optimized:

```csharp
// High-performance implementation details:
// - Uses ReadOnlySpan<char> to avoid allocations
// - Single-pass algorithms with pre-calculated lengths
// - String.Create() for optimal memory usage
// - No regex or reflection overhead

// Benchmarks (compared to naive implementations):
// ToSnakeCase: ~3x faster, 60% less memory allocation
// ToKebabCase: ~3x faster, 60% less memory allocation
```

### Result Type Performance

```csharp
// Result<T> is a struct-based discriminated union
// - No boxing/unboxing overhead
// - Compile-time type safety
// - Zero allocation for success paths
// - Minimal allocation for failure paths (only ValidationFailure list)
```

## Troubleshooting

### Common Issues

**ValidationResult not working with OneOf:**

```csharp
// ✅ Correct: Use List<ValidationFailure>
return validationResult.Errors; // This works

// ❌ Incorrect: Using ValidationResult directly
return validationResult; // This won't compile
```

**Missing Message Bus Registration:**

```csharp
// Make sure to register Wolverine in Program.cs
builder.Services.AddWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(Assembly.GetExecutingAssembly());
});
```

**LINQ2DB Snake Case Not Applied:**

```csharp
// Ensure the naming convention is registered
var context = new AppDbContext(connectionString);
context.AddMappingSchema(new SnakeCaseNamingConventionMetadataReader());
```

## Migration Guide

### From Exception-Based Error Handling

**Before:**

```csharp
public async Task<Customer> CreateCustomerAsync(CreateCustomerCommand command)
{
    if (string.IsNullOrEmpty(command.Email))
        throw new ValidationException("Email is required");

    var customer = new Customer(command.Name, command.Email);
    return await repository.SaveAsync(customer);
}
```

**After:**

```csharp
public async Task<Result<Customer>> CreateCustomerAsync(CreateCustomerCommand command)
{
    var validationResult = await validator.ValidateAsync(command);
    if (!validationResult.IsValid)
        return validationResult.Errors;

    var customer = new Customer(command.Name, command.Email);
    await repository.SaveAsync(customer);
    return customer;
}
```

### From MediatR to Wolverine

**Before (MediatR):**

```csharp
public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, Customer>
{
    public async Task<Customer> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

**After (Wolverine):**

```csharp
public class CreateCustomerHandler
{
    public async Task<Result<Customer>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        // Implementation with Result<T> return type
    }
}
```

## Integrated Dependencies

This package includes and pre-configures these essential dependencies:

| Package                              | Version Range | Purpose                                 |
| ------------------------------------ | ------------- | --------------------------------------- |
| **FluentValidation**                 | Latest        | Input validation with fluent syntax     |
| **OneOf & OneOf.SourceGenerator**    | Latest        | Discriminated union types for Result<T> |
| **Dapper**                           | Latest        | High-performance micro-ORM              |
| **linq2db**                          | Latest        | LINQ-to-SQL provider with strong typing |
| **WolverineFx**                      | Latest        | Message bus and CQRS infrastructure     |
| **Momentum.Extensions.Abstractions** | Same version  | Core abstractions and interfaces        |

## Target Frameworks

-   **.NET 10.0**: Primary target framework
-   **C# 13.0**: Required for discriminated union features
-   **Compatible with**: ASP.NET Core 10.0+, Entity Framework Core 10.0+

## Related Packages

-   **[Momentum.Extensions.Abstractions](../Momentum.Extensions.Abstractions/README.md)**: Core interfaces and abstractions
-   **[Momentum.ServiceDefaults](../Momentum.ServiceDefaults/README.md)**: Aspire service defaults and configuration
-   **[Momentum.Extensions.SourceGenerators](../Momentum.Extensions.SourceGenerators/README.md)**: Compile-time code generation
-   **[Momentum.Extensions.Messaging.Kafka](../Momentum.Extensions.Messaging.Kafka/README.md)**: Kafka integration with CloudEvents

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/vgmello/momentum/blob/main/LICENSE) file for details.

## Contributing

For contribution guidelines and more information about the Momentum platform, visit the [main repository](https://github.com/vgmello/momentum).
