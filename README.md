# Momentum

A template-driven .NET 9 microservices solution that transforms how you build business applications. Like **Shadcn/ui** for React components, Momentum provides you with production-ready code patterns that you can import, customize, and own completely.

## Why Choose Momentum?

**🚀 Minimal Ceremony, Maximum Productivity**

-   No complex abstractions or unnecessary layers
-   Real-world business patterns that mirror your actual operations
-   Code so intuitive that non-technical stakeholders can understand it

**⚡ Modern Stack, Battle-Tested Patterns**

-   .NET 9, Orleans, Kafka, PostgreSQL
-   Event-driven microservices architecture
-   Comprehensive testing with Testcontainers

**📦 Template-Driven Approach**

-   Copy the code you need, modify what you want
-   No framework lock-in or hidden magic
-   Full control over your codebase

## TL;DR - Core Philosophy

**Real-World Mirroring**: Every folder, class, and method corresponds directly to business operations

-   `Commands/` = Actions your business performs
-   `Queries/` = Information your business retrieves
-   `Events/` = Things that happen in your business

**No Smart Objects**: Entities are data records, not self-modifying objects

-   Infrastructure elements support functionality like utilities in an office
-   Front office = Synchronous APIs (immediate responses)
-   Back office = Asynchronous processing (background work)

## Code Structure and Design Philosophy

### Overview

This template is intentionally structured to mirror real-world business operations and organizational structures.
Each part of the code corresponds or should correspond directly to a real-world role or operation, ensuring that the code remains 100% product-oriented and easy to understand.
The idea is that main operations/actions would be recognizable by a non-technical product person.

### Real-World Mirroring

For instance, if your business handles creating orders, the code includes a clear and
direct set of actions to handle order creation.
Smaller tasks, or sub-actions, needed to complete a main action are also represented in a similar manner.
If a sub-action is only used within one main action, it remains nested inside that operation. If it needs to be reused by multiple operations,
it is extracted and made reusable, but still mirroring the real-world scenario.

### Avoiding Unnecessary Abstractions

This design philosophy avoids unnecessary abstractions. There are no additional layers like repositories or services unless they represent
something that exists in the real business. Infrastructure elements like logging or authorization are present as they support the system's
functionality, same as water pipes and electricity support a business office. Even the database is viewed as a digital parallel to a
real-world archive or filing system.

### No "Domain" Objects

A key principle is the absence of smart objects. This means that a business entity, for example, is not an object that can change itself.
Instead, it is simply treated as a digital record, and all modifications are performed by "external" actors (something is changing the record,
the record does not change itself). This ensures that the code reflects digital representations of real-world entities and processes,
rather than trying to replicate objects with their own behaviors.

### Synchronous and Asynchronous Operations

The template also distinguishes between synchronous and asynchronous operations.
The API represents the front office of your business, handling synchronous operations where immediate responses are expected.
In contrast, the back office is represented by asynchronous operations that do not require immediate responses, allowing for efficient,
behind-the-scenes processing.

## How to Use Momentum

```bash
# Install the template
dotnet new install Momentum.Templates

# Create a solution using the template
dotnet new mmt -n MyService
```

### What You Get Out of the Box

-   **🏗️ Entity Management**: Flexible data models with real-world business entity patterns
-   **⚙️ Workflow Processing**: Orleans-based stateful processing for complex business workflows
-   **📡 Event Integration**: Event-driven architecture with Kafka for cross-service communication
-   **🌐 Modern APIs**: REST and gRPC endpoints with OpenAPI documentation
-   **🧪 Comprehensive Testing**: Unit, integration, and architecture tests with real infrastructure
-   **📊 Observability**: Built-in logging, metrics, and distributed tracing

## Template Architecture

The template follows a microservices architecture with shared platform libraries:

```
.
├── docs/                            # VitePress documentation system
├── infra/                           # Infrastructure and database
│   └── AppDomain.Database/          # Liquibase Database project
├── src/                             # Source code projects
│   ├── AppDomain/                      # Domain logic (customizable)
│   ├── AppDomain.Api/                  # REST/gRPC endpoints
│   ├── AppDomain.AppHost/              # .NET Aspire orchestration
│   ├── AppDomain.BackOffice/           # Background processing
│   ├── AppDomain.BackOffice.Orleans/   # Orleans stateful processing
│   └── AppDomain.Contracts/            # Integration events and models
├── tests/                           # Testing projects
│   └── AppDomain.Tests/                # Unit, Integration, and Architecture tests
└── libs/                            # Shared libraries
    └── Momentum/                    # Momentum libs
        ├── src/                     # Platform source code
        │   ├── Momentum.Extensions.*
        │   ├── Momentum.ServiceDefaults.*
        │   └── ...
        └── tests/                   # Platform tests
```

## Port Configuration

The template uses a systematic port allocation pattern for consistent service endpoints.

