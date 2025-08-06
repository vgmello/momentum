// Copyright (c) ABCDEG. All rights reserved.

using Momentum.Extensions.Abstractions.Dapper;
using System.Collections.Immutable;

namespace Momentum.Extensions.SourceGenerators.DbCommand;

/// <summary>
///     Abstract base class containing metadata about a type marked with DbCommandAttribute.
/// </summary>
/// <remarks>
///     This class is used by the source generator to hold all necessary information
///     about a command type for generating database handlers and parameter providers.
/// </remarks>
internal abstract class DbCommandTypeInfo(
    string name,
    string qualifiedTypeName,
    string typeDeclaration,
    DbCommandAttribute dbCommandAttribute)
{
    /// <summary>
    ///     Represents information about a property that will be mapped to a database parameter.
    /// </summary>
    /// <param name="PropertyName">The name of the property in the C# type.</param>
    /// <param name="ParameterName">The name of the database parameter (may differ due to case conversion or Column attribute).</param>
    internal record PropertyInfo(string PropertyName, string ParameterName);

    /// <summary>
    ///     Contains information about the result type of a command.
    /// </summary>
    /// <param name="TypeName">The simple name of the result type.</param>
    /// <param name="QualifiedTypeName">The fully qualified name of the result type.</param>
    /// <param name="GenericArgumentResultFullTypeName">The full type name of the generic argument if the result is a collection.</param>
    /// <param name="IsIntegralType">Whether the result type is an integral type (e.g., int, long).</param>
    /// <param name="IsEnumerableResult">Whether the result is a collection type.</param>
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

    public ImmutableArray<INamedTypeSymbol> ParentTypes { get; protected init; }

    public bool IsNestedType => ParentTypes.Length > 0;

    public bool IsGlobalType => string.IsNullOrEmpty(Namespace);

    public bool HasCustomDbPropertyNames => DbProperties.Any(p => p.ParameterName != p.PropertyName);
}
