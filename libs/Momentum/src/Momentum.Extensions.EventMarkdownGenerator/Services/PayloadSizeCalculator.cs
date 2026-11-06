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
    protected abstract int GetStringValueOverhead();

    /// <summary>Overhead bytes for a property entry including key and delimiters.</summary>
    protected abstract int GetPropertyOverhead(string propertyName);

    /// <summary>Overhead bytes for object wrappers (e.g., JSON { } adds 2 bytes).</summary>
    protected abstract int GetObjectOverhead();

    /// <summary>Overhead bytes per element separator (e.g., JSON comma: 1 byte).</summary>
    protected abstract int GetElementSeparatorOverhead();

    /// <summary>Overhead bytes for collection wrappers (e.g., JSON [ ] adds 2 bytes).</summary>
    protected abstract int GetCollectionOverhead();

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

            // Uri serializes as a string, so it is sized the same way (MaxLength/StringLength or dynamic)
            if (underlyingType == typeof(string) || underlyingType == typeof(Uri))
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
            // Uri serializes as a string of unknown length; IsPrimitiveType matches it, so check it first
            if (type == typeof(Uri))
            {
                return new PayloadSizeResult
                {
                    SizeBytes = 0,
                    IsAccurate = false,
                    Warning = "Dynamic string size in collection"
                };
            }

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
        var source = GetDataAnnotationSource(property);

        var maxLengthAttr = GetAttribute<MaxLengthAttribute>(source);
        var stringLengthAttr = GetAttribute<StringLengthAttribute>(source);
        var lengthAttr = GetAttribute<LengthAttribute>(source);
        var rangeAttr = GetAttribute<RangeAttribute>(source);

        // [MaxLength] without an argument means "no limit" (Length = -1), so it gives no usable bound
        var maxLength = stringLengthAttr?.MaximumLength
                        ?? (maxLengthAttr is { Length: > 0 } ? (int?)maxLengthAttr.Length : null)
                        ?? lengthAttr?.MaximumLength;

        return new DataAnnotationConstraints
        {
            MaxLength = maxLength,
            MaxRange = rangeAttr is { Maximum: int maxRange } ? maxRange : null
        };
    }

    private static readonly Type[] SizeAnnotationTypes =
        [typeof(MaxLengthAttribute), typeof(StringLengthAttribute), typeof(LengthAttribute), typeof(RangeAttribute)];

    /// <summary>
    ///     Picks where to read size annotations from: the property when it has any of them, otherwise the
    ///     same-named, same-typed constructor parameter — <c>record E([MaxLength(50)] string Name)</c> applies
    ///     the attribute to the parameter. Reading from a single source keeps property and parameter
    ///     annotations from being mixed. The reflected (derived) type is searched before the declaring type
    ///     so positional parameters forwarded to a base record are found too.
    /// </summary>
    private static ICustomAttributeProvider GetDataAnnotationSource(PropertyInfo property)
    {
        if (SizeAnnotationTypes.Any(t => property.IsDefined(t, inherit: true)))
            return property;

        Type?[] candidateTypes = [property.ReflectedType, property.DeclaringType];

        var parameter = candidateTypes
            .Where(t => t is not null)
            .Distinct()
            .SelectMany(t => t!.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .SelectMany(c => c.GetParameters())
            .FirstOrDefault(p => p.ParameterType == property.PropertyType
                                 && string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase)
                                 && SizeAnnotationTypes.Any(t => p.IsDefined(t, inherit: false)));

        return (ICustomAttributeProvider?)parameter ?? property;
    }

    private static T? GetAttribute<T>(ICustomAttributeProvider provider) where T : Attribute =>
        provider.GetCustomAttributes(typeof(T), inherit: true).OfType<T>().FirstOrDefault();

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
            "DateOnly" => 4,
            "TimeOnly" => 8,
            "TimeSpan" => 8,
            "Guid" => 16,
            "Half" => 2,
            "Int128" => 16,
            "UInt128" => 16,
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
