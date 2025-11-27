# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**Momentum Libraries** is a collection of .NET 10 libraries that provide platform services, extensions, and source generators for the Momentum template system. These libraries work both standalone and as part of generated solutions.

**Key Libraries**:
- **Momentum.Extensions**: Core utilities, result types, and messaging abstractions
- **Momentum.ServiceDefaults**: Aspire-based service configuration and observability
- **Momentum.Extensions.SourceGenerators**: Compile-time code generation
- **Momentum.Extensions.EventMarkdownGenerator**: Documentation generation from events
- **Momentum.Extensions.Messaging.Kafka**: Kafka integration with CloudEvents

## Development Commands

### Building and Testing

```bash
# Build all libraries
dotnet build

# Run all tests
dotnet test

# Build specific library
dotnet build src/Momentum.Extensions

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Momentum.Extensions.Tests
```

### Source Generator Development

```bash
# Build source generators with diagnostic output
dotnet build src/Momentum.Extensions.SourceGenerators -v diagnostic

# Debug source generators (enables generator debugging)
dotnet build -p:MomentumGeneratorVerbose=true

# View generated files output
dotnet build -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=Generated
```

### Event Documentation Generation

```bash
# Generate event documentation from test assembly
cd src/Momentum.Extensions.EventMarkdownGenerator
dotnet run -- generate --assembly "../../tests/TestEvents/bin/Debug/net10.0/TestEvents.dll" --output "../../docs/generated"

# Test different generation scenarios
cd tests/Momentum.Extensions.EventMarkdownGenerator.Tests
dotnet test --filter "IntegrationTests"
```

### Documentation Development

```bash
# Build and serve library documentation
cd docs && pnpm install && pnpm dev

# Generate DocFX documentation
cd docs && dotnet build
```

### NuGet Package Development

```bash
# Pack libraries for local testing
dotnet pack --configuration Release

# Pack specific library
dotnet pack src/Momentum.Extensions --configuration Release

# Install local package for testing
dotnet add reference /path/to/package.nupkg
```

## Architecture & Library Design

### Library Structure

Each library follows consistent patterns:

```
src/Momentum.[LibraryName]/
├── [LibraryName].csproj          # Project with package metadata
├── [LibraryName].props           # MSBuild integration (if applicable)
├── README.md                     # Complete package documentation
├── [Core implementation files]
└── bin/, obj/                    # Build outputs
```

### Package Dependencies Pattern

Libraries use conditional dependencies to work both standalone and in template context:

```xml
<!-- Standalone development (always include) -->
<ItemGroup Condition="'$(DevelopmentDependency)' != 'true'">
  <ProjectReference Include="../Momentum.Extensions.Abstractions/Momentum.Extensions.Abstractions.csproj" />
</ItemGroup>

<!-- Template context (package references) -->
<ItemGroup Condition="'$(DevelopmentDependency)' == 'true'">
  <PackageReference Include="Momentum.Extensions.Abstractions" Version="$(MomentumVersion)" />
</ItemGroup>
```

### Source Generator Architecture

Source generators in `Momentum.Extensions.SourceGenerators`:

- **DbCommand Generator**: Creates database command handlers and parameter providers
- **Event Documentation Generator**: Extracts XML docs and generates markdown
- **Analyzer Infrastructure**: Roslyn analyzers for code quality

### Service Defaults Pattern

`Momentum.ServiceDefaults` provides:

- **Aspire Integration**: Complete .NET Aspire service defaults
- **Observability Stack**: OpenTelemetry + Serilog configuration  
- **Messaging Infrastructure**: WolverineFx with PostgreSQL persistence
- **Resilience Patterns**: HTTP client resilience and circuit breakers

## Testing Strategy

### Unit Testing

```bash
# Run unit tests for specific library
dotnet test tests/Momentum.Extensions.Tests

# Run with detailed output
dotnet test tests/Momentum.Extensions.Tests --logger "console;verbosity=detailed"
```

### Source Generator Testing

```bash
# Test source generators with different scenarios
dotnet test tests/Momentum.Extensions.SourceGenerators.Tests

# Debug source generator compilation
dotnet test tests/Momentum.Extensions.SourceGenerators.Tests --filter "DbCommandSourceGen"
```

### Integration Testing Scenarios

The EventMarkdownGenerator has comprehensive scenario-based testing:

```bash
# Run scenario tests
cd tests/Momentum.Extensions.EventMarkdownGenerator.Tests
dotnet test --filter "ScenarioBasedIntegrationTests"

# Add new scenarios in IntegrationTestScenarios/
# Each scenario has: config.json, input.xml, expected/ output
```

