// Copyright (c) Momentum .NET. All rights reserved.

using System.Text;

namespace Momentum.Extensions.SourceGenerators.DbCommand.Writers;

internal class DbCommandHandlerSourceGenWriter : SourceGenBaseWriter
{
    public static string Write(DbCommandTypeInfo dbCommandTypeInfo)
    {
        var returnTypeDeclaration = dbCommandTypeInfo.ResultType is null
            ? "global::System.Threading.Tasks.Task"
            : $"global::System.Threading.Tasks.Task<{dbCommandTypeInfo.ResultType!.QualifiedTypeName}>";

        var dapperCall = CreateDapperCall(dbCommandTypeInfo.ResultType, dbCommandTypeInfo);

        var sourceBuilder = new StringBuilder();

        AppendFileHeader(sourceBuilder);
        AppendNamespace(sourceBuilder, dbCommandTypeInfo.Namespace);

        if (dbCommandTypeInfo.IsNestedType)
        {
            AppendContainingTypeStarts(sourceBuilder, dbCommandTypeInfo.ParentTypes);
        }
        else
        {
            var handlerClassName = $"{dbCommandTypeInfo.TypeName}Handler";
            sourceBuilder.AppendLine($"public static class {handlerClassName}");
            sourceBuilder.AppendLine("{");
        }

        var dataSourceKey = dbCommandTypeInfo.DbCommandAttribute.DataSource;
        var dataSourceParameterDeclaration = string.IsNullOrEmpty(dataSourceKey)
            ? "global::System.Data.Common.DbDataSource datasource"
            : $"[global::Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute(\"{dataSourceKey}\")] global::System.Data.Common.DbDataSource datasource";

        sourceBuilder.AppendLine(
            $"    public static async {returnTypeDeclaration} HandleAsync(global::{dbCommandTypeInfo.QualifiedTypeName} command, {dataSourceParameterDeclaration}, global::System.Threading.CancellationToken cancellationToken = default)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        await using var connection = await datasource.OpenConnectionAsync(cancellationToken);");
        sourceBuilder.AppendLine("        var dbParams = command.ToDbParams();");
        sourceBuilder.AppendLine($"        return await {dapperCall}");
        sourceBuilder.AppendLine("    }");

        if (dbCommandTypeInfo.IsNestedType)
        {
            AppendContainingTypeEnds(sourceBuilder, dbCommandTypeInfo.ParentTypes);
        }
        else
        {
            sourceBuilder.AppendLine("}");
        }

        return sourceBuilder.ToString();
    }

    private static string CreateDapperCall(DbCommandTypeInfo.ResultTypeInfo? resultTypeInfo, DbCommandTypeInfo dbCommandTypeInfo)
    {
        var dbCommandAttribute = dbCommandTypeInfo.DbCommandAttribute;
        var commandText = GetCommandText(dbCommandAttribute, dbCommandTypeInfo);
        var commandType = string.IsNullOrEmpty(dbCommandAttribute.Sp)
            ? "System.Data.CommandType.Text"
            : "System.Data.CommandType.StoredProcedure";

        var commandDefinitionCall =
            $"new global::Dapper.CommandDefinition(\"{commandText}\", dbParams, commandType: global::{commandType}, " +
            "cancellationToken: cancellationToken)";

        var methodCall = CreateMethodDapperCall(resultTypeInfo, dbCommandAttribute);

        return $"global::Dapper.SqlMapper.{methodCall}(connection, {commandDefinitionCall});";
    }

    private static string GetCommandText(DbCommandAttribute commandAttributesValues, DbCommandTypeInfo dbCommandTypeInfo)
    {
        if (!string.IsNullOrEmpty(commandAttributesValues.Fn))
        {
            var baseCommand = commandAttributesValues.Fn;
            var parameters = string.Join(", ", dbCommandTypeInfo.DbProperties.Select(p => $"@{p.ParameterName}"));

            if (baseCommand.StartsWith('$'))
                baseCommand = $"SELECT * FROM {baseCommand[1..]}";

            return $"{baseCommand}({parameters})";
        }

        return commandAttributesValues.Sp ?? commandAttributesValues.Sql!;
    }

    private static string CreateMethodDapperCall(DbCommandTypeInfo.ResultTypeInfo? resultTypeInfo,
        DbCommandAttribute commandAttributesValues)
    {
        if (resultTypeInfo is null)
        {
            return "ExecuteAsync";
        }

        // ICommand<short/int/long>
        if (resultTypeInfo.IsIntegralType)
        {
            return commandAttributesValues.NonQuery
                ? "ExecuteAsync"
                : $"ExecuteScalarAsync<{resultTypeInfo.GenericArgumentResultFullTypeName}>";
        }

        // ICommand<IEnumerable<TResul>>
        if (resultTypeInfo.IsEnumerableResult)
        {
            return $"QueryAsync<{resultTypeInfo.GenericArgumentResultFullTypeName}>";
        }

        // Single object result
        return $"QueryFirstOrDefaultAsync<{resultTypeInfo.GenericArgumentResultFullTypeName}>";
    }
}
