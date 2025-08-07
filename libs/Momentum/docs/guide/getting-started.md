---
title: Getting Started with Momentum
description: A template-driven .NET 9 microservices solution that transforms how you build business applications. Like Shadcn/ui for React components, provides production-ready code patterns you can copy, customize, and own.
date: 2024-01-15
---

# Getting Started with Momentum

A template-driven .NET 9 microservices solution that transforms how you build business applications. Like **Shadcn/ui** for React components, Momentum provides you with production-ready code patterns that you can copy, customize, and own completely.

## Why Choose Momentum?

**🚀 Minimal Ceremony, Maximum Productivity**

-   No complex abstractions or unnecessary layers
-   Real-world business patterns that mirror your actual operations
-   Code so intuitive that non-technical stakeholders can understand it

**📦 Template-Driven Approach**

-   Copy/import the code you need, modify what you want
-   No framework lock-in or hidden magic
-   Full control over your codebase

**⚡ Modern Stack, Battle-Tested Patterns**

-   .NET 9, Orleans, Kafka, PostgreSQL
-   Event-driven microservices architecture
-   Comprehensive testing with Testcontainers

**🤖 LLM-Friendly Architecture**

-   Natural patterns that AI models understand perfectly
-   Accelerates development with AI coding assistants
-   Self-documenting code structure

> **New to CQRS or event-driven architecture?** This guide assumes basic familiarity with these patterns. If you need background, see our [Architecture Overview](./arch/) first.

## Core Philosophy: Real-World Mirroring

**Real-World Mirroring**: Every folder, class, and method corresponds directly to business operations

-   `Commands/` = Actions your business performs
-   `Queries/` = Information your business retrieves
-   `Events/` = Things that happen in your business

**No Smart Objects**: Entities are data records, not self-modifying objects

-   Infrastructure elements support functionality like utilities in an office
-   Front office = Synchronous APIs (immediate responses)
-   Back office = Asynchronous processing (background work)

### Template-First Approach

Rather than being a traditional framework, Momentum operates as an **opinionated template** that you can:

-   Install as NuGet packages for managed dependencies
-   Import source code directly into your project for full control
-   Customize patterns to fit your specific requirements

This approach gives you the flexibility of code ownership while maintaining the benefits of proven patterns and configurations.

### Real-World Business Mirroring

This template is intentionally structured to mirror real-world business operations and organizational structures. Each part of the code corresponds or should correspond directly to a real-world role or operation, ensuring that the code remains 100% product-oriented and easy to understand.

For instance, if your business handles creating orders, the code includes a clear and direct set of actions to handle order creation. Smaller tasks, or sub-actions, needed to complete a main action are also represented in a similar manner.

### Avoiding Unnecessary Abstractions

This design philosophy avoids unnecessary abstractions. There are no additional layers like repositories or services unless they represent something that exists in the real business. Infrastructure elements like logging or authorization are present as they support the system's functionality, same as water pipes and electricity support a business office.

### Core Features

-   **[CQRS Pattern](./cqrs/)** - Clean separation of commands and queries with automatic validation
-   **[Database Operations](./database/)** - Type-safe operations with LinqToDB and the DbCommand pattern
-   **[Event-Driven Messaging](./messaging/)** - Kafka integration with CloudEvents and automatic topic management
-   **[Service Configuration](./service-configuration/)** - Pre-configured logging, telemetry, health checks, and observability
-   **[Testing Utilities](./testing/)** - Comprehensive unit and integration testing patterns with Testcontainers
-   **[Error Handling](./error-handling)** - Result pattern and structured exception management

### Why Choose Momentum?

-   **Production-Ready**: Battle-tested patterns used in high-scale applications
-   **Developer Experience**: Minimal boilerplate with powerful abstractions that reduce ceremony
-   **Maintainable**: Clear architectural boundaries and consistent patterns
-   **Supportable**: Well-documented patterns and comprehensive observability
-   **Testable**: Clear separation of concerns enables comprehensive testing
-   **Type-Safe**: Compile-time guarantees with source-generated database commands

