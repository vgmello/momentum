# Type Extensions Examples

## GetPropertiesWithAttribute Examples

### Working with Record Attributes

```csharp
// Record with attributes on constructor parameters
public record OrderRequest(
    [Required] string CustomerId,
    [Range(1, 100)] int Quantity,
    string? Notes);

// Find all properties with validation attributes
var type = typeof(OrderRequest);
var requiredProps = type.GetPropertiesWithAttribute<RequiredAttribute>();
var rangeProps = type.GetPropertiesWithAttribute<RangeAttribute>();

// requiredProps contains: { CustomerId }
// rangeProps contains: { Quantity }
```

### Mixed Property and Parameter Attributes

```csharp
public record CustomerData(
    [EmailAddress] string Email,
    string Name)
{
    [Phone]
    public string? PhoneNumber { get; init; }
}

// Find all properties with validation attributes
var validatedProps = typeof(CustomerData).GetPropertiesWithAttribute<ValidationAttribute>();
// Returns: { Email, PhoneNumber }
```

### Using in Validation Framework

```csharp
public static class ValidationExtensions
{
    public static ValidationResult ValidateObject<T>(T obj)
    {
        var type = typeof(T);
        var validatedProperties = type.GetPropertiesWithAttribute<ValidationAttribute>();
        
        var errors = new List<string>();
        foreach (var property in validatedProperties)
        {
            var value = property.GetValue(obj);
            var attributes = property.GetCustomAttributes<ValidationAttribute>();
            
            foreach (var attr in attributes)
            {
                if (!attr.IsValid(value))
                {
                    errors.Add($"{property.Name}: {attr.ErrorMessage}");
                }
            }
        }
        
        return new ValidationResult(errors);
    }
}
```

## GetCustomAttribute Examples

### Retrieving Validation Attributes from Records

```csharp
public record UserRegistration(
    [Required] [EmailAddress] string Email,
    [MinLength(8)] string Password,
    string DisplayName);

var type = typeof(UserRegistration);
var emailProperty = type.GetProperty(nameof(UserRegistration.Email));
var primaryConstructor = type.GetPrimaryConstructor();

// Get attributes from constructor parameter
var requiredAttr = emailProperty.GetCustomAttribute<RequiredAttribute>(primaryConstructor);
var emailAttr = emailProperty.GetCustomAttribute<EmailAddressAttribute>(primaryConstructor);

// Both attributes are found even though they were applied to the constructor parameter
Console.WriteLine(requiredAttr != null); // True
Console.WriteLine(emailAttr != null);    // True
```

### Framework Usage for Dynamic Validation

```csharp
public static class RecordValidator
{
    public static IEnumerable<ValidationError> Validate<T>(T record)
    {
        var type = typeof(T);
        var primaryConstructor = type.GetPrimaryConstructor();
        var properties = type.GetProperties();
        
        foreach (var property in properties)
        {
            var value = property.GetValue(record);
            
            // Check for Required attribute
            var requiredAttr = property.GetCustomAttribute<RequiredAttribute>(primaryConstructor);
            if (requiredAttr != null && value == null)
            {
                yield return new ValidationError(property.Name, "Field is required");
            }
            
            // Check for StringLength attribute
            var lengthAttr = property.GetCustomAttribute<StringLengthAttribute>(primaryConstructor);
            if (lengthAttr != null && value is string str && str.Length > lengthAttr.MaximumLength)
            {
                yield return new ValidationError(property.Name, $"Maximum length is {lengthAttr.MaximumLength}");
            }
        }
    }
}
```

## GetPrimaryConstructor Examples

### Working with Different Constructor Patterns

