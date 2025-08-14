# Momentum.Extensions.Abstractions

Core abstractions and interfaces for the Momentum platform. This foundational package defines contracts, base types, and abstractions used across all Momentum libraries. Essential for extensibility and loose coupling.

## Overview

The `Momentum.Extensions.Abstractions` package provides the foundational contracts and base types that enable extensibility and loose coupling throughout the Momentum platform. As a dependency-free abstraction layer, it defines interfaces and base classes that other Momentum libraries build upon.

## Installation

Add the package to your project using the .NET CLI:

```bash
dotnet add package Momentum.Extensions.Abstractions
```

Or using the Package Manager Console:

```powershell
Install-Package Momentum.Extensions.Abstractions
```

## Key Features

-   **Framework Contracts**: Core interfaces that define framework behavior
-   **Base Types**: Abstract base classes for implementing custom behaviors
-   **Dependency-Free**: No external dependencies to avoid version conflicts
-   **Broad Compatibility**: Targets .NET Standard 2.1 for maximum compatibility
-   **Extensibility**: Designed for loose coupling and testability

## Getting Started

### Prerequisites

-   .NET Standard 2.1 compatible runtime
-   C# 7.3 or later

### Basic Usage

#### Implementing Core Interfaces

```csharp
using Momentum.Extensions.Abstractions;

// Example: Implementing a service contract
public class UserService : IUserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Implementation logic
        return await repository.GetByIdAsync(id);
    }
}
```

#### Extending Base Classes

```csharp
using Momentum.Extensions.Abstractions;

// Example: Custom message handler
public class OrderCreatedHandler : MessageHandlerBase<OrderCreated>
{
    protected override async Task HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        // Custom handling logic
        await ProcessOrderAsync(message.OrderId, cancellationToken);
    }
}
```

#### Using Result Types

```csharp
using Momentum.Extensions.Abstractions;

// Example: Method returning a result
public Result<Customer> ValidateCustomer(CustomerData data)
{
    if (string.IsNullOrEmpty(data.Email))
    {
        return Result<Customer>.Failure("Email is required");
    }

    var customer = new Customer(data.Email, data.Name);
    return Result<Customer>.Success(customer);
}
```

## Architecture

This package sits at the foundation of the Momentum library ecosystem:

```
Application Code
├── Momentum.Extensions
├── Momentum.ServiceDefaults
├── Momentum.ServiceDefaults.Api
└── Momentum.Extensions.Abstractions ← Foundation
```

## Design Principles

-   **Zero Dependencies**: No external package references to prevent version conflicts
-   **Stable APIs**: Contracts designed for long-term stability
-   **Performance First**: Minimal overhead abstractions
-   **Extensibility**: Every component supports customization and extension

## Target Frameworks

-   **.NET Standard 2.1**: Compatible with:
    -   .NET Core 3.0 and later
    -   .NET 5.0 and later
    -   .NET Framework 4.8

## Related Packages

-   [Momentum.Extensions](../Momentum.Extensions/README.md) - Core utilities and implementations
-   [Momentum.ServiceDefaults](../Momentum.ServiceDefaults/README.md) - Service configuration defaults
-   [Momentum.Extensions.SourceGenerators](../Momentum.Extensions.SourceGenerators/README.md) - Code generation utilities

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/vgmello/momentum/blob/main/LICENSE) file for details.

## Contributing

For contribution guidelines and more information about the Momentum platform, visit the [main repository](https://github.com/vgmello/momentum).
