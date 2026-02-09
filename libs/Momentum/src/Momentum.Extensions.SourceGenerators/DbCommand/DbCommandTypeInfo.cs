// Copyright (c) Momentum .NET. All rights reserved.

using System.Collections.Immutable;

namespace Momentum.Extensions.SourceGenerators.DbCommand;

/// <summary>
///     Abstract base class containing comprehensive metadata about a type marked with <see cref="DbCommandAttribute" />.
///     Serves as the foundation for code generation by encapsulating all analysis results and type information.
/// </summary>
/// <remarks>
///     <para>
///         <strong>Purpose in Code Generation:</strong>
///     </para>
///     <para>
///         This class acts as a data container that bridges the gap between Roslyn's type analysis and source code generation.
///         It aggregates all information needed to generate both parameter providers and command handlers.
///     </para>
/// 
///     <para>
///         <strong>Core Responsibilities:</strong>
///     </para>
///     <list type="bullet">
///         <item><strong>Type Metadata:</strong> Stores fully qualified names, declarations, and namespace information</item>
///         <item><strong>Attribute Analysis:</strong> Contains parsed DbCommandAttribute values and configuration</item>
///         <item><strong>Parameter Mapping:</strong> Holds property-to-parameter mapping information with name conversion</item>
///         <item><strong>Return Type Analysis:</strong> Determines result handling strategy based on implemented interfaces</item>
///         <item><strong>Nesting Support:</strong> Tracks parent types for nested class generation</item>
///     </list>
/// 
///     <para>
///         <strong>Integration with Generation Pipeline:</strong>
///     </para>
///     <list type="bullet">
///         <item>Created during incremental generator analysis phase</item>
///         <item>Passed to source writers for code generation</item>
///         <item>Contains all information needed for both DbExt and Handler file generation</item>
///         <item>Supports diagnostic reporting for validation errors</item>
///     </list>
/// 
///     <para>
///         <strong>Property Mapping Strategy:</strong>
///     </para>
///     <para>The class tracks how C# properties are mapped to database parameters, handling:</para>
///     <list type="bullet">
///         <item>Record primary constructor parameters</item>
///         <item>Regular public properties</item>
///         <item>Parameter case conversion (None, SnakeCase)</item>
///         <item>Custom column name overrides via [Column] attribute</item>
///         <item>Global parameter prefixing</item>
///     </list>
/// 
///     <para>
///         <strong>Return Type Analysis:</strong>
///     </para>
///     <para>Analyzes the implemented ICommand&lt;T&gt; or IQuery&lt;T&gt; interface to determine:</para>
///     <list type="bullet">
///         <item>Whether the type is a command that returns data</item>
///         <item>Single object vs. collection return types</item>
///         <item>Integral types for row count or scalar operations</item>
///         <item>Appropriate Dapper method selection (Execute, Query, QueryFirst, etc.)</item>
///     </list>
/// </remarks>
internal abstract class DbCommandTypeInfo(
    string name,
    string qualifiedTypeName,
    string typeDeclaration,
    DbCommandAttribute dbCommandAttribute)
{
    /// <summary>
    ///     Represents the mapping between a C# property and its corresponding database parameter.
    /// </summary>
    /// <param name="PropertyName">
    ///     The original property name as declared in the C# type (e.g., "FirstName", "UserId").
    /// </param>
    /// <param name="ParameterName">
    ///     The final database parameter name after applying naming conventions and transformations.
    ///     This includes the effects of:
    ///     <list type="bullet">
    ///         <item>DbParamsCase conversion (e.g., "FirstName" → "first_name" for SnakeCase)</item>
    ///         <item>[Column("custom_name")] attribute overrides</item>
    ///         <item>Global parameter prefixes from MSBuild configuration</item>
    ///     </list>
    /// </param>
    /// <remarks>
    ///     <para>
    ///         <strong>Usage in Generated Code:</strong>
    ///     </para>
    ///     <para>This mapping drives the generation of the ToDbParams() method. For example:</para>
    ///     <list type="bullet">
    ///         <item>PropertyName="FirstName", ParameterName="first_name" generates: <c>first_name = this.FirstName</c></item>
    ///         <item>PropertyName="Email", ParameterName="email_addr" generates: <c>email_addr = this.Email</c> (from [Column] attribute)</item>
    ///     </list>
    /// 
    ///     <para>
    ///         <strong>Parameter Name Resolution Priority:</strong>
    ///     </para>
    ///     <list type="number">
    ///         <item>[Column("name")] attribute (highest priority)</item>
    ///         <item>DbParamsCase setting (SnakeCase conversion)</item>
    ///         <item>Global parameter prefix from MSBuild</item>
    ///         <item>Original property name (lowest priority)</item>
    ///     </list>
    /// </remarks>
    internal record PropertyInfo(string PropertyName, string ParameterName);

    /// <summary>
    ///     Contains comprehensive analysis of a command's return type, used to determine the appropriate Dapper execution strategy.
    /// </summary>
    /// <param name="TypeName">
    ///     The simple, unqualified name of the result type (e.g., "int", "User", "IEnumerable").
    /// </param>
    /// <param name="QualifiedTypeName">
    ///     The fully qualified name of the result type including namespace and generic parameters
    ///     (e.g., "global::System.Int32", "global::MyApp.Models.User", "global::System.Collections.Generic.IEnumerable&lt;
    ///     global::MyApp.Models.Order&gt;").
    /// </param>
    /// <param name="GenericArgumentResultFullTypeName">
    ///     For collection types, this contains the fully qualified name of the element type
    ///     (e.g., for IEnumerable&lt;User&gt;, this would be "global::MyApp.Models.User").
    ///     For non-collection types, this matches QualifiedTypeName.
    /// </param>
    /// <param name="IsIntegralType">
    ///     True if the result type is an integral type (int, long, short, etc.).
    ///     This affects whether ExecuteAsync (row count) or ExecuteScalarAsync (scalar value) is used.
    /// </param>
    /// <param name="IsEnumerableResult">
    ///     True if the result type implements IEnumerable&lt;T&gt;, indicating a collection result.
    ///     This determines whether to use QueryAsync (collection) or QueryFirstOrDefaultAsync (single item).
    /// </param>
    /// <remarks>
    ///     <para>
    ///         <strong>Dapper Method Selection Logic:</strong>
    ///     </para>
    ///     <para>The ResultTypeInfo properties drive the selection of appropriate Dapper methods:</para>
    ///     <list type="bullet">
    ///         <item><strong>No ResultType (null):</strong> Uses ExecuteAsync (fire-and-forget commands)</item>
    ///         <item><strong>IsIntegralType + NonQuery=true:</strong> Uses ExecuteAsync (returns row count)</item>
    ///         <item><strong>IsIntegralType + NonQuery=false:</strong> Uses ExecuteScalarAsync&lt;int&gt; (scalar query)</item>
    ///         <item><strong>IsEnumerableResult=true:</strong> Uses QueryAsync&lt;T&gt; (collection results)</item>
    ///         <item><strong>Single object result:</strong> Uses QueryFirstOrDefaultAsync&lt;T&gt; (single or null)</item>
    ///     </list>
    /// 
    ///     <para>
    ///         <strong>Generic Type Handling:</strong>
    ///     </para>
    ///     <para>The generator properly handles complex generic types:</para>
    ///     <list type="bullet">
    ///         <item>IEnumerable&lt;User&gt; → QueryAsync&lt;User&gt;</item>
    ///         <item>List&lt;Order&gt; → QueryAsync&lt;Order&gt;</item>
    ///         <item>User → QueryFirstOrDefaultAsync&lt;User&gt;</item>
    ///         <item>int → ExecuteScalarAsync&lt;int&gt; or ExecuteAsync</item>
    ///     </list>
    /// 
    ///     <para>
    ///         <strong>Global Namespace Qualification:</strong>
    ///     </para>
    ///     <para>
    ///         All type names include global:: prefixes to avoid naming conflicts in generated code,
    ///         ensuring the generated handlers work correctly regardless of using statements in consumer code.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         <strong>Result Type Analysis Examples:</strong>
    ///     </para>
    ///     <code>
    /// // ICommand&lt;int&gt; example:
    /// TypeName = "Int32"
    /// QualifiedTypeName = "global::System.Int32"
    /// GenericArgumentResultFullTypeName = "global::System.Int32"
    /// IsIntegralType = true
    /// IsEnumerableResult = false
    /// // Generates: ExecuteScalarAsync&lt;global::System.Int32&gt; or ExecuteAsync
    /// 
    /// // IQuery&lt;IEnumerable&lt;User&gt;&gt; example:
    /// TypeName = "IEnumerable"
    /// QualifiedTypeName = "global::System.Collections.Generic.IEnumerable&lt;global::MyApp.Models.User&gt;"
    /// GenericArgumentResultFullTypeName = "global::MyApp.Models.User"
    /// IsIntegralType = false
    /// IsEnumerableResult = true
    /// // Generates: QueryAsync&lt;global::MyApp.Models.User&gt;
    /// 
    /// // IQuery&lt;User&gt; example:
    /// TypeName = "User"
    /// QualifiedTypeName = "global::MyApp.Models.User"
    /// GenericArgumentResultFullTypeName = "global::MyApp.Models.User"
    /// IsIntegralType = false
    /// IsEnumerableResult = false
    /// // Generates: QueryFirstOrDefaultAsync&lt;global::MyApp.Models.User&gt;
    /// </code>
    /// </example>
    public record ResultTypeInfo(
        string TypeName,
        string QualifiedTypeName,
        string GenericArgumentResultFullTypeName,
        bool IsIntegralType,
        bool IsEnumerableResult);

    public string? Namespace { get; protected init; }

    public string TypeName { get; } = name;

    public string QualifiedTypeName { get; } = qualifiedTypeName;

    public string TypeDeclaration { get; } = typeDeclaration;

    public DbCommandAttribute DbCommandAttribute { get; } = dbCommandAttribute;

    public ImmutableArray<PropertyInfo> DbProperties { get; protected init; }

    public ResultTypeInfo? ResultType { get; protected init; }

    public bool ImplementsICommandInterface => ResultType is not null;

    /// <summary>
    ///     Represents metadata about a containing (parent) type for nested type generation.
    /// </summary>
    /// <param name="TypeDeclaration">The type declaration string (e.g., "public partial class ParentType").</param>
    /// <param name="QualifiedName">The fully qualified type name.</param>
    /// <param name="IsGlobalNamespace">True if the containing type is in the global namespace.</param>
    internal record ContainingTypeInfo(string TypeDeclaration, string QualifiedName, bool IsGlobalNamespace);

    public ImmutableArray<ContainingTypeInfo> ParentTypes { get; protected init; }

    public bool IsNestedType => ParentTypes.Length > 0;

    public bool IsGlobalType => string.IsNullOrEmpty(Namespace);

    public bool HasCustomDbPropertyNames => DbProperties.Any(p => p.ParameterName != p.PropertyName);
}
