// Copyright (c) Momentum .NET. All rights reserved.

using System.Collections.Immutable;

namespace Momentum.Extensions.SourceGenerators.DbCommand;

internal static class DbCommandAnalyzers
{
    private static readonly DiagnosticDescriptor NonQueryWithGenericResultWarning = new(
        id: "MMT001",
        title: "NonQuery attribute used with generic ICommand<TResult>",
        messageFormat:
        "DbCommandAttribute's NonQuery property is true for command '{0}' which implements ICommand<{1}>. " +
        "NonQuery is only valid for ICommand<int>.",
        category: "DbCommandSourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CommandMissingInterfaceError = new(
        id: "MMT002",
        title: "Command missing ICommand<TResult> interface",
        messageFormat:
        "Class '{0}' is decorated with DbCommandAttribute specifying 'sp', 'sql', or 'fn' for handler generation, " +
        "but it does not implement ICommand<TResult> or IQuery<TResult>. Handler cannot be generated without a result type.",
        category: "DbCommandSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MutuallyExclusivePropertiesError = new(
        id: "MMT003",
        title: "Mutually exclusive properties specified in DbCommandAttribute",
        messageFormat: "Class '{0}' has multiple command properties specified in DbCommandAttribute. " +
                       "The properties 'Sp', 'Sql', and 'Fn' are mutually exclusive - specify only one of these.",
        category: "DbCommandSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor UnexpectedGeneratorError = new(
        id: "MMT004",
        title: "Unexpected error during source generation",
        messageFormat: "An unexpected error occurred while generating code for '{0}': {1}",
        category: "DbCommandSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidFunctionNameError = new(
        id: "MMT005",
        title: "Invalid SQL function name in DbCommandAttribute",
        messageFormat: "Class '{0}' has an invalid function name '{1}' in DbCommandAttribute. " +
                       "Function names must be valid SQL identifiers (letters, digits, underscores) " +
                       "optionally schema-qualified (schema.name) or bracket/quote-delimited ([name] or \"name\").",
        category: "DbCommandSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SQL identifier: plain, [bracketed], or "quoted", with optional schema qualification
    private static readonly System.Text.RegularExpressions.Regex ValidSqlIdentifierPattern = new(
        @"^(""?[a-zA-Z_]\w*""?|\[[a-zA-Z_]\w*\])(\.\s*(""?[a-zA-Z_]\w*""?|\[[a-zA-Z_]\w*\]))*$",
        System.Text.RegularExpressions.RegexOptions.Compiled,
        System.TimeSpan.FromSeconds(1));

    // Dangerous SQL injection patterns
    private static readonly System.Text.RegularExpressions.Regex DangerousSqlPattern = new(
        @";|--\s|/\*",
        System.Text.RegularExpressions.RegexOptions.Compiled,
        System.TimeSpan.FromSeconds(1));

    /// <summary>
    ///     If <see cref="DbCommandTypeInfo.ResultTypeInfo" /> is null, it means ICommand/IQuery&lt;TResult&gt; was not found.
    ///     An error diagnostic MMT002 will be logged by ExtractTypeInfo and reported by Initialize's output action.
    ///     Generation should be skipped by the check in Initialize's RegisterSourceOutput action.
    /// </summary>
    public static void ExecuteMissingInterfaceAnalyzer(INamedTypeSymbol typeSymbol,
        DbCommandTypeInfo.ResultTypeInfo? resultTypeInfo, DbCommandAttribute dbCommandAttribute, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (resultTypeInfo is null && (!string.IsNullOrWhiteSpace(dbCommandAttribute.Sp) ||
                                       !string.IsNullOrWhiteSpace(dbCommandAttribute.Sql) ||
                                       !string.IsNullOrWhiteSpace(dbCommandAttribute.Fn)))
        {
            var typeLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(CommandMissingInterfaceError, typeLocation, typeSymbol.Name);

            diagnostics.Add(diagnostic);
        }
    }

    /// <summary>
    ///     If DbCommand attribute's NonQuery property is true, the command must implement ICommand&lt;int&gt;
    /// </summary>
    public static void ExecuteNonQueryWithNonIntegralResultAnalyzer(INamedTypeSymbol typeSymbol,
        DbCommandTypeInfo.ResultTypeInfo? resultTypeInfo, DbCommandAttribute dbCommandAttribute, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (dbCommandAttribute.NonQuery && resultTypeInfo is { IsIntegralType: false })
        {
            var typeLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(NonQueryWithGenericResultWarning,
                typeLocation, typeSymbol.Name, resultTypeInfo.TypeName);

            diagnostics.Add(diagnostic);
        }
    }

    /// <summary>
    ///     Validates that the DbCommand attribute has only one of Sp, Sql, or Fn specified.
    ///     Adds a MMT003 error diagnostic if multiple properties are provided,
    ///     as these are mutually exclusive options for specifying the database command.
    /// </summary>
    public static void ExecuteMutuallyExclusivePropertiesAnalyzer(INamedTypeSymbol typeSymbol,
        DbCommandAttribute dbCommandAttribute, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var providedProperties = new List<bool>
        {
            !string.IsNullOrWhiteSpace(dbCommandAttribute.Sp),
            !string.IsNullOrWhiteSpace(dbCommandAttribute.Sql),
            !string.IsNullOrWhiteSpace(dbCommandAttribute.Fn)
        };

        if (providedProperties.Count(p => p) > 1)
        {
            var typeLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(MutuallyExclusivePropertiesError, typeLocation, typeSymbol.Name);

            diagnostics.Add(diagnostic);
        }
    }

    public static void ExecuteInvalidFunctionNameAnalyzer(INamedTypeSymbol typeSymbol,
        DbCommandAttribute dbCommandAttribute, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (string.IsNullOrWhiteSpace(dbCommandAttribute.Fn))
            return;

        var fnName = dbCommandAttribute.Fn;

        // $-prefixed names (e.g. "$schema.fn_name") must be valid SQL identifiers
        // since they become "SELECT * FROM <identifier>(@params)"
        if (fnName.StartsWith("$"))
        {
            var identifier = fnName.Substring(1);
            if (!ValidSqlIdentifierPattern.IsMatch(identifier))
            {
                var typeLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
                var diagnostic = Diagnostic.Create(InvalidFunctionNameError, typeLocation, typeSymbol.Name, fnName);
                diagnostics.Add(diagnostic);
            }
            return;
        }

        // Non-$-prefixed fn values can be SQL expressions (e.g. "select * from schema.fn")
        // Only check for dangerous injection patterns
        if (DangerousSqlPattern.IsMatch(fnName))
        {
            var typeLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(InvalidFunctionNameError, typeLocation, typeSymbol.Name, fnName);
            diagnostics.Add(diagnostic);
        }
    }
}
