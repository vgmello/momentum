# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**Momentum .NET** is a comprehensive template system that generates production-ready microservices solutions using .NET 10. The repository contains both the template engine (`mmt`) and supporting library ecosystem.

**Key Components**:
- **Template System** (`dotnet new mmt`): Generates complete microservices solutions
- **Momentum Libraries**: Reusable .NET libraries for service defaults, extensions, and patterns
- **Sample Application** (AppDomain): Complete microservices example demonstrating patterns

## Development Commands

### Template Development and Testing

```bash
# Install template for testing
# IMPORTANT: Only install the template from the main repo folder do not copy the template folder to another location
dotnet new install ./ --force

# Generate test solution (standard)
dotnet new mmt -n TestService --allow-scripts yes

# Generate test solution with local NuGet packages for development
dotnet new mmt -n TestService --allow-scripts yes --local

# Run comprehensive template tests (automatically uses --local flag)
./scripts/Run-TemplateTests.ps1

# Run specific test category
./scripts/Run-TemplateTests.ps1 -Category component-isolation

# Test with early exit after failures
./scripts/Run-TemplateTests.ps1 -MaxFailures 3
```

### Local NuGet Package Development

```bash
# Build and pack Momentum libraries to local NuGet feed
cd libs/Momentum
dotnet build Momentum.slnx

# Test template with fresh local packages
cd ../../
dotnet new install ./ --force
dotnet new mmt -n TestLocal --allow-scripts yes --local --project-only
```

### Building and Testing

```bash
# Build entire solution
dotnet build

# Build sample application
dotnet build AppDomain.slnx

# Run all tests
dotnet test

# Run integration tests with real infrastructure
dotnet test tests/AppDomain.Tests/Integration/

# Run end-to-end tests
dotnet test tests/AppDomain.Tests.E2E/

# Run architecture compliance tests
dotnet test tests/AppDomain.Tests/Architecture/
```

### Development Environment Setup

```bash
# Start infrastructure services
docker-compose --profile db --profile messaging up -d

# Run database migrations
docker-compose --profile db up app-domain-db-migrations

# Start sample application with Aspire
dotnet run --project src/AppDomain.AppHost
# Aspire Dashboard: https://localhost:18110
# API: https://localhost:8101 (REST), :8102 (gRPC)

# Start documentation site
cd docs && bun install && bun run dev
# Documentation: http://localhost:8119
```

**Aspire Database Setup**:
- PostgreSQL with persistent storage
- Automatic Liquibase migrations on startup
- PgAdmin for database management
- Health checks validate schema and constraints
- Services wait for database and migrations before starting

**Database Connection**:
- Services automatically get connection strings from Aspire
- Health checks validate connectivity and schema integrity
- LinqToDB configured with snake_case naming conventions
- Dapper available for custom queries and health checks

### Library Development

```bash
# Build specific library
dotnet build libs/Momentum/src/Momentum.Extensions

# Run library tests
dotnet test libs/Momentum/tests/Momentum.Extensions.Tests

# Pack libraries for distribution
dotnet pack libs/Momentum/src/Momentum.Extensions --configuration Release

# Build library documentation
cd libs/Momentum/docs && bun install && bun run dev
```

### Source Generator Development

```bash
# Build with generator debugging
dotnet build libs/Momentum/src/Momentum.Extensions.SourceGenerators -p:MomentumGeneratorVerbose=true

# View generated source files
dotnet build -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=Generated

# Test source generators
dotnet test libs/Momentum/tests/Momentum.Extensions.SourceGenerators.Tests
```

## Architecture Overview

### Template System Architecture

The template (`mmt`) generates microservices solutions with:

**Generated Project Structure**:
```
YourService/
├── src/
│   ├── YourService.Api/              # REST & gRPC endpoints
│   ├── YourService.BackOffice/       # Background event processing
│   ├── YourService.BackOffice.Orleans/ # Stateful processing (optional)
│   ├── YourService.AppHost/          # Aspire orchestration
│   ├── YourService/                  # Core domain logic
│   └── YourService.Contracts/        # Integration events
├── infra/
│   └── YourService.Database/         # Liquibase migrations
├── tests/
│   └── YourService.Tests/            # Comprehensive testing
├── docs/                             # VitePress documentation
└── compose.yml                       # Docker services
```

**Template Configuration**:
- Components: `--api`, `--backoffice`, `--orleans`, `--aspire`, `--docs`
- Infrastructure: `--db [none|npgsql|liquibase]`, `--kafka`
- Customization: `--org "Company"`, `--port 8100`, `--no-sample`
- Libraries: `--libs [defaults|api|ext|kafka|generators]`
- Development: `--local` (use local NuGet packages for testing)

### Domain-Driven Design Patterns

Generated services follow CQRS with Wolverine:

