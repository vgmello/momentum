# Operations.Extensions

Core extensions library for the Operations platform providing common utilities, result types, messaging abstractions, and data access helpers.

## Installation

```bash
dotnet add package Operations.Extensions
```

## Features

- **Result Type**: A robust result type for handling success/failure states without exceptions
- **Messaging Abstractions**: Base classes and interfaces for message-based architectures  
- **Dapper Extensions**: Helper methods and utilities for Dapper ORM
- **Validation Integration**: FluentValidation integration helpers
- **OneOf Support**: Discriminated union types via OneOf library

## Dependencies

This package includes the following dependencies:
- FluentValidation - For object validation
- OneOf & OneOf.SourceGenerator - For discriminated unions
- Dapper - For data access
- WolverineFx - For messaging infrastructure
- Operations.Extensions.Abstractions - Core abstractions

## Basic Usage

### Result Type

```csharp
public Result<User> GetUser(int id)
{
    var user = repository.Find(id);
    return user != null 
        ? Result<User>.Success(user)
        : Result<User>.Failure("User not found");
}
```

### Messaging

The library provides base classes for implementing message handlers and event processing with WolverineFx.

### Dapper Extensions

Enhanced Dapper functionality is available in the `Dapper` namespace for simplified data access patterns.

## Requirements

- .NET 9.0 or later

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/vgmello/momentum-sample/blob/main/LICENSE) file for details.

## Contributing

For more information about the Operations platform and contribution guidelines, please visit the [main repository](https://github.com/vgmello/momentum-sample).