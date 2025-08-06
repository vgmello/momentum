// Copyright (c) ABCDEG. All rights reserved.

using Momentum.Extensions.Abstractions.Dapper;
using Momentum.Extensions.Abstractions.Extensions;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.SourceGenerators.Extensions;
using System.Collections.Immutable;

namespace Momentum.Extensions.SourceGenerators.DbCommand;

internal class DbCommandTypeInfoSourceGen : DbCommandTypeInfo
{
    internal static string DbCommandAttributeFullName { get; } = typeof(DbCommandAttribute).FullName!;

    private static string CommandInterfaceFullName { get; } = $"{typeof(ICommand<>).Namespace}.ICommand<TResult>";
    private static string QueryInterfaceFullName { get; } = $"{typeof(IQuery<>).Namespace}.IQuery<TResult>";

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
        ParentTypes = typeSymbol.GetContainingTypesTree();
        DiagnosticsToReport = ExecuteAnalyzers(typeSymbol);
    }

    public ImmutableList<Diagnostic> DiagnosticsToReport { get; }

    public bool HasErrors => DiagnosticsToReport.Any(d => d.Severity == DiagnosticSeverity.Error);

    private ImmutableList<Diagnostic> ExecuteAnalyzers(INamedTypeSymbol typeSymbol)
    {
        var diagnostics = new List<Diagnostic>();

        DbCommandAnalyzers.ExecuteMissingInterfaceAnalyzer(typeSymbol, ResultType, DbCommandAttribute, diagnostics);
        DbCommandAnalyzers.ExecuteNonQueryWithNonIntegralResultAnalyzer(typeSymbol, ResultType, DbCommandAttribute, diagnostics);
        DbCommandAnalyzers.ExecuteMutuallyExclusivePropertiesAnalyzer(typeSymbol, DbCommandAttribute, diagnostics);

        return diagnostics.ToImmutableList();
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
            it.OriginalDefinition.ToDisplayString().StartsWith(CommandInterfaceFullName));

        if (commandInterface is not null)
            return GetResultInfo(commandInterface);

        var queryInterface = typeSymbol.AllInterfaces.FirstOrDefault(it =>
            it.OriginalDefinition.ToDisplayString().StartsWith(QueryInterfaceFullName));

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

    private static ImmutableArray<PropertyInfo> GetDbCommandObjProperties(
        INamedTypeSymbol typeSymbol, DbParamsCase paramsCase, DbCommandSourceGenSettings settings)
    {
        Dictionary<string, PropertyInfo> primaryProperties;

        if (typeSymbol.IsRecord)
        {
            var primaryConstructor = typeSymbol.Constructors.FirstOrDefault(c => c.IsPrimaryConstructor());
            primaryProperties = primaryConstructor?.Parameters
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
            .Select(p => new PropertyInfo(p.Name, GetParameterName(p, paramsCase, settings)));

        return primaryProperties.Values.Concat(normalProps).ToImmutableArray();
    }

    private static string GetParameterName(ISymbol prop, DbParamsCase paramsCase, DbCommandSourceGenSettings settings)
    {
        var columnNameAttribute = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.Contains(nameof(ColumnAttribute)) == true);

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
