# AppDomain Solution

The AppDomain Service manages cashiers, invoices, and payment processing within the broader platform. It provides both REST and gRPC APIs for managing AppDomain operations and integrates with other services through event-driven messaging.

## TL;DR

This codebase is designed as an app template that mirrors real-world AppDomain department operations\*, making it intuitive for junior engineers and non-technical product people to understand - each folder under AppDomain represents a sub-department with its main activities and processes, categorized as commands or queries. The design focuses on minimal ceremony code, avoiding almost all common tech abstractions, treats infrastructure like office utilities, uses digital representations of what would be "paper" records that can't change themselves but can be modified by external actors, Front office/desk operations are exposed as public sync APIs, and back office operations are supported by async event handlers.
The aim was for it to be extremely simple to use and very developer-friendly, however, as a positive, unexpected side effect, the simplicity and real-world mirroring approach also make the codebase naturally LLM-friendly, modern models can easily understand code that follows familiar real-world patterns.

## Code Structure and Design Philosophy

### Overview

This codebase is intentionally structured to mirror the real-world operations and organizational structure of a AppDomain department.
Each part of the code corresponds or should correspond directly to a real-world role or operation, ensuring that the code remains 100% product-oriented and easy to understand.
The idea is that main operations/actions would be recognizable by a non-technical product person.

### Real-World Mirroring

For instance, if the AppDomain department handles creating invoices, the code includes a clear and
direct set of actions to handle invoice creation.
Smaller tasks, or sub-actions, needed to complete a main action are also represented in a similar manner.
If a sub-action is only used within one main action, it remains nested inside that operation. If it needs to be reused by multiple operations,
it is extracted and made reusable, but still mirroring the real-world scenario.

### Avoiding Unnecessary Abstractions

This design philosophy avoids unnecessary abstractions. There are no additional layers like repositories or services unless they represent
something that exists in the real department. Infrastructure elements like logging or authorization are present as they support the system’s
functionality, same as water pipes and electricity support a AppDomain department office. Even the database is viewed as a digital parallel to a
real-world archive or filing system.

### No "Domain" Objects

A key principle is the absence of smart objects. This means that an invoice, for example, is not an object that can change itself.
Instead, it is simply treated as a digital record, and all modifications are performed by "external" actors (something is changing the invoice,
the invoice does not change itself). This ensures that the code reflects digital representations of real-world entities and processes,
rather than trying to replicate objects with their own behaviors.

### Synchronous and Asynchronous Operations

The codebase also distinguishes between synchronous and asynchronous operations.
The API represents the front office of the AppDomain department, handling synchronous operations where immediate responses are expected.
In contrast, the back office is represented by asynchronous operations that do not require immediate responses, allowing for efficient,
behind-the-scenes processing.

## What is the AppDomain Service?

The AppDomain Service is part of a .NET 9 microservices system built using Domain-Driven Design principles. It provides a complete AppDomain solution with modern web UI and comprehensive testing. It handles:

-   **Cashier Management**: Create and manage cashiers with multi-currency support
-   **Invoice Processing**: Handle invoice lifecycle with Orleans-based stateful processing
-   **Payment Integration**: Process payments and emit integration events
-   **Cross-Service Integration**: React to business events from other services like Accounting
-   **Modern Web UI**: SvelteKit-based responsive web application
-   **Comprehensive Testing**: Unit, integration, and end-to-end testing with real browsers

## Service Architecture

The solution follows a microservices architecture with shared platform libraries:

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

## Port Configuration

The AppDomain service uses the following ports:

### Aspire Dashboard

-   **Aspire Dashboard:** 18100 (HTTP) / 18110 (HTTPS)
-   **Aspire Resource Service:** 8100 (HTTP) / 8110 (HTTPS)

### Service Ports (8100-8119)

-   **AppDomain.UI:** 8105 (HTTP) / 8115 (HTTPS)
-   **AppDomain.Api:** 8101 (HTTP) / 8111 (HTTPS) / 8102 (gRPC insecure)
-   **AppDomain.BackOffice:** 8103 (HTTP) / 8113 (HTTPS)
-   **AppDomain.BackOffice.Orleans:** 8104 (HTTP) / 8114 (HTTPS)
-   **Documentation Service:** 8119

### Shared Services

-   **54320**: PostgreSQL
-   **4317/4318**: OpenTelemetry OTLP

## Prerequisites

-   Docker (optional, for containerized deployment) or..
-   .NET 9 SDK
-   PostgreSQL running on localhost:5432 (username: `postgres`, password: `password@`)
-   Liquibase CLI (for manual database setup)

## Additional Resources

-   TBD
