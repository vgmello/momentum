---
title: CQRS Implementation Pattern
---

# CQRS Implementation Pattern

This document outlines the Command Query Responsibility Segregation (CQRS) pattern implementation used in the AppDomain application. The design mirrors real-world department operations, making it intuitive for junior engineers and non-technical product people to understand.

## Philosophy

The CQRS implementation follows these core principles:

-   **Real-world department metaphor**: Each folder represents a sub-department with its main activities
-   **Minimal ceremony**: Avoiding unnecessary abstractions and complex patterns
-   **Infrastructure as utilities**: Database, messaging, and other infrastructure are treated like office utilities
-   **Digital paper records**: Immutable data structures that can't change themselves
-   **Front office/Back office separation**: Public APIs for front office, async handlers for back office
-   **Developer-friendly**: Simple patterns that are easy to understand and maintain
-   **LLM-friendly**: Familiar real-world patterns that modern AI can easily comprehend

## Domain Structure

Each domain (like Cashiers, Invoices, Bills) follows this consistent structure:

</code></pre>
src/AppDomain/Cashiers/
├── Commands/ # Write operations (what the department does)
├── Queries/ # Read operations (what the department knows)
├── Contracts/ # Public interfaces and models
│ ├── Models/ # Data structures
│ └── IntegrationEvents/ # Inter-department notifications
└── Data/
└── Entities/ # Database representations
</code></pre>

## Command Pattern

Commands represent work orders sent to departments. They follow a simple, consistent pattern.

### Command Structure

<<< @/../src/AppDomain/Cashiers/Commands/CreateCashier.cs{10-10}

### Command Validation

<<< @/../src/AppDomain/Cashiers/Commands/CreateCashier.cs{12-20}

### Command Handler

<pre v-pre class="language-csharp"><code>
public static partial class CreateCashierCommandHandler
{
    [DbCommand(sp: "AppDomain.cashiers_create", nonQuery: true)]
    public partial record DbCommand(Guid CashierId, string Name, string? Email) : ICommand&lt;int&gt;;

    public static async Task&lt;(Result&lt;CashierModel&gt;, CashierCreated?)&gt; Handle(CreateCashierCommand command, IMessageBus messaging,
        CancellationToken cancellationToken)
    {
        if (command.Name.Contains("error"))
        {
            throw new DivideByZeroException("Forced test unhandled exception to simulate error scenarios");
        }

        var cashierId = Guid.CreateVersion7();

        var insertCommand = new DbCommand(cashierId, command.Name, command.Email);

        await messaging.InvokeCommandAsync(insertCommand, cancellationToken);

        var result = new CashierModel
        {
            TenantId = command.TenantId,
            CashierId = cashierId,
            Name = command.Name,
            Email = command.Email
        };

        var createdEvent = new CashierCreated(result.TenantId, PartitionKeyTest: 0, result);

        return (result, createdEvent);
    }
}
</code></pre>

### Database Command

<pre v-pre class="language-csharp"><code>
[DbCommand(sp: "AppDomain.cashiers_create", nonQuery: true)]
public partial record DbCommand(Guid CashierId, string Name, string? Email) : ICommand&lt;int&gt;;
</code></pre>

## Query Pattern

Queries represent information requests to departments. They focus on data retrieval without side effects.

### Query Structure

<pre v-pre class="language-csharp"><code>
public record GetCashiersQuery : IQuery&lt;IEnumerable&lt;GetCashiersQuery.Result&gt;&gt;
{
    [Range(1, 1000)]
    public int Limit { get; set; } = 1000;

    [Range(0, int.MaxValue)]
    public int Offset { get; set; }

    public record Result(Guid TenantId, Guid CashierId, string Name, string Email);
}
</code></pre>

### Query Handler

<pre v-pre class="language-csharp"><code>
public static partial class GetCashiersQueryHandler
{
    [DbCommand]
    private sealed partial record DbCommand(int Limit, int Offset);

    public static async Task&lt;IEnumerable&lt;GetCashiersQuery.Result&gt;&gt; Handle(GetCashiersQuery query, NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = """
                               SELECT null::uuid as tenant_id, cashier_id, name, email
                               FROM AppDomain.cashiers
                               LIMIT @limit OFFSET @offset
                           """;

        var cashiers = await connection.QueryAsync&lt;GetCashiersQuery.Result&gt;(sql, new DbCommand(query.Limit, query.Offset).ToDbParams());

        return cashiers;
    }
}
</code></pre>

