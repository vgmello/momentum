// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Momentum.ServiceDefaults.Extensions;

/// <summary>
///     Provides extension methods for reflection operations on types, with special support for C# records and init-only properties.
/// </summary>
/// <remarks>
///     <!--@include: @code/reflection/type-extensions-detailed.md#class-overview -->
/// </remarks>
public static class TypeExtensions
{
    /// <summary>
    ///     Gets all properties that have a specified attribute, either directly on the property
    ///     or on the corresponding parameter of the type's primary constructor (for records).
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to search for.</typeparam>
    /// <param name="type">The type to examine for attributed properties.</param>
    /// <returns>
    ///     A read-only set of properties that have the specified attribute, either directly
    ///     applied to the property or to the corresponding constructor parameter.
    /// </returns>
    /// <remarks>
    ///     <!--@include: @code/reflection/type-extensions-detailed.md#get-properties-with-attribute -->
    /// </remarks>
    /// <example>
    ///     <!--@include: @code/examples/type-extensions-examples.md#GetPropertiesWithAttribute Examples -->
    /// </example>
    public static IReadOnlySet<PropertyInfo> GetPropertiesWithAttribute<TAttribute>(this Type type)
        where TAttribute : Attribute
    {
        var propertiesWithAttribute = new HashSet<PropertyInfo>();
        var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        AddPropertiesWithDirectAttribute<TAttribute>(allProperties, propertiesWithAttribute);
        AddPropertiesFromConstructorParameters<TAttribute>(type, allProperties, propertiesWithAttribute);

        return propertiesWithAttribute;
    }

    private static void AddPropertiesWithDirectAttribute<TAttribute>(
        PropertyInfo[] allProperties,
        HashSet<PropertyInfo> propertiesWithAttribute) where TAttribute : Attribute
    {
        foreach (var prop in allProperties)
        {
            if (prop.IsDefined(typeof(TAttribute), inherit: true))
            {
                propertiesWithAttribute.Add(prop);
            }
        }
    }

    private static void AddPropertiesFromConstructorParameters<TAttribute>(
        Type type,
        PropertyInfo[] allProperties,
        HashSet<PropertyInfo> propertiesWithAttribute) where TAttribute : Attribute
    {
        var primaryConstructor = type.GetPrimaryConstructor();
        if (primaryConstructor is null)
            return;

        var parameters = primaryConstructor.GetParameters();

        foreach (var param in parameters)
        {
            if (!param.IsDefined(typeof(TAttribute), inherit: true))
                continue;

            var matchingProperty = FindMatchingProperty(allProperties, param);
            if (matchingProperty is not null)
            {
                propertiesWithAttribute.Add(matchingProperty);
            }
        }
    }

    private static PropertyInfo? FindMatchingProperty(PropertyInfo[] properties, ParameterInfo param)
    {
        foreach (var prop in properties)
        {
            if (prop.Name == param.Name && prop.PropertyType == param.ParameterType)
                return prop;
        }

        return null;
    }

    /// <summary>
    ///     Gets a custom attribute from a property, with fallback to the corresponding constructor parameter for records.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to retrieve.</typeparam>
    /// <param name="propertyInfo">The property to examine for the attribute.</param>
    /// <param name="primaryConstructor">
    ///     The primary constructor to check for parameter attributes, if the property doesn't have the attribute
    ///     directly.
    /// </param>
    /// <returns>
    ///     The attribute instance if found on the property or corresponding constructor parameter;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <!--@include: @code/reflection/type-extensions-detailed.md#get-custom-attribute -->
    /// </remarks>
    /// <example>
    ///     <!--@include: @code/examples/type-extensions-examples.md#GetCustomAttribute Examples -->
    /// </example>
    public static TAttribute? GetCustomAttribute<TAttribute>(this PropertyInfo propertyInfo, ConstructorInfo? primaryConstructor)
        where TAttribute : Attribute
    {
        var attribute = propertyInfo.GetCustomAttribute<TAttribute>();

        if (attribute is not null)
            return attribute;

        attribute = primaryConstructor?.GetParameters()
            .FirstOrDefault(param => param.Name == propertyInfo.Name && param.ParameterType == propertyInfo.PropertyType)?
            .GetCustomAttribute<TAttribute>();

        return attribute;
    }

    /// <summary>
    ///     Finds the primary constructor of a type using reflection heuristics optimized for records and primary constructor patterns.
    /// </summary>
    /// <param name="type">The type to examine for a primary constructor.</param>
    /// <returns>
    ///     The <see cref="ConstructorInfo" /> for the primary constructor if found;
    ///     otherwise, <c>null</c> if no primary constructor pattern is detected.
    /// </returns>
    /// <remarks>
    ///     <!--@include: @code/reflection/type-extensions-detailed.md#get-primary-constructor -->
    /// </remarks>
    /// <example>
    ///     <!--@include: @code/examples/type-extensions-examples.md#GetPrimaryConstructor Examples -->
    /// </example>
    public static ConstructorInfo? GetPrimaryConstructor(this Type type)
    {
        var initOnlyProperties = CollectInitOnlyProperties(type);

        if (initOnlyProperties is null || initOnlyProperties.Count == 0)
            return null;

        return FindMatchingConstructor(type, initOnlyProperties);
    }

    private static List<PropertyInfo>? CollectInitOnlyProperties(Type type)
    {
        var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        List<PropertyInfo>? initOnlyProperties = null;

        foreach (var prop in allProperties)
        {
            if (prop.IsInitOnly())
            {
                initOnlyProperties ??= [];
                initOnlyProperties.Add(prop);
            }
        }

        return initOnlyProperties;
    }

    private static ConstructorInfo? FindMatchingConstructor(Type type, List<PropertyInfo> initOnlyProperties)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        foreach (var constructor in constructors)
        {
            if (constructor.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
                continue;

            if (AllParametersMatchProperties(constructor.GetParameters(), initOnlyProperties))
                return constructor;
        }

        return null;
    }

    private static bool AllParametersMatchProperties(ParameterInfo[] parameters, List<PropertyInfo> properties)
    {
        foreach (var param in parameters)
        {
            if (!HasMatchingProperty(param, properties))
                return false;
        }

        return true;
    }

    private static bool HasMatchingProperty(ParameterInfo param, List<PropertyInfo> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.Name == param.Name && prop.PropertyType == param.ParameterType)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Determines whether a property is declared with the init-only setter (C# 9+ feature).
    /// </summary>
    /// <param name="property">The property to examine.</param>
    /// <returns>
    ///     <c>true</c> if the property has an init-only setter; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <!--@include: @code/reflection/type-extensions-detailed.md#is-init-only -->
    /// </remarks>
    /// <example>
    ///     <!--@include: @code/examples/type-extensions-examples.md#IsInitOnly Examples -->
    /// </example>
    public static bool IsInitOnly(this PropertyInfo property)
    {
        if (property is not { CanRead: true, CanWrite: true, SetMethod: not null })
            return false;

        return property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
    }
}