```
YourService/
├── Customers/                    # Business domain
│   ├── Commands/                 # Actions (CreateCustomer, etc.)
│   ├── Queries/                  # Data retrieval (GetCustomer, etc.)
│   ├── Data/                     # Database operations
│   └── Contracts/                # Integration events
└── Orders/                       # Another domain
    ├── Commands/
    ├── Queries/
    ├── Data/
    └── Contracts/
```

### Technology Stack Integration

**Core Technologies**:
- **.NET 10** with **Aspire**: Orchestration and observability
- **Wolverine**: CQRS message handling with PostgreSQL persistence
- **Orleans**: Stateful actor processing (optional)
- **PostgreSQL** + **Liquibase**: Database with version-controlled migrations
- **Apache Kafka**: Event streaming with CloudEvents standard
- **OpenTelemetry**: Distributed tracing and metrics

**Port Allocation** (base port configurable, default 8100):
- Aspire Dashboard: 18110 (HTTPS), 18100 (HTTP)
- API: 8111 (HTTPS), 8101 (HTTP), 8102 (gRPC)
- BackOffice: 8113 (HTTPS), 8103 (HTTP)
- Orleans: 8114 (HTTPS), 8104 (HTTP)
- Documentation: 8119 (HTTP)
- PostgreSQL: 54320
- Kafka: 59092

### Library System

**Momentum Libraries** provide standalone capabilities:

- **Momentum.Extensions**: Result types, validation, data access abstractions
- **Momentum.ServiceDefaults**: Aspire integration, observability, service configuration
- **Momentum.ServiceDefaults.Api**: OpenAPI, gRPC, route conventions
- **Momentum.Extensions.SourceGenerators**: DbCommand code generation
- **Momentum.Extensions.Messaging.Kafka**: CloudEvents and Kafka integration

### Local Development with `--local` Flag

The `--local` flag enables template testing with locally built Momentum libraries:

**What it does**:
- Copies `local-mmt-version.txt` and `local-feed-path.txt` to the generated project
- The post-setup tool reads these files and:
  - Hardcodes the local version into `Directory.Packages.props` (`MomentumVersion`)
  - Creates a `nuget.config` with the local NuGet feed source
  - Cleans up the text files
- Automatically used by `Run-TemplateTests.ps1` for template testing

**How the lib build generates local packages**:
- `libs/Momentum/Directory.Build.targets` generates version `1000.0.0-pre.{timestamp}`
- Packs all libraries to `libs/Momentum/.local/nuget/`
- Writes `local-mmt-version.txt` and `local-feed-path.txt` to the repo root

**Usage**:
```bash
# Build and pack libraries first
dotnet build libs/Momentum/Momentum.slnx

# Install template and generate with local packages
dotnet new install ./ --force
dotnet new mmt -n TestLocal --allow-scripts yes --local --project-only
```

## Testing Strategy

### Architecture Testing

The solution includes comprehensive architecture tests:

```bash
# Test CQRS pattern compliance
dotnet test --filter "CqrsPatternRulesTests"

# Test domain isolation
dotnet test --filter "DomainIsolationRulesTests"

# Test dependency direction
dotnet test --filter "DependencyDirectionRulesTests"

# Test Orleans grain ownership
dotnet test --filter "OrleansGrainOwnershipRulesTests"
```

### Integration Testing with Real Infrastructure

Integration tests use Testcontainers for real database and messaging:

```bash
# Run integration tests (starts PostgreSQL container)
dotnet test tests/AppDomain.Tests/Integration/

# Run E2E tests (full stack testing)
dotnet test tests/AppDomain.Tests.E2E/
```

### Template Validation

Comprehensive template testing across scenarios:

```bash
# Test all template configurations
./scripts/Run-TemplateTests.ps1

# Test specific scenarios
./scripts/Run-TemplateTests.ps1 -Category real-world-patterns

# Available categories: component-isolation, database-config, port-config,
# org-names, library-config, real-world-patterns, orleans-combinations, edge-cases
```

## Common Development Patterns

### Error Handling with Result Types

```csharp
// Command handlers return Result<T>
public async Task<Result<Customer>> Handle(CreateCustomerCommand command)
{
    var validationResult = await validator.ValidateAsync(command);
    if (!validationResult.IsValid)
        return Result<Customer>.Failure(validationResult.Errors);

    // Business logic...
    return Result<Customer>.Success(customer);
}
```

### Database Operations with Source Generation

```csharp
// Mark for source generation
[DbCommand]
public record GetCustomersQuery(int Page, int Size) : IQuery<IEnumerable<Customer>>;

// Generated handler automatically available via DI
```

### Event Publishing

