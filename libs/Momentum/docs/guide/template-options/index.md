---
title: Template Options
description: Complete guide to configuration options and customization capabilities for the Momentum .NET template, enabling you to generate solutions precisely tailored to your architectural and business requirements.
date: 2024-01-15
---

# Template Options

Complete guide to configuration options and customization capabilities for the Momentum .NET template, enabling you to generate solutions precisely tailored to your architectural and business requirements.

## Overview

The Momentum template (`dotnet new mmt`) provides extensive customization through conditional compilation and parameter-driven generation. Understanding these options helps you create solutions that match your exact requirements:

- **Component Selection**: Choose which projects and features to include
- **Infrastructure Configuration**: Database, messaging, and observability setup
- **Library Management**: Control dependency inclusion and customization
- **Content Options**: Sample code, solution structure, and documentation
- **Post-Generation Actions**: Automated configuration and setup

## Template Parameter Reference

### Project Component Parameters

Control which projects are generated in your solution:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--api` | bool | true | Generate REST/gRPC API project for synchronous operations |
| `--back-office` | bool | true | Generate background processing project for asynchronous operations |
| `--orleans` | bool | false | Generate Orleans-based stateful processing project |
| `--aspire` | bool | true | Generate .NET Aspire orchestration project for local development |
| `--docs` | bool | true | Generate VitePress documentation project |

[!TIP]
When no specific components are specified (all defaults), the template generates a complete solution with all components included.

### Infrastructure Configuration Parameters

Configure your application's infrastructure dependencies:

| Parameter | Type | Default | Options | Description |
|-----------|------|---------|---------|-------------|
| `--db-config` | choice | default | `default`, `npgsql`, `liquibase`, `none` | Database setup configuration |
| `--kafka` | bool | true | - | Include Apache Kafka messaging infrastructure |
| `--port` | int | 8100 | - | Base port number for all services |

**Database Configuration Options**:
- **`default`**: PostgreSQL with Liquibase migrations (recommended for most scenarios)
- **`npgsql`**: PostgreSQL database provider only (for custom migration strategies)
- **`liquibase`**: Database-agnostic migrations only (for existing databases)
- **`none`**: No database configuration (for pure messaging or stateless services)

### Content and Structure Parameters

Control the generated content and solution structure:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--no-sample` | bool | false | Skip generating sample Cashiers/Invoices domain code |
| `--project-only` | bool | false | Generate only projects without solution files, .gitignore, etc. |
| `--org` | string | "[ProjectName] Team" | Organization name for copyright headers and documentation |

### Library Integration Parameters

Control how Momentum libraries are included in your solution:

| Parameter | Type | Default | Options | Description |
|-----------|------|---------|---------|-------------|
| `--libs` | choice | none | `none`, `defaults`, `api`, `ext`, `kafka`, `generators` | Which libraries to include as project references |
| `--lib-name` | string | "Momentum" | - | Custom prefix to replace "Momentum" in library names |

**Library Inclusion Options**:
- **`none`**: Use NuGet packages for all Momentum libraries (recommended for production)
- **`defaults`**: Include Momentum.ServiceDefaults as project reference
- **`api`**: Include API-related libraries (ServiceDefaults.Api, XmlDocs)
- **`ext`**: Include Momentum.Extensions for core functionality
- **`kafka`**: Include Kafka messaging extensions
- **`generators`**: Include source generators for DbCommand and other features

[!NOTE]
Multiple library options can be specified: `--libs defaults,api,ext`

## Conditional Compilation System

The Momentum template uses sophisticated conditional compilation to generate precisely the code you need:

### Compilation Symbols

The template automatically computes these symbols based on your parameters:

| Symbol | Condition | Purpose |
|--------|-----------|---------|
| `INCLUDE_API` | `--api` or all defaults | Include API-related code |
| `INCLUDE_BACK_OFFICE` | `--back-office` or all defaults | Include background processing code |
| `INCLUDE_ORLEANS` | `--orleans` is specified | Include Orleans-specific code |
| `INCLUDE_ASPIRE` | `--aspire` or all defaults | Include Aspire orchestration |
| `INCLUDE_SAMPLE` | `--no-sample` is NOT specified | Include sample domain code |
| `USE_PGSQL` | Database configuration includes PostgreSQL | Include PostgreSQL setup |
| `USE_LIQUIBASE` | Database configuration includes Liquibase | Include migration setup |
| `USE_KAFKA` | `--kafka` and has backend components | Include Kafka messaging |

