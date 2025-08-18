# Partition Key Provider Factory

## Performance-Optimized Key Generation {#performance-optimized}

This method is called on every event published, therefore performance is important, that is why I'm using expression trees instead of reflection.

### Generated Code Pattern

The factory generates compiled expressions that efficiently retrieve partition keys:

```csharp
instance => {partitionKeyProperty1}.ToString() + "-" + {partitionKeyProperty2}.ToString()...
```

## Expression Tree Compilation

### Single Property Scenario

For messages with a single partition key property:

```csharp
var singlePropertyExpression = Expression.Lambda<Func<TMessage, string>>(
    Expression.Call(convertToString, 
        Expression.Convert(Expression.Property(parameter, prop), typeof(object))),
    parameter
).Compile();
```

### Multiple Properties Scenario

For messages with multiple partition key properties, values are concatenated with hyphens:

```csharp
var concatMethod = typeof(string).GetMethod(nameof(string.Concat), [typeof(string[])])!;
var expressionsWithSeparator = new List<Expression>();

for (var i = 0; i < stringValueExpressions.Count; i++)
{
    expressionsWithSeparator.Add(stringValueExpressions[i]);
    
    if (i < stringValueExpressions.Count - 1)
    {
        expressionsWithSeparator.Add(Expression.Constant("-"));
    }
}

var combinedExpression = Expression.Call(concatMethod, 
    Expression.NewArrayInit(typeof(string), expressionsWithSeparator));
```

## Property Discovery and Ordering

### Attribute-Based Discovery

Properties are discovered using the `PartitionKeyAttribute`:

```csharp
var partitionKeyProperties = messageType.GetPropertiesWithAttribute<PartitionKeyAttribute>();
```

### Order Resolution

Properties are ordered based on:

1. **Explicit Order**: `PartitionKeyAttribute.Order` property
2. **Alphabetical**: Property name as secondary sort

```csharp
var orderedPartitionKeyProperties = partitionKeyProperties
    .OrderBy(p => p.GetCustomAttribute<PartitionKeyAttribute>(primaryConstructor)?.Order ?? 0)
    .ThenBy(p => p.Name).ToArray();
```

## Primary Constructor Support

The factory supports records and classes with primary constructors by examining constructor parameters for partition key attributes:

```csharp
var primaryConstructor = messageType.GetPrimaryConstructor();
var attribute = property.GetCustomAttribute<PartitionKeyAttribute>(primaryConstructor);
```

## Compilation Performance

### Expression Tree Benefits

- **Compile-time Generation**: Expressions compiled once per message type
- **Runtime Efficiency**: No reflection overhead during message publishing
- **Type Safety**: Compile-time validation of property access
- **Memory Efficiency**: Minimal allocation during key generation

### Caching Strategy

The generated functions are typically cached by the calling infrastructure, ensuring:

- **One-time Compilation**: Per message type compilation cost
- **Reusable Functions**: Cached delegates for repeated use
- **Minimal Memory Footprint**: Single delegate per message type

## Usage Examples

### Single Partition Key

```csharp
public record OrderCreated(
    [PartitionKey] Guid CustomerId,
    Guid OrderId,
    decimal Amount
);

// Generated: customer => customer.CustomerId.ToString()
```

### Multiple Partition Keys with Ordering

```csharp
public record ProductUpdated(
    [PartitionKey(Order = 1)] string Category,
    [PartitionKey(Order = 0)] Guid StoreId,
    Guid ProductId
);

// Generated: product => product.StoreId.ToString() + "-" + product.Category.ToString()
```