```csharp
[EventTopic("your-service.customers.customer-created")]
public record CustomerCreated([PartitionKey] Guid CustomerId, string Name);

// Published via Wolverine message bus
await messageBus.PublishAsync(new CustomerCreated(customer.Id, customer.Name));
```

## Build Configuration

### MSBuild Properties

Global configuration in `Directory.Build.props`:
- **Target Framework**: .NET 10.0
- **Nullable Reference Types**: Enabled
- **Analyzers**: .NET analyzers + SonarAnalyzer enabled
- **Code Style**: Enforced in build

### Template Conditional Compilation

Template uses sophisticated conditional compilation:

```xml
<!-- Components controlled by computed symbols -->
<ItemGroup Condition="'$(INCLUDE_API)' == 'true'">
  <ProjectReference Include="src/YourService.Api/YourService.Api.csproj" />
</ItemGroup>
```

## Special Considerations

### Template vs Library Development

The repository serves dual purposes:
- **Template development**: Testing and building the `mmt` template
- **Library development**: Standalone Momentum libraries in `libs/Momentum/`

### Database Migrations

Uses Liquibase for version-controlled schema management:
- Setup scripts in `infra/YourService.Database/Liquibase/`
- Automatic migration via Docker Compose
- Separate service_bus and application schemas

**IMPORTANT - Database Organization Rules**:
- **Table Definitions**: Each table should be in its own file under `tables/` directory
- **Constraints & Indexes**: Should be defined WITHIN the same file as the table they apply to
- **Avoid Separate Constraint Files**: Do NOT create separate `constraints_and_indexes.sql` files
- **Logical Grouping**: Keep related constraints, indexes, triggers, and functions with their tables

**Correct Structure**:
```
infra/YourService.Database/Liquibase/main/
├── main.sql                    # Schema creation
├── invoices/
│   ├── tables/
│   │   └── invoices.sql             # Table + constraints + indexes + triggers
│   └── procedures/
│       └── invoice_operations.sql
└── customers/
    ├── tables/
        └── customers.sql            # Table + constraints + indexes + triggers
    └── procedures/
        └── customer_operations.sql
```

**Example Table File Structure**:
```sql
-- Table creation
CREATE TABLE main.invoices (...);

-- Additional columns (if needed)
ALTER TABLE main.invoices ADD COLUMN ...;

-- Constraints
ALTER TABLE main.invoices ADD CONSTRAINT chk_amount_positive CHECK (amount > 0);

-- Foreign keys
ALTER TABLE main.invoices ADD CONSTRAINT fk_invoice_customer ...;

-- Indexes
CREATE INDEX idx_invoices_status ON main.invoices(tenant_id, status);

-- Triggers and functions
CREATE OR REPLACE FUNCTION update_invoice_version() ...;
CREATE TRIGGER tr_invoice_version BEFORE UPDATE ON main.invoices ...;
```

### Troubleshooting Database Issues

**Common Problems and Solutions**:

1. **Health Checks Failing**:
   ```bash
   # Check if database is running
   curl http://localhost:8101/status

   # Check Aspire dashboard for service status
   open https://localhost:18110
   ```

2. **Migration Issues**:
   ```bash
   # Check Liquibase logs in Aspire dashboard
   # Verify changelog.xml includes all table files
   # Ensure changesets have unique IDs
   ```

3. **Missing Constraints/Indexes**:
   ```sql
   -- Verify constraints exist
   SELECT constraint_name, constraint_type
   FROM information_schema.table_constraints
   WHERE table_schema = 'main';

   -- Verify indexes exist
   SELECT schemaname, tablename, indexname
   FROM pg_indexes
   WHERE schemaname = 'main';
   ```

4. **Connection String Issues**:
   - Aspire automatically configures connection strings
   - Check appsettings.json for overrides
   - Verify service references in AppHost Program.cs

### Observability Integration

Complete observability stack configured by default:
- **Structured Logging**: Serilog with enrichment
- **Metrics**: OpenTelemetry with custom meters
- **Tracing**: Distributed tracing across services
- **Health Checks**: Built-in health endpoints

**Health Check Endpoints**:
- `/status` - Liveness probe (cached status, no auth)
- `/health/internal` - Readiness probe (localhost only, detailed in dev)
- `/health` - Public health check (requires auth, detailed)

**Database Health Checks**:
- Connectivity validation
- Schema verification (tables, constraints, indexes)
- Basic data access testing
- Constraint and foreign key validation

**Aspire Configuration**:
- Database resources with health checks
- Liquibase migration dependencies
- Service orchestration with proper wait conditions
- Automatic service discovery and load balancing

### Development vs Production Configuration

Template generates environment-specific configurations:
- **Development**: Uses Aspire dashboard, verbose logging
- **Production**: Optimized for container deployment
- **Docker Compose**: Full-stack local development environment
- ALWAYS TEST ANY CHANGES YOU MADE
