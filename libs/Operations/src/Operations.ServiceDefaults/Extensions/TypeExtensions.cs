// Copyright (c) ABCDEG. All rights reserved.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Operations.ServiceDefaults.Extensions;

public static class TypeExtensions
{
    /// <summary>
    ///     Gets all properties that have a specified attribute, either directly on the property
    ///     or on the corresponding parameter of the type's primary constructor (for records).
    /// </summary>
    public static IReadOnlySet<PropertyInfo> GetPropertiesWithAttribute<TAttribute>(this Type type)
        where TAttribute : Attribute
    {
        var propertiesWithAttribute = new HashSet<PropertyInfo>();
        var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in allProperties.Where(p => p.IsDefined(typeof(TAttribute), inherit: true)))
        {
            propertiesWithAttribute.Add(prop);
        }

        var primaryConstructor = type.GetPrimaryConstructor();

        if (primaryConstructor is not null)
        {
            var parametersWithAttribute = primaryConstructor.GetParameters()
                .Where(p => p.IsDefined(typeof(TAttribute), inherit: true));

            foreach (var param in parametersWithAttribute)
            {
                var matchingProperty = allProperties
                    .FirstOrDefault(prop => prop.Name == param.Name && prop.PropertyType == param.ParameterType);

                if (matchingProperty is not null)
                    propertiesWithAttribute.Add(matchingProperty);
            }
        }

        return propertiesWithAttribute;
    }

    /// <summary>
    ///     Get property attribute, if the property is a Record property the attribute is derived from the constructor parameter
    /// </summary>
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
    ///     Helper method to find the primary constructor of a type using reflection heuristics.
    ///     This should work reliably for records.
    /// </summary>
    public static ConstructorInfo? GetPrimaryConstructor(this Type type)
    {
        var initOnlyProperties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.IsInitOnly()).ToArray();

        if (initOnlyProperties.Length == 0)
            return null;

        var constructorCandidates = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(c => c.GetCustomAttribute<CompilerGeneratedAttribute>() is null);

        foreach (var constructor in constructorCandidates)
        {
            var parameters = constructor.GetParameters();

            var allParamsMatch = parameters.All(param =>
                initOnlyProperties.Any(prop => prop.Name == param.Name && prop.PropertyType == param.ParameterType)
            );

            if (allParamsMatch)
                return constructor;
        }

        return null;
    }

    public static bool IsInitOnly(this PropertyInfo property)
    {
        if (property is not { CanRead: true, CanWrite: true, SetMethod: not null })
            return false;

        return property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
    }
}
