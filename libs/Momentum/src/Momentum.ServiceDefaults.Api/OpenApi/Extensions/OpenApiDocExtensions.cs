// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.XmlDocs;
using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Momentum.ServiceDefaults.Api.OpenApi.Extensions;

public static class OpenApiDocExtensions
{
    public static JsonNode? ConvertToJsonNode(this Type type, string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return Type.GetTypeCode(underlyingType) switch
        {
            TypeCode.String or TypeCode.Char => JsonValue.Create(value),
            TypeCode.Boolean => ParseBoolean(value),
            TypeCode.Int16 => ParseShort(value),
            TypeCode.Int32 => ParseInt(value),
            TypeCode.Int64 => ParseLong(value),
            TypeCode.Single => ParseFloat(value),
            TypeCode.Double => ParseDouble(value),
            TypeCode.Decimal => ParseDecimal(value),
            TypeCode.Byte => ParseByte(value),
            TypeCode.DateTime => ParseDateTime(value),
            _ => NonStandardTypesHandler(underlyingType, value)
        };

        static JsonNode? NonStandardTypesHandler(Type type, string value) =>
            type switch
            {
                _ when type == typeof(Guid) => JsonValue.Create(value),
                _ when type == typeof(DateTimeOffset) => ParseDateTimeOffset(value),
                _ when type == typeof(DateOnly) => ParseDateOnly(value),
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
            schema.Example = type.ConvertToJsonNode(xmlDoc.Example);
        }
    }

    private static JsonNode? ParseBoolean(string value) =>
        bool.TryParse(value, out var result) ? JsonValue.Create(result) : null;

    private static JsonNode? ParseShort(string value) =>
        short.TryParse(value, out var result) ? JsonValue.Create(result) : null;

    private static JsonNode? ParseInt(string value) =>
        int.TryParse(value, out var result) ? JsonValue.Create(result) : null;

    private static JsonNode? ParseLong(string value) =>
        long.TryParse(value, out var result) ? JsonValue.Create(result) : null;

    private static JsonNode? ParseFloat(string value) =>
        float.TryParse(value, out var result) ? JsonValue.Create(result) : null;

    private static JsonNode? ParseDouble(string value) =>
        double.TryParse(value, out var result) ? JsonValue.Create(result) : null;

    private static JsonNode? ParseDecimal(string value) =>
        decimal.TryParse(value, out var result) ? JsonValue.Create(result) : null;

    private static JsonNode? ParseByte(string value) =>
        byte.TryParse(value, out var result) ? JsonValue.Create((int)result) : null;

    private static JsonNode? ParseDateTime(string value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? JsonValue.Create(result.ToString("O", CultureInfo.InvariantCulture)) : null;

    private static JsonNode? ParseDateTimeOffset(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? JsonValue.Create(result.ToString("O", CultureInfo.InvariantCulture)) : null;

    private static JsonNode? ParseDateOnly(string value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? JsonValue.Create(result.ToString("O", CultureInfo.InvariantCulture)) : null;
}
