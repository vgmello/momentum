# DbCommand Attribute Examples

## Basic Stored Procedure Command

```csharp
[DbCommand(sp: "create_user")]
public record CreateUserCommand(string Name, string Email) : ICommand<int>;

// Generated usage:
var command = new CreateUserCommand("John Doe", "john@example.com");
var userId = await CreateUserCommandHandler.HandleAsync(command, dataSource, cancellationToken);
```

## SQL Query with Custom Parameter Names

```csharp
[DbCommand(sql: "SELECT * FROM users WHERE created_date >= @from_date AND status = @user_status", 
          paramsCase: DbParamsCase.SnakeCase)]
public record GetRecentUsersQuery(
    [Column("from_date")] DateTime Since,
    [Column("user_status")] string Status) : IQuery<IEnumerable<User>>;

// Generated ToDbParams() creates: { from_date = Since, user_status = Status }
```

## Database Function Call

```csharp
[DbCommand(fn: "$get_user_orders")]
public record GetUserOrdersQuery(int UserId, bool IncludeInactive = false) : IQuery<IEnumerable<Order>>;

// Generated SQL: "SELECT * FROM get_user_orders(@UserId, @IncludeInactive)"
```

## Non-Query Command (INSERT/UPDATE/DELETE)

```csharp
[DbCommand(sql: "UPDATE users SET last_login = CURRENT_TIMESTAMP WHERE id = @UserId", nonQuery: true)]
public record UpdateLastLoginCommand(int UserId) : ICommand<int>;

// Returns number of affected rows
```

## Repository Integration Pattern

```csharp
public class UserRepository
{
    private readonly DbDataSource _dataSource;
    
    public UserRepository(DbDataSource dataSource) => _dataSource = dataSource;
    
    public async Task<int> CreateUserAsync(string name, string email, CancellationToken ct = default)
    {
        var command = new CreateUserCommand(name, email);
        return await CreateUserCommandHandler.HandleAsync(command, _dataSource, ct);
    }
    
    public async Task<IEnumerable<User>> GetRecentUsersAsync(DateTime since, string status, CancellationToken ct = default)
    {
        var query = new GetRecentUsersQuery(since, status);
        return await GetRecentUsersQueryHandler.HandleAsync(query, _dataSource, ct);
    }
}
```

## Dependency Injection with Named Data Sources

```csharp
[DbCommand(sp: "archive_old_orders", dataSource: "ArchiveDatabase")]
public record ArchiveOldOrdersCommand(DateTime OlderThan) : ICommand<int>;

// Generated handler will resolve keyed service: [FromKeyedServices("ArchiveDatabase")] DbDataSource dataSource
```