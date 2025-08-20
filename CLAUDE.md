# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**Momentum is a sophisticated .NET template (dotnet new template) that generates customized microservices solutions.** It mirrors real-world business operations and combines modern technologies like Orleans, gRPC, Kafka, and PostgreSQL in a Domain-Driven Design architecture.

**Template Identity**: `mmt` (shortName for `dotnet new mmt`)

This repository serves as the template source code with extensive conditional compilation and parameter-based customization.

## Key Architecture Principles

-   **Real-World Mirroring**: Code structure directly corresponds to business operations
-   **No Smart Objects**: Entities are data records, not self-modifying objects
-   **Front Office vs Back Office**: Synchronous APIs vs Asynchronous processing
-   **Event-Driven**: Integration events via Kafka for service communication

## Development Commands

### Building and Testing

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Build specific project
dotnet build src/AppDomain.Api
```

### Template Development
```bash
# Install template locally for testing
dotnet new install .

# Test template generation (ALWAYS redirect output!)
dotnet new mmt -n TestProject --allow-scripts yes > /dev/null 2>&1

# Uninstall template
dotnet new uninstall Momentum.Template
```

### Running the Application

**Recommended: .NET Aspire (Orchestrated)**

```bash
# Start complete application stack with orchestration
dotnet run --project src/AppDomain.AppHost
# Access Aspire Dashboard: https://localhost:18110
```

**Alternative: Docker Compose**

```bash
# Run all services
docker compose up

# Run specific profiles
docker compose --profile api up
docker compose --profile backoffice up
```

### Database Operations

```bash
# Run database migrations
docker compose up AppDomain-db-migrations

# Reset database (destroys all data)
docker compose down -v
```

### Documentation

```bash
# Start documentation server
cd docs && pnpm dev

# Generate event documentation
cd docs && pnpm docs:events

