// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.SourceGenerators.Extensions;
using System.Collections.Immutable;

namespace Momentum.Extensions.SourceGenerators.DbCommand;

/// <summary>
///     Concrete implementation of <see cref="DbCommandTypeInfo" /> that performs comprehensive analysis of types marked with
///     <see cref="DbCommandAttribute" />
///     using Roslyn's INamedTypeSymbol. This class bridges Roslyn's type system with the source generation pipeline.
/// </summary>
/// <remarks>
///     <para>
///         <strong>Analysis Responsibilities:</strong>
///     </para>
///     <para>This class performs deep analysis of annotated types to extract all information needed for code generation:</para>
///
///     <list type="bullet">
///         <item><strong>Attribute Parsing:</strong> Extracts and validates DbCommandAttribute constructor arguments</item>
///         <item>
///             <strong>Interface Analysis:</strong> Determines if the type implements ICommand&lt;T&gt; or IQuery&lt;T&gt; and extracts
///             return types
///         </item>
///         <item><strong>Property Discovery:</strong> Identifies all properties and constructor parameters for parameter mapping</item>
///         <item><strong>Parameter Mapping:</strong> Applies naming conventions, Column attributes, and global settings</item>
///         <item><strong>Type Hierarchy:</strong> Handles nested types and namespace resolution</item>
///         <item><strong>Diagnostic Generation:</strong> Validates configuration and reports errors/warnings</item>
///     </list>
///
///     <para>
///         <strong>Roslyn Integration Pattern:</strong>
///     </para>
///     <para>The class follows Roslyn's incremental generator patterns:</para>
///     <list type="bullet">
///         <item>Receives INamedTypeSymbol from ForAttributeWithMetadataName transform</item>
///         <item>Performs analysis using Roslyn's semantic model</item>
///         <item>Produces immutable data structures for code generation</item>
///         <item>Reports diagnostics back to the compilation process</item>
///     </list>
///
///     <para>
///         <strong>Property Analysis Strategy:</strong>
///     </para>
///     <list type="bullet">
///         <item><strong>Records:</strong> Primary constructor parameters are automatically included</item>
///         <item><strong>Regular Classes:</strong> Public properties with getters are included</item>
///         <item><strong>Parameter Naming:</strong> Applies case conversion and prefix rules</item>
///         <item><strong>Column Attributes:</strong> Overrides automatic naming with explicit values</item>
///     </list>
///
///     <para>
///         <strong>Return Type Analysis:</strong>
///     </para>
///     <para>Analyzes implemented interfaces to determine appropriate Dapper method selection:</para>
///     <list type="bullet">
///         <item>Detects ICommand&lt;TResult&gt; and IQuery&lt;TResult&gt; implementations</item>
///         <item>Extracts generic type arguments for result mapping</item>
///         <item>Identifies collection types (IEnumerable&lt;T&gt;) for QueryAsync usage</item>
///         <item>Detects integral types for Execute vs ExecuteScalar selection</item>
///     </list>
///
///     <para>
///         <strong>Validation and Diagnostics:</strong>
///     </para>
///     <para>Performs compile-time validation using dedicated analyzers:</para>
///     <list type="bullet">
///         <item><strong>Interface Compliance:</strong> Ensures proper ICommand/IQuery implementation</item>
///         <item><strong>Mutually Exclusive Parameters:</strong> Validates that only one of sp/sql/fn is specified</item>
///         <item><strong>NonQuery Flag Validation:</strong> Warns about incorrect nonQuery usage patterns</item>
///         <item><strong>Type Compatibility:</strong> Ensures return types are compatible with database operations</item>
///     </list>
///
///     <para>
///         <strong>Performance Considerations:</strong>
///     </para>
///     <list type="bullet">
///         <item>Lazy evaluation of expensive operations</item>
///         <item>Immutable data structures for incremental generation</item>
///         <item>Efficient symbol traversal and caching</item>
///         <item>Minimal allocations during analysis</item>
///     </list>
/// </remarks>
internal sealed class DbCommandTypeInfoSourceGen : DbCommandTypeInfo, IEquatable<DbCommandTypeInfoSourceGen>
{
    internal static string DbCommandAttributeFullName { get; } = typeof(DbCommandAttribute).FullName!;

