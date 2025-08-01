// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.Abstractions.Dapper;
using System.Text;

namespace Operations.Extensions.SourceGenerators.DbCommand.Writers;

internal class DbCommandDbParamsSourceGenWriter : SourceGenBaseWriter
{
    private static readonly string DbParamsProviderInterfaceFullName = typeof(IDbParamsProvider).FullName!;

    public static string Write(DbCommandTypeInfo dbCommandTypeInfo)
    {
        var sourceBuilder = new StringBuilder();

        AppendFileHeader(sourceBuilder);
        AppendNamespace(sourceBuilder, dbCommandTypeInfo.Namespace);
        AppendContainingTypeStarts(sourceBuilder, dbCommandTypeInfo.ParentTypes);

        var typeDeclaration = dbCommandTypeInfo.TypeDeclaration;

        if (!dbCommandTypeInfo.TypeDeclaration.Contains("abstract") && !dbCommandTypeInfo.TypeDeclaration.Contains("sealed"))
        {
            typeDeclaration = $"sealed {dbCommandTypeInfo.TypeDeclaration}";
        }

        sourceBuilder.AppendLine($"{typeDeclaration} : global::{DbParamsProviderInterfaceFullName}");
        sourceBuilder.AppendLine("{");

        GenerateToDbParamsMethod(sourceBuilder, dbCommandTypeInfo);

        sourceBuilder.AppendLine("}");

        AppendContainingTypeEnds(sourceBuilder, dbCommandTypeInfo.ParentTypes);

        return sourceBuilder.ToString();
    }

    private static void GenerateToDbParamsMethod(StringBuilder sb, DbCommandTypeInfo dbCommandTypeInfo)
    {
        sb.AppendLine("    public global::System.Object ToDbParams()");
        sb.AppendLine("    {");

        if (dbCommandTypeInfo.HasCustomDbPropertyNames)
        {
            sb.AppendLine("        var p = new");
            sb.AppendLine("        {");

            var properties = dbCommandTypeInfo.DbProperties.ToList();

            for (var i = 0; i < properties.Count - 1; i++)
            {
                var prop = properties[i];
                sb.AppendLine($"            {prop.ParameterName} = this.{prop.PropertyName},");
            }

            var lastProp = properties[^1];
            sb.AppendLine($"            {lastProp.ParameterName} = this.{lastProp.PropertyName}");

            sb.AppendLine("        };");
            sb.AppendLine("        return p;");
        }
        else
        {
            sb.AppendLine("        return this;");
        }

        sb.AppendLine("    }");
    }
}
