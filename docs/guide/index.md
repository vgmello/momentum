# AppDomain Solution Overview

Welcome to the AppDomain Solution documentation. This comprehensive system provides enterprise-grade AppDomain management capabilities built on modern architectural patterns.

## System Overview

The AppDomain Solution uses Domain-Oriented Vertical Slice Architecture to manage the complete AppDomain lifecycle for organizations. See [Architecture Overview](/arch/) for details.

## Design Philosophy

This codebase follows a real-world mirroring approach:

-   **Product-Oriented**: Each folder represents a department with recognizable operations
-   **Minimal Abstractions**: No unnecessary layers - only what represents real-world entities
-   **Simple Records**: Invoices and cashiers are data records, not smart objects
-   **Clear Separation**: Front office (sync APIs) vs back office (async processing)

## Project Structure

```
.
├── docs/                            # DocFX documentation system
├── infra/                           # Infrastructure and database
│   └── AppDomain.Database/            # Liquibase Database project
├── src/                             # Source code projects
│   ├── AppDomain/                     # Domain logic
│   ├── AppDomain.Api/                 # REST/gRPC endpoints
│   ├── AppDomain.AppHost/             # .NET Aspire orchestration
│   ├── AppDomain.BackOffice/          # Background processing
│   ├── AppDomain.BackOffice.Orleans/  # Orleans stateful processing
│   └── AppDomain.Contracts/           # Integration events and models
├── tests/                           # Testing projects
│   └── AppDomain.Tests/               # Unit, Integration, and Architecture tests
└── libs/                            # Shared libraries
    └── Operations/                  # Operations libs
        ├── src/                     # Platform source code
        │   ├── Momentum.Extensions.*
        │   ├── Momentum.ServiceDefaults.*
        │   └── ...
        └── tests/                   # Platform tests
```

## Core Domains

### Cashiers

The Cashiers domain manages the personnel responsible for handling payments and AppDomain operations. Key features include:

-   Complete CRUD operations for cashier management
-   Event-driven workflows for cashier lifecycle
-   Payment tracking and reconciliation
-   Integration with invoice processing

[Learn more about Cashiers →](/guide/cashiers/)

### Invoices

The Invoices domain handles the complete invoice lifecycle from creation to payment. Core capabilities:

-   Invoice creation with validation
-   Status management (Draft, Finalized, Paid, Cancelled)
-   Payment processing workflows
-   Automated event-driven notifications

[Learn more about Invoices →](/guide/invoices/)

### Bills

The Bills domain provides comprehensive bill management functionality (coming soon):

-   Bill creation and tracking
-   Recurring AppDomain support
-   Integration with invoicing system

[Learn more about Bills →](/guide/bills/)

## Architecture Highlights

### Vertical Slice Architecture

Momentum implements **Vertical Slice Architecture**, a powerful architectural pattern that organizes code by feature rather than by technical layers. This approach dramatically improves maintainability, reduces coupling, and makes the codebase more intuitive for both developers and business stakeholders.

#### What is Vertical Slice Architecture?

Instead of organizing code horizontally by technical concerns (Controllers, Services, Repositories), Vertical Slice Architecture organizes code vertically by business features or use cases. Each "slice" contains everything needed for a specific business operation.

**Traditional Layered Architecture:**

```
├── Controllers/           # All controllers
├── Services/             # All business logic
├── Repositories/         # All data access
└── Models/               # All data models
```

**Vertical Slice Architecture:**

```
├── Cashiers/             # Complete cashier domain
│   ├── Commands/         # Actions (CreateCashier, UpdateCashier)
│   ├── Queries/          # Information retrieval (GetCashier, GetCashiers)
│   ├── Data/             # Domain-specific data access
│   └── Contracts/        # Events and models
└── Invoices/             # Complete invoice domain
    ├── Commands/         # Actions (CreateInvoice, CancelInvoice)
    ├── Queries/          # Information retrieval (GetInvoice, GetInvoices)
    ├── Data/             # Domain-specific data access
    └── Contracts/        # Events and models
```

#### Benefits in Microservices Context

**1. Feature Cohesion**

-   All code related to a business feature lives together
-   Changes to a feature are localized to a single directory
-   Easy to understand the complete flow of a business operation

**2. Team Autonomy**

-   Teams can own entire vertical slices without stepping on each other
-   Independent deployment and development cycles
-   Clear boundaries for code ownership

**3. Reduced Coupling**

-   Each slice is self-contained with minimal dependencies
-   Changes in one domain rarely affect another
-   Natural boundaries for microservice decomposition

**4. Business Alignment**

-   Code structure mirrors business operations
-   Non-technical stakeholders can navigate the codebase
-   Features map directly to business capabilities

#### Momentum's Implementation

**CQRS Pattern Integration**
Each domain slice follows the Command Query Responsibility Segregation (CQRS) pattern:

-   **Commands/**: Actions that change system state (CreateCashier, CancelInvoice)
-   **Queries/**: Read operations that retrieve information (GetCashier, GetInvoices)
-   **Data/**: Domain-specific database access and entity mapping
-   **Contracts/**: Events and models for communication

**Example: Cashiers Domain Structure**

```
src/AppDomain/Cashiers/
├── Commands/
│   ├── CreateCashier.cs      # Business action: hire new cashier
│   ├── UpdateCashier.cs      # Business action: update cashier info
│   └── DeleteCashier.cs      # Business action: remove cashier
├── Queries/
│   ├── GetCashier.cs         # Retrieve single cashier
│   └── GetCashiers.cs        # List all cashiers
├── Data/
│   ├── DbMapper.cs           # Database mapping logic
│   └── Entities/             # Database entities
└── Contracts/
    ├── IntegrationEvents/    # Cross-service events
    └── Models/               # Public models
```

**Cross-Cutting Concerns**
While business logic is vertically sliced, infrastructure concerns are handled separately:

-   **Core/**: Shared database infrastructure and base entities
-   **Infrastructure/**: Dependency injection and service registration
-   **libs/Momentum/**: Platform libraries for messaging, validation, etc.

#### Practical Example: Invoice Creation Flow

When creating an invoice, all related code lives in the `Invoices/` slice:

1. **API Endpoint** calls `CreateInvoice` command
2. **Command Handler** in `Commands/CreateInvoice.cs` contains business logic
3. **Database Access** via `Data/DbMapper.cs` handles persistence
4. **Integration Event** `InvoiceCreated` notifies other services
5. **Query Handlers** in `Queries/` provide read access to created invoices

This entire flow is contained within the Invoices slice, making it easy to understand, test, and modify.

#### Comparison to Traditional Layered Architecture

| Aspect                | Layered Architecture            | Vertical Slice Architecture |
| --------------------- | ------------------------------- | --------------------------- |
| **Code Organization** | By technical concern            | By business feature         |
| **Change Impact**     | Spreads across multiple layers  | Contained within slice      |
| **Team Boundaries**   | Artificial technical boundaries | Natural business boundaries |
| **Testing**           | Complex setup across layers     | Focused feature testing     |
| **Understanding**     | Requires mental mapping         | Direct business alignment   |
| **Coupling**          | High between layers             | Low between slices          |

#### Best Practices in Momentum

**1. Keep Slices Independent**

-   Minimize dependencies between domains
-   Use integration events for cross-domain communication
-   Avoid shared business logic between slices

**2. Embrace Duplication**

-   Prefer duplication over coupling
-   Each slice can have its own models and validation
-   Shared infrastructure only, not business logic

**3. Clear Boundaries**

-   Each slice represents a distinct business capability
-   Use domain events for internal coordination
-   Integration events for external communication

**4. Consistent Structure**

-   Every slice follows the same Commands/Queries/Data/Contracts pattern
-   Predictable structure improves maintainability
-   New team members can quickly understand any domain

[Explore the Architecture →](/arch/)

### Event-Driven Design

-   Integration events for cross-domain communication
-   Event sourcing for audit trails
-   Asynchronous processing with message queues
-   Real-time updates via event streams

[Learn about Events →](/arch/events)

### Technology Stack

-   **.NET 10**: Latest framework features and performance
-   **PostgreSQL**: Robust data persistence with Liquibase migrations
-   **Docker & Aspire**: Container orchestration and local development
-   **Orleans**: Actor-based distributed processing
-   **Refit + Refitter**: Type-safe REST API clients with OpenAPI code generation for E2E testing

## Getting Started

Ready to begin? Follow our comprehensive getting started guide:

1. **Setup Development Environment**: Install prerequisites and tools
2. **Run the Application**: Choose between Aspire, Docker, or manual setup
3. **Make Your First API Call**: Create cashiers and invoices
4. **Explore the Codebase**: Understand the project structure

[Get Started →](/guide/getting-started)

## Developer Resources

### For New Developers

-   [Development Environment Setup](/guide/dev-setup)
-   [First Contribution Guide](/guide/first-contribution)
-   [Debugging Tips](/guide/debugging)

### API Documentation

-   [REST API Reference](${API_BASE_URL}/scalar)
-   [Integration Events](/events/)

### Advanced Topics

-   [CQRS Implementation](/arch/cqrs)
-   [Error Handling Patterns](/arch/error-handling)
-   [Testing Strategies](/arch/testing)

The design makes the codebase intuitive for developers, product teams, and even AI assistants to understand and work with.

## Next Steps

-   **Explore the Domains**: Deep dive into [Cashiers](/guide/cashiers/), [Invoices](/guide/invoices/), or [Bills](/guide/bills/)
-   **Understand the Architecture**: Learn about our [architectural patterns](/arch/)
-   **Start Contributing**: Follow our [development guide](/guide/dev-setup)
-   **Review the APIs**: Check out the [API documentation](${API_BASE_URL}/scalar)