### Conditional Code Patterns

**C# Source Files**:
```csharp
#if USE_KAFKA
builder.Services.AddKafkaMessaging(builder.Configuration);
#endif

#if INCLUDE_ORLEANS
app.MapOrleansEndpoints();
#endif
```

**Project Files**:
```xml
<!--#if (INCLUDE_API)-->
<ProjectReference Include="..\AppDomain.Api\AppDomain.Api.csproj" />
<!--#endif-->

<!--#if (USE_KAFKA)-->
<PackageReference Include="Confluent.Kafka" Version="2.3.0" />
<!--#endif-->
```

**Configuration Files**:
```yaml
# #if (USE_KAFKA)
  kafka:
    image: confluentinc/cp-kafka:latest
# #endif
```

## Template Usage Examples and Scenarios

### Minimal API-Only Service

For lightweight, stateless services focused on REST/gRPC endpoints:

```bash
dotnet new mmt -n OrderService \
  --api \
  --no-back-office \
  --no-orleans \
  --no-docs \
  --no-sample \
  --db-config none
```

**Generated Projects**: `OrderService.Api`, `OrderService` (minimal)
**Use Case**: API gateways, proxy services, read-only endpoints

### Orleans-Heavy Processing Service

For stateful, event-driven processing without REST APIs:

```bash
dotnet new mmt -n ProcessingEngine \
  --orleans \
  --no-web-api \
  --port 9000 \
  --kafka
```

**Generated Projects**: `ProcessingEngine.BackOffice.Orleans`, `ProcessingEngine.BackOffice`, `ProcessingEngine`, `ProcessingEngine.Contracts`
**Use Case**: Event processing, workflow engines, stateful business logic

### Full-Stack Microservice

Complete microservice with all capabilities:

```bash
dotnet new mmt -n EcommercePlatform \
  --org "Acme Corp" \
  --port 7000 \
  --orleans \
  --kafka \
  --docs
```

**Generated Projects**: All projects including Orleans, API, BackOffice, documentation
**Use Case**: Complete business domains, complex workflows, full-featured services

### Development Environment Setup

For contributing to Momentum or extending the platform:

```bash
dotnet new mmt -n DevApp \
  --libs defaults,api,ext,generators \
  --lib-name AcmePlatform \
  --no-sample
```

**Features**:
- Project references to Momentum libraries for debugging
- Custom library naming for organizational branding
- Clean slate without sample code

## Decision Guide for Template Parameters

### Choose Your Architecture Pattern

**API-First Services** (Front Office focus):
```bash
# Synchronous request/response patterns
dotnet new mmt -n ServiceName --api --no-back-office --no-orleans
```

**Event-Driven Services** (Back Office focus):
```bash
# Asynchronous message processing
dotnet new mmt -n ServiceName --back-office --no-web-api --kafka
```

**Hybrid Services** (Full CQRS):
```bash
# Both synchronous and asynchronous capabilities
dotnet new mmt -n ServiceName --api --back-office --kafka
```

**Stateful Processing** (Orleans-based):
```bash
# Complex state management and workflows
dotnet new mmt -n ServiceName --orleans --kafka
```

### Database Strategy Selection

| Strategy | When to Use | Parameters |
|----------|-------------|------------|
| **Full Database** | New projects with data persistence | `--db-config default` |
| **PostgreSQL Only** | Existing database or custom migrations | `--db-config npgsql` |
| **Liquibase Only** | Database-agnostic or multiple DB types | `--db-config liquibase` |
| **No Database** | Stateless services or external data | `--db-config none` |

### Library Integration Strategy

