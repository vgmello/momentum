// Copyright (c) Momentum .NET. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Calculates estimated payload sizes for event properties based on type analysis
///     and data annotation constraints (MaxLength, Range, StringLength).
/// </summary>
public static class PayloadSizeCalculator
{
    /// <summary>
    ///     Calculates the estimated size in bytes for a property's serialized payload.
    /// </summary>
    /// <param name="property">The property to analyze for size constraints.</param>
    /// <param name="propertyType">The type of the property.</param>
    /// <returns>A result containing the estimated size, accuracy flag, and any warnings.</returns>
    public static PayloadSizeResult CalculatePropertySize(PropertyInfo property, Type propertyType)
    {
        return CalculatePropertySize(property, propertyType, []);
    }

    private static PayloadSizeResult CalculatePropertySize(PropertyInfo property, Type propertyType, HashSet<Type> visitedTypes)
    {
        try
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (TypeUtils.IsPrimitiveType(underlyingType))
            {
                return new PayloadSizeResult
                {
                    SizeBytes = GetPrimitiveTypeSize(underlyingType),
                    IsAccurate = true,
                    Warning = null
                };
            }

            if (underlyingType == typeof(string))
            {
                return CalculateStringSize(property);
            }

            if (TypeUtils.IsCollectionType(underlyingType))
            {
                return CalculateCollectionSize(property, underlyingType, visitedTypes);
            }

            // Handle complex types
            return CalculateComplexTypeSize(underlyingType, visitedTypes);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or TypeLoadException)
        {
            return new PayloadSizeResult
            {
                SizeBytes = 0,
                IsAccurate = false,
                Warning = $"Unable to analyze property due to missing dependency ({ex.GetType().Name})"
            };
        }
    }

    /// <summary>
    ///     Calculates string payload size using worst-case UTF-32 encoding (4 bytes per character).
    ///     This provides a conservative upper bound for JSON serialization which typically uses UTF-8.
    ///     Uses MaxLength or StringLength attributes when available for accurate estimation.
    /// </summary>
    private static PayloadSizeResult CalculateStringSize(PropertyInfo property)
    {
        var constraints = GetDataAnnotationConstraints(property);

        if (constraints.MaxLength.HasValue)
        {
            // Use 4 bytes per character (UTF-32) as worst-case estimate
            return new PayloadSizeResult
            {
                SizeBytes = constraints.MaxLength.Value * 4,
                IsAccurate = true,
                Warning = null
            };
        }

        return new PayloadSizeResult
        {
            SizeBytes = 0,
            IsAccurate = false,
            Warning = "Dynamic size - no MaxLength constraint"
        };
    }

    private static PayloadSizeResult CalculateCollectionSize(PropertyInfo property, Type collectionType, HashSet<Type> visitedTypes)
    {
        var elementType = TypeUtils.GetElementType(collectionType);

        if (elementType == null)
        {
            return new PayloadSizeResult
            {
                SizeBytes = 0,
                IsAccurate = false,
                Warning = "Unknown collection element type"
            };
        }

        var constraints = GetDataAnnotationConstraints(property);
        var estimatedCount = constraints.MaxRange ?? 10;

        var elementSizeResult = CalculateTypeSize(elementType, visitedTypes);

        return new PayloadSizeResult
        {
            SizeBytes = elementSizeResult.SizeBytes * estimatedCount,
            IsAccurate = elementSizeResult.IsAccurate && constraints.MaxRange.HasValue,
            Warning = constraints.MaxRange.HasValue ? elementSizeResult.Warning : "Collection size estimated (no Range constraint)"
        };
    }

    private static PayloadSizeResult CalculateComplexTypeSize(Type type, HashSet<Type> visitedTypes)
    {
        // Prevent infinite recursion
        if (!visitedTypes.Add(type))
        {
            return new PayloadSizeResult
            {
                SizeBytes = 0,
                IsAccurate = false,
                Warning = "Circular reference detected"
            };
        }

        try
        {
            var totalSize = 0;
            var isAccurate = true;
            var warnings = new List<string>();

            try
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    try
                    {
                        var propertyResult = CalculatePropertySize(property, property.PropertyType, visitedTypes);
                        totalSize += propertyResult.SizeBytes;

                        if (!propertyResult.IsAccurate)
                        {
                            isAccurate = false;
                        }

                        if (!string.IsNullOrEmpty(propertyResult.Warning))
                        {
                            warnings.Add($"{property.Name}: {propertyResult.Warning}");
                        }
                    }
                    catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or TypeLoadException)
                    {
                        // Handle missing dependencies gracefully
                        warnings.Add($"{property.Name}: Unable to analyze due to missing dependency ({ex.GetType().Name})");
                        isAccurate = false;
                    }
                }
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or TypeLoadException)
            {
                // Handle missing dependencies when getting properties
                return new PayloadSizeResult
                {
                    SizeBytes = 0,
                    IsAccurate = false,
                    Warning = $"Unable to analyze type due to missing dependency ({ex.GetType().Name})"
                };
            }

            return new PayloadSizeResult
            {
                SizeBytes = totalSize,
                IsAccurate = isAccurate,
                Warning = warnings.Count > 0 ? string.Join(", ", warnings) : null
            };
        }
        finally
        {
            visitedTypes.Remove(type);
        }
    }

    private static PayloadSizeResult CalculateTypeSize(Type type, HashSet<Type> visitedTypes)
    {
        try
        {
            if (TypeUtils.IsPrimitiveType(type))
            {
                return new PayloadSizeResult
                {
                    SizeBytes = GetPrimitiveTypeSize(type),
                    IsAccurate = true,
                    Warning = null
                };
            }

            if (type == typeof(string))
            {
                return new PayloadSizeResult
                {
                    SizeBytes = 0,
                    IsAccurate = false,
                    Warning = "Dynamic string size in collection"
                };
            }

            return CalculateComplexTypeSize(type, visitedTypes);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or TypeLoadException)
        {
            return new PayloadSizeResult
            {
                SizeBytes = 0,
                IsAccurate = false,
                Warning = $"Unable to analyze type due to missing dependency ({ex.GetType().Name})"
            };
        }
    }

    private static DataAnnotationConstraints GetDataAnnotationConstraints(PropertyInfo property)
    {
        var result = new DataAnnotationConstraints();

        // Check MaxLength attribute
        var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();

        if (maxLengthAttr != null)
        {
            result.MaxLength = maxLengthAttr.Length;
        }

        // Check StringLength attribute
        var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();

        if (stringLengthAttr != null)
        {
            result.MaxLength = stringLengthAttr.MaximumLength;
        }

        // Check Range attribute
        var rangeAttr = property.GetCustomAttribute<RangeAttribute>();

        if (rangeAttr is { Maximum: int maxRange })
        {
            result.MaxRange = maxRange;
        }

        return result;
    }

    private static int GetPrimitiveTypeSize(Type type)
    {
        return type.Name switch
        {
            "Boolean" => 1,
            "Byte" => 1,
            "SByte" => 1,
            "Int16" => 2,
            "UInt16" => 2,
            "Int32" => 4,
            "UInt32" => 4,
            "Int64" => 8,
            "UInt64" => 8,
            "Single" => 4,
            "Double" => 8,
            "Decimal" => 16,
            "DateTime" => 8,
            "DateTimeOffset" => 10,
            "TimeSpan" => 8,
            "Guid" => 16,
            _ when type.IsEnum => 4,
            _ => 4 // Default fallback
        };
    }

    private sealed record DataAnnotationConstraints
    {
        public int? MaxLength { get; set; }
        public int? MaxRange { get; set; }
    }
}

public record PayloadSizeResult
{
    public int SizeBytes { get; init; }
    public bool IsAccurate { get; init; }
    public string? Warning { get; init; }
}
