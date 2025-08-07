---
title: Architecture Overview
description: Understand Momentum's architectural philosophy, design patterns, and core principles for building scalable, maintainable .NET services.
date: 2024-01-15
---

# Architecture Overview

Momentum .NET embodies a distinctive architectural philosophy that balances structure with flexibility. Like **Shadcn/ui** for React components, it provides proven patterns and pre-configured building blocks that you can copy, customize, and own completely.

## Core Philosophy: Real-World Mirroring

### Real-World Business Operations

This template is intentionally structured to mirror real-world business operations and organizational structures. Each part of the code corresponds or should correspond directly to a real-world role or operation, ensuring that the code remains 100% product-oriented and easy to understand.

**Real-World Mirroring**: Every folder, class, and method corresponds directly to business operations

-   `Commands/` = Actions your business performs
-   `Queries/` = Information your business retrieves
-   `Events/` = Things that happen in your business

**No Smart Objects**: Entities are data records, not self-modifying objects

-   Infrastructure elements support functionality like utilities in an office
-   Front office = Synchronous APIs (immediate responses)
-   Back office = Asynchronous processing (background work)

### Avoiding Unnecessary Abstractions

This design philosophy avoids unnecessary abstractions. There are no additional layers like repositories or services unless they represent something that exists in the real business. Infrastructure elements like logging or authorization are present as they support the system's functionality, same as water pipes and electricity support a business office. Even the database is viewed as a digital parallel to a real-world archive or filing system.

### No "Domain" Objects

A key principle is the absence of smart objects. This means that a business entity, for example, is not an object that can change itself. Instead, it is simply treated as a digital record, and all modifications are performed by "external" actors (something is changing the record, the record does not change itself). This ensures that the code reflects digital representations of real-world entities and processes, rather than trying to replicate objects with their own behaviors.

### Synchronous and Asynchronous Operations

The template also distinguishes between synchronous and asynchronous operations. The API represents the front office of your business, handling synchronous operations where immediate responses are expected. In contrast, the back office is represented by asynchronous operations that do not require immediate responses, allowing for efficient, behind-the-scenes processing.

## Architectural Philosophy

### Template-Driven Development

Momentum operates as an **opinionated template system** rather than a traditional framework:

-   **Code Ownership**: Import source code directly into your project for full control
-   **Package Flexibility**: Use NuGet packages for managed dependencies
-   **Customization**: Adapt patterns to your specific domain requirements
-   **Evolution**: Modify and extend patterns as your application grows

This approach provides the benefits of proven architectural patterns while maintaining the flexibility to evolve your codebase organically.

### Product-Oriented Design

Services in Momentum mirror **real-world business operations**:

-   **Business Capability Alignment**: Each service corresponds to a distinct business capability
-   **Feature-Oriented Structure**: Code organization reflects product features, not technical layers
-   **Minimal Ceremony**: Reduced boilerplate that focuses on business value
-   **Domain-Driven**: Architecture emerges from domain requirements, not technical constraints

## Core Architectural Patterns

### CQRS (Command Query Responsibility Segregation)

Commands and queries are explicitly separated:

```csharp
// Commands: Change state and return results
public record CreateUserCommand(string Name, string Email) : ICommand<Result<User>>;

// Queries: Read data without side effects
public record GetUserQuery(Guid Id) : IQuery<Result<User>>;
```

**Benefits:**

-   Clear intention and responsibility
-   Independent scaling of read vs write operations
-   Simplified testing and validation
-   Explicit business operation contracts

### Event-Driven Architecture

Integration events enable loosely coupled service communication:

```csharp
/// <summary>
/// Published when a new user is successfully created in the system.
/// </summary>
[EventTopic<User>]
public record UserCreated(
    [PartitionKey] Guid TenantId,
    User User
);
```

**Key characteristics:**

-   **Asynchronous Processing**: Non-blocking service interactions
-   **Event Sourcing**: Complete audit trail of business operations
-   **Scalability**: Services can independently process events
-   **Resilience**: Failure in one service doesn't cascade to others

### Database Command Pattern

Type-safe database operations with source generation:

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

**Advantages:**

-   **Type Safety**: Compile-time verification of database operations
-   **Performance**: Source-generated commands eliminate reflection
-   **Testability**: Database operations are easily mockable
-   **Maintainability**: Clear separation of business and data logic

## Object Model Architecture

### Layered Object Responsibilities

Momentum defines clear object types with distinct responsibilities:

#### 1. Public API Layer

-   **API Requests/Responses**: External service contracts
-   **Transformation**: Convert between external and internal representations
-   **Validation**: Basic input validation and formatting

#### 2. Business Logic Layer

-   **Action Contracts**: Commands and queries representing business operations
-   **Handlers**: Business logic implementation
-   **Validation**: Domain-specific business rules