### Database Query

<pre v-pre class="language-csharp"><code>
[DbCommand]
private sealed partial record DbCommand(int Limit, int Offset);
</code></pre>

## Models and Contracts

### Domain Models

Models represent the "digital paper records" that departments work with:

<pre v-pre class="language-csharp"><code>
public record Cashier
{
    public Guid TenantId { get; set; }

    public Guid CashierId { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public List&lt;CashierPayment&gt; CashierPayments { get; set; } = [];
}
</code></pre>

### Data Entities

Database entities represent how records are stored:

<pre v-pre class="language-csharp"><code>
public record Cashier : Entity
{
    public Guid CashierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}
</code></pre>

## Integration Events

Events are notifications sent between departments when important things happen.

### Event Structure

<pre v-pre class="language-csharp"><code>
/// &lt;summary&gt;
///     Published when a new cashier is successfully created in the AppDomain system. This event contains the complete cashier data and partition
///     key information for proper message routing.
/// &lt;/summary&gt;
/// &lt;remarks&gt;
///     - The cashier creation process completes successfully
///     - Some other actions
/// &lt;/remarks&gt;
[EventTopic&lt;Cashier&gt;]
public record CashierCreated([PartitionKey] Guid TenantId, [PartitionKey] int PartitionKeyTest, Cashier Cashier);
</code></pre>

### Event Publishing

Events are returned from handlers but not directly published:

<pre v-pre class="language-csharp"><code>
// In handler - return event to be published
var createdEvent = new CashierCreated(command.TenantId, command.TenantId.GetHashCode(), result.Value);
return (result, createdEvent);
</code></pre>

## API Integration

Controllers act as the front office reception desk, handling external requests.

### Controller Pattern

<pre v-pre class="language-csharp"><code>
/// &lt;returns&gt;The cashier details if found&lt;/returns&gt;
/// &lt;response code="200" /&gt;
/// &lt;response code="404"&gt;If the cashier is not found&lt;/response&gt;
/// &lt;response code="400"&gt;If the provided ID is invalid&lt;/response&gt;
[HttpGet("{id:guid}")]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType&lt;object&gt;(StatusCodes.Status400BadRequest)]
public async Task&lt;ActionResult&lt;Cashier&gt;&gt; GetCashier([FromRoute] Guid id, CancellationToken cancellationToken)
{
    var cashier = await bus.InvokeQueryAsync(new GetCashierQuery(id), cancellationToken);

    return cashier;
}

/// &lt;summary&gt;
///     Retrieves a list of cashiers with optional filtering
/// &lt;/summary&gt;
/// &lt;param name="query"&gt;Query parameters for filtering and pagination&lt;/param&gt;
/// &lt;param name="cancellationToken"&gt;Cancellation token&lt;/param&gt;
/// &lt;returns&gt;A list of cashiers matching the specified criteria&lt;/returns&gt;
</code></pre>

### Result Handling

<pre v-pre class="language-csharp"><code>
/// &lt;response code="400"&gt;If query parameters are invalid&lt;/response&gt;
[HttpGet]
[ProducesResponseType&lt;object&gt;(StatusCodes.Status400BadRequest)]
public async Task&lt;ActionResult&lt;IEnumerable&lt;GetCashiersQuery.Result&gt;&gt;&gt; GetCashiers([FromQuery] GetCashiersQuery query,
    CancellationToken cancellationToken)
{
    var cashiers = await bus.InvokeQueryAsync(query, cancellationToken);

    return Ok(cashiers);
}

/// &lt;summary&gt;
///     Creates a new cashier in the system
/// &lt;/summary&gt;
/// &lt;param name="command"&gt;The cashier creation request containing name and email&lt;/param&gt;
/// &lt;param name="cancellationToken"&gt;Cancellation token&lt;/param&gt;
/// &lt;returns&gt;The created cashier details&lt;/returns&gt;
/// &lt;response code="201" /&gt;
/// &lt;response code="400"&gt;If the request data is invalid or validation fails&lt;/response&gt;
/// &lt;response code="409"&gt;If a cashier with the same email already exists&lt;/response&gt;
</code></pre>