## Fundamental Concepts: Types of Objects

Understanding the different types of objects in Momentum is essential for building maintainable, well-architected services. Each object type has a specific purpose and responsibility:

### Public API Contracts

**Public API requests and responses** serve as contracts between the external world and the API layer:

-   **HTTP/gRPC Requests/Responses**: Define the external interface for your service
-   **Purpose**: Provide stable contracts for external consumers
-   **Transformation**: Converted to/from action contracts internally
-   **Best Practice**: Can contain public models from the Contracts namespace
-   **Important**: Avoid exposing action contracts directly—this leads to API layer concerns leaking into business logic (like `JsonIgnore` attributes or `internal` properties)

### Action Contracts (Business Operations)

**Action contracts** (`CreateSomethingCommand`, `GetSomethingQuery`) represent the business operations supported by your domain:

-   **Commands**: Change state and typically span multiple business operations
-   **Queries**: Read data without side effects
-   **Transformation**: In CRUD scenarios, converted to/from database entities
-   **Purpose**: Define the business capabilities of your service

### Database Entities

**Entity objects** represent database tables in C# and handle data persistence:

-   **Purpose**: Map directly to database table structures
-   **Scope**: Used exclusively within the data access layer
-   **Transformation**: Converted to/from business models and action contracts
-   **Best Practice**: Keep entities focused on data representation, not business logic

### Integration Events

**Integration Events** communicate important domain changes to other services:

-   **Purpose**: Published when significant business actions occur
-   **Minimal Transformation**: Limited mapping, often containing public contract models
-   **Cross-Service Communication**: Enable loosely coupled service interactions
-   **Event-Driven**: Support asynchronous processing patterns

### Public Models

**Models** are standard domain objects that can be shared across service boundaries:

-   **Purpose**: Represent core business concepts
-   **Visibility**: Safe to include in public contracts and integration events
-   **Consistency**: Provide stable representations of domain entities
-   **Reusability**: Used across different layers and contexts

### Object Flow Example

Here's how these objects typically flow through a request:

```
External Request → API Request → Action Contract → Entity → Database
                                      ↓
                                Integration Event → External Services
                                      ↓
                                 Public Model → API Response → External Response
```

This clear separation ensures:

-   **Clean boundaries** between layers
-   **Testable components** with distinct responsibilities
-   **Maintainable code** that's easy to reason about
-   **Flexible evolution** without breaking external contracts

## Quick Start

### Prerequisites

