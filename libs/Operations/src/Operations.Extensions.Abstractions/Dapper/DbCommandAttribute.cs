// Copyright (c) ABCDEG. All rights reserved.

namespace Operations.Extensions.Abstractions.Dapper;

/// <summary>
///     Attribute to define database command properties and behavior for a class.
///     Triggers generation of a ToDbParams() method. If 'sp', 'sql', or 'fn' is provided,
///     also triggers generation of a command handler method.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="DbCommandAttribute" /> class.
/// </remarks>
/// <param name="sp">The name of the stored procedure. Mutually exclusive with <paramref name="sql" /> and <paramref name="fn" />.</param>
/// <param name="sql">The SQL query text. Mutually exclusive with <paramref name="sp" /> and <paramref name="fn" />.</param>
/// <param name="fn">
///     The function SQL query text. Parameters will be auto-generated based on record properties. Mutually exclusive with
///     <paramref name="sp" /> and <paramref name="sql" />. The '$' prefix on the function name is replaced with 'select * from'
/// </param>
/// <param name="paramsCase">DB params case</param>
/// <param name="nonQuery">
///     Indicates the nature of the command, with its primary effect on commands implementing ICommand&lt;int&gt;.
///     Default is true.
///     <para>
///         If true: For ICommand&lt;int&gt;, the generated handler uses ExecuteAsync (rows affected). For other ICommand&lt;TResult&gt;, a
///         diagnostic may be issued.
///         If false: For ICommand&lt;int&gt;, the generated handler uses ExecuteScalarAsync&lt;int&gt;. For other ICommand&lt;TResult&gt;, a
///         query is performed.
///     </para>
/// </param>
/// <param name="dataSource">Indicates which datasource to use, which when provided a keyed service will be resolved</param>
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