| Strategy | When to Use | Parameters |
|----------|-------------|------------|
| **NuGet Packages** | Production deployments | `--libs none` (default) |
| **Development Setup** | Contributing to Momentum | `--libs defaults,api,ext,generators` |
| **Selective References** | Specific debugging needs | `--libs defaults,api` |
| **Custom Branding** | Organizational forks | `--lib-name YourPrefix` |

## Project Structure Variations

### Complete Solution Structure
```
MyApp/                          # --project-only false (default)
├── src/
│   ├── MyApp.Api/              # --api (default: true)
│   ├── MyApp.BackOffice/       # --back-office (default: true)
│   ├── MyApp.BackOffice.Orleans/ # --orleans (default: false)
│   ├── MyApp.AppHost/          # --aspire (default: true)
│   ├── MyApp/                  # Core domain (automatic with backend)
│   └── MyApp.Contracts/        # Integration events (automatic with backend)
├── tests/
│   └── MyApp.Tests/            # Comprehensive testing (automatic with backend)
├── infra/
│   └── MyApp.Database/         # --db-config default|liquibase
├── docs/                       # --docs (default: true)
├── libs/Momentum/              # --libs [any option except none]
├── MyApp.sln                   # Solution file
├── compose.yml                 # Docker Compose configuration
├── .gitignore                  # Git configuration
└── README.md                   # Project documentation
```

### Minimal Project Structure
```
MyApp/                          # --project-only
├── src/
│   ├── MyApp.Api/              # --api --no-back-office
│   └── MyApp/                  # Minimal core
└── tests/
    └── MyApp.Tests/            # Basic testing
```

### Orleans-Focused Structure
```
MyApp/                          # --orleans --no-web-api
├── src/
│   ├── MyApp.BackOffice.Orleans/
│   ├── MyApp.BackOffice/
│   ├── MyApp/
│   └── MyApp.Contracts/
├── tests/
│   └── MyApp.Tests/
└── infra/
    └── MyApp.Database/
```

## Library Integration Details

### Understanding Library Dependencies

The Momentum platform consists of several interconnected libraries:

```
Momentum.ServiceDefaults
├── Core service configuration
├── OpenTelemetry setup
├── Health checks
└── Logging configuration

Momentum.ServiceDefaults.Api
├── API-specific extensions
├── Swagger/OpenAPI configuration
├── gRPC setup
└── Depends on: ServiceDefaults, XmlDocs

Momentum.Extensions
├── Core utilities and extensions
├── Database command generation
├── Result patterns
└── Validation helpers

Momentum.Extensions.Messaging.Kafka
├── Kafka integration
├── CloudEvents support
├── Message serialization
└── Depends on: Extensions

Momentum.Extensions.SourceGenerators
├── DbCommand source generation
├── Event documentation generation
└── Compilation-time code generation
```

### Package References (Default)
Recommended for production deployments:
```xml
<PackageReference Include="Momentum.ServiceDefaults" Version="1.0.0" />
<PackageReference Include="Momentum.Extensions" Version="1.0.0" />
```

**Benefits**:
- Stable, versioned dependencies
- Faster build times
- Smaller repository size

### Project References (Development)
Use when contributing to Momentum or customizing libraries:
```xml
<ProjectReference Include="../libs/Momentum/src/Momentum.ServiceDefaults/Momentum.ServiceDefaults.csproj" />
<ProjectReference Include="../libs/Momentum/src/Momentum.Extensions/Momentum.Extensions.csproj" />
```

**Benefits**:
- Source-level debugging
- Immediate reflection of changes
- Ability to modify library code

### Custom Library Naming
For organizational forks or branding:
```bash
dotnet new mmt -n MyApp --lib-name "AcmePlatform"
```

**Result**:
```xml
<ProjectReference Include="../libs/AcmePlatform/src/AcmePlatform.ServiceDefaults/AcmePlatform.ServiceDefaults.csproj" />
```

## Post-Generation Actions

The template includes automated post-setup actions that run immediately after generation:

### Automatic Configuration

1. **Port Configuration Updates**
   - Replaces all instances of `SERVICE_BASE_PORT` with your specified port
   - Updates Docker Compose, Aspire configuration, and project settings
   - Ensures consistent port allocation across all services

2. **Library Renaming**
   - Replaces "Momentum" prefix with your custom `--lib-name` value
   - Updates file names, namespaces, and project references
   - Maintains library dependency relationships

3. **Solution Integration**
   - Adds generated projects to the solution file
   - Configures project dependencies and build order
   - Updates solution folders for organization

4. **Cleanup Operations**
   - Removes temporary post-setup tools
   - Cleans up template generation artifacts
   - Finalizes project structure

### Manual Configuration Required

After generation, you'll need to configure environment-specific settings:

```bash
# 1. Update connection strings for your environment
# Edit appsettings.Development.json in each service:
{
  "ConnectionStrings": {
    "AppDomainDb": "Host=localhost;Database=myapp;Username=dev;Password=dev"
  }
}

# 2. Configure external service endpoints
# Update Kafka, Orleans, and other service configurations

# 3. Set up authentication if needed
# Configure JWT, OAuth, or other auth mechanisms

# 4. Customize observability settings
# Configure OpenTelemetry exporters, logging levels, metrics
```

## Advanced Customization Patterns

### Template Parameter Combinations

**Microservice Architecture** (Multiple services):
```bash
# Gateway service
dotnet new mmt -n ApiGateway --api --no-back-office --no-sample --port 8000

# Processing service
dotnet new mmt -n OrderProcessor --back-office --orleans --no-web-api --port 8100

# Notification service
dotnet new mmt -n NotificationService --back-office --no-web-api --no-sample --port 8200
```

**Monolithic Application** (Single comprehensive service):
```bash
dotnet new mmt -n MonolithApp --api --back-office --orleans --docs --port 8000
```

**Event-Driven System** (Heavy messaging focus):
```bash
dotnet new mmt -n EventProcessor --back-office --orleans --kafka --no-web-api --no-sample
```

### Custom Template Modification

For organizations needing custom template behavior:

1. **Fork the Template Repository**
2. **Modify Conditional Compilation**:
   ```json
   "symbols": {
     "custom-feature": {
       "type": "parameter",
       "datatype": "bool",
       "description": "Include custom organizational feature"
     }
   }
   ```
3. **Add Custom Project Templates**
4. **Extend Post-Generation Actions**

## Best Practices and Guidelines

### Parameter Selection Strategy

1. **Start Minimal, Add Complexity**
   ```bash
   # Begin with minimal requirements
   dotnet new mmt -n MyService --api --no-back-office --no-sample

   # Add components as requirements evolve
   dotnet new mmt -n MyService --api --back-office --kafka
   ```

2. **Consider Deployment Target Early**
   - **Container environments**: Include Aspire for local development
   - **Cloud deployments**: Use package references for libraries
   - **On-premises**: Consider Liquibase for database-agnostic migrations

3. **Plan for Observability**
   - Always include health checks and logging
   - Consider OpenTelemetry exporters for your monitoring stack
   - Plan distributed tracing across services

### Development Workflow Best Practices

**Template Generation Workflow**:
```bash
# 1. Create template with project references for development
dotnet new mmt -n MyApp --libs defaults,api,ext --no-sample

# 2. Develop and test with immediate feedback
dotnet build
dotnet test

# 3. Switch to package references for production builds
# Update project files to use PackageReference instead of ProjectReference
```

**Consistent Naming and Ports**:
```bash
# Use consistent port ranges for related services
dotnet new mmt -n UserService --port 8100
dotnet new mmt -n OrderService --port 8200
dotnet new mmt -n PaymentService --port 8300
```

### Team Collaboration Guidelines

**Template Configuration Documentation**:
```yaml
# team-template-config.yml
project-templates:
  api-service:
    command: "dotnet new mmt -n {name} --api --no-back-office --no-sample --port {port}"
    use-case: "REST/gRPC endpoints without background processing"

  processing-service:
    command: "dotnet new mmt -n {name} --back-office --orleans --no-web-api --kafka --port {port}"
    use-case: "Event-driven background processing with state"

  full-service:
    command: "dotnet new mmt -n {name} --api --back-office --kafka --docs --port {port}"
    use-case: "Complete CQRS service with API and processing"
```

