// Copyright (c) Momentum .NET. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Shared utility methods for type checking and name formatting.
///     Consolidates type-related logic from multiple service classes.
/// </summary>
public static class TypeUtils
{
    /// <summary>
    ///     Determines if a type is a simple value type (primitives, enums, common framework types).
    /// </summary>
    public static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               Nullable.GetUnderlyingType(type) != null && IsPrimitiveType(Nullable.GetUnderlyingType(type)!);
    }

    /// <summary>
    ///     Determines if a type is a collection type (arrays, lists, dictionaries, sets, etc.).
    /// </summary>
    public static bool IsCollectionType(Type type)
    {
        if (type.IsArray)
            return true;

        if (!type.IsGenericType)
            return false;

        var genericDef = type.GetGenericTypeDefinition();

        return genericDef == typeof(List<>) ||
               genericDef == typeof(IList<>) ||
               genericDef == typeof(IEnumerable<>) ||
               genericDef == typeof(ICollection<>) ||
               genericDef == typeof(IReadOnlyList<>) ||
               genericDef == typeof(IReadOnlyCollection<>) ||
               genericDef == typeof(HashSet<>) ||
               genericDef == typeof(ISet<>) ||
               genericDef == typeof(Dictionary<,>) ||
               genericDef == typeof(IDictionary<,>) ||
               genericDef == typeof(IReadOnlyDictionary<,>);
    }

    /// <summary>
    ///     Determines if a type is complex (not a simple type, enum, or common framework type).
    ///     Uses cycle detection to handle recursive type references.
    /// </summary>
    public static bool IsComplexType(Type type)
    {
        return IsComplexType(type, []);
    }

    /// <summary>
    ///     Internal overload with visited types tracking for cycle detection.
    /// </summary>
    private static bool IsComplexType(Type type, HashSet<Type> visitedTypes)
    {
        // Prevent infinite recursion by tracking visited types
        if (!visitedTypes.Add(type))
        {
            return false; // Already visited, assume not complex to break cycles
        }

        try
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (IsCollectionType(underlyingType))
            {
                if (underlyingType.IsArray)
                {
                    var elementType = underlyingType.GetElementType();

                    return elementType != null && IsComplexType(elementType, visitedTypes);
                }

                if (underlyingType.IsGenericType)
                {
                    var elementType = underlyingType.GetGenericArguments().FirstOrDefault();

                    return elementType != null && IsComplexType(elementType, visitedTypes);
                }
            }

            // Primitive types and common framework types are not complex
            return !underlyingType.IsPrimitive
                   && underlyingType != typeof(string)
                   && underlyingType != typeof(DateTime)
                   && underlyingType != typeof(DateTimeOffset)
                   && underlyingType != typeof(TimeSpan)
                   && underlyingType != typeof(Guid)
                   && underlyingType != typeof(decimal)
                   && !underlyingType.IsEnum;
        }
        finally
        {
            // Clean up visited types for this path
            visitedTypes.Remove(type);
        }
    }

    /// <summary>
    /// Gets the full namespace + type name without assembly qualification.
    /// This is useful for creating clean filenames and identifiers while preserving full type identification.
    /// </summary>
    /// <param name="type">The type to get the clean name for</param>
    /// <returns>Full namespace + type name without assembly info (e.g., "System.Collections.Generic.Dictionary&lt;System.String, System.Object&gt;")</returns>
    public static string GetCleanTypeName(Type type)
    {
        if (type.FullName != null && !type.FullName.Contains("[["))
        {
            // Simple case - no assembly qualification
            return type.FullName;
        }
        
        // For generic types or types with assembly qualification, build the name manually
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            var namespaceName = genericTypeDef.Namespace ?? "";
            var typeName = genericTypeDef.Name;
            
            // Remove the backtick and number for generic types (e.g., Dictionary`2 -> Dictionary)
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex > 0)
            {
                typeName = typeName[..backtickIndex];
            }
            
            var genericArgs = type.GetGenericArguments()
                .Select(GetCleanTypeName)
                .ToArray();
            
            if (genericArgs.Length > 0)
            {
                return $"{namespaceName}.{typeName}<{string.Join(", ", genericArgs)}>";
            }
            
            return $"{namespaceName}.{typeName}";
        }
        
        // Non-generic type
        return $"{type.Namespace}.{type.Name}";
    }

    /// <summary>
    ///     Gets a friendly display name for a type (e.g., "string" instead of "String").
    ///     Handles nullable types and generics appropriately.
    /// </summary>
    public static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(DateTimeOffset)) return "DateTimeOffset";
        if (type == typeof(TimeSpan)) return "TimeSpan";
        if (type == typeof(Guid)) return "Guid";

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);

        if (underlyingType != null)
        {
            return GetFriendlyTypeName(underlyingType) + "?";
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            // Check if the type name contains a backtick (generic type indicator)
            var backtickIndex = type.Name.IndexOf('`');
            var genericTypeName = backtickIndex >= 0 ? type.Name[..backtickIndex] : type.Name;
            var genericArgs = type.GetGenericArguments().Select(GetFriendlyTypeName);

            return $"{genericTypeName}<{string.Join(", ", genericArgs)}>";
        }

        return type.Name;
    }

    /// <summary>
    ///     Gets the element type from a collection type (arrays or generic collections).
    ///     For dictionaries, returns the value type (second generic argument).
    /// </summary>
    public static Type? GetElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            var genericDef = type.GetGenericTypeDefinition();

            // For dictionary types, return the value type (second argument)
            if (genericDef == typeof(Dictionary<,>) ||
                genericDef == typeof(IDictionary<,>) ||
                genericDef == typeof(IReadOnlyDictionary<,>))
            {
                return genericArgs.Length > 1 ? genericArgs[1] : null;
            }

            // For other collections, return the first (and typically only) type argument
            return genericArgs.FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    ///     Gets the element type name from a collection type.
    /// </summary>
    public static string? GetElementTypeName(Type type)
    {
        var elementType = GetElementType(type);

        return elementType?.Name;
    }

    /// <summary>
    ///     Collects all complex types from a collection of event properties.
    /// </summary>
    public static HashSet<Type> CollectComplexTypesFromProperties(IEnumerable<EventPropertyMetadata> properties)
    {
        var complexTypes = new HashSet<Type>();

        foreach (var property in properties)
        {
            if (property.IsComplexType)
            {
                if (!IsCollectionType(property.PropertyType))
                {
                    complexTypes.Add(property.PropertyType);
                }
                else
                {
                    var elementType = GetElementType(property.PropertyType);

                    if (elementType != null && IsComplexType(elementType))
                    {
                        complexTypes.Add(elementType);
                    }
                }
            }
        }

        return complexTypes;
    }

    /// <summary>
    ///     Collects all nested complex types from a parent type recursively.
    /// </summary>
    public static void CollectNestedComplexTypes(Type parentType, HashSet<Type> schemaTypes)
    {
        var properties = parentType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var propertyType in properties.Select(p => p.PropertyType))
        {
            if (!IsComplexType(propertyType))
                continue;

            if (!IsCollectionType(propertyType))
            {
                if (schemaTypes.Add(propertyType))
                {
                    // If this is a new type, recursively process its properties
                    CollectNestedComplexTypes(propertyType, schemaTypes);
                }
            }
            else
            {
                var elementType = GetElementType(propertyType);

                if (elementType is not null && IsComplexType(elementType) && schemaTypes.Add(elementType))
                {
                    // If this is a new type, recursively process its properties
                    CollectNestedComplexTypes(elementType, schemaTypes);
                }
            }
        }
    }

    /// <summary>
    ///     Determines if a property is required based on RequiredAttribute or nullability context.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns>True if the property is required; otherwise, false.</returns>
    public static bool IsRequiredProperty(PropertyInfo property)
    {
        if (property.GetCustomAttribute<RequiredAttribute>() != null)
            return true;

        var nullabilityContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(property);

        return nullabilityInfo.WriteState == NullabilityState.NotNull;
    }
}
