# AppDomain Solution Overview

Welcome to the AppDomain Solution documentation. This comprehensive system provides enterprise-grade AppDomain management capabilities built on modern architectural patterns.

## System Overview

The AppDomain Solution is a domain-driven application that manages the complete AppDomain lifecycle for organizations. Built with Vertical Slice Architecture principles and event-driven patterns, it provides a scalable and maintainable foundation for AppDomain operations.

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
        │   ├── Operations.Extensions.*
        │   ├── Operations.ServiceDefaults.*
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

-   TODO: Fill the details

[Explore the Architecture →](/arch/)

### Event-Driven Design

-   Integration events for cross-domain communication
-   Event sourcing for audit trails
-   Asynchronous processing with message queues
-   Real-time updates via event streams

[Learn about Events →](/arch/events)

### Technology Stack

-   **.NET 9**: Latest framework features and performance
-   **PostgreSQL**: Robust data persistence with Liquibase migrations
-   **Docker & Aspire**: Container orchestration and local development
-   **Orleans**: Actor-based distributed processing

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

-   [REST API Reference](/api/)
-   [Integration Events](/events/)
-   [gRPC Services](/api/grpc)

### Advanced Topics

-   [CQRS Implementation](/arch/cqrs)
-   [Error Handling Patterns](/arch/error-handling)
-   [Testing Strategies](/arch/testing)

The design makes the codebase intuitive for developers, product teams, and even AI assistants to understand and work with.

## Next Steps

-   **Explore the Domains**: Deep dive into [Cashiers](/guide/cashiers/), [Invoices](/guide/invoices/), or [Bills](/guide/bills/)
-   **Understand the Architecture**: Learn about our [architectural patterns](/arch/)
-   **Start Contributing**: Follow our [development guide](/guide/dev-setup)
-   **Review the APIs**: Check out the [API documentation](/api/)