## Common Development Tasks

### Adding New Library

1. Create project structure: `src/Momentum.[LibraryName]/`
2. Add to solution: `Momentum.slnx`
3. Create README.md with package documentation
4. Add MSBuild props file if needed
5. Create corresponding test project in `tests/`

### Working with Source Generators

```bash
# Enable generator debugging during development
dotnet build -p:MomentumGeneratorVerbose=true

# View generated source files
ls obj/Debug/net10.0/generated/Momentum.Extensions.SourceGenerators/

# Test generator with sample code
dotnet new console -n GeneratorTest
cd GeneratorTest && dotnet add package Momentum.Extensions.SourceGenerators
# Add test source code and build
```

### Event Documentation Development

```bash
# Create test event assembly
cd tests/TestEvents
dotnet build

# Generate documentation
cd ../../src/Momentum.Extensions.EventMarkdownGenerator
dotnet run -- generate --assembly "../../../tests/TestEvents/bin/Debug/net10.0/TestEvents.dll" --output "./test-output"

# View generated markdown files
ls test-output/
```

### Package Versioning

Version is controlled by `version.txt`:

```bash
# Update version for all packages
echo "1.2.0" > version.txt

# Development builds get timestamp suffix automatically
# Release builds use exact version from file
```

## Build Configuration

### MSBuild Properties

Global properties in `Directory.Build.props`:

```xml
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<CodeAnalysisRuleSet>../../Momentum.ruleset</CodeAnalysisRuleSet>
```

### Source Generator Properties

Control source generation behavior:

```xml
<!-- Enable generator debugging -->
<MomentumGeneratorVerbose>true</MomentumGeneratorVerbose>

<!-- Output generated files -->
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
```

## Development Environment

### Prerequisites

- .NET 10.0 SDK
- Visual Studio 2022 17.8+ or VS Code with C# extension
- Node.js and pnpm (for documentation)

### IDE Setup

**Visual Studio**:
- Install .NET Compiler Platform SDK for source generator development
- Enable "Show generated files" in Tools → Options → Text Editor → C#

**VS Code**:
- Install C# Dev Kit extension
- Enable Roslyn analyzer logging for generator debugging

## Special Considerations

### Template Context vs Standalone

Libraries must work in both contexts:

- **Standalone Development**: Uses ProjectReferences for development
- **Template Context**: Uses PackageReferences when consumed by generated solutions

### Source Generator Debugging

```bash
# Enable generator debugging
export DOTNET_EnableSourceGeneratorDebugging=1

# Launch debugger when generator runs
dotnet build --verbosity diagnostic
```

### Event Documentation XML Requirements

Events must have proper XML documentation:

```csharp
[EventTopic("main.cashiers.cashier-created")]
public record CashierCreated(Guid CashierId, string Name);
```

## Testing Integration with Template

```bash
# Test libraries in template context
cd ../../  # Go to main repository
mkdir -p _temp && cd _temp

# Generate solution with library references (ALWAYS redirect output!)
dotnet new mmt -n LibTest --libs defaults,api,ext --lib-name TestPlatform > /dev/null 2>&1

cd LibTest
dotnet build  # Should use packaged versions of libraries
```

## CRITICAL DOCUMENTATION BUILD ISSUE

**TASK: Fix ALL VitePress build failures - every single file must render perfectly**

The documentation build is failing with Vue parsing errors across multiple files. The CSharpGenericsPlugin must be enhanced to handle ALL Vue parsing issues, not just move errors between files.

**Current Status:**
- Plugin successfully processes 40+ files with C# generics
- rdbms-migrations.md was fixed but error moved to adding-domains/index.md
- Multiple files still have "Invalid end tag" Vue parsing errors
- GOAL: Complete build success with zero errors

**Required Solution:**
- The plugin must identify and fix ALL files with Vue parsing issues
- Every markdown file with XML/HTML content must render without errors
- The build command `pnpm docs:build` must succeed completely
- No partial solutions - ALL files must work

**Plugin Enhancement Needed:**
The CSharpGenericsPlugin in `.vitepress/plugins/csharpGenerics.ts` needs to:
1. Detect ALL files with Vue parsing issues (not just specific ones)
2. Apply comprehensive fixes to every problematic file
3. Ensure zero build failures
4. Maintain proper documentation rendering quality

This is a blocking issue for documentation deployment and must be resolved completely.