```csharp
// Record - primary constructor automatically detected
public record Customer(string Name, string Email, int Age);

// Class with primary constructor (C# 12+)
public class Order(Guid id, string customerEmail, decimal total)
{
    public Guid Id { get; } = id;
    public string CustomerEmail { get; } = customerEmail;
    public decimal Total { get; } = total;
}

// Immutable class with init-only properties
public class Product
{
    public Product(string name, decimal price, string category)
    {
        Name = name;
        Price = price;
        Category = category;
    }
    
    public string Name { get; init; }
    public decimal Price { get; init; }
    public string Category { get; init; }
}

// All of these will have their primary constructors detected
var customerCtor = typeof(Customer).GetPrimaryConstructor();
var orderCtor = typeof(Order).GetPrimaryConstructor();
var productCtor = typeof(Product).GetPrimaryConstructor();

Console.WriteLine(customerCtor?.GetParameters().Length); // 3
Console.WriteLine(orderCtor?.GetParameters().Length);    // 3
Console.WriteLine(productCtor?.GetParameters().Length);  // 3
```

### Using Primary Constructor for Metadata Extraction

```csharp
public static class TypeAnalyzer
{
    public static ConstructorMetadata AnalyzeType(Type type)
    {
        var primaryConstructor = type.GetPrimaryConstructor();
        if (primaryConstructor == null)
        {
            return new ConstructorMetadata(false, Array.Empty<ParameterInfo>());
        }
        
        var parameters = primaryConstructor.GetParameters();
        var hasValidationAttributes = parameters.Any(p => 
            p.GetCustomAttributes<ValidationAttribute>().Any());
            
        return new ConstructorMetadata(hasValidationAttributes, parameters);
    }
}

public record ConstructorMetadata(bool HasValidation, ParameterInfo[] Parameters);
```

### Limitations and Edge Cases

```csharp
// This class will NOT be detected as having a primary constructor
// because it has mutable properties
public class MutableClass
{
    public MutableClass(string name) { Name = name; }
    public string Name { get; set; } // Mutable property
}

// This class has multiple constructors that could match
// The method will return one of them, but it's not guaranteed which
public class AmbiguousClass
{
    public AmbiguousClass(string name) { Name = name; }
    public AmbiguousClass(string name, int age) { Name = name; Age = age; }
    
    public string Name { get; init; }
    public int Age { get; init; }
}
```

## IsInitOnly Examples

### Detecting Init-Only Properties

```csharp
public class CustomerData
{
    public string Name { get; init; }        // Init-only
    public string Email { get; init; }       // Init-only
    public string Phone { get; set; }        // Mutable
    public DateTime CreatedAt { get; }        // Read-only
}

var type = typeof(CustomerData);
var properties = type.GetProperties();

foreach (var prop in properties)
{
    var isInitOnly = prop.IsInitOnly();
    Console.WriteLine($"{prop.Name}: {(isInitOnly ? "Init-only" : "Mutable/Read-only")}");
}

// Output:
// Name: Init-only
// Email: Init-only
// Phone: Mutable/Read-only
// CreatedAt: Mutable/Read-only
```

### Using for Immutability Analysis

```csharp
public static class ImmutabilityAnalyzer
{
    public static bool IsImmutableType(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        return properties.All(prop => 
            !prop.CanWrite ||           // Read-only property
            prop.IsInitOnly());         // Init-only property
    }
    
    public static IEnumerable<string> GetMutableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.CanWrite && !prop.IsInitOnly())
            .Select(prop => prop.Name);
    }
}

// Usage
var isImmutable = ImmutabilityAnalyzer.IsImmutableType(typeof(CustomerData));
var mutableProps = ImmutabilityAnalyzer.GetMutableProperties(typeof(CustomerData));

Console.WriteLine($"Is immutable: {isImmutable}");                    // False
Console.WriteLine($"Mutable properties: {string.Join(", ", mutableProps)}"); // Phone
```

### Framework Integration Example

```csharp
// Custom serializer that handles init-only properties differently
public class ImmutableJsonConverter : JsonConverter
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.GetProperties()
            .Any(prop => prop.IsInitOnly());
    }
    
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Special handling for types with init-only properties
        // Implementation would use constructor or with-expressions
        return base.Read(ref reader, typeToConvert, options);
    }
}
```