-   **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
-   **Docker Desktop** - [Download here](https://www.docker.com/products/docker-desktop/) (optional but recommended)

**Alternative Setup (No Docker):**

-   PostgreSQL on localhost:5432 (username: `postgres`, password: `password@`)
-   Liquibase CLI for database migrations

## How to Use Momentum

### Option 1: Template Approach (Recommended)

```bash
# Use as a GitHub template or clone directly
git clone https://github.com/your-org/momentum.git my-new-project
cd my-new-project
# Replace [Domain] with your business domain throughout the codebase
```

### Option 2: Selective Import

Copy specific patterns and components you need:

-   Commands and Queries for CQRS patterns
-   Event handling infrastructure
-   Orleans stateful processing setup
-   Testcontainers integration test patterns
-   Database migration patterns with Liquibase

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

### Creating a New Project from Template

1. **Start with the AppHost template:**

```bash
# Clone or use the momentum template
dotnet new console -n YourApp.AppHost
cd YourApp.AppHost
```

2. **Add the Momentum ServiceDefaults:**

```xml
<PackageReference Include="Momentum.ServiceDefaults" Version="1.0.0" />
```

3. **Set up your Program.cs:**

```csharp
using Momentum.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add all Momentum service defaults
builder.AddServiceDefaults();

var app = builder.Build();

// Configure your endpoints here
app.MapGet("/", () => "Hello Momentum!");

// Run with Momentum enhancements
await app.RunAsync(args);
```

4. **Configure domain assembly discovery:**

Create a marker interface and register your domain assembly:

```csharp
// YourApp.Domain/IYourDomainAssembly.cs
namespace YourApp.Domain;

public interface IYourDomainAssembly;
```

```csharp
// In your API project Program.cs or AssemblyInfo.cs
using YourApp.Domain;

[assembly: DomainAssembly(typeof(IYourDomainAssembly))]
```

> **Important**: The `DomainAssembly` attribute enables automatic discovery of commands, queries, validators, and event handlers in your domain assemblies.

## Core Concepts

### 1. Commands and Queries

Momentum follows CQRS principles with clear separation:

**Commands** - Change state and return results:

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand<Result<User>>;
```

**Queries** - Read data without side effects:

```csharp
public record GetUserQuery(Guid Id) : IQuery<Result<User>>;
```

### 2. Handlers

Handlers contain your business logic:

```csharp
public static class CreateUserCommandHandler
{
    public static async Task<(Result<User>, UserCreated?)> Handle(
        CreateUserCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        // Your business logic here
        var user = new User(command.Name, command.Email);

        // Database operations
        var dbCommand = new InsertUserDbCommand(user);
        var insertedUser = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        // Create integration event
        var userCreated = new UserCreated(user.TenantId, user);

        return (insertedUser.ToModel(), userCreated);
    }
}
```

### 3. Database Operations

Use the `DbCommand` pattern for type-safe database operations:

```csharp
public static class CreateUserCommandHandler
{
    public record DbCommand(User User) : ICommand<User>;

    public static async Task<User> Handle(DbCommand command, AppDb db, CancellationToken cancellationToken)
    {
        return await db.Users.InsertWithOutputAsync(command.User, token: cancellationToken);
    }
}
```

### 4. Integration Events

Publish events for cross-service communication:

```csharp
// Contracts/IntegrationEvents/UserCreated.cs
using Momentum.ServiceDefaults.Messaging;

/// <summary>
/// Published when a new user is successfully created in the system.
/// This event notifies other services that a user account is available.
/// </summary>
[EventTopic<User>]
public record UserCreated(
    [PartitionKey] Guid TenantId,
    User User
);
```

> **Note**: Integration events are automatically published by the framework when returned from command handlers. XML documentation is required for event discovery and generated documentation.

## Your First Complete Example

Let's build a complete example with a `CreateUser` command:

### 1. Define the Command

```csharp
// Commands/CreateUser.cs
using FluentValidation;

namespace YourApp.Users.Commands;

public record CreateUserCommand(Guid TenantId, string Name, string Email) : ICommand<Result<User>>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

### 2. Create the Handler

```csharp
// Commands/CreateUser.cs (continued)
public static class CreateUserCommandHandler
{
    public record DbCommand(Data.Entities.User User) : ICommand<Data.Entities.User>;

    public static async Task<(Result<User>, UserCreated?)> Handle(
        CreateUserCommand command,
        IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        var dbCommand = CreateInsertCommand(command);
        var insertedUser = await messaging.InvokeCommandAsync(dbCommand, cancellationToken);

        var result = insertedUser.ToModel();
        var createdEvent = new UserCreated(result.TenantId, result);

        return (result, createdEvent);
    }

    public static async Task<Data.Entities.User> Handle(DbCommand command, AppDb db, CancellationToken cancellationToken)
    {
        return await db.Users.InsertWithOutputAsync(command.User, token: cancellationToken);
    }

    private static DbCommand CreateInsertCommand(CreateUserCommand command) =>
        new(new Data.Entities.User
        {
            TenantId = command.TenantId,
            UserId = Guid.CreateVersion7(),
            Name = command.Name,
            Email = command.Email,
            CreatedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow
        });
}
```

### 3. Define the Integration Event

```csharp
// Contracts/IntegrationEvents/UserCreated.cs
/// <summary>
/// Published when a new user is successfully created in the system.
/// </summary>
[EventTopic<User>]
public record UserCreated(
    [PartitionKey] Guid TenantId,
    User User
);
```

### 4. Add API Endpoints

```csharp
// In your API project
app.MapPost("/users", async (CreateUserCommand command, IMessageBus messaging) =>
{
    var (result, integrationEvent) = await messaging.InvokeAsync(command);

    if (result.IsSuccess)
    {
        // Event will be automatically published
        return Results.Created($"/users/{result.Value.Id}", result.Value);
    }

    return Results.BadRequest(result.Errors);
});
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

## Port Configuration

The template uses the following default port configuration:

### Aspire Dashboard

-   **Aspire Dashboard:** 18100 (HTTP) / 18110 (HTTPS)
-   **Aspire Resource Service:** 8100 (HTTP) / 8110 (HTTPS)

### Service Ports (8100-8119)

-   **[Domain].UI:** 8105 (HTTP) / 8115 (HTTPS)
-   **[Domain].Api:** 8101 (HTTP) / 8111 (HTTPS) / 8102 (gRPC insecure)
-   **[Domain].BackOffice:** 8103 (HTTP) / 8113 (HTTPS)
-   **[Domain].BackOffice.Orleans:** 8104 (HTTP) / 8114 (HTTPS)
-   **Documentation Service:** 8119

### Infrastructure Services

-   **54320**: PostgreSQL
-   **4317/4318**: OpenTelemetry OTLP

## Running Your Application

### Local Development with .NET Aspire

Momentum applications work seamlessly with .NET Aspire for local development:

```bash
# Start the complete application stack
dotnet run --project src/AppDomain.AppHost
```

This automatically starts:

-   **Your API service** with hot reload
-   **PostgreSQL database** with automatic migrations
-   **Apache Kafka** for event messaging
-   **Aspire Dashboard** for monitoring and debugging

> **Tip**: Open the Aspire Dashboard at `https://localhost:18110` to view logs, traces, and metrics in real-time.

### Manual Development Setup

If not using Aspire, start dependencies manually:

```bash
# Start database and Kafka with Docker Compose
docker compose up postgres kafka -d

# Run database migrations
dotnet run --project infra/AppDomain.Database.Migrations

# Start your API
dotnet run --project src/AppDomain.Api
```

### Database Management

```bash
# Reset database
docker compose down -v && docker compose up AppDomain-db AppDomain-db-migrations

# Access database
# Connect to localhost:54320 with credentials postgres/password@
```

### Testing

Run your tests with:

```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/AppDomain.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Next Steps

Now that you have a working Momentum application, explore these key areas:

### Essential Reading

1. **[CQRS Patterns](./cqrs/)** - Master commands, queries, and handlers
2. **[Database Integration](./database/)** - Learn the DbCommand pattern and entity mapping
3. **[Error Handling](./error-handling)** - Understand the Result pattern and validation

### Advanced Topics

4. **[Event-Driven Messaging](./messaging/)** - Deep dive into Kafka and integration events
5. **[Service Configuration](./service-configuration/)** - Configure observability and service defaults
6. **[Testing Strategies](./testing/)** - Comprehensive testing with Testcontainers

### Reference Materials

7. **[Best Practices](./best-practices)** - Production-ready patterns and guidelines
8. **[Troubleshooting Guide](./troubleshooting)** - Common issues and solutions
9. **[Architecture Overview](./arch/)** - System design and decision records

> **Quick Reference**: Bookmark the [Troubleshooting Guide](./troubleshooting) for common setup issues and solutions.

## Common Patterns

### Error Handling

Momentum uses the `Result<T>` pattern for error handling:

```csharp
public static async Task<Result<User>> Handle(GetUserQuery query, AppDb db, CancellationToken cancellationToken)
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == query.Id, cancellationToken);

    if (user is not null)
    {
        return user.ToModel(); // Success
    }

    return new List<ValidationFailure> { new("Id", "User not found") }; // Error
}
```

### Validation

FluentValidation is automatically integrated into the command pipeline:

```csharp
// Commands/CreateUser.cs (continued)
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Please provide a valid email address");
    }
}
```

> **Automatic Execution**: Validators run before command handlers. If validation fails, the handler never executes and validation errors are returned immediately.

### Configuration

Configure services in your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Momentum defaults (logging, telemetry, messaging, validation)
builder.AddServiceDefaults();

// Add your database
builder.AddDatabase<AppDb>();

// Add custom services
builder.Services.AddScoped<IYourService, YourService>();

var app = builder.Build();

// Configure middleware and endpoints
app.MapControllers();

await app.RunAsync(args);
```