## Database Interaction

Database operations use the `[DbCommand]` attribute for automatic code generation.

### Command Database Operations

<pre v-pre class="language-csharp"><code>
[DbCommand(sp: "AppDomain.cashiers_create", nonQuery: true)]
public partial record InsertCashierCommand(
    Guid TenantId,
    Guid CashierId,
    string Name,
    string Email
) : ICommand&lt;int&gt;;
</code></pre>

### Query Database Operations

<pre v-pre class="language-csharp"><code>
[DbCommand(fn: "SELECT * FROM AppDomain.cashiers_get")]
public partial record GetCashiersDbQuery(
    int Limit,
    int Offset
) : IQuery&lt;IEnumerable&lt;CashierModel&gt;&gt;;
</code></pre>

## Error Handling

The system uses the `Result&lt;T&gt;` pattern for explicit error handling without exceptions.

### Result Pattern

<pre v-pre class="language-csharp"><code>
// Success case
return Result.Success(cashier);

// Failure case
return Result.Failure&lt;CashierModel&gt;(new ValidationFailure("Name", "Name is required"));
</code></pre>

### Validation Pattern

<pre v-pre class="language-csharp"><code>
public class CreateCashierValidator : AbstractValidator&lt;CreateCashierCommand&gt;
{
    public CreateCashierValidator()
    {
        RuleFor(c =&gt; c.Name).NotEmpty();
        RuleFor(c =&gt; c.Name).MaximumLength(100);
        RuleFor(c =&gt; c.Name).MinimumLength(2);
    }
}
</code></pre>

## Testing Strategy

### Unit Testing

Tests focus on business logic without infrastructure concerns:

<pre v-pre class="language-csharp"><code>[Test]
public async Task Handle_Should_CreateCashier_When_ValidCommand()
{
    // Arrange
    var command = new CreateCashierCommand(Guid.NewGuid(), "John Doe", "john@example.com");
    var messaging = Substitute.For&lt;IMessageBus&gt;();

    // Act
    var (result, integrationEvent) = await CreateCashierCommandHandler.Handle(command, messaging, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("John Doe");
    result.Email.Should().Be("john@example.com");
    integrationEvent.Should().NotBeNull();
}
</code></pre>

### Integration Testing

Integration tests verify complete request/response cycles:

<pre v-pre class="language-csharp"><code>[Test]
public async Task CreateCashier_Should_ReturnCreatedResult_When_ValidRequest()
{
    // Arrange
    var request = new CreateCashierCommand(Guid.NewGuid(), "Jane Smith", "jane@example.com");

    // Act
    var response = await Client.PostAsJsonAsync("/api/v1/cashiers", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var cashier = await response.Content.ReadFromJsonAsync&lt;Cashier&gt;();
    cashier.Should().NotBeNull();
    cashier.Name.Should().Be("Jane Smith");
}
</code></pre>

## Message Bus Integration

The message bus acts as the internal communication system between departments.

### Command Execution

<pre v-pre class="language-csharp"><code>
// Execute command
var result = await messaging.InvokeCommandAsync(command, cancellationToken);

// Execute query
var data = await messaging.InvokeQueryAsync(query, cancellationToken);
</code></pre>

### Event Publishing

<pre v-pre class="language-csharp"><code>
// Events are automatically published when returned from handlers
return (result, new CashierCreated(tenantId, partitionKey, cashier));
</code></pre>

## Back Office Processing

Back office operations handle asynchronous processing through event handlers.

### Event Handler Pattern

<pre v-pre class="language-csharp"><code>
public class CashierCreatedHandler : IDistributedEventHandler&lt;CashierCreated&gt;
{
    public async Task Handle(CashierCreated integrationEvent, CancellationToken cancellationToken)
    {
        // Handle the cashier created event in the back office
        // This could include updating read models, sending notifications, etc.
        await ProcessCashierCreatedAsync(integrationEvent.Cashier, cancellationToken);
    }

    private async Task ProcessCashierCreatedAsync(Cashier cashier, CancellationToken cancellationToken)
    {
        // Back office processing logic
        Console.WriteLine($"Processing cashier created: {cashier.Name}");
    }
}
</code></pre>

## Best Practices

### 1. Command Design

-   **Single responsibility**: Each command does one thing
-   **Immutable**: Use record types for command definitions
-   **Validated**: Include validation rules close to the command
-   **Explicit results**: Return Result&lt;T&gt; instead of throwing exceptions

### 2. Query Design

-   **Read-only**: Queries should never modify data
-   **Paginated**: Always implement pagination for list queries
-   **Filtered**: Support filtering parameters where appropriate
-   **Cached**: Consider caching for frequently accessed data

### 3. Handler Design

-   **Static methods**: Use static handlers for better performance
-   **Dependency injection**: Inject dependencies through parameters
-   **Transactional**: Commands should be transactional
-   **Event generation**: Return events for important state changes

### 4. Model Design

-   **Immutable**: Use record types or readonly properties
-   **Validated**: Include validation attributes on models
-   **Mapped**: Separate API models from database entities
-   **Documented**: Include XML documentation for public APIs

### 5. Database Design

-   **Stored procedures**: Use stored procedures for complex operations
-   **Functions**: Use functions for read operations
-   **Parameters**: Use parameterized queries to prevent SQL injection
-   **Indexes**: Add appropriate indexes for query performance

## Anti-Patterns to Avoid

### ❌ Don't Do This

<pre v-pre class="language-csharp"><code>
// Complex inheritance hierarchies
public abstract class BaseCommandHandler&lt;TCommand, TResult&gt; { }

// Mutable commands
public class CreateCashierCommand
{
    public string Name { get; set; }  // Mutable!
}

// Throwing exceptions for business logic
public void Handle(CreateCashierCommand command)
{
    if (string.IsNullOrEmpty(command.Name))
        throw new ArgumentException("Name is required");  // Don't throw!
}
</code></pre>

### ✅ Do This Instead

<pre v-pre class="language-csharp"><code>
// Simple static handlers
public static class CreateCashierCommandHandler
{
    public static async Task&lt;(Result&lt;CashierModel&gt;, CashierCreated)&gt; Handle(...)
}

// Immutable commands
public record CreateCashierCommand(
    string Name,
    string Email
) : ICommand&lt;Result&lt;CashierModel&gt;&gt;;

// Result pattern for business logic
public static async Task&lt;Result&lt;CashierModel&gt;&gt; Handle(...)
{
    if (string.IsNullOrEmpty(command.Name))
        return Result.Failure&lt;CashierModel&gt;(new ValidationFailure("Name", "Name is required"));
}
</code></pre>

## Implementation Checklist

When implementing a new domain, follow this checklist:

### Commands

-   [ ] Create command record with validation
-   [ ] Implement static handler method
-   [ ] Add database command with `[DbCommand]` attribute
-   [ ] Return Result&lt;T&gt; and integration event
-   [ ] Add unit tests for handler logic

### Queries

-   [ ] Create query record with parameters
-   [ ] Implement static handler method
-   [ ] Add database query with `[DbCommand]` attribute
-   [ ] Return appropriate data types
-   [ ] Add unit tests for query logic

### Contracts

-   [ ] Define domain models in Contracts/Models
-   [ ] Create integration events in Contracts/IntegrationEvents
-   [ ] Add XML documentation for public APIs
-   [ ] Version contracts appropriately

### API

-   [ ] Create controller with thin wrappers
-   [ ] Use message bus for all operations
-   [ ] Implement proper HTTP status codes
-   [ ] Add API documentation

### Database

-   [ ] Create stored procedures for commands
-   [ ] Create functions for queries
-   [ ] Add appropriate indexes
-   [ ] Include database migration scripts

### Testing

-   [ ] Unit tests for all handlers
-   [ ] Integration tests for API endpoints
-   [ ] Test both success and failure scenarios
-   [ ] Include performance tests for critical paths

## Conclusion

This CQRS implementation prioritizes simplicity, maintainability, and developer productivity. By following real-world department metaphors and avoiding unnecessary abstractions, the codebase remains approachable for developers of all skill levels while maintaining the benefits of CQRS architecture.

The pattern ensures:

-   Clear separation between reads and writes
-   Scalable message-based communication
-   Robust error handling
-   Comprehensive testing support
-   Easy integration with external systems

Use this guide as a reference when implementing new domains or extending existing functionality in the AppDomain system.
