# Momentum.Extensions.Abstractions

Core abstractions and interfaces for the Momentum platform. This foundational package defines the essential contracts, base types, and abstractions that enable extensibility and loose coupling across all Momentum libraries.

## Installation

```bash
dotnet add package Momentum.Extensions.Abstractions
```

## Purpose

This package serves as the foundation layer for the Momentum platform, providing:

-   Core interfaces and contracts
-   Base abstract classes
-   Common types and enumerations
-   Extensibility points for framework components

## Features

-   **Framework Contracts**: Interfaces that define the core framework behavior
-   **Extensibility**: Abstract base classes for implementing custom behaviors
-   **Loose Coupling**: Enables dependency inversion and testability
-   **Broad Compatibility**: Targets .NET Standard 2.1 for maximum compatibility

## Architecture Role

This package sits at the bottom of the Momentum library dependency chain:

```
┌─────────────────────────────────────────┐
│           Application Code              │
├─────────────────────────────────────────┤
│ Momentum.Extensions                   │
│ Momentum.ServiceDefaults             │
│ Momentum.Extensions.SourceGenerators │
├─────────────────────────────────────────┤
│ Momentum.Extensions.Abstractions     │ ← This Package
└─────────────────────────────────────────┘
```

## Usage

### Implementing Interfaces

```csharp
using Momentum.Extensions.Abstractions;

public class MyService : IMyServiceContract
{
    // Implementation details
}
```

### Extending Base Classes

```csharp
using Momentum.Extensions.Abstractions;

public class CustomHandler : HandlerBase
{
    protected override Task HandleAsync(IMessage message)
    {
        // Custom implementation
    }
}
```

## Design Principles

-   **Minimal Dependencies**: No external package dependencies to avoid version conflicts
-   **Stable Contracts**: Interfaces designed for long-term stability
-   **Extensibility First**: Every abstraction is designed with extensibility in mind
-   **Performance Aware**: Abstractions designed to minimize overhead

## Target Framework

-   **.NET Standard 2.1**: Ensures compatibility with .NET Core 3.0+, .NET 5+, and .NET Framework 4.8

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/vgmello/momentum-sample/blob/main/LICENSE) file for details.

## Contributing

For more information about the Momentum platform and contribution guidelines, please visit the [main repository](https://github.com/vgmello/momentum-sample).
