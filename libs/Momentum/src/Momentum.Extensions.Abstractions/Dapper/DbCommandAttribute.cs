// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.Abstractions.Dapper;

/// <summary>
///     Marks a class for database command code generation, creating efficient data access patterns with minimal boilerplate.
/// </summary>
/// <param name="sp">The name of the stored procedure to execute. Mutually exclusive with <paramref name="sql" /> and <paramref name="fn" />.</param>
/// <param name="sql">The SQL query text to execute. Mutually exclusive with <paramref name="sp" /> and <paramref name="fn" />.</param>
/// <param name="fn">
///     The database function name to call. Parameters are auto-generated from record properties. Mutually exclusive with
///     <paramref name="sp" /> and <paramref name="sql" />. Use '$' prefix (e.g., "$get_user_orders") to generate "SELECT * FROM get_user_orders(...)" syntax.
/// </param>
/// <param name="paramsCase">Specifies how property names are converted to database parameter names. Defaults to global MSBuild configuration.</param>
/// <param name="nonQuery">
///     Controls the execution strategy for database commands. Default is false.
///     <para>
///         If true: Uses Dapper's ExecuteAsync() for commands returning row counts (INSERT/UPDATE/DELETE operations).
///         If false: Uses appropriate query methods (QueryAsync, QueryFirstOrDefaultAsync, ExecuteScalarAsync) based on the return type.
///     </para>
/// </param>
/// <param name="dataSource">The keyed data source name for dependency injection. If null, uses the default registered data source.</param>
/// <remarks>
/// <para><strong>Generated Code Behavior:</strong></para>
/// <para>This attribute triggers the source generator to create:</para>
/// <list type="number">
///   <item><strong>ToDbParams() Extension Method:</strong> Converts class properties to Dapper-compatible parameter objects</item>
///   <item><strong>Command Handler Method:</strong> Static async method that executes the database command (when sp/sql/fn provided)</item>
/// </list>
/// 
/// <para><strong>Command Handler Generation:</strong></para>
/// <para>Handlers are generated as static methods in a companion class (e.g., CreateUserCommandHandler.HandleAsync) that:</para>
/// <list type="bullet">
///   <item>Accept the command object, DbDataSource, and CancellationToken</item>
///   <item>Open database connections automatically</item>
///   <item>Map parameters using the generated ToDbParams() method</item>
///   <item>Execute appropriate Dapper methods based on return type and nonQuery setting</item>
///   <item>Handle connection disposal and async patterns correctly</item>
/// </list>
/// 
/// <para><strong>Parameter Mapping Rules:</strong></para>
/// <list type="bullet">
///   <item>Record properties and primary constructor parameters are automatically mapped</item>
///   <item>Parameter names follow the paramsCase setting (None, SnakeCase, or global default)</item>
///   <item>Use [Column("custom_name")] attribute to override specific parameter names</item>
///   <item>MSBuild property DbCommandParamPrefix adds global prefixes to all parameters</item>
/// </list>
/// 
/// <para><strong>Return Type Handling:</strong></para>
/// <list type="bullet">
///   <item><c>ICommand&lt;int/long&gt;</c>: Returns row count (ExecuteAsync) or scalar value (ExecuteScalarAsync)</item>
///   <item><c>ICommand&lt;TResult&gt;</c>: Returns single object (QueryFirstOrDefaultAsync&lt;TResult&gt;)</item>
///   <item><c>ICommand&lt;IEnumerable&lt;TResult&gt;&gt;</c>: Returns collection (QueryAsync&lt;TResult&gt;)</item>
///   <item><c>ICommand</c> (no return type): Executes command without returning data (ExecuteAsync)</item>
/// </list>
/// 
/// <para><strong>MSBuild Integration:</strong></para>
/// <para>Global configuration through MSBuild properties:</para>
/// <list type="bullet">
///   <item><c>DbCommandDefaultParamCase</c>: Sets default parameter case conversion (None, SnakeCase)</item>
///   <item><c>DbCommandParamPrefix</c>: Adds prefix to all generated parameter names</item>
/// </list>
/// 
/// <para><strong>Requirements:</strong></para>
/// <list type="bullet">
///   <item>Target class must implement ICommand&lt;TResult&gt; or IQuery&lt;TResult&gt; (or parameterless versions)</item>
///   <item>Class must be partial if nested within another type</item>
///   <item>Only one of sp, sql, or fn can be specified per command</item>
///   <item>Assembly must reference Momentum.Extensions.SourceGenerators</item>
/// </list>
/// </remarks>
/// <example>
/// <para><strong>Basic Stored Procedure Command:</strong></para>
/// <code>
/// [DbCommand(sp: "create_user")]
/// public record CreateUserCommand(string Name, string Email) : ICommand&lt;int&gt;;
/// 
/// // Generated usage:
/// var command = new CreateUserCommand("John Doe", "john@example.com");
/// var userId = await CreateUserCommandHandler.HandleAsync(command, dataSource, cancellationToken);
/// </code>
/// 
/// <para><strong>SQL Query with Custom Parameter Names:</strong></para>
/// <code>
/// [DbCommand(sql: "SELECT * FROM users WHERE created_date >= @from_date AND status = @user_status", 
///           paramsCase: DbParamsCase.SnakeCase)]
/// public record GetRecentUsersQuery(
///     [Column("from_date")] DateTime Since,
///     [Column("user_status")] string Status) : IQuery&lt;IEnumerable&lt;User&gt;&gt;;
/// 
/// // Generated ToDbParams() creates: { from_date = Since, user_status = Status }
/// </code>
/// 
/// <para><strong>Database Function Call:</strong></para>
/// <code>
/// [DbCommand(fn: "$get_user_orders")]
/// public record GetUserOrdersQuery(int UserId, bool IncludeInactive = false) : IQuery&lt;IEnumerable&lt;Order&gt;&gt;;
/// 
/// // Generated SQL: "SELECT * FROM get_user_orders(@UserId, @IncludeInactive)"
/// </code>
/// 
/// <para><strong>Non-Query Command (INSERT/UPDATE/DELETE):</strong></para>
/// <code>
/// [DbCommand(sql: "UPDATE users SET last_login = CURRENT_TIMESTAMP WHERE id = @UserId", nonQuery: true)]
/// public record UpdateLastLoginCommand(int UserId) : ICommand&lt;int&gt;;
/// 
/// // Returns number of affected rows
/// </code>
/// 
/// <para><strong>Repository Integration Pattern:</strong></para>
/// <code>
/// public class UserRepository
/// {
///     private readonly DbDataSource _dataSource;
///     
///     public UserRepository(DbDataSource dataSource) => _dataSource = dataSource;
///     
///     public async Task&lt;int&gt; CreateUserAsync(string name, string email, CancellationToken ct = default)
///     {
///         var command = new CreateUserCommand(name, email);
///         return await CreateUserCommandHandler.HandleAsync(command, _dataSource, ct);
///     }
///     
///     public async Task&lt;IEnumerable&lt;User&gt;&gt; GetRecentUsersAsync(DateTime since, string status, CancellationToken ct = default)
///     {
///         var query = new GetRecentUsersQuery(since, status);
///         return await GetRecentUsersQueryHandler.HandleAsync(query, _dataSource, ct);
///     }
/// }
/// </code>
/// 
/// <para><strong>Dependency Injection with Named Data Sources:</strong></para>
/// <code>
/// [DbCommand(sp: "archive_old_orders", dataSource: "ArchiveDatabase")]
/// public record ArchiveOldOrdersCommand(DateTime OlderThan) : ICommand&lt;int&gt;;
/// 
/// // Generated handler will resolve keyed service: [FromKeyedServices("ArchiveDatabase")] DbDataSource dataSource
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DbCommandAttribute(
    string? sp = null,
    string? sql = null,
    string? fn = null,
    DbParamsCase paramsCase = DbParamsCase.Unset,
    bool nonQuery = false,
    string? dataSource = null) : Attribute
{
    /// <summary>
    ///     If set, a command handler will be generated using this stored procedure.
    /// </summary>
    public string? Sp { get; } = sp;

    /// <summary>
    ///     If set, a command handler will be generated using this SQL query.
    /// </summary>
    public string? Sql { get; } = sql;

    /// <summary>
    ///     If set, a command handler will be generated using this function SQL query.
    ///     Parameters will be automatically appended based on record properties.
    /// </summary>
    public string? Fn { get; } = fn;

    /// <summary>
    ///     Specifies how property names are converted to database parameter names in the generated ToDbParams() method.
    ///     <para>
    ///         - <see cref="DbParamsCase.Unset" />: Uses the global default specified by the DbCommandDefaultParamCase MSBuild property
    ///     </para>
    ///     <para>
    ///         - <see cref="DbParamsCase.None" />: Uses property names as-is without any conversion
    ///     </para>
    ///     <para>
    ///         - <see cref="DbParamsCase.SnakeCase" />: Converts property names to snake_case (e.g., FirstName -> first_name)
    ///     </para>
    ///     <para>
    ///         Individual properties can override this behavior using the [Column("custom_name")] attribute.
    ///     </para>
    /// </summary>
    public DbParamsCase ParamsCase { get; } = paramsCase;

    /// <summary>
    ///     Indicates the nature of the command. This flag primarily influences behavior for ICommand&lt;int/long&gt;.
    ///     <para>
    ///         If true:<br />
    ///         - For ICommand&lt;int/long&gt;: The generated handler will use Dapper's ExecuteAsync (expecting rows affected).<br />
    ///         - For ICommand&lt;TResult&gt; where TResult is not int: A warning will be issued by the source generator,
    ///         as using NonQuery=true with a command expecting a specific data structure is atypical. The handler will default to execute
    ///         a Query or QueryFirstOrDefault call and return default(TResult).
    ///     </para>
    ///     <para>
    ///         If false:<br />
    ///         - For ICommand&lt;int&gt;: The generated handler will use Dapper's ExecuteScalarAsync&lt;int&gt; (expecting a scalar integer query
    ///         result).<br />
    ///         - For ICommand&lt;TResult&gt; where TResult is not int: The handler will perform a query (e.g., QueryFirstOrDefault or
    ///         Query).
    ///     </para>
    /// </summary>
    public bool NonQuery { get; } = nonQuery;

    /// <summary>
    ///     Gets the data source key.
    /// </summary>
    public string? DataSource { get; } = dataSource;
}

public enum DbParamsCase
{
    Unset = -1,

    /// <summary>
    ///     Use the property names as-is (default).
    /// </summary>
    None = 0,

    /// <summary>
    ///     Convert property names to snake_case.
    /// </summary>
    SnakeCase = 1
}