    private static string CommandInterfaceFullName { get; } = $"{typeof(ICommand<>).Namespace}.ICommand<TResult>";
    private static string QueryInterfaceFullName { get; } = $"{typeof(IQuery<>).Namespace}.IQuery<TResult>";

    /// <summary>
    ///     Initializes a new instance by performing comprehensive analysis of the provided type symbol.
    /// </summary>
    /// <param name="typeSymbol">The Roslyn INamedTypeSymbol representing the type marked with DbCommandAttribute.</param>
    /// <param name="settings">Global MSBuild settings for parameter naming and prefixing.</param>
    /// <remarks>
    ///     <para>This constructor orchestrates the entire analysis process:</para>
    ///     <list type="number">
    ///         <item><strong>Basic Type Info:</strong> Extracts name, qualified name, and type declaration</item>
    ///         <item><strong>Attribute Analysis:</strong> Parses DbCommandAttribute constructor arguments</item>
    ///         <item><strong>Namespace Resolution:</strong> Determines containing namespace for code generation</item>
    ///         <item><strong>Property Analysis:</strong> Maps all properties to database parameters with naming rules</item>
    ///         <item><strong>Interface Analysis:</strong> Determines return type and execution strategy</item>
    ///         <item><strong>Hierarchy Analysis:</strong> Tracks parent types for nested class generation</item>
    ///         <item><strong>Validation:</strong> Runs analyzers and collects diagnostics</item>
    ///     </list>
    ///
    ///     <para>The analysis is performed immediately during construction to support incremental generation caching.</para>
    /// </remarks>
    public DbCommandTypeInfoSourceGen(INamedTypeSymbol typeSymbol, DbCommandSourceGenSettings settings) :
        base(
            typeSymbol.Name,
            typeSymbol.GetQualifiedName(),
            typeSymbol.GetTypeDeclaration(),
            GetDbCommandAttribute(typeSymbol))
    {
        Namespace = typeSymbol.ContainingNamespace.IsGlobalNamespace ? null : typeSymbol.ContainingNamespace.ToDisplayString();
        DbProperties = GetDbCommandObjProperties(typeSymbol, DbCommandAttribute.ParamsCase, settings);
        ResultType = GetDbCommandResultInfo(typeSymbol);
        ParentTypes = typeSymbol.GetContainingTypesTree()
            .Select(t => new ContainingTypeInfo(
                t.GetTypeDeclaration(),
                t.GetQualifiedName(),
                t.ContainingNamespace.IsGlobalNamespace))
            .ToImmutableArray();
        DiagnosticsToReport = ExecuteAnalyzers(typeSymbol);
    }

    /// <summary>
    ///     Gets the collection of diagnostics (errors, warnings, info) generated during type analysis.
    ///     These diagnostics are reported back to the compilation process and shown to developers.
    /// </summary>
    /// <value>
    ///     An immutable array of Roslyn Diagnostic objects representing validation results.
    ///     Empty if no issues were found during analysis.
    /// </value>
    /// <remarks>
    ///     <para>Diagnostics are generated by dedicated analyzer methods that validate:</para>
    ///     <list type="bullet">
    ///         <item><strong>Interface Implementation:</strong> Ensures ICommand/IQuery interfaces are properly implemented</item>
    ///         <item><strong>Attribute Validation:</strong> Checks that sp, sql, and fn parameters are mutually exclusive</item>
    ///         <item><strong>NonQuery Usage:</strong> Warns when nonQuery=true is used with non-integral return types</item>
    ///         <item><strong>Type Compatibility:</strong> Validates that return types are compatible with database operations</item>
    ///     </list>
    /// </remarks>
    public ImmutableArray<Diagnostic> DiagnosticsToReport { get; }

