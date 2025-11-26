// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Momentum.Extensions.SourceGenerators.Extensions;

/// <summary>
///     Extension methods for Roslyn symbol analysis used by source generators.
///     Provides utilities for type inspection, attribute extraction, and code generation helpers.
/// </summary>
internal static class SourceGeneratorExtensions
{
    private static SymbolDisplayFormat FullyQualifiedFormat { get; } = SymbolDisplayFormat
        .FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static SymbolDisplayFormat FullyQualifiedFormatNoGlobal { get; } = FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    /// <summary>
    ///     Extracts a typed constructor argument from an attribute at the specified index.
    /// </summary>
    /// <typeparam name="T">The expected type of the argument.</typeparam>
    /// <param name="attribute">The attribute data to extract from.</param>
    /// <param name="index">The zero-based index of the constructor argument.</param>
    /// <returns>The typed argument value, or default if not found or type mismatch.</returns>
    public static T? GetConstructorArgument<T>(this AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Value is T argValue)
            return argValue;

        return default;
    }

    /// <summary>
    ///     Retrieves attribute data for an attribute with the specified fully qualified name.
    /// </summary>
    /// <param name="symbol">The symbol to search for attributes.</param>
    /// <param name="attributeFullName">The fully qualified name of the attribute class.</param>
    /// <returns>The attribute data if found; otherwise, null.</returns>
    public static AttributeData? GetAttribute(this ISymbol symbol, string attributeFullName)
    {
        return symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    /// <summary>
    ///     Generates a type declaration string suitable for code generation (e.g., "public partial record MyType").
    /// </summary>
    /// <param name="typeSymbol">The type symbol to generate a declaration for.</param>
    /// <returns>A string containing the accessibility, partial keyword, type kind, and name with generic parameters.</returns>
    public static string GetTypeDeclaration(this INamedTypeSymbol typeSymbol)
    {
        var kind = typeSymbol.TypeKind switch
        {
            TypeKind.Class => typeSymbol.IsRecord ? "partial record" : "partial class",
            TypeKind.Struct => typeSymbol.IsRecord ? "partial record struct" : "partial struct",
            _ => "partial class"
        };

        var genericArgDefinition = typeSymbol.IsGenericType ? typeSymbol.TypeArguments.GetGenericsDeclaration() : string.Empty;

        return $"{SyntaxFacts.GetText(typeSymbol.DeclaredAccessibility)} {kind} {typeSymbol.Name}{genericArgDefinition}";
    }

    /// <summary>
    ///     Gets the fully qualified name of a type symbol.
    /// </summary>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <param name="withGlobalNamespace">If true, includes the "global::" prefix.</param>
    /// <returns>The fully qualified type name.</returns>
    public static string GetQualifiedName(this ITypeSymbol typeSymbol, bool withGlobalNamespace = false) =>
        typeSymbol.ToDisplayString(withGlobalNamespace ? FullyQualifiedFormat : FullyQualifiedFormatNoGlobal);

    /// <summary>
    ///     Gets the hierarchy of containing (parent) types for nested type support.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to analyze.</param>
    /// <returns>An immutable array of parent types, ordered from outermost to innermost.</returns>
    public static ImmutableArray<INamedTypeSymbol> GetContainingTypesTree(this ITypeSymbol typeSymbol)
    {
        var arrayBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        var parent = typeSymbol.ContainingType;

        while (parent is not null)
        {
            arrayBuilder.Add(parent);
            parent = parent.ContainingType;
        }

        arrayBuilder.Reverse();

        return arrayBuilder.ToImmutable();
    }

    /// <summary>
    ///     Formats generic type arguments for code generation (e.g., "&lt;T1, T2&gt;").
    /// </summary>
    /// <param name="typeArguments">The type arguments to format.</param>
    /// <returns>A formatted generic type parameter string, or empty string if no arguments.</returns>
    public static string GetGenericsDeclaration(this ImmutableArray<ITypeSymbol> typeArguments)
    {
        if (typeArguments.IsEmpty)
            return string.Empty;

        return $"<{string.Join(", ", typeArguments.Select(ta => ta.GetQualifiedName(withGlobalNamespace: true)))}>";
    }

    /// <summary>
    ///     Determines whether a constructor is a primary constructor (record parameter list).
    /// </summary>
    /// <param name="constructor">The constructor method symbol to check.</param>
    /// <returns>True if this is a primary constructor; otherwise, false.</returns>
    public static bool IsPrimaryConstructor(this IMethodSymbol constructor)
    {
        var isCloneConstructor = constructor.Parameters.Length == 1 &&
                                 SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, constructor.ContainingType);

        return !isCloneConstructor && constructor is { IsImplicitlyDeclared: false, Parameters.Length: > 0 }
                                   && constructor.GetAttribute(typeof(CompilerGeneratedAttribute).FullName!) is null
                                   && constructor.ContainingType.DeclaringSyntaxReferences.Any(sr =>
                                       sr.GetSyntax() is RecordDeclarationSyntax
                                       {
                                           ParameterList.Parameters.Count: > 0
                                       });
    }

    /// <summary>
    ///     Determines whether a type is an integral numeric type (sbyte, byte, short, ushort, int, uint, long, ulong).
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <returns>True if the type is an integral type; otherwise, false.</returns>
    public static bool IsIntegralType(this ITypeSymbol symbol) =>
        symbol.SpecialType switch
        {
            SpecialType.System_SByte => true,
            SpecialType.System_Byte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            _ => false
        };

    /// <summary>
    ///     Determines whether a type implements IEnumerable or IEnumerable&lt;T&gt;.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <returns>True if the type implements IEnumerable; otherwise, false.</returns>
    public static bool ImplementsIEnumerable(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces.Any(i =>
            i.Name == "IEnumerable" &&
            (
                i.ContainingNamespace.ToDisplayString() == "System.Collections" ||
                i.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
            )
        );
    }

    /// <summary>
    ///     Converts a fully qualified type name to a safe file name for generated source files.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified type name.</param>
    /// <param name="isGlobalNamespace">If true, prefixes the file name with "global_".</param>
    /// <returns>A file-system safe string suitable for use as a file name.</returns>
    public static string GetFileName(this string fullTypeName, bool isGlobalNamespace)
    {
        var fileName = fullTypeName
            .Replace('.', '_')
            .Replace('<', '_')
            .Replace('>', '_');

        return isGlobalNamespace ? $"global_{fileName}" : fileName;
    }
}
