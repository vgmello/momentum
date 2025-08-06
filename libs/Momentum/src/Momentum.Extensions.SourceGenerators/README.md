# Momentum.Extensions.SourceGenerators

Source generators for the Momentum platform providing compile-time code generation utilities. This package contains Roslyn analyzers and source generators that reduce boilerplate code and improve developer productivity.

## Installation

```bash
dotnet add package Momentum.Extensions.SourceGenerators
```

## Features

-   **Compile-Time Code Generation**: Generates code during compilation based on your source code
-   **Roslyn Analyzers**: Built on the Roslyn compiler platform for C#
-   **Zero Runtime Overhead**: All code generation happens at compile time
-   **Abstractions Integration**: Works with Momentum.Extensions.Abstractions types

## How It Works

This package is a Roslyn component that integrates with the C# compiler to generate code based on patterns found in your source code. The generated code is emitted during compilation and becomes part of your assembly.

### Package Structure

As a source generator package:

-   The generator DLL is packaged in `analyzers/dotnet/cs`
-   Includes the Momentum.Extensions.Abstractions dependency
-   Does not include build output in the consuming project

## Dependencies

-   Microsoft.CodeAnalysis.CSharp - Roslyn compiler APIs
-   Microsoft.CodeAnalysis.Analyzers - Analyzer infrastructure
-   Momentum.Extensions.Abstractions - Core abstractions

## Usage

Once installed, the source generators will automatically run during compilation. The specific generators included will analyze your code and generate appropriate implementations based on the patterns they detect.

### Generated Code Location

During development, generated code can be found in:

```
obj/Internal/Generated/
```

## Requirements

-   .NET Standard 2.1 compatible projects
-   C# compiler with source generator support

## Development

This is a Roslyn component project with special packaging requirements:

-   `IsRoslynComponent` is set to true
-   `IncludeBuildOutput` is false (generators are packaged as analyzers)
-   Targets netstandard2.1 for broad compatibility

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/vgmello/momentum-sample/blob/main/LICENSE) file for details.

## Contributing

For more information about the Momentum platform and contribution guidelines, please visit the [main repository](https://github.com/vgmello/momentum-sample).
