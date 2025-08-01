// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Operations.Extensions.XmlDocs;

namespace Operations.ServiceDefaults.Api.OpenApi.Extensions;

public static class OpenApiDocExtensions
{
    public static IOpenApiPrimitive? ConvertToOpenApiType(this Type type, string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return Type.GetTypeCode(underlyingType) switch
        {
            TypeCode.String => new OpenApiString(value),
            TypeCode.Char => new OpenApiString(value),
            TypeCode.Boolean => Parse<bool>(bool.TryParse, r => new OpenApiBoolean(r), value),
            TypeCode.Int16 => Parse<int>(int.TryParse, r => new OpenApiInteger(r), value),
            TypeCode.Int32 => Parse<int>(int.TryParse, r => new OpenApiInteger(r), value),
            TypeCode.Int64 => Parse<long>(long.TryParse, r => new OpenApiLong(r), value),
            TypeCode.Single => Parse<float>(float.TryParse, r => new OpenApiFloat(r), value),
            TypeCode.Double => Parse<double>(double.TryParse, r => new OpenApiDouble(r), value),
            TypeCode.Decimal => Parse<double>(double.TryParse, r => new OpenApiDouble(r), value), // openapi does not have a Decimal type
            TypeCode.Byte => Parse<byte>(byte.TryParse, r => new OpenApiInteger(r), value),
            TypeCode.DateTime => Parse<DateTime>(DateTime.TryParse, r => new OpenApiDateTime(r), value),
            _ => NonStandardTypesHandler(underlyingType, value)
        };

        static IOpenApiPrimitive? NonStandardTypesHandler(Type type, string value) =>
            type switch
            {
                _ when type == typeof(Guid) => new OpenApiString(value),
                _ when type == typeof(DateTimeOffset) => Parse<DateTimeOffset>(DateTimeOffset.TryParse, r => new OpenApiDateTime(r), value),
                _ when type == typeof(DateOnly)
                    => Parse<DateOnly>(DateOnly.TryParse, r => new OpenApiDate(r.ToDateTime(TimeOnly.MinValue)), value),
                _ => null
            };
    }

    public static void EnrichWithXmlDocInfo(this OpenApiSchema schema, XmlDocumentationInfo xmlDoc, Type type)
    {
        if (xmlDoc.Summary is not null)
        {
            schema.Description = xmlDoc.Summary;
        }

        if (xmlDoc.Remarks is not null)
        {
            schema.Description += $"\n\n{xmlDoc.Remarks}";
        }

        if (xmlDoc.Example is not null)
        {
            schema.Example = type.ConvertToOpenApiType(xmlDoc.Example);
        }
    }

    private delegate bool ParseDelegate<T>(string value, out T result);

    private static IOpenApiPrimitive? Parse<T>(ParseDelegate<T> parser, Func<T, IOpenApiPrimitive> fact, string value)
        => parser.Invoke(value, out var parseResult) ? fact(parseResult) : null;
}
