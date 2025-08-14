# Momentum.Extensions.SourceGenerators

Source generators for the Momentum platform providing compile-time code generation utilities to reduce boilerplate and improve developer productivity. Includes generators for common patterns used across Momentum libraries.

## Overview

The `Momentum.Extensions.SourceGenerators` package provides Roslyn-based source generators that automatically generate code during compilation. These generators analyze your source code and create implementations for common patterns, reducing boilerplate and ensuring consistency across the Momentum platform.

## Installation

Add the package to your project using the .NET CLI:

```bash
dotnet add package Momentum.Extensions.SourceGenerators
```

Or using the Package Manager Console:

```powershell
Install-Package Momentum.Extensions.SourceGenerators
```

## Key Features

-   **Compile-Time Generation**: Code generation happens during compilation with zero runtime overhead
-   **Pattern-Based**: Automatically detects and implements common coding patterns
-   **Roslyn Integration**: Built on the robust Roslyn compiler platform
-   **IDE Support**: Generated code is available in IntelliSense and debugging
-   **MSBuild Integration**: Seamlessly integrates with the build process

## Getting Started

### Prerequisites

-   .NET Standard 2.1 compatible project
-   C# compiler with source generator support (C# 9.0+)
-   Visual Studio 2019 16.9+ or VS Code with C# extension

### Basic Usage

Once installed, the source generators automatically activate during compilation. No additional configuration is required.

#### Viewing Generated Code

During development, you can view generated code in several ways:

**In Visual Studio:**

1. **Solution Explorer** → **Dependencies** → **Analyzers** → **Momentum.Extensions.SourceGenerators**
2. Expand to see generated files

**File System Location:**

```
obj/Debug/net9.0/generated/Momentum.Extensions.SourceGenerators/
```

**Enable Generated File Output:**

```xml
<!-- Add to your .csproj to emit generated files -->
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

## Available Generators

### Command Handler Generator

Automatically generates command handler registrations and routing.

```csharp
// Input: Decorate your handlers
[GenerateHandler]
public class CreateUserHandler
{
    public async Task<Result<User>> Handle(CreateUserCommand command)
    {
        // Implementation
    }
}

// Generated: Registration code
public static class HandlerRegistrations
{
    public static IServiceCollection AddGeneratedHandlers(this IServiceCollection services)
    {
        services.AddScoped<CreateUserHandler>();
        return services;
    }
}
```

### Query Generator

Generates query implementations based on method signatures.

```csharp
// Input: Interface with query methods
public interface IUserQueries
{
    [GenerateQuery("SELECT * FROM Users WHERE Id = @Id")]
    Task<User?> GetByIdAsync(int id);

    [GenerateQuery("SELECT * FROM Users WHERE IsActive = @IsActive ORDER BY Name")]
    Task<IEnumerable<User>> GetActiveUsersAsync(bool isActive = true);
}

// Generated: Implementation using Dapper
public class UserQueries : IUserQueries
{
    private readonly IDbConnection connection;

    public UserQueries(IDbConnection connection)
    {
        this.connection = connection;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await connection.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id",
            new { Id = id });
    }

    // Additional generated methods...
}
```

### Result Type Generator

Generates extension methods for working with Result types.

```csharp
// Input: Custom result types
[GenerateResultExtensions]
public record ValidationResult<T>(T Value, List<ValidationError> Errors);

// Generated: Extension methods
public static class ValidationResultExtensions
{
    public static ValidationResult<TResult> Map<T, TResult>(
        this ValidationResult<T> result,
        Func<T, TResult> mapper)
    {
        // Generated implementation
    }

    public static async Task<ValidationResult<TResult>> MapAsync<T, TResult>(
        this ValidationResult<T> result,
        Func<T, Task<TResult>> mapper)
    {
        // Generated implementation
    }
}
```

## Configuration

### MSBuild Properties

Control generator behavior with MSBuild properties:

```xml
<PropertyGroup>
  <!-- Enable/disable specific generators -->
  <MomentumGenerateHandlers>true</MomentumGenerateHandlers>
  <MomentumGenerateQueries>true</MomentumGenerateQueries>
  <MomentumGenerateResults>true</MomentumGenerateResults>

  <!-- Output generated files for debugging -->
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### Global Configuration

Create an `analyzers.globalconfig` file for project-wide settings:

```ini
# analyzers.globalconfig
is_global = true

# Momentum generator settings
momentum.generators.enabled = true
momentum.generators.emit_debug_info = false
momentum.generators.namespace_prefix = MyApp.Generated
```

## Advanced Usage

### Custom Attributes

Define custom attributes to control code generation:

```csharp
// Custom attribute for specialized generation
[AttributeUsage(AttributeTargets.Class)]
public class GenerateRepositoryAttribute : Attribute
{
    public string TableName { get; set; } = string.Empty;
    public bool GenerateCrud { get; set; } = true;
}

// Usage
[GenerateRepository(TableName = "Users", GenerateCrud = true)]
public partial class UserRepository
{
    // Generated methods will be added to this partial class
}
```

### Conditional Generation

Use preprocessor directives to control generation:

```csharp
#if GENERATE_OPTIMIZED_QUERIES
[GenerateOptimizedQuery]
public class PerformanceCriticalQueries
{
    // Only generates optimized versions in release builds
}
#endif
```

## Troubleshooting

### Common Issues

**Generator Not Running**

-   Ensure the package is properly referenced
-   Check that you're using a supported C# version
-   Rebuild the solution to trigger generation

**Generated Code Not Visible**

-   Enable `EmitCompilerGeneratedFiles` in your project file
-   Check the output path: `obj/Debug/[framework]/generated/`
-   In Visual Studio, expand **Dependencies** → **Analyzers**

**Compilation Errors**

-   Verify that generated code dependencies are available
-   Check that partial classes are properly declared
-   Ensure attributes are correctly applied

### Debugging Generated Code

Enable detailed logging:

```xml
<PropertyGroup>
  <MomentumGeneratorVerbose>true</MomentumGeneratorVerbose>
</PropertyGroup>
```

View detailed MSBuild output:

```bash
dotnet build -v diagnostic
```

## Package Structure

As a Roslyn component package, this has a specialized structure:

-   **Analyzer DLL**: Packaged in `analyzers/dotnet/cs/`
-   **Dependencies**: Momentum.Extensions.Abstractions included
-   **No Runtime Output**: Generators don't add runtime dependencies
-   **Build Integration**: MSBuild props/targets for configuration

## Target Frameworks

-   **.NET Standard 2.1**: Ensures compatibility with all generator hosts
-   **C# 9.0+**: Required for source generator support
-   Compatible with .NET Core 3.1+, .NET 5+, and .NET Framework 4.8

## Related Packages

-   [Momentum.Extensions.Abstractions](../Momentum.Extensions.Abstractions/README.md) - Core abstractions and attributes
-   [Momentum.Extensions](../Momentum.Extensions/README.md) - Runtime utilities and helpers
-   [Momentum.ServiceDefaults](../Momentum.ServiceDefaults/README.md) - Service configuration

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/vgmello/momentum/blob/main/LICENSE) file for details.

## Contributing

For contribution guidelines and more information about the Momentum platform, visit the [main repository](https://github.com/vgmello/momentum).
