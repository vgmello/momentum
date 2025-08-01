// Copyright (c) ABCDEG. All rights reserved.

using LinqToDB.Mapping;
using LinqToDB.Metadata;
using Operations.Extensions.Abstractions.Extensions;
using System.Reflection;

namespace Operations.Extensions.Data.LinqToDb;

public class SnakeCaseNamingConventionMetadataReader : IMetadataReader
{
    private readonly AttributeReader _reader = new();

    public MappingAttribute[] GetAttributes(Type type)
    {
        var attributes = _reader.GetAttributes(type);

        if (type.IsAbstract)
            return attributes;

        var allTableAttributes = attributes.OfType<TableAttribute>().ToList();
        var directTableAttributes = type.GetCustomAttributes<TableAttribute>(inherit: false);

        var tableAttribute = directTableAttributes.FirstOrDefault() ?? new TableAttribute();

        tableAttribute.Name ??= ToSnakeCase(type.Name.Pluralize());
        tableAttribute.Schema ??= allTableAttributes.FirstOrDefault(a => a.Schema is not null)?.Schema;

        return attributes.Except(allTableAttributes).Concat([tableAttribute]).ToArray();
    }

    public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
    {
        var attributes = _reader.GetAttributes(type, memberInfo);
        var hasColumnAttribute = false;

        foreach (var columnAttribute in attributes.OfType<ColumnAttribute>())
        {
            columnAttribute.Name ??= ToSnakeCase(memberInfo.GetCustomAttribute<ColumnAttribute>()?.Name ?? memberInfo.Name);
            hasColumnAttribute = true;
        }

        if (hasColumnAttribute)
            return attributes;

        if (type.IsDefined(typeof(TableAttribute), inherit: true))
        {
            return attributes.Concat([
                new ColumnAttribute
                {
                    Name = ToSnakeCase(memberInfo.GetCustomAttribute<ColumnAttribute>()?.Name ?? memberInfo.Name)
                }
            ]).ToArray();
        }

        return attributes;
    }

    public MemberInfo[] GetDynamicColumns(Type type) => [];

    public string GetObjectID() => nameof(SnakeCaseNamingConventionMetadataReader);

    private static string ToSnakeCase(string name) => name.ToSnakeCase();
}