### Port Assignment Pattern

<!--- TODO: Replace the ports with the template variables --->

#### **Aspire Dashboard Ports**

-   **HTTP**: Service base + 10,000 (e.g., 8100 → 18100)
-   **HTTPS**: Service base + 10,010 (e.g., 8100 → 18110)

#### **Port Pattern Within Each Service Block (XX00-XX19)**

```
Aspire Resource Service: XX00 (HTTP) / XX10 (HTTPS)
Main API:               XX01 (HTTP) / XX11 (HTTPS) / XX02 (gRPC-HTTP)
BackOffice:             XX03 (HTTP) / XX13 (HTTPS)
Orleans:                XX04 (HTTP) / XX14 (HTTPS)
UI/Frontend:            XX05 (HTTP) / XX15 (HTTPS)
Documentation:          XX19 (reserved for last port of range)
```

### Default Port Assignments (8100-8119)

#### Aspire Services

-   **Aspire Dashboard:** 18100 (HTTP) / 18110 (HTTPS)
-   **Aspire Resource Service:** 8100 (HTTP) / 8110 (HTTPS)

#### Application Services

-   **AppDomain.Api:** 8101 (HTTP) / 8111 (HTTPS) / 8102 (gRPC insecure)
-   **AppDomain.BackOffice:** 8103 (HTTP) / 8113 (HTTPS)
-   **AppDomain.BackOffice.Orleans:** 8104 (HTTP) / 8114 (HTTPS)
-   **AppDomain.UI:** 8105 (HTTP) / 8115 (HTTPS)
-   **Documentation Service:** 8119

#### Infrastructure Services

-   **54320**: PostgreSQL
-   **9092**: Apache Kafka
-   **4317/4318**: OpenTelemetry OTLP (gRPC/HTTP)

## Prerequisites

-   **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
-   **Docker Desktop** - [Download here](https://www.docker.com/products/docker-desktop/) (optional but recommended)

**Alternative Setup (No Docker):**

-   PostgreSQL on localhost:5432 (username: `postgres`, password: `password@`)
-   Liquibase CLI for database migrations

## Getting Started

### Quick Start (5 minutes)

```bash
# 1. Clone the template
git clone https://github.com/your-org/momentum.git my-business-app
cd my-business-app

# 2. Run the complete application stack
dotnet run --project src/AppDomain.AppHost

# 3. Open your browser to:
# - Aspire Dashboard: https://localhost:18110
# - API: https://localhost:8111
# - Documentation: http://localhost:8119
```

### Customize for Your Business (15 minutes)

1. **Replace the domain name**:

    ```bash
    # Replace "AppDomain" with your business domain (e.g., "Ecommerce", "Finance")
    # Update folder names, namespaces, and configuration
    ```

2. **Define your business entities**:

    ```bash
    # Edit src/[YourDomain]/Commands/ - actions your business performs
    # Edit src/[YourDomain]/Queries/ - information your business retrieves
    # Update infra/[YourDomain].Database/ - database schema
    ```

3. **Test your changes**:

    ```bash
    dotnet test                    # Run all tests
    dotnet build                   # Ensure everything compiles
    ```

4. **Start developing**:
    - Add your business logic to Commands and Queries
    - Update database migrations in the `infra/` folder
    - Customize integration events for your business processes
    - Modify UI components in your preferred frontend framework

## Key Technologies

-   **.NET Aspire**: Application orchestration and service discovery
-   **Orleans**: Stateful actor-based processing for complex workflows
-   **Wolverine**: CQRS/MediatR-style command handling with Kafka integration
-   **PostgreSQL**: Primary database with Liquibase migrations
-   **Apache Kafka**: Event streaming and message bus
-   **gRPC + REST**: API protocols
-   **Testcontainers**: Integration testing with real infrastructure

## Documentation & Resources

### Local Documentation

Start the documentation server for comprehensive guides:

```bash
cd docs && pnpm dev
```

**Available Documentation:**

-   📋 **Architecture Decisions**: Why we made specific design choices
-   🎯 **Development Guidelines**: Best practices and coding standards
-   🔌 **API Documentation**: Auto-generated REST and gRPC API docs
-   📡 **Event Integration**: Event schemas and cross-service communication
-   🧪 **Testing Strategies**: Unit, integration, and architecture testing approaches

### Quick Reference Commands

```bash
# Development
dotnet run --project src/AppDomain.AppHost    # Start all services
dotnet build                                  # Build all projects
dotnet test                                   # Run all tests

# Database
docker compose up AppDomain-db-migrations    # Run database migrations
docker compose down -v                       # Reset database

# Documentation
cd docs && pnpm docs:build                   # Build documentation
cd docs && pnpm docs:events                  # Generate event documentation
```

### Contributing

Momentum is designed to be copied and customized. However, if you've created patterns or improvements that would benefit the broader community, contributions to the template are welcome!