    /// <summary>
    ///     Gets a value indicating whether any error-level diagnostics were generated during analysis.
    ///     When true, code generation is skipped for this type to prevent compilation failures.
    /// </summary>
    /// <value>
    ///     <c>true</c> if any diagnostics have severity level Error; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         The source generator uses this property to determine whether to proceed with code generation.
    ///         Types with errors are skipped, but warnings and info diagnostics don't prevent generation.
    ///     </para>
    ///
    ///     <para>
    ///         <strong>Error vs Warning Strategy:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item><strong>Errors:</strong> Configuration issues that would cause generated code to fail compilation</item>
    ///         <item><strong>Warnings:</strong> Potentially problematic configurations that still generate valid code</item>
    ///         <item><strong>Info:</strong> Helpful information about code generation decisions</item>
    ///     </list>
    /// </remarks>
    public bool HasErrors => DiagnosticsToReport.Any(d => d.Severity == DiagnosticSeverity.Error);

    public bool Equals(DbCommandTypeInfoSourceGen? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return TypeName == other.TypeName
               && QualifiedTypeName == other.QualifiedTypeName
               && TypeDeclaration == other.TypeDeclaration
               && Namespace == other.Namespace
               && DbCommandAttribute.Sp == other.DbCommandAttribute.Sp
               && DbCommandAttribute.Sql == other.DbCommandAttribute.Sql
               && DbCommandAttribute.Fn == other.DbCommandAttribute.Fn
               && DbCommandAttribute.ParamsCase == other.DbCommandAttribute.ParamsCase
               && DbCommandAttribute.NonQuery == other.DbCommandAttribute.NonQuery
               && DbCommandAttribute.DataSource == other.DbCommandAttribute.DataSource
               && ResultType == other.ResultType
               && DbProperties.SequenceEqual(other.DbProperties)
               && ParentTypes.SequenceEqual(other.ParentTypes);
    }