**Shared Development Environment**:
- Use `--libs defaults,api,ext` for consistent debugging experience
- Maintain shared `--org` value for copyright consistency
- Document port allocation strategy to avoid conflicts

## Common Use Cases and Solutions

### Microservice Architecture
Generate multiple specialized services:

```bash
# API Gateway (lightweight, stateless)
dotnet new mmt -n ApiGateway \
  --api --no-back-office --no-orleans \
  --no-sample --db-config none --port 8000

# User Management (CRUD operations)
dotnet new mmt -n UserService \
  --api --back-office \
  --db-config default --port 8100

# Order Processing (stateful workflows)
dotnet new mmt -n OrderService \
  --api --back-office --orleans \
  --kafka --port 8200

# Notification Service (pure messaging)
dotnet new mmt -n NotificationService \
  --back-office --no-web-api \
  --kafka --db-config none --port 8300
```

### Event-Driven System
Heavy messaging with complex state management:

```bash
dotnet new mmt -n EventProcessor \
  --back-office --orleans \
  --kafka --no-web-api \
  --no-sample --port 8100
```

### API-First Development
Focus on REST/gRPC endpoints:

```bash
dotnet new mmt -n ApiService \
  --api --no-back-office \
  --db-config npgsql --no-kafka \
  --no-sample --port 8100
```

### Legacy Integration
Integrate with existing systems:

```bash
dotnet new mmt -n LegacyBridge \
  --back-office --no-web-api \
  --db-config none --no-kafka \
  --no-sample --port 8100
```

## Troubleshooting Template Generation

### Common Issues and Solutions

**Template Installation**:
```bash
# If template not found
dotnet new install Momentum.Template

# If outdated template
dotnet new uninstall Momentum.Template
dotnet new install Momentum.Template
```

**Build Errors After Generation**:
```bash
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build

# Check for missing dependencies
dotnet list package --outdated
```

**Port Conflicts**:
```bash
# Check for port usage
netstat -an | grep :8100

# Generate with different port
dotnet new mmt -n MyApp --port 9000
```

## Quick Reference

### Essential Parameter Combinations

| Scenario | Command |
|----------|---------|
| **API Only** | `dotnet new mmt -n App --api --no-back-office --no-sample` |
| **Processing Only** | `dotnet new mmt -n App --back-office --no-web-api --kafka` |
| **Full CQRS** | `dotnet new mmt -n App --api --back-office --kafka` |
| **Orleans State** | `dotnet new mmt -n App --orleans --kafka` |
| **Development** | `dotnet new mmt -n App --libs defaults,api,ext --no-sample` |
| **Minimal** | `dotnet new mmt -n App --project-only --no-sample --db-config none` |

### Port Allocation Guide

| Service Type | Suggested Range | Example |
|--------------|----------------|---------|
| **API Services** | 8000-8099 | `--port 8000` |
| **Processing Services** | 8100-8199 | `--port 8100` |
| **Orleans Services** | 8200-8299 | `--port 8200` |
| **Utility Services** | 8300-8399 | `--port 8300` |

## Next Steps

1. **Install the Template**:
   ```bash
   dotnet new install Momentum.Template
   ```

2. **Choose Your Architecture**: Review the decision guide above

3. **Generate Your Solution**: Use appropriate parameters for your needs

4. **Follow the Walkthrough**: See [Template Walkthrough](../template-walkthrough/index.md) for step-by-step guidance

5. **Configure Services**: Review [Service Configuration](../service-configuration/index.md) for setup details

## Related Documentation

- [Template Walkthrough](../template-walkthrough/index.md) - Step-by-step generation guide
- [Service Configuration](../service-configuration/index.md) - Post-generation configuration
- [Database Guide](../database/index.md) - Database setup and migrations
- [Messaging Guide](../messaging/index.md) - Kafka and event handling
- [CQRS Guide](../cqrs/index.md) - Commands, queries, and patterns