## Troubleshooting Common Issues

### Quick Fixes

| Issue                           | Solution                                                                            |
| ------------------------------- | ----------------------------------------------------------------------------------- |
| **"Assembly not found" errors** | Verify `[DomainAssembly(typeof(IYourDomainAssembly))]` is added to your API project |
| **Database connection fails**   | Check connection string format and ensure database is running                       |
| **Event publishing fails**      | Verify Kafka is running and topics are being created automatically                  |
| **Validation not working**      | Ensure validators are in assemblies marked with `[DomainAssembly]`                  |
| **Commands not discovered**     | Check that your domain assembly is properly registered                              |

### Getting More Help

If you encounter issues not covered here, check the comprehensive [Troubleshooting Guide](./troubleshooting) which includes:

-   Detailed error diagnostics
-   Performance troubleshooting
-   Docker and deployment issues
-   Testing problems and solutions

You can also review the [Best Practices Guide](./best-practices) for recommended patterns and common pitfalls to avoid.

## Template Architecture

The template follows a microservices architecture with shared platform libraries:

```
.
├── docs/                            # VitePress documentation system
├── infra/                           # Infrastructure and database
│   └── [Domain].Database/             # Liquibase Database project
├── src/                             # Source code projects
│   ├── [Domain]/                      # Domain logic (customizable)
│   ├── [Domain].Api/                  # REST/gRPC endpoints
│   ├── [Domain].AppHost/              # .NET Aspire orchestration
│   ├── [Domain].BackOffice/           # Background processing
│   ├── [Domain].BackOffice.Orleans/   # Orleans stateful processing
│   └── [Domain].Contracts/            # Integration events and models
├── tests/                           # Testing projects
│   └── [Domain].Tests/                # Unit, Integration, and Architecture tests
└── libs/                            # Shared libraries
    └── Operations/                  # Operations libs
        ├── src/                     # Platform source code
        │   ├── Operations.Extensions.*
        │   ├── Operations.ServiceDefaults.*
        │   └── ...
        └── tests/                   # Platform tests
```

## What You Get Out of the Box

-   **🏗️ Entity Management**: Flexible data models with real-world business entity patterns
-   **⚙️ Workflow Processing**: Orleans-based stateful processing for complex business workflows
-   **📡 Event Integration**: Event-driven architecture with Kafka for cross-service communication
-   **🌐 Modern APIs**: REST and gRPC endpoints with OpenAPI documentation
-   **🧪 Comprehensive Testing**: Unit, integration, and architecture tests with real infrastructure
-   **📊 Observability**: Built-in logging, metrics, and distributed tracing

## Key Technologies

-   **.NET Aspire**: Application orchestration and service discovery
-   **Orleans**: Stateful actor-based processing for complex workflows
-   **Wolverine**: CQRS/MediatR-style command handling with Kafka integration
-   **PostgreSQL**: Primary database with Liquibase migrations
-   **Apache Kafka**: Event streaming and message bus
-   **gRPC + REST**: API protocols
-   **Testcontainers**: Integration testing with real infrastructure

## Quick Reference Commands

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

## Resources

-   [Architecture Overview](./arch/)
-   [API Reference](/reference/Momentum)
-   [Best Practices](./best-practices)
-   [Sample Applications](https://github.com/vgmello/momentum/tree/main/src)