    public override bool Equals(object? obj) => Equals(obj as DbCommandTypeInfoSourceGen);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + TypeName.GetHashCode();
            hash = hash * 31 + QualifiedTypeName.GetHashCode();
            hash = hash * 31 + (Namespace?.GetHashCode() ?? 0);
            hash = hash * 31 + DbCommandAttribute.ParamsCase.GetHashCode();
            return hash;
        }
    }

    private ImmutableArray<Diagnostic> ExecuteAnalyzers(INamedTypeSymbol typeSymbol)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        DbCommandAnalyzers.ExecuteMissingInterfaceAnalyzer(typeSymbol, ResultType, DbCommandAttribute, diagnostics);
        DbCommandAnalyzers.ExecuteNonQueryWithNonIntegralResultAnalyzer(typeSymbol, ResultType, DbCommandAttribute, diagnostics);
        DbCommandAnalyzers.ExecuteMutuallyExclusivePropertiesAnalyzer(typeSymbol, DbCommandAttribute, diagnostics);
        DbCommandAnalyzers.ExecuteInvalidFunctionNameAnalyzer(typeSymbol, DbCommandAttribute, diagnostics);

        return diagnostics.ToImmutable();
    }

    private static DbCommandAttribute GetDbCommandAttribute(INamedTypeSymbol typeSymbol)
    {
        var attributeData = typeSymbol.GetAttribute(DbCommandAttributeFullName)!;

        var spValue = attributeData.GetConstructorArgument<string>(index: 0);
        var sqlValue = attributeData.GetConstructorArgument<string>(index: 1);
        var fnValue = attributeData.GetConstructorArgument<string>(index: 2);
        var dbParamCaseValue = attributeData.GetConstructorArgument<int>(index: 3);
        var nonQueryValue = attributeData.GetConstructorArgument<bool>(index: 4);
        var dataSourceValue = attributeData.GetConstructorArgument<string>(index: 5);

        return new DbCommandAttribute(
            sp: spValue,
            sql: sqlValue,
            fn: fnValue,
            paramsCase: (DbParamsCase)dbParamCaseValue,
            nonQuery: nonQueryValue,
            dataSource: dataSourceValue);
    }

    private static ResultTypeInfo? GetDbCommandResultInfo(INamedTypeSymbol typeSymbol)
    {
        var commandInterface = typeSymbol.AllInterfaces.FirstOrDefault(it =>
            it.OriginalDefinition.ToDisplayString() == CommandInterfaceFullName);

        if (commandInterface is not null)
            return GetResultInfo(commandInterface);

        var queryInterface = typeSymbol.AllInterfaces.FirstOrDefault(it =>
            it.OriginalDefinition.ToDisplayString() == QueryInterfaceFullName);

        return queryInterface is not null ? GetResultInfo(queryInterface) : null;
    }

    private static ResultTypeInfo? GetResultInfo(INamedTypeSymbol messageInterface)
    {
        if (messageInterface.TypeArguments[0] is not INamedTypeSymbol resultType)
            return null;

        var resultFullTypeName = resultType.GetQualifiedName(withGlobalNamespace: true);

        var genericArgumentResultFullTypeName = resultFullTypeName;
        var isEnumerableResult = false;

        var implementsIEnumerable = resultType.OriginalDefinition.ImplementsIEnumerable();

        if (implementsIEnumerable && resultType.TypeArguments.FirstOrDefault() is INamedTypeSymbol enumerableTypeArg)
        {
            isEnumerableResult = true;
            genericArgumentResultFullTypeName = enumerableTypeArg.GetQualifiedName(withGlobalNamespace: true);
        }

        return new ResultTypeInfo(
            TypeName: resultType.Name,
            QualifiedTypeName: resultFullTypeName,
            GenericArgumentResultFullTypeName: genericArgumentResultFullTypeName,
            IsIntegralType: resultType.IsIntegralType(),
            IsEnumerableResult: isEnumerableResult);
    }

    private static readonly string DbCommandIgnoreAttributeName = typeof(DbCommandIgnoreAttribute).FullName!;

    private static ImmutableArray<PropertyInfo> GetDbCommandObjProperties(
        INamedTypeSymbol typeSymbol, DbParamsCase paramsCase, DbCommandSourceGenSettings settings)
    {
        Dictionary<string, PropertyInfo> primaryProperties;

        if (typeSymbol.IsRecord)
        {
            var primaryConstructor = typeSymbol.Constructors.FirstOrDefault(c => c.IsPrimaryConstructor());
            primaryProperties = primaryConstructor?.Parameters
                .Where(p => p.GetAttribute(DbCommandIgnoreAttributeName) is null)
                .Select(p => new PropertyInfo(p.Name, GetParameterName(p, paramsCase, settings)))
                .ToDictionary(p => p.PropertyName, p => p) ?? [];
        }
        else
        {
            primaryProperties = [];
        }

        var normalProps = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !primaryProperties.ContainsKey(p.Name))
            .Where(p => p is { DeclaredAccessibility: Accessibility.Public, IsStatic: false, GetMethod: not null })
            .Where(p => p.GetAttributes().All(a => a.AttributeClass?.ToDisplayString() != DbCommandIgnoreAttributeName))
            .Select(p => new PropertyInfo(p.Name, GetParameterName(p, paramsCase, settings)));

        return [.. primaryProperties.Values.Concat(normalProps)];
    }

    private static string GetParameterName(ISymbol prop, DbParamsCase paramsCase, DbCommandSourceGenSettings settings)
    {
        var columnNameAttribute = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute");

        var customColumnName = columnNameAttribute?.GetConstructorArgument<string>(index: 0);

        if (customColumnName is not null)
            return customColumnName;

        var effectiveParamsCase = paramsCase == DbParamsCase.Unset ? settings.DbCommandDefaultParamCase : paramsCase;

        var paramName = effectiveParamsCase switch
        {
            DbParamsCase.SnakeCase => prop.Name.ToSnakeCase(),
            _ => prop.Name
        };

        return string.IsNullOrEmpty(settings.DbCommandParamPrefix) ? paramName : $"{settings.DbCommandParamPrefix}{paramName}";
    }
}
