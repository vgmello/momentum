# Getting Started with Momentum

Momentum is a highly opinionated .NET template designed for building scalable, event-driven microservices. This guide will help you get up and running quickly with Momentum's core concepts and patterns.

## What is Momentum?

Momentum provides a comprehensive set of pre-configured libraries and patterns for:

- **CQRS (Command Query Responsibility Segregation)** - Clean separation of read and write operations
- **Event-Driven Architecture** - Integration and domain events with Kafka messaging
- **Database Integration** - Type-safe database operations with LinqToDB
- **Service Defaults** - Pre-configured logging, telemetry, health checks, and more
- **Testing Support** - Built-in patterns for unit and integration testing

## Quick Start

### Prerequisites

- .NET 9.0 SDK or later
- Docker and Docker Compose
- Your favorite IDE (Visual Studio, VS Code, or JetBrains Rider)

### Creating Your First Project

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

### Domain Assembly Setup

Mark your domain assemblies so Momentum can discover them:

```csharp
using Momentum.ServiceDefaults;
using YourApp.Domain;

[assembly: DomainAssembly(typeof(IYourDomainAssembly))]
```

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
/// <summary>
/// Published when a new user is successfully created.
/// </summary>
[EventTopic<User>]
public record UserCreated(
    [PartitionKey] Guid TenantId,
    User User
);
```

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

## Running Your Application

### Local Development

Use the .NET Aspire AppHost for local development:

```bash
dotnet run --project src/YourApp.AppHost
```

This starts:
- Your API service
- Database (PostgreSQL)
- Message broker (Kafka)
- All configured dependencies

### Database Migrations

Momentum uses Liquibase for database migrations:

```bash
# Run migrations
docker compose up your-app-db-migrations

# Reset database (for development)
docker compose down -v && docker compose up your-app-db your-app-db-migrations
```

### Testing

Run your tests with:

```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/YourApp.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Next Steps

Now that you have the basics working, explore these areas:

1. **[CQRS Patterns](./cqrs/)** - Deep dive into commands, queries, and handlers
2. **[Messaging & Events](./messaging/)** - Learn about integration events and Kafka configuration
3. **[Database Integration](./database/)** - Master the DbCommand pattern and entity mapping
4. **[Service Configuration](./service-configuration/)** - Understand ServiceDefaults and observability
5. **[Testing](./testing/)** - Learn unit and integration testing patterns

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

FluentValidation is integrated into the command pipeline:

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);
            
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

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

## Troubleshooting

### Common Issues

**"Assembly not found" errors:** Make sure you've marked your domain assemblies with `[DomainAssembly]`.

**Database connection issues:** Verify your connection strings and ensure migrations have run.

**Event publishing fails:** Check Kafka configuration and ensure topics are created.

**Validation not working:** Ensure validators are in assemblies marked with `[DomainAssembly]`.

For more detailed troubleshooting, see our [Troubleshooting Guide](./troubleshooting).

## Resources

- [Architecture Overview](./arch/)
- [API Reference](/reference/Momentum)
- [Best Practices](./best-practices)
- [Sample Applications](https://github.com/vgmello/momentum/tree/main/src)