#### 3. Data Access Layer

-   **Entities**: Database table representations
-   **DbCommands**: Type-safe database operations
-   **Mapping**: Entity to model transformations

#### 4. Integration Layer

-   **Integration Events**: Cross-service communication contracts
-   **Public Models**: Shareable domain representations
-   **Event Handlers**: Process incoming integration events

### Information Flow

The typical flow through these layers follows a predictable pattern:

```
External Client
    � HTTP/gRPC Request
API Layer (Controllers/Services)
    � Action Contract
Business Logic Layer (Handlers)
    � DbCommand
Data Access Layer (Database)
    � Integration Event
Message Bus (Kafka)
    � Event
Other Services
```

This layered approach ensures:

-   **Clear boundaries** between responsibilities
-   **Independent testing** of each layer
-   **Flexible evolution** without breaking contracts
-   **Scalable architecture** that supports growth

## Service Architecture Patterns

### API Services (Front Office)

Handle synchronous request/response operations:

-   **REST/gRPC endpoints** for external clients
-   **Input validation** and request transformation
-   **Business operation coordination** via commands/queries
-   **Response formatting** and error handling

### Background Services (Back Office)

Process asynchronous operations and events:

-   **Event processing** from message queues
-   **Long-running tasks** and scheduled jobs
-   **Integration with external systems**
-   **Batch processing operations**

### Data Services

Manage persistence and data operations:

-   **Database schema management** with migrations
-   **Type-safe query operations** with LinqToDB
-   **Transaction management** and data consistency
-   **Read/write optimization** strategies

## Scalability Considerations

### Horizontal Scaling

Services are designed for independent scaling:

-   **Stateless design** enables easy replication
-   **Database separation** prevents resource contention
-   **Event partitioning** distributes processing load
-   **Container-ready** for orchestration platforms

### Performance Optimization

Built-in patterns support high-performance operations:

-   **Source generation** eliminates runtime reflection
-   **Connection pooling** and efficient resource usage
-   **Async operations** prevent thread blocking
-   **Caching strategies** reduce external dependency calls

### Resilience Patterns

Architecture supports fault tolerance:

-   **Circuit breaker** patterns for external dependencies
-   **Retry policies** with exponential backoff
-   **Dead letter queues** for failed message processing
-   **Health checks** for service monitoring

## Testing Architecture

### Unit Testing

Focused testing of business logic:

-   **Handler testing** with mocked dependencies
-   **Validation testing** with various input scenarios
-   **Result pattern testing** for error conditions
-   **Entity mapping testing** for data transformations

### Integration Testing

End-to-end testing with real infrastructure:

-   **Testcontainers** for database and messaging infrastructure
-   **API testing** with HTTP clients
-   **Event processing testing** with message publishing
-   **Database operation testing** with real queries

## Configuration and Observability

### Service Defaults

Pre-configured observability and operational concerns:

-   **Structured logging** with correlation IDs
-   **OpenTelemetry integration** for distributed tracing
-   **Health checks** for service monitoring
-   **Metrics collection** for performance monitoring

### Configuration Management

Environment-specific configuration:

-   **appsettings.json** for environment variables
-   **Service discovery** for inter-service communication
-   **Feature flags** for gradual rollouts
-   **Secret management** for sensitive configuration

## Architecture Decision Records

Major architectural decisions are documented and tracked:

-   **Template vs Framework Choice**: Why we chose a template approach
-   **CQRS Implementation**: Command/query separation patterns
-   **Event-Driven Design**: Integration event patterns and topics
-   **Database Strategy**: LinqToDB and migration approaches
-   **Testing Strategy**: Unit vs integration testing boundaries

## Best Practices

### Development Guidelines

-   **Feature-driven organization**: Structure code around business capabilities
-   **Explicit contracts**: Clear interfaces between layers
-   **Fail-fast validation**: Early validation prevents invalid state
-   **Immutable models**: Reduce complexity and improve predictability

### Operational Guidelines

-   **Monitoring and alerting**: Comprehensive observability
-   **Graceful degradation**: Service resilience under failure
-   **Database migration safety**: Zero-downtime deployment strategies
-   **Event schema evolution**: Backward-compatible event changes

## Next Steps

To dive deeper into specific architectural areas:

-   **[CQRS Patterns](../cqrs/)** - Commands, queries, and handlers
-   **[Database Architecture](../database/)** - DbCommand pattern and entity design
-   **[Messaging Architecture](../messaging/)** - Event-driven patterns and Kafka integration
-   **[Testing Architecture](../testing/)** - Unit and integration testing strategies
-   **[Service Configuration](../service-configuration/)** - Observability and operational patterns

For practical implementation guidance, see the [Getting Started Guide](../getting-started) and [Best Practices](../best-practices).
