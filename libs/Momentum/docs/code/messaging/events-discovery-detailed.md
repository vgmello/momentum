# Distributed Events Discovery

## Local Domain Discovery {#local-domain-discovery}

This only applies to "local" domain assemblies

The discovery process identifies integration event types within application domain assemblies by:

1. **Assembly Selection**: Gathering domain assemblies and the entry assembly
2. **Namespace Filtering**: Looking for types in namespaces ending with `.IntegrationEvents`
3. **Domain Prefix Matching**: Ensuring events belong to the same domain context

### Assembly Resolution Process

```csharp
Assembly[] appAssemblies = [.. DomainAssemblyAttribute.GetDomainAssemblies(), ServiceDefaultsExtensions.EntryAssembly];

var domainPrefixes = appAssemblies
    .Select(a => a.GetName().Name)
    .Where(assemblyName => assemblyName is not null)
    .Select(assemblyName =>
    {
        var mainNamespaceIndex = assemblyName!.IndexOf('.');
        return mainNamespaceIndex >= 0 ? assemblyName[..mainNamespaceIndex] : assemblyName;
    })
    .ToHashSet();
```

### Domain-Scoped Discovery

The method filters assemblies to only include those belonging to the same domain:

```csharp
var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies()
    .Where(assembly =>
    {
        var name = assembly.GetName().Name;
        return name is not null && domainPrefixes.Any(prefix => name.StartsWith(prefix));
    })
    .ToArray();
```

## Handler-Associated Events Discovery {#handler-associated-events}

This method identifies distributed events by analyzing handler method parameters and ensures that only events with corresponding handlers are included.

### Handler Method Discovery

The discovery process examines all handler methods across domain assemblies:

```csharp
Assembly[] handlerAssemblies = [.. DomainAssemblyAttribute.GetDomainAssemblies(), ServiceDefaultsExtensions.EntryAssembly];

var candidateHandlers = handlerAssemblies
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => type is { IsClass: true })
    .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
```

### Handler Method Identification

Methods are identified as handlers based on naming conventions:

- **Handle**, **HandleAsync**
- **Handles**, **HandlesAsync** 
- **Consume**, **ConsumeAsync**
- **Consumes**, **ConsumesAsync**

```csharp
private static readonly HashSet<string> HandlerMethodNames = new(StringComparer.OrdinalIgnoreCase)
{
    HandlerChain.Handle,
    HandlerChain.Handle + Async,
    HandlerChain.Handles,
    HandlerChain.Handles + Async,
    HandlerChain.Consume,
    HandlerChain.Consume + Async,
    HandlerChain.Consumes,
    HandlerChain.Consumes + Async
};
```

### Parameter Analysis

For each valid handler method, parameters are analyzed to extract integration event types:

```csharp
var integrationEvents = handlerMethods.SelectMany(method =>
    method.GetParameters()
        .Select(parameter => parameter.ParameterType)
        .Where(IsIntegrationEventType)
).ToHashSet();
```

## Integration Event Type Identification

### Namespace-Based Detection

Integration events are identified by their namespace pattern:

```csharp
private static bool IsIntegrationEventType(Type messageType) => 
    messageType.Namespace?.EndsWith(IntegrationEventsNamespace) == true;
```

Where `IntegrationEventsNamespace` is `.IntegrationEvents`.

### Domain Boundary Enforcement

This approach ensures that:

- **Local Events Only**: Only events from the same domain are discovered
- **Handler Coupling**: Events are only included if they have corresponding handlers
- **Namespace Convention**: Enforces consistent namespace organization
- **Type Safety**: Compile-time validation of event-handler relationships

## Future Improvements

### Source Generation

The discovery process could be optimized using source generation:

```csharp
// TODO: Use source generation in the future for this
private static IEnumerable<MethodInfo> GetHandlerMethods()
```

Source generation would provide:

- **Compile-time Discovery**: Pre-computed event-handler mappings
- **Performance Optimization**: Elimination of runtime reflection
- **Build-time Validation**: Early detection of missing handlers
- **Code Generation**: Automatic registration of discovered types