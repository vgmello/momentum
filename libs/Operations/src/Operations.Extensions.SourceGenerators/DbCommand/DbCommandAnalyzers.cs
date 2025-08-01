// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.Abstractions.Dapper;

namespace Operations.Extensions.SourceGenerators.DbCommand;

internal static class DbCommandAnalyzers
{
    private static readonly DiagnosticDescriptor NonQueryWithGenericResultWarning = new(
        id: "DB_COMMAND_GEN001",
        title: "NonQuery attribute used with generic ICommand<TResult>",
        messageFormat:
        "DbCommandAttribute's NonQuery property is true for command '{0}' which implements ICommand<{1}>. " +
        "NoQuery are only valid for ICommand<int>.",
        category: "DbCommandSourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CommandMissingInterfaceError = new(
        id: "DB_COMMAND_GEN002",
        title: "Command missing ICommand<TResult> interface",
        messageFormat:
        "Class '{0}' is decorated with DbCommandAttribute specifying 'sp', 'sql', or 'fn' for handler generation, " +
        "but it does not implement ICommand<TResult> or IQuery<TResult>. Handler cannot be generated without a result type.",
        category: "DbCommandSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MutuallyExclusivePropertiesError = new(
        id: "DB_COMMAND_GEN003",
        title: "Mutually exclusive properties specified in DbCommandAttribute",
        messageFormat: "Class '{0}' has multiple command properties specified in DbCommandAttribute. " +
                       "The properties 'Sp', 'Sql', and 'Fn' are mutually exclusive - specify only one of these.",
        category: "DbCommandSourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    ///     If <see cref="DbCommandTypeInfo.ResultTypeInfo" /> is null, it means ICommand/IQuery&lt;TResult&gt; was not found.
    ///     An error diagnostic DB_COMMAND_GEN002 will be logged by ExtractTypeInfo and reported by Initialize's output action.
    ///     Generation should be skipped by the check in Initialize's RegisterSourceOutput action.
    /// </summary>
    public static void ExecuteMissingInterfaceAnalyzer(INamedTypeSymbol typeSymbol,
        DbCommandTypeInfo.ResultTypeInfo? resultTypeInfo, DbCommandAttribute dbCommandAttribute, List<Diagnostic> diagnostics)
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
        DbCommandTypeInfo.ResultTypeInfo? resultTypeInfo, DbCommandAttribute dbCommandAttribute, List<Diagnostic> diagnostics)
    {
        if (dbCommandAttribute.NonQuery && resultTypeInfo?.TypeName != nameof(Int32))
        {
            var typeLocation = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
            var diagnostic = Diagnostic.Create(NonQueryWithGenericResultWarning,
                typeLocation, typeSymbol.Name, resultTypeInfo?.TypeName ?? "null");

            diagnostics.Add(diagnostic);
        }
    }

    /// <summary>
    ///     Validates that the DbCommand attribute has only one of Sp, Sql, or Fn specified.
    ///     Adds a DB_COMMAND_GEN003 error diagnostic if multiple properties are provided,
    ///     as these are mutually exclusive options for specifying the database command.
    /// </summary>
    public static void ExecuteMutuallyExclusivePropertiesAnalyzer(INamedTypeSymbol typeSymbol,
        DbCommandAttribute dbCommandAttribute, List<Diagnostic> diagnostics)
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
}
