# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Essential Commands

### Building and Running

-   **Start the complete application stack**: `dotnet run --project src/AppDomain.AppHost`
-   **Build all projects**: `dotnet build`
-   **Run specific services with Docker**: `docker compose --profile api up` or `docker compose --profile backoffice up`
-   **Run database migrations**: `docker compose up AppDomain-db-migrations`

### Testing

-   **Run all tests**: `dotnet test`
-   **Run specific test project**: `dotnet test tests/AppDomain.Tests`
-   **Run tests with coverage**: `dotnet test --collect:"XPlat Code Coverage"`

### Documentation

-   **Start documentation server**: `cd docs && pnpm dev`
-   **Build documentation**: `cd docs && pnpm docs:build`
-   **Generate event documentation**: `cd docs && pnpm docs:events`

### Database Management

-   **Reset database**: `docker compose down -v && docker compose up AppDomain-db AppDomain-db-migrations`
-   **Access database**: Connect to `localhost:54320` with credentials `postgres/password@`

## Architecture Overview

This is a .NET 9 microservices solution that follows Domain-Driven Design principles with event-driven architecture. The codebase deliberately mirrors real-world AppDomain department operations to maintain intuitive understanding.

### Core Design Philosophy

-   **Real-world mirroring**: Each code component corresponds directly to real AppDomain department roles/operations
-   **No smart domain objects**: Entities are data records, not self-modifying objects
-   **Front office vs Back office**: Synchronous APIs (front office) vs asynchronous event processing (back office)
-   **Minimal abstractions**: Infrastructure elements support functionality like utilities in an office

### Service Structure

```
src/
├── AppDomain/                    # Core domain logic (Commands, Queries, Events)
├── AppDomain.Api/               # REST/gRPC endpoints (front office)
├── AppDomain.AppHost/           # .NET Aspire orchestration
├── AppDomain.BackOffice/        # Background event processing
├── AppDomain.BackOffice.Orleans/ # Stateful processing with Orleans
└── AppDomain.Contracts/         # Integration events and shared models
```

### Key Technologies

-   **.NET Aspire**: Application orchestration and service discovery
-   **Orleans**: Stateful actor-based processing for invoices
-   **Wolverine**: CQRS/MediatR-style command handling with Kafka integration
-   **PostgreSQL**: Primary database with Liquibase migrations
-   **Apache Kafka**: Event streaming and message bus
-   **gRPC + REST**: API protocols
-   **Testcontainers**: Integration testing with real infrastructure

### Domain Structure

Each domain area (Cashiers, Invoices) follows consistent patterns:

-   `Commands/` - Write operations (CreateCashier, UpdateCashier, etc.)
-   `Queries/` - Read operations (GetCashier, GetCashiers, etc.)
-   `Contracts/IntegrationEvents/` - Cross-service events
-   `Contracts/Models/` - Shared data contracts
-   `Data/` - Database entities and mapping

### Event-Driven Integration

-   **Integration Events**: Cross-service communication (CashierCreated, InvoicePaid, etc.)
-   **Domain Events**: Internal domain notifications (InvoiceGenerated)
-   **Event Documentation**: Auto-generated from XML comments using Momentum.Extensions.EventMarkdownGenerator

### Custom Source Generators

The solution includes custom source generators in `libs/Momentum/`:

-   **DbCommand Generator**: Generates type-safe database command handlers
-   **Event Documentation Generator**: Creates markdown documentation from integration events

### Testing Strategy

-   **Unit Tests**: Domain logic in `tests/AppDomain.Tests/Unit/`
-   **Integration Tests**: Full stack testing with Testcontainers in `tests/AppDomain.Tests/Integration/`
-   **Architecture Tests**: Enforce architectural constraints using NetArchTest

### Operations Libraries

Shared platform libraries in `libs/Momentum/` provide:

-   **ServiceDefaults**: Common hosting, logging, OpenTelemetry, health checks
-   **Extensions**: CQRS abstractions, database extensions, result types
-   **Source Generators**: Code generation for DbCommands and event documentation

### Development Workflow

1. Use .NET Aspire AppHost for local development (starts all services)
2. Database changes go through Liquibase migrations in `infra/AppDomain.Database/`
3. Integration events require XML documentation for auto-generated docs
4. Follow CQRS patterns - separate command/query handlers
5. Architecture tests enforce design constraints automatically

### Documentation System

-   **VitePress**: Documentation framework with TypeScript automation
-   **Auto-generated**: Event schemas and API docs generated from code
-   **ADR Tracking**: Architecture Decision Records in `docs/arch/adr/`

IMPORTANT: use `\_temp` folder to store the template tests instead of using /tmp, create a ./\_temp/ directory and use that

## Template Testing Guidelines

CRITICAL: dotnet new template causes buffer overflow errors in Claude Code. ALWAYS use output redirection:

**MANDATORY**: Always use output redirection to prevent buffer overflow crashes:

```bash
dotnet new mmt [options] > /dev/null 2>&1
```

**NEVER** run `dotnet new mmt` without output redirection - it will crash Claude Code with RangeError: Invalid string length.

Additional guidelines:

-   Clean the \_temp directory before each test: `rm -rf _temp/TestProject && mkdir -p _temp`
-   Test each configuration separately to isolate issues
-   Verify the build after template generation: `dotnet build --verbosity quiet`
-   Check if expected projects were created with `ls -la` after generation

- Use -allow-scripts yes to run the template without needing a post action confirmation

## Library Conditional Dependencies Pattern

When updating library project files in `libs/Momentum/src/`, follow this pattern to ensure they work both standalone and in template context:

### Key Principles:
1. **ItemGroup separation by type** - PackageReferences and ProjectReferences are in separate ItemGroups
2. **Standalone compatibility** - Libraries work both standalone (with ProjectReferences) and in template context
3. **MSBuild condition for standalone** - Use `Condition="'$(libs)' == 'libname'"` on ProjectReference

### Pattern Example:
```xml
<ItemGroup>
    <PackageReference Include="ExternalPackage"/>
    <!--#if (libs == libname) -->
    <PackageReference Include="Momentum.Dependency"/>
    <!--#endif -->
</ItemGroup>

<!--#if (libs != libname) -->
<ItemGroup>
    <ProjectReference Include="..\Momentum.Dependency\Momentum.Dependency.csproj" Condition="'$(libs)' == 'libname'"/>
</ItemGroup>
<!--#endif -->
```

This ensures:
- When template processes with `libs != libname`: The ProjectReference ItemGroup is removed entirely
- When template processes with `libs == libname`: The ItemGroup stays but ProjectReference has a false condition
- When building standalone (no template): The ItemGroup stays and condition is undefined/empty, so ProjectReference is included

### Special Cases:
- **Extensions.Abstractions and Extensions.XmlDocs**: Always NuGet packages, never project references when imported
- **libs/Momentum folder must work standalone**: Always include ProjectReferences with MSBuild conditions for standalone builds