# Build documentation
cd docs && pnpm docs:build
```

## Project Structure & Architecture

### Core Services

-   **AppDomain.Api**: REST/gRPC endpoints (Front Office - synchronous operations)
-   **AppDomain.BackOffice**: Event processing service (Back Office - asynchronous)
-   **AppDomain.BackOffice.Orleans**: Stateful processing with Microsoft Orleans
-   **AppDomain.AppHost**: .NET Aspire orchestration for local development
-   **AppDomain**: Core domain logic with Commands, Queries, and Events
-   **AppDomain.Contracts**: Integration events and shared models

### Infrastructure

-   **infra/AppDomain.Database**: Liquibase database migrations
-   **docs/**: VitePress documentation with auto-generated event docs
-   **libs/Momentum**: Shared platform libraries with extensions and service defaults

### Testing Strategy

-   **tests/AppDomain.Tests**: Comprehensive testing including:
    -   Unit tests for business logic
    -   Integration tests with Testcontainers
    -   Architecture tests to enforce design rules

## Technology Stack

-   **.NET 9**: Primary framework
-   **Microsoft Orleans**: Stateful actor-based processing
-   **Wolverine**: CQRS/MediatR pattern with message handling
-   **PostgreSQL + Liquibase**: Database with version-controlled migrations
-   **Apache Kafka**: Event streaming and message bus
-   **gRPC + REST**: Dual API protocols
-   **Testcontainers**: Real infrastructure for testing
-   **OpenTelemetry**: Observability and distributed tracing

## Domain Pattern (CQRS)

### Commands (Actions)

Located in `src/AppDomain/[Domain]/Commands/`

-   Represent business actions (CreateCashier, CancelInvoice)
-   Handled by Wolverine message handlers
-   Can publish integration events

### Queries (Information Retrieval)

Located in `src/AppDomain/[Domain]/Queries/`

-   Retrieve business information (GetCashier, GetInvoices)
-   Read-only operations
-   Optimized for specific UI needs

### Events

-   **Domain Events**: `src/AppDomain/[Domain]/Contracts/DomainEvents/` (internal)
-   **Integration Events**: `src/AppDomain.Contracts/IntegrationEvents/` (cross-service)
-   Follow Kafka topic naming: `app_domain.[domain].[event-type]`

### Database Access

-   Uses Dapper with custom `DbCommand` source generators
-   Database procedures located in `infra/AppDomain.Database/Liquibase/app_domain/[domain]/procedures/`
-   Entity mapping in `src/AppDomain/[Domain]/Data/DbMapper.cs`

## Port Configuration (Default: 8100 base)

| Service                      | HTTP  | HTTPS | gRPC | Description           |
| ---------------------------- | ----- | ----- | ---- | --------------------- |
| Aspire Dashboard             | 18100 | 18110 | -    | Development dashboard |
| AppDomain.Api                | 8101  | 8111  | 8102 | REST & gRPC endpoints |
| AppDomain.BackOffice         | 8103  | 8113  | -    | Background processing |
| AppDomain.BackOffice.Orleans | 8104  | 8114  | -    | Orleans silo          |
| Documentation                | 8119  | -     | -    | VitePress docs        |
| PostgreSQL                   | 54320 | -     | -    | Database              |
| Kafka                        | 59092 | -     | -    | Message broker        |

## Code Generation

### Source Generators

-   **DbCommand**: Generates database command handlers and parameter providers
-   **Event Documentation**: Auto-generates markdown from XML documentation
-   **Protobuf**: gRPC service and model generation from .proto files

### Custom Momentum Extensions

-   **Messaging**: Kafka integration with CloudEvents
-   **Database**: Enhanced Dapper with source generation
-   **Service Defaults**: Common configuration patterns
-   **Validation**: FluentValidation integration with Wolverine

## Common Development Tasks

### Adding New Business Domain

1. Create folder structure in `src/AppDomain/[NewDomain]/`
2. Add Commands, Queries, and Data folders
3. Create integration events in `src/AppDomain.Contracts/IntegrationEvents/`
4. Add API endpoints in `src/AppDomain.Api/[NewDomain]/`
5. Create database migrations in `infra/AppDomain.Database/`

### Working with Orleans Grains

-   Grain interfaces: `src/AppDomain.BackOffice.Orleans/[Domain]/Grains/I[Name]Grain.cs`
-   Grain implementations: `src/AppDomain.BackOffice.Orleans/[Domain]/Grains/[Name]Grain.cs`
-   State classes: `src/AppDomain.BackOffice.Orleans/[Domain]/Grains/[Name]State.cs`

### Integration Testing

-   Use `IntegrationTestFixture` base class in `tests/AppDomain.Tests/Integration/`
-   Testcontainers automatically provision PostgreSQL and Kafka
-   Test categories: Unit, Integration, Architecture

## Configuration Management

### Environment Variables

-   Standard .NET configuration hierarchy
-   Connection strings in `ConnectionStrings__AppDomainDb` format
-   Kafka configuration via `Kafka__BootstrapServers`
-   Orleans clustering via `Orleans__*` settings

### Development Overrides

-   `appsettings.Development.json` files in each service
-   Aspire manages service-to-service configuration
-   Docker Compose provides infrastructure defaults

## Template System (.NET Template)

### Template Installation & Usage

```bash
# Install the template from source (when in template directory)
dotnet new install .

# Create a new solution with default configuration
dotnet new mmt -n MyBusinessApp

# Uninstall template
dotnet new uninstall Momentum.Template
```

### Template Parameters

**Core Components** (defaults to all enabled):
- `--aspire`: Include .NET Aspire orchestration project (default: true)
- `--web-api`: Include REST/gRPC API project (default: true)
- `--back-office`: Include background processing project (default: true)
- `--orleans`: Include Orleans stateful processing project (default: false)
- `--docs`: Include VitePress documentation project (default: true)

**Configuration Options**:
- `--db-config`: Database setup (`default`, `npgsql`, `liquibase`, `none`)
- `--kafka`: Include Apache Kafka messaging (default: true)
- `--port`: Base port number for services (default: 8100)
- `--org`: Organization/team name for copyright headers

**Content Options**:
- `--no-sample`: Skip generating sample Cashiers/Invoices code
- `--project-only`: Generate only projects without solution files

**Library Options**:
- `--libs`: How to include Momentum libraries (`none`, `defaults`, `api`, `ext`, `kafka`, `generators`)
- `--lib-name`: Custom prefix to replace "Momentum" in library names

### Template Generation Examples

```bash
# Minimal API-only setup
dotnet new mmt -n OrderService --web-api --no-back-office --no-orleans --no-docs --no-sample

# Orleans-heavy processing service  
dotnet new mmt -n ProcessingEngine --orleans --no-web-api --port 9000

# Full stack with custom organization
dotnet new mmt -n EcommercePlatform --org "Acme Corp" --port 7000

# Include Momentum libs as project references
dotnet new mmt -n DevApp --libs defaults,api,ext --lib-name AcmePlatform
```

### Template Conditional Compilation

The template uses conditional compilation symbols throughout the codebase:

**Primary Symbols**:
- `INCLUDE_API`, `INCLUDE_BACK_OFFICE`, `INCLUDE_ORLEANS`
- `INCLUDE_ASPIRE`, `INCLUDE_DOCS`, `INCLUDE_SAMPLE`
- `USE_PGSQL`, `USE_LIQUIBASE`, `USE_KAFKA`

**Conditional Formats**:
- C# files: `#if INCLUDE_API` / `#endif`
- Project files: `<!--#if (INCLUDE_API)-->` / `<!--#endif-->`
- YAML files: `# #if (INCLUDE_API)` / `# #endif`

### Post-Setup Actions

After template generation, automated post-setup tasks run:

1. **Port Configuration**: Updates all port references with specified base port
2. **Library Rename**: Replaces "Momentum" prefix in library names if `--lib-name` specified
3. **Solution Updates**: Adds generated projects to solution file
4. **Cleanup**: Removes temporary post-setup tools

### Template File Structure Patterns

**Conditional Project References**:
```xml
<!--#if (INCLUDE_API)-->
<ProjectReference Include="..\AppDomain.Api\AppDomain.Api.csproj" />
<!--#endif-->
```

**Conditional Source Code**:
```csharp
#if USE_KAFKA
builder.Services.AddKafkaMessaging(builder.Configuration);
#endif
```

**Parameter Replacements**:
- `AppDomain` → Project name (sourceName)
- `ORG_NAME` → Organization parameter
- `SERVICE_BASE_PORT` → Port parameter
- `app_domain` → Snake case project name

### Template Testing Guidelines

**CRITICAL**: dotnet new template causes buffer overflow errors in Claude Code. ALWAYS use output redirection.

**Template Testing Workflow**:

```bash
# Setup test environment
mkdir -p _temp && cd _temp

# MANDATORY: Always redirect output to prevent crashes
dotnet new mmt -n TestProject [options] --allow-scripts yes > /dev/null 2>&1

# Verify generation and build
cd TestProject
ls -la  # Check expected projects were created
dotnet build --verbosity quiet  # Verify solution builds

# Test specific functionality
dotnet run --project src/TestProject.AppHost  # If Aspire included

# Cleanup
cd ../.. && rm -rf _temp/TestProject
```

**Testing Different Configurations**:

```bash
# Test minimal setup
dotnet new mmt -n MinimalTest --project-only --no-sample --no-docs > /dev/null 2>&1

# Test Orleans-only
dotnet new mmt -n OrleansTest --orleans --no-web-api --no-back-office > /dev/null 2>&1

# Test custom port/org
dotnet new mmt -n CustomTest --port 9000 --org "Test Corp" > /dev/null 2>&1

# Test library renaming
dotnet new mmt -n LibTest --libs defaults --lib-name TestPlatform > /dev/null 2>&1
```

**Post-Generation Verification**:

```bash
# Check conditional compilation worked
grep -r "INCLUDE_API" src/  # Should be replaced or removed
grep -r "SERVICE_BASE_PORT" .  # Should be replaced with actual port

# Verify post-setup actions ran
ls .local/tools/  # Should not exist (cleaned up)

# Test database migrations (if included)
docker compose up AppDomain-db-migrations > /dev/null 2>&1
```

**NEVER** run `dotnet new mmt` without output redirection - it will crash Claude Code with RangeError: Invalid string length.

## Library Conditional Dependencies Pattern

When updating library project files in `libs/Momentum/src/`, follow this pattern to ensure they work both standalone and in template context:

### Key Principles:

1. **ItemGroup separation by type** - PackageReferences and ProjectReferences are in separate ItemGroups
2. **Standalone compatibility** - Libraries work both standalone (with ProjectReferences) and in template context

### Special Cases:

-   **Extensions.Abstractions and Extensions.XmlDocs**: Always NuGet packages, never project references when imported
-   **libs/Momentum folder must work standalone**: Always include ProjectReferences with MSBuild conditions for standalone builds

- ALWAYS test the documentation with 'pnpm docs:build' before you can assert the any documentation changes are working.