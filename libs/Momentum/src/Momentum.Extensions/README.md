# Momentum.Extensions

Core extensions library for Momentum .NET providing common utilities, result types, messaging abstractions, and data access helpers. Includes integrations with Dapper, FluentValidation, OneOf, and WolverineFx.

## Overview

The `Momentum.Extensions` package provides essential utilities and extensions that streamline development with the Momentum platform. It includes robust result types for error handling, messaging abstractions for event-driven architectures, data access helpers, and validation utilities.

## Installation

Add the package to your project using the .NET CLI:

```bash
dotnet add package Momentum.Extensions
```

Or using the Package Manager Console:

```powershell
Install-Package Momentum.Extensions
```

## Key Features

-   **Result Types**: Robust result handling for success/failure states without exceptions
-   **Messaging Infrastructure**: Base classes and interfaces for message-based architectures
-   **Data Access Helpers**: Enhanced Dapper extensions and utilities
-   **Validation Integration**: FluentValidation helpers and extensions
-   **Discriminated Unions**: OneOf support for type-safe union types
-   **LINQ2DB Integration**: Database mapping and query utilities

## Getting Started

### Prerequisites

-   .NET 9.0 or later
-   C# 12.0 or later

### Basic Usage

#### Working with Result Types

```csharp
using Momentum.Extensions;

public class UserService
{
    public Result<User> GetUser(int id)
    {
        var user = userRepository.Find(id);

        return user switch
        {
            null => Result<User>.Failure("User not found"),
            _ => Result<User>.Success(user)
        };
    }

    public async Task<Result> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var getUserResult = GetUser(id);
        if (getUserResult.IsFailure)
        {
            return Result.Failure(getUserResult.Error);
        }

        // Update logic here
        await userRepository.UpdateAsync(getUserResult.Value);
        return Result.Success();
    }
}
```

#### Message Handling with WolverineFx

```csharp
using Momentum.Extensions;
using WolverineFx;

// Define a message/command
public record CreateUser(string Name, string Email);

// Message handler
public class CreateUserHandler
{
    private readonly IUserRepository userRepository;
    private readonly IMessageBus messageBus;

    public CreateUserHandler(IUserRepository userRepository, IMessageBus messageBus)
    {
        this.userRepository = userRepository;
        this.messageBus = messageBus;
    }

    public async Task<Result<User>> Handle(CreateUser command)
    {
        var user = new User(command.Name, command.Email);
        await userRepository.SaveAsync(user);

        // Publish integration event
        await messageBus.PublishAsync(new UserCreated(user.Id, user.Name, user.Email));

        return Result<User>.Success(user);
    }
}
```

#### FluentValidation Integration

```csharp
using FluentValidation;
using Momentum.Extensions;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}

public class UserController : ControllerBase
{
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        [FromServices] IValidator<CreateUserRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        // Process the valid request
        return Ok();
    }
}
```

#### Dapper Extensions

```csharp
using Dapper;
using Momentum.Extensions.Data;

public class UserRepository
{
    private readonly IDbConnection connection;

    public UserRepository(IDbConnection connection)
    {
        this.connection = connection;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Users WHERE Id = @Id";
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        const string sql = "SELECT * FROM Users WHERE IsActive = true ORDER BY Name";
        return await connection.QueryAsync<User>(sql);
    }
}
```

#### OneOf Discriminated Unions

```csharp
using OneOf;

// Define a union type for API responses
[GenerateOneOf]
public partial class ApiResponse<T> : OneOfBase<T, ValidationError, NotFoundError>
{
}

public class ValidationError
{
    public List<string> Errors { get; init; } = [];
}

public class NotFoundError
{
    public string ResourceType { get; init; } = string.Empty;
    public string ResourceId { get; init; } = string.Empty;
}

// Usage in controller
public class UsersController : ControllerBase
{
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await userService.GetUserAsync(id);

        return result.Match<IActionResult>(
            success: user => Ok(user),
            validationError: error => BadRequest(error.Errors),
            notFoundError: error => NotFound($"{error.ResourceType} with ID {error.ResourceId} not found")
        );
    }
}
```

## Integrated Dependencies

This package includes and configures the following dependencies:

| Package                              | Purpose                                   |
| ------------------------------------ | ----------------------------------------- |
| **FluentValidation**                 | Object validation with fluent syntax      |
| **OneOf & OneOf.SourceGenerator**    | Discriminated union types                 |
| **Dapper**                           | Lightweight ORM for data access           |
| **linq2db**                          | LINQ to database provider                 |
| **WolverineFx**                      | Messaging infrastructure and CQRS support |
| **Momentum.Extensions.Abstractions** | Core abstractions and contracts           |

## Configuration

The package provides MSBuild properties for common configuration:

```xml
<PropertyGroup>
  <!-- Enable nullable reference types -->
  <Nullable>enable</Nullable>

  <!-- Enable OneOf source generation -->
  <OneOf_GenerateSourceGenerator>true</OneOf_GenerateSourceGenerator>
</PropertyGroup>
```

## Best Practices

### Result Type Usage

```csharp
// ✅ Good: Chain results without nested exception handling
public async Task<Result<ProcessedOrder>> ProcessOrderAsync(int orderId)
{
    var getOrderResult = await GetOrderAsync(orderId);
    if (getOrderResult.IsFailure)
        return Result<ProcessedOrder>.Failure(getOrderResult.Error);

    var validateResult = ValidateOrder(getOrderResult.Value);
    if (validateResult.IsFailure)
        return Result<ProcessedOrder>.Failure(validateResult.Error);

    return await ProcessValidOrderAsync(getOrderResult.Value);
}
```

### Message Handler Design

```csharp
// ✅ Good: Keep handlers focused and testable
public class OrderCreatedHandler
{
    public async Task Handle(OrderCreated orderCreated, CancellationToken cancellationToken)
    {
        // Single responsibility: update inventory
        await inventoryService.ReserveItemsAsync(orderCreated.Items, cancellationToken);
    }
}
```

## Target Frameworks

-   **.NET 9.0**: Primary target framework
-   Compatible with ASP.NET Core 9.0 and later

## Related Packages

-   [Momentum.Extensions.Abstractions](../Momentum.Extensions.Abstractions/README.md) - Core abstractions
-   [Momentum.ServiceDefaults](../Momentum.ServiceDefaults/README.md) - Service configuration
-   [Momentum.Extensions.SourceGenerators](../Momentum.Extensions.SourceGenerators/README.md) - Code generators

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/vgmello/momentum/blob/main/LICENSE) file for details.

## Contributing

For contribution guidelines and more information about the Momentum platform, visit the [main repository](https://github.com/vgmello/momentum).
