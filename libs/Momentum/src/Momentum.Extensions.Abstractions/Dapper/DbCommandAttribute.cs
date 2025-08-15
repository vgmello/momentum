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
/// <!--@include: @code/database/db-command-attribute-detailed.md -->
/// </remarks>
/// <example>
/// <!--@include: @code/examples/dbcommand-examples.md -->
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
