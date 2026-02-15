# String Encoding Attribute & Serialization Overhead Strategy

**Date**: 2026-02-15
**Status**: Approved

## Problem

The `PayloadSizeCalculator` currently hardcodes 4 bytes per character for string size estimation (worst-case UTF-8/UTF-32). This is overly conservative for most use cases and doesn't account for serialization format overhead (JSON quotes, property keys, delimiters, etc.).

## Solution

Two complementary features:

1. **`[StringEncoding]` attribute** - Controls bytes-per-character at assembly, class, or property level
2. **`ISerializationOverheadCalculator` strategy** - Pluggable serialization overhead calculation (JSON, Binary, extensible to Avro/Protobuf)

## Design

### 1. StringEncodingAttribute

**Location**: `libs/Momentum/src/Momentum.Extensions.Abstractions/Messaging/StringEncodingAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Property)]
public class StringEncodingAttribute : Attribute
{
    public int BytesPerChar { get; set; } = 1;
}
```

**Resolution order**: Property > Class > Assembly > default (1 byte/char)

### 2. ISerializationOverheadCalculator

**Location**: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/`

```csharp
public interface ISerializationOverheadCalculator
{
    string FormatName { get; }
    int GetStringValueOverhead();
    int GetPropertyOverhead(string propertyName);
    int GetObjectOverhead();
    int GetElementSeparatorOverhead();
    int GetCollectionOverhead();
}
```

**Implementations**:
- `JsonOverheadCalculator`: Quotes (2B), `"key":` (key.Length + 3), `{}` (2B), `,` (1B), `[]` (2B)
- `BinaryOverheadCalculator`: Returns 0 for all (no text-based overhead)

### 3. PayloadSizeCalculator Changes

- Accept `ISerializationOverheadCalculator` parameter
- String size: `(maxLength * bytesPerChar) + stringValueOverhead + propertyOverhead`
- Complex type: `objectOverhead + sum(propertyOverhead) + separatorOverhead * (count - 1)`
- Collection: `collectionOverhead + separatorOverhead * (count - 1)`
- New helper: `ResolveStringEncoding(PropertyInfo, Type, Assembly)` walks attribute hierarchy

### 4. CLI & GeneratorOptions

- `GeneratorOptions.SerializationFormat`: string property, default `"json"`
- CLI: `--format json|binary` option
- `GenerateCommand` resolves format string to `ISerializationOverheadCalculator` instance

## Files to Create

- `libs/Momentum/src/Momentum.Extensions.Abstractions/Messaging/StringEncodingAttribute.cs`
- `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/ISerializationOverheadCalculator.cs`
- `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/JsonOverheadCalculator.cs`
- `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/BinaryOverheadCalculator.cs`

## Files to Modify

- `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/PayloadSizeCalculator.cs`
- `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Models/GeneratorOptions.cs`
- `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/GenerateCommand.cs`
- `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/AssemblyEventDiscovery.cs` (pass overhead calculator through)
