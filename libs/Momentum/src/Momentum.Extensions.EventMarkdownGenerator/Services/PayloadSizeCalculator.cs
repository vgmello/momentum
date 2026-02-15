// Copyright (c) Momentum .NET. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Calculates estimated payload sizes for event properties based on type analysis
///     and data annotation constraints (MaxLength, Range, StringLength).
///     Subclass to define serialization-format-specific overhead (JSON, Binary, etc.).
/// </summary>
public abstract class PayloadSizeCalculator
{
    /// <summary>Display name of the serialization format (e.g., "JSON", "Binary").</summary>
    public abstract string FormatName { get; }

    /// <summary>Overhead bytes for a serialized string value (e.g., JSON quotes: 2 bytes).</summary>
    public abstract int GetStringValueOverhead();

    /// <summary>Overhead bytes for a property entry including key and delimiters.</summary>
    public abstract int GetPropertyOverhead(string propertyName);

    /// <summary>Overhead bytes for object wrappers (e.g., JSON { } adds 2 bytes).</summary>
    public abstract int GetObjectOverhead();

    /// <summary>Overhead bytes per element separator (e.g., JSON comma: 1 byte).</summary>
    public abstract int GetElementSeparatorOverhead();

    /// <summary>Overhead bytes for collection wrappers (e.g., JSON [ ] adds 2 bytes).</summary>
    public abstract int GetCollectionOverhead();

    /// <summary>
    ///     Creates a PayloadSizeCalculator for the specified serialization format.
    /// </summary>
    public static PayloadSizeCalculator Create(string format) => format.ToLowerInvariant() switch
    {
        "json" => new JsonPayloadSizeCalculator(),
        "binary" => new BinaryPayloadSizeCalculator(),
        _ => throw new ArgumentException(
            $"Unknown serialization format: '{format}'. Supported: json, binary.",
            nameof(format))
    };

    /// <summary>
    ///     Calculates the estimated size in bytes for a property's serialized payload.
    /// </summary>
    /// <param name="property">The property to analyze for size constraints.</param>
    /// <param name="propertyType">The type of the property.</param>
    /// <returns>A result containing the estimated size, accuracy flag, and any warnings.</returns>
    public PayloadSizeResult CalculatePropertySize(PropertyInfo property, Type propertyType)
    {
        return CalculatePropertySize(property, propertyType, []);
    }

    /// <summary>
    ///     Resolves the BytesPerChar value from StringEncoding attributes in the hierarchy:
    ///     Property > Class > Assembly > default (1).
    /// </summary>
    internal static int ResolveStringEncoding(PropertyInfo property)
    {
        // Property-level attribute takes highest priority
        var propertyAttr = property.GetCustomAttribute<StringEncodingAttribute>();
        if (propertyAttr != null)
        {
            return propertyAttr.BytesPerChar;
        }

        // Class-level attribute is next
        var classAttr = property.DeclaringType?.GetCustomAttribute<StringEncodingAttribute>();
        if (classAttr != null)
        {
            return classAttr.BytesPerChar;
        }

        // Assembly-level attribute is last
        var assemblyAttr = property.DeclaringType?.Assembly.GetCustomAttribute<StringEncodingAttribute>();
        if (assemblyAttr != null)
        {
            return assemblyAttr.BytesPerChar;
        }

        // Default: 1 byte per character
        return 1;
    }

    private PayloadSizeResult CalculatePropertySize(PropertyInfo property, Type propertyType, HashSet<Type> visitedTypes)
    {
        try
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(string))
            {
                return CalculateStringSize(property);
            }

            if (TypeUtils.IsPrimitiveType(underlyingType))
            {
                return new PayloadSizeResult
                {
                    SizeBytes = GetPrimitiveTypeSize(underlyingType),
                    IsAccurate = true,
                    Warning = null
                };
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

    private PayloadSizeResult CalculateStringSize(PropertyInfo property)
    {
        var constraints = GetDataAnnotationConstraints(property);

        if (constraints.MaxLength.HasValue)
        {
            var bytesPerChar = ResolveStringEncoding(property);
            var overhead = GetStringValueOverhead();

            return new PayloadSizeResult
            {
                SizeBytes = (constraints.MaxLength.Value * bytesPerChar) + overhead,
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

    private PayloadSizeResult CalculateCollectionSize(PropertyInfo property, Type collectionType, HashSet<Type> visitedTypes)
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

        var totalSize = elementSizeResult.SizeBytes * estimatedCount;

        totalSize += GetCollectionOverhead();

        if (estimatedCount > 1)
        {
            totalSize += GetElementSeparatorOverhead() * (estimatedCount - 1);
        }

        return new PayloadSizeResult
        {
            SizeBytes = totalSize,
            IsAccurate = elementSizeResult.IsAccurate && constraints.MaxRange.HasValue,
            Warning = constraints.MaxRange.HasValue ? elementSizeResult.Warning : "Collection size estimated (no Range constraint)"
        };
    }

    private int CalculateSerializationOverhead(int propertyCount)
    {
        var overhead = GetObjectOverhead();

        if (propertyCount > 1)
        {
            overhead += GetElementSeparatorOverhead() * (propertyCount - 1);
        }

        return overhead;
    }

    private PayloadSizeResult CalculateComplexTypeSize(Type type, HashSet<Type> visitedTypes)
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
                var propertyCount = 0;

                foreach (var property in properties)
                {
                    try
                    {
                        var propertyResult = CalculatePropertySize(property, property.PropertyType, visitedTypes);
                        totalSize += propertyResult.SizeBytes;
                        propertyCount++;

                        totalSize += GetPropertyOverhead(property.Name);

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

                totalSize += CalculateSerializationOverhead(propertyCount);
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

    private PayloadSizeResult CalculateTypeSize(Type type, HashSet<Type> visitedTypes)
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
        var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();
        var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
        var rangeAttr = property.GetCustomAttribute<RangeAttribute>();

        return new DataAnnotationConstraints
        {
            MaxLength = stringLengthAttr?.MaximumLength ?? maxLengthAttr?.Length,
            MaxRange = rangeAttr is { Maximum: int maxRange } ? maxRange : null
        };
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
        public int? MaxLength { get; init; }
        public int? MaxRange { get; init; }
    }
}
