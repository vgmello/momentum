// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Momentum.ServiceDefaults.Extensions;

/// <summary>
///     Provides extension methods for reflection operations on types, with special support for C# records and init-only properties.
/// </summary>
/// <remarks>
///     <para>This class contains utilities specifically designed to work with modern C# language features
///     including records, init-only properties, and primary constructors. These extensions are particularly
///     useful for framework code that needs to discover attributes and metadata from both traditional
///     properties and record parameters.</para>
///     
///     <para>Key scenarios supported:</para>
///     <list type="bullet">
///         <item>Attribute discovery on record parameters and their corresponding properties</item>
///         <item>Primary constructor detection for records and classes</item>
///         <item>Init-only property identification for immutable data patterns</item>
///         <item>Reflection-based metadata extraction for validation and serialization</item>
///     </list>
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
    ///     <para>This method is essential for working with C# records where attributes can be applied
    ///     to constructor parameters and are not automatically inherited by the generated properties.
    ///     It searches both the property itself and the matching constructor parameter.</para>
    ///     
    ///     <para>The method handles the following scenarios:</para>
    ///     <list type="bullet">
    ///         <item>Traditional properties with attributes directly applied</item>
    ///         <item>Record properties where attributes are applied to constructor parameters</item>
    ///         <item>Mixed scenarios where some properties have direct attributes and others have parameter attributes</item>
    ///         <item>Classes with primary constructors (C# 12+ feature)</item>
    ///     </list>
    ///     
    ///     <para>This functionality is commonly used by validation frameworks, serializers,
    ///     and other reflection-based libraries that need to discover metadata consistently
    ///     across different property declaration styles.</para>
    /// </remarks>
    /// <example>
    ///     <para><strong>Working with Record Attributes:</strong></para>
    ///     <code>
    /// // Record with attributes on constructor parameters
    /// public record OrderRequest(
    ///     [Required] string CustomerId,
    ///     [Range(1, 100)] int Quantity,
    ///     string? Notes);
    /// 
    /// // Find all properties with validation attributes
    /// var type = typeof(OrderRequest);
    /// var requiredProps = type.GetPropertiesWithAttribute&lt;RequiredAttribute&gt;();
    /// var rangeProps = type.GetPropertiesWithAttribute&lt;RangeAttribute&gt;();
    /// 
    /// // requiredProps contains: { CustomerId }
    /// // rangeProps contains: { Quantity }
    /// </code>
    ///     
    ///     <para><strong>Mixed Property and Parameter Attributes:</strong></para>
    ///     <code>
    /// public record CustomerData(
    ///     [EmailAddress] string Email,
    ///     string Name)
    /// {
    ///     [Phone]
    ///     public string? PhoneNumber { get; init; }
    /// }
    /// 
    /// // Find all properties with validation attributes
    /// var validatedProps = typeof(CustomerData).GetPropertiesWithAttribute&lt;ValidationAttribute&gt;();
    /// // Returns: { Email, PhoneNumber }
    /// </code>
    ///     
    ///     <para><strong>Using in Validation Framework:</strong></para>
    ///     <code>
    /// public static class ValidationExtensions
    /// {
    ///     public static ValidationResult ValidateObject&lt;T&gt;(T obj)
    ///     {
    ///         var type = typeof(T);
    ///         var validatedProperties = type.GetPropertiesWithAttribute&lt;ValidationAttribute&gt;();
    ///         
    ///         var errors = new List&lt;string&gt;();
    ///         foreach (var property in validatedProperties)
    ///         {
    ///             var value = property.GetValue(obj);
    ///             var attributes = property.GetCustomAttributes&lt;ValidationAttribute&gt;();
    ///             
    ///             foreach (var attr in attributes)
    ///             {
    ///                 if (!attr.IsValid(value))
    ///                 {
    ///                     errors.Add($"{property.Name}: {attr.ErrorMessage}");
    ///                 }
    ///             }
    ///         }
    ///         
    ///         return new ValidationResult(errors);
    ///     }
    /// }
    /// </code>
    /// </example>
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
    ///     Gets a custom attribute from a property, with fallback to the corresponding constructor parameter for records.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to retrieve.</typeparam>
    /// <param name="propertyInfo">The property to examine for the attribute.</param>
    /// <param name="primaryConstructor">The primary constructor to check for parameter attributes, if the property doesn't have the attribute directly.</param>
    /// <returns>
    ///     The attribute instance if found on the property or corresponding constructor parameter;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>This method implements a two-stage attribute lookup strategy:</para>
    ///     <list type="number">
    ///         <item>First, check if the attribute is directly applied to the property</item>
    ///         <item>If not found and a primary constructor is provided, check the corresponding constructor parameter</item>
    ///     </list>
    ///     
    ///     <para>This approach is necessary because in C# records, attributes applied to constructor
    ///     parameters are not automatically inherited by the generated properties. This method provides
    ///     a unified way to retrieve attributes regardless of where they were originally declared.</para>
    ///     
    ///     <para>The method matches constructor parameters to properties by name and type, ensuring
    ///     that the correct parameter attribute is associated with the corresponding property.</para>
    /// </remarks>
    /// <example>
    ///     <para><strong>Retrieving Validation Attributes from Records:</strong></para>
    ///     <code>
    /// public record UserRegistration(
    ///     [Required] [EmailAddress] string Email,
    ///     [MinLength(8)] string Password,
    ///     string DisplayName);
    /// 
    /// var type = typeof(UserRegistration);
    /// var emailProperty = type.GetProperty(nameof(UserRegistration.Email));
    /// var primaryConstructor = type.GetPrimaryConstructor();
    /// 
    /// // Get attributes from constructor parameter
    /// var requiredAttr = emailProperty.GetCustomAttribute&lt;RequiredAttribute&gt;(primaryConstructor);
    /// var emailAttr = emailProperty.GetCustomAttribute&lt;EmailAddressAttribute&gt;(primaryConstructor);
    /// 
    /// // Both attributes are found even though they were applied to the constructor parameter
    /// Console.WriteLine(requiredAttr != null); // True
    /// Console.WriteLine(emailAttr != null);    // True
    /// </code>
    ///     
    ///     <para><strong>Framework Usage for Dynamic Validation:</strong></para>
    ///     <code>
    /// public static class RecordValidator
    /// {
    ///     public static IEnumerable&lt;ValidationError&gt; Validate&lt;T&gt;(T record)
    ///     {
    ///         var type = typeof(T);
    ///         var primaryConstructor = type.GetPrimaryConstructor();
    ///         var properties = type.GetProperties();
    ///         
    ///         foreach (var property in properties)
    ///         {
    ///             var value = property.GetValue(record);
    ///             
    ///             // Check for Required attribute
    ///             var requiredAttr = property.GetCustomAttribute&lt;RequiredAttribute&gt;(primaryConstructor);
    ///             if (requiredAttr != null && value == null)
    ///             {
    ///                 yield return new ValidationError(property.Name, "Field is required");
    ///             }
    ///             
    ///             // Check for StringLength attribute
    ///             var lengthAttr = property.GetCustomAttribute&lt;StringLengthAttribute&gt;(primaryConstructor);
    ///             if (lengthAttr != null && value is string str && str.Length > lengthAttr.MaximumLength)
    ///             {
    ///                 yield return new ValidationError(property.Name, $"Maximum length is {lengthAttr.MaximumLength}");
    ///             }
    ///         }
    ///     }
    /// }
    /// </code>
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
    ///     The <see cref="ConstructorInfo"/> for the primary constructor if found;
    ///     otherwise, <c>null</c> if no primary constructor pattern is detected.
    /// </returns>
    /// <remarks>
    ///     <para>This method implements a heuristic approach to identify primary constructors by:</para>
    ///     <list type="number">
    ///         <item>Finding all init-only properties on the type</item>
    ///         <item>Locating non-compiler-generated constructors</item>
    ///         <item>Matching constructor parameters to init-only properties by name and type</item>
    ///         <item>Returning the constructor where all parameters have corresponding init-only properties</item>
    ///     </list>
    ///     
    ///     <para>This approach is particularly effective for:</para>
    ///     <list type="bullet">
    ///         <item>C# records (which always have primary constructors)</item>
    ///         <item>Classes with primary constructors (C# 12+ feature)</item>
    ///         <item>Immutable classes following init-only property patterns</item>
    ///         <item>DTOs and value objects designed for immutability</item>
    ///     </list>
    ///     
    ///     <para><strong>Important:</strong> This method relies on naming conventions and property patterns.
    ///     It may not work correctly for types with multiple constructors that could match the heuristics,
    ///     or for types that don't follow standard immutable object patterns.</para>
    /// </remarks>
    /// <example>
    ///     <para><strong>Working with Different Constructor Patterns:</strong></para>
    ///     <code>
    /// // Record - primary constructor automatically detected
    /// public record Customer(string Name, string Email, int Age);
    /// 
    /// // Class with primary constructor (C# 12+)
    /// public class Order(Guid id, string customerEmail, decimal total)
    /// {
    ///     public Guid Id { get; } = id;
    ///     public string CustomerEmail { get; } = customerEmail;
    ///     public decimal Total { get; } = total;
    /// }
    /// 
    /// // Immutable class with init-only properties
    /// public class Product
    /// {
    ///     public Product(string name, decimal price, string category)
    ///     {
    ///         Name = name;
    ///         Price = price;
    ///         Category = category;
    ///     }
    ///     
    ///     public string Name { get; init; }
    ///     public decimal Price { get; init; }
    ///     public string Category { get; init; }
    /// }
    /// 
    /// // All of these will have their primary constructors detected
    /// var customerCtor = typeof(Customer).GetPrimaryConstructor();
    /// var orderCtor = typeof(Order).GetPrimaryConstructor();
    /// var productCtor = typeof(Product).GetPrimaryConstructor();
    /// 
    /// Console.WriteLine(customerCtor?.GetParameters().Length); // 3
    /// Console.WriteLine(orderCtor?.GetParameters().Length);    // 3
    /// Console.WriteLine(productCtor?.GetParameters().Length);  // 3
    /// </code>
    ///     
    ///     <para><strong>Using Primary Constructor for Metadata Extraction:</strong></para>
    ///     <code>
    /// public static class TypeAnalyzer
    /// {
    ///     public static ConstructorMetadata AnalyzeType(Type type)
    ///     {
    ///         var primaryConstructor = type.GetPrimaryConstructor();
    ///         if (primaryConstructor == null)
    ///         {
    ///             return new ConstructorMetadata(false, Array.Empty&lt;ParameterInfo&gt;());
    ///         }
    ///         
    ///         var parameters = primaryConstructor.GetParameters();
    ///         var hasValidationAttributes = parameters.Any(p => 
    ///             p.GetCustomAttributes&lt;ValidationAttribute&gt;().Any());
    ///             
    ///         return new ConstructorMetadata(hasValidationAttributes, parameters);
    ///     }
    /// }
    /// 
    /// public record ConstructorMetadata(bool HasValidation, ParameterInfo[] Parameters);
    /// </code>
    ///     
    ///     <para><strong>Limitations and Edge Cases:</strong></para>
    ///     <code>
    /// // This class will NOT be detected as having a primary constructor
    /// // because it has mutable properties
    /// public class MutableClass
    /// {
    ///     public MutableClass(string name) { Name = name; }
    ///     public string Name { get; set; } // Mutable property
    /// }
    /// 
    /// // This class has multiple constructors that could match
    /// // The method will return one of them, but it's not guaranteed which
    /// public class AmbiguousClass
    /// {
    ///     public AmbiguousClass(string name) { Name = name; }
    ///     public AmbiguousClass(string name, int age) { Name = name; Age = age; }
    ///     
    ///     public string Name { get; init; }
    ///     public int Age { get; init; }
    /// }
    /// </code>
    /// </example>
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

    /// <summary>
    ///     Determines whether a property is declared with the init-only setter (C# 9+ feature).
    /// </summary>
    /// <param name="property">The property to examine.</param>
    /// <returns>
    ///     <c>true</c> if the property has an init-only setter; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>This method detects init-only properties by examining the property's setter method
    ///     for the presence of the <see cref="IsExternalInit"/> modifier. Init-only properties
    ///     can only be set during object initialization (in constructors, initializers, or with statements).</para>
    ///     
    ///     <para>Init-only properties are commonly used in:</para>
    ///     <list type="bullet">
    ///         <item>Immutable data transfer objects (DTOs)</item>
    ///         <item>Value objects and entity models</item>
    ///         <item>Configuration objects</item>
    ///         <item>Record types (which use init-only properties by default)</item>
    ///         <item>API request/response models</item>
    ///     </list>
    ///     
    ///     <para>This information is useful for framework code that needs to distinguish between
    ///     mutable and immutable properties, such as serializers, validators, and ORM mapping.</para>
    /// </remarks>
    /// <example>
    ///     <para><strong>Detecting Init-Only Properties:</strong></para>
    ///     <code>
    /// public class CustomerData
    /// {
    ///     public string Name { get; init; }        // Init-only
    ///     public string Email { get; init; }       // Init-only
    ///     public string Phone { get; set; }        // Mutable
    ///     public DateTime CreatedAt { get; }        // Read-only
    /// }
    /// 
    /// var type = typeof(CustomerData);
    /// var properties = type.GetProperties();
    /// 
    /// foreach (var prop in properties)
    /// {
    ///     var isInitOnly = prop.IsInitOnly();
    ///     Console.WriteLine($"{prop.Name}: {(isInitOnly ? "Init-only" : "Mutable/Read-only")}");
    /// }
    /// 
    /// // Output:
    /// // Name: Init-only
    /// // Email: Init-only
    /// // Phone: Mutable/Read-only
    /// // CreatedAt: Mutable/Read-only
    /// </code>
    ///     
    ///     <para><strong>Using for Immutability Analysis:</strong></para>
    ///     <code>
    /// public static class ImmutabilityAnalyzer
    /// {
    ///     public static bool IsImmutableType(Type type)
    ///     {
    ///         var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    ///         
    ///         return properties.All(prop => 
    ///             !prop.CanWrite ||           // Read-only property
    ///             prop.IsInitOnly());         // Init-only property
    ///     }
    ///     
    ///     public static IEnumerable&lt;string&gt; GetMutableProperties(Type type)
    ///     {
    ///         return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    ///             .Where(prop => prop.CanWrite && !prop.IsInitOnly())
    ///             .Select(prop => prop.Name);
    ///     }
    /// }
    /// 
    /// // Usage
    /// var isImmutable = ImmutabilityAnalyzer.IsImmutableType(typeof(CustomerData));
    /// var mutableProps = ImmutabilityAnalyzer.GetMutableProperties(typeof(CustomerData));
    /// 
    /// Console.WriteLine($"Is immutable: {isImmutable}");                    // False
    /// Console.WriteLine($"Mutable properties: {string.Join(", ", mutableProps)}"); // Phone
    /// </code>
    ///     
    ///     <para><strong>Framework Integration Example:</strong></para>
    ///     <code>
    /// // Custom serializer that handles init-only properties differently
    /// public class ImmutableJsonConverter : JsonConverter
    /// {
    ///     public override bool CanConvert(Type typeToConvert)
    ///     {
    ///         return typeToConvert.GetProperties()
    ///             .Any(prop => prop.IsInitOnly());
    ///     }
    ///     
    ///     public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    ///     {
    ///         // Special handling for types with init-only properties
    ///         // Implementation would use constructor or with-expressions
    ///         return base.Read(ref reader, typeToConvert, options);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static bool IsInitOnly(this PropertyInfo property)
    {
        if (property is not { CanRead: true, CanWrite: true, SetMethod: not null })
            return false;

        return property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
    }
}
