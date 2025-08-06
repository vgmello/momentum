// Copyright (c) ABCDEG. All rights reserved.

using Momentum.Extensions.Abstractions.Dapper;
using Momentum.Extensions.SourceGenerators.DbCommand.Writers;
using Momentum.Extensions.SourceGenerators.Extensions;

namespace Momentum.Extensions.SourceGenerators.DbCommand;

/// <summary>
///     Source generator that creates database command handlers and parameter providers for types marked with DbCommandAttribute.
/// </summary>
/// <remarks>
///     This generator:
///     <list type="bullet">
///         <item>Generates ToDbParams() extension methods for parameter mapping</item>
///         <item>Creates Wolverine command handlers for database Momentum</item>
///         <item>Supports stored procedures, SQL queries, and functions</item>
///         <item>Handles parameter case conversion (None, SnakeCase)</item>
///         <item>Respects Column attributes for custom parameter names</item>
///     </list>
///     The default parameter case can be configured via MSBuild property: DbCommandDefaultParamCase
/// </remarks>
[Generator]
public class DbCommandSourceGenerator : IIncrementalGenerator
{
    /// <summary>
    ///     Initializes the incremental generator.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var dbCommandSettingsProvider = context.AnalyzerConfigOptionsProvider
            .Select((o, _) => GetDbCommandSettingsFromMsBuild(o));

        var commandTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: DbCommandTypeInfoSourceGen.DbCommandAttributeFullName,
                predicate: static (_, _) => true,
                transform: static (ctx, _) => ctx.TargetSymbol as INamedTypeSymbol)
            .Where(static typeInfo => typeInfo is not null)
            .Combine(dbCommandSettingsProvider)
            .Select((combinedValues, _) =>
            {
                var (namedTypeSymbol, dbCommandSettings) = combinedValues;

                return new DbCommandTypeInfoSourceGen(namedTypeSymbol!, dbCommandSettings);
            });

        context.RegisterSourceOutput(commandTypes, static (spc, dbCommandTypeInfo) =>
        {
            dbCommandTypeInfo.DiagnosticsToReport.ForEach(spc.ReportDiagnostic);

            // Only proceed with generation if there are no errors
            if (dbCommandTypeInfo.HasErrors)
            {
                return;
            }

            GenerateDbExtensionsPart(spc, dbCommandTypeInfo);
            GenerateHandlerPart(spc, dbCommandTypeInfo);
        });
    }

    private static void GenerateDbExtensionsPart(SourceProductionContext spc, DbCommandTypeInfo dbCommandTypeInfo)
    {
        var generatedDbParamsSource = DbCommandDbParamsSourceGenWriter.Write(dbCommandTypeInfo);

        var fileName = dbCommandTypeInfo.QualifiedTypeName.GetFileName(dbCommandTypeInfo.IsGlobalType);
        spc.AddSource($"{fileName}.DbExt.g.cs", generatedDbParamsSource);
    }

    private static void GenerateHandlerPart(SourceProductionContext spc, DbCommandTypeInfo dbCommandTypeInfo)
    {
        if (string.IsNullOrWhiteSpace(dbCommandTypeInfo.DbCommandAttribute.Sp) &&
            string.IsNullOrWhiteSpace(dbCommandTypeInfo.DbCommandAttribute.Sql) &&
            string.IsNullOrWhiteSpace(dbCommandTypeInfo.DbCommandAttribute.Fn))
            return; // No handler needed if Sp, Sql, or Fn is not provided

        var generatedHandlerSource = DbCommandHandlerSourceGenWriter.Write(dbCommandTypeInfo);

        var fileName = GetHandlerFileName(dbCommandTypeInfo);
        spc.AddSource($"{fileName}.g.cs", generatedHandlerSource);
    }

    private static DbCommandSourceGenSettings GetDbCommandSettingsFromMsBuild(AnalyzerConfigOptionsProvider optionsProvider)
    {
        var options = optionsProvider.GlobalOptions;

        var paramsCase = DbParamsCase.None;
        if (options.TryGetValue($"build_property.{nameof(DbCommandSourceGenSettings.DbCommandDefaultParamCase)}", out var stringValue))
            Enum.TryParse(stringValue, out paramsCase);

        options.TryGetValue($"build_property.{nameof(DbCommandSourceGenSettings.DbCommandParamPrefix)}", out var dbColumnPrefix);

        return new DbCommandSourceGenSettings(paramsCase, dbColumnPrefix ?? string.Empty);
    }

    private static string GetHandlerFileName(DbCommandTypeInfo dbCommandTypeInfo)
    {
        if (!dbCommandTypeInfo.IsNestedType)
        {
            return dbCommandTypeInfo.QualifiedTypeName.GetFileName(dbCommandTypeInfo.IsGlobalType);
        }

        var parentType = dbCommandTypeInfo.ParentTypes.Last();

        return parentType.GetQualifiedName().GetFileName(parentType.ContainingNamespace.IsGlobalNamespace);
    }
}
