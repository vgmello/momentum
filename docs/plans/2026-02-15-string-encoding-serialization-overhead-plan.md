# StringEncoding Attribute & Serialization Overhead Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a `[StringEncoding]` attribute for per-char byte control and an `ISerializationOverheadCalculator` strategy for format-aware payload size estimation.

**Architecture:** The `StringEncodingAttribute` lives in Abstractions (where users already reference it). The strategy interface and implementations live in the EventMarkdownGenerator. The `PayloadSizeCalculator` is refactored from static to accept a strategy, and the CLI gets a `--format` option.

**Tech Stack:** .NET 10, xUnit v3, Shouldly, NSubstitute

---

### Task 1: StringEncodingAttribute

**Files:**
- Create: `libs/Momentum/src/Momentum.Extensions.Abstractions/Messaging/StringEncodingAttribute.cs`
- Test: `libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/StringEncodingAttributeTests.cs`

**Step 1: Write the failing tests**

```csharp
// libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/StringEncodingAttributeTests.cs
using Momentum.Extensions.Abstractions.Messaging;
using Shouldly;
using System.Reflection;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class StringEncodingAttributeTests
{
    [Fact]
    public void DefaultBytesPerChar_ShouldBeOne()
    {
        var attr = new StringEncodingAttribute();
        attr.BytesPerChar.ShouldBe(1);
    }

    [Fact]
    public void BytesPerChar_ShouldBeSettable()
    {
        var attr = new StringEncodingAttribute { BytesPerChar = 4 };
        attr.BytesPerChar.ShouldBe(4);
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToAssembly()
    {
        var usage = typeof(StringEncodingAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        usage.ShouldNotBeNull();
        (usage!.ValidOn & AttributeTargets.Assembly).ShouldNotBe((AttributeTargets)0);
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToClass()
    {
        var usage = typeof(StringEncodingAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        usage.ShouldNotBeNull();
        (usage!.ValidOn & AttributeTargets.Class).ShouldNotBe((AttributeTargets)0);
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToProperty()
    {
        var usage = typeof(StringEncodingAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        usage.ShouldNotBeNull();
        (usage!.ValidOn & AttributeTargets.Property).ShouldNotBe((AttributeTargets)0);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests --filter "StringEncodingAttributeTests" --no-restore`
Expected: FAIL - `StringEncodingAttribute` type not found

**Step 3: Write minimal implementation**

```csharp
// libs/Momentum/src/Momentum.Extensions.Abstractions/Messaging/StringEncodingAttribute.cs
// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.Abstractions.Messaging;

/// <summary>
///     Controls string byte-size estimation for payload size calculations.
///     Apply at assembly, class, or property level. Resolution order: Property > Class > Assembly > default (1).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Property)]
public class StringEncodingAttribute : Attribute
{
    /// <summary>
    ///     Bytes per character for string size estimation.
    ///     Common values: 1 (UTF-8 ASCII/Latin), 2 (UTF-16), 4 (worst-case UTF-8/UTF-32).
    /// </summary>
    public int BytesPerChar { get; set; } = 1;
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests --filter "StringEncodingAttributeTests"`
Expected: All 5 tests PASS

**Step 5: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.Abstractions/Messaging/StringEncodingAttribute.cs \
       libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/StringEncodingAttributeTests.cs
git commit -m "feat: add StringEncodingAttribute for per-char byte control"
```

---

### Task 2: ISerializationOverheadCalculator Interface

**Files:**
- Create: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/ISerializationOverheadCalculator.cs`

**Step 1: Write the interface**

```csharp
// libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/ISerializationOverheadCalculator.cs
// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Calculates serialization format overhead for payload size estimation.
///     Implement this interface to support additional formats (Avro, Protobuf, etc.).
/// </summary>
public interface ISerializationOverheadCalculator
{
    /// <summary>Display name of the serialization format (e.g., "JSON", "Binary").</summary>
    string FormatName { get; }

    /// <summary>Overhead bytes for a serialized string value (e.g., JSON quotes: 2 bytes).</summary>
    int GetStringValueOverhead();

    /// <summary>Overhead bytes for a property entry including key and delimiters (e.g., JSON: "key": adds key.Length + 3).</summary>
    int GetPropertyOverhead(string propertyName);

    /// <summary>Overhead bytes for object wrappers (e.g., JSON { } adds 2 bytes).</summary>
    int GetObjectOverhead();

    /// <summary>Overhead bytes per element separator (e.g., JSON comma: 1 byte).</summary>
    int GetElementSeparatorOverhead();

    /// <summary>Overhead bytes for collection wrappers (e.g., JSON [ ] adds 2 bytes).</summary>
    int GetCollectionOverhead();
}
```

**Step 2: Commit**

This is just an interface, no test needed yet - it will be tested through the implementations.

```bash
git add libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/ISerializationOverheadCalculator.cs
git commit -m "feat: add ISerializationOverheadCalculator interface"
```

---

### Task 3: JsonOverheadCalculator Implementation

**Files:**
- Create: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/JsonOverheadCalculator.cs`
- Test: `libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/JsonOverheadCalculatorTests.cs`

**Step 1: Write the failing tests**

```csharp
// libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/JsonOverheadCalculatorTests.cs
using Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;
using Shouldly;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class JsonOverheadCalculatorTests
{
    private readonly JsonOverheadCalculator _calculator = new();

    [Fact]
    public void FormatName_ShouldBeJson()
    {
        _calculator.FormatName.ShouldBe("JSON");
    }

    [Fact]
    public void GetStringValueOverhead_ShouldReturn2ForQuotes()
    {
        // JSON wraps strings in double quotes: "value" = 2 bytes overhead
        _calculator.GetStringValueOverhead().ShouldBe(2);
    }

    [Theory]
    [InlineData("Id", 5)]       // "Id": = 2 (quotes) + 2 (Id) + 1 (colon) = 5
    [InlineData("Name", 7)]     // "Name": = 2 + 4 + 1 = 7
    [InlineData("X", 4)]        // "X": = 2 + 1 + 1 = 4
    public void GetPropertyOverhead_ShouldIncludeKeyQuotesAndColon(string name, int expected)
    {
        _calculator.GetPropertyOverhead(name).ShouldBe(expected);
    }

    [Fact]
    public void GetObjectOverhead_ShouldReturn2ForBraces()
    {
        // JSON objects: { } = 2 bytes
        _calculator.GetObjectOverhead().ShouldBe(2);
    }

    [Fact]
    public void GetElementSeparatorOverhead_ShouldReturn1ForComma()
    {
        _calculator.GetElementSeparatorOverhead().ShouldBe(1);
    }

    [Fact]
    public void GetCollectionOverhead_ShouldReturn2ForBrackets()
    {
        // JSON arrays: [ ] = 2 bytes
        _calculator.GetCollectionOverhead().ShouldBe(2);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests --filter "JsonOverheadCalculatorTests" --no-restore`
Expected: FAIL - `JsonOverheadCalculator` type not found

**Step 3: Write minimal implementation**

```csharp
// libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/JsonOverheadCalculator.cs
// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Calculates JSON serialization overhead for payload size estimation.
///     Accounts for quotes, property keys, colons, commas, braces, and brackets.
/// </summary>
public sealed class JsonOverheadCalculator : ISerializationOverheadCalculator
{
    public string FormatName => "JSON";

    /// <summary>Opening and closing double quotes around string values: 2 bytes.</summary>
    public int GetStringValueOverhead() => 2;

    /// <summary>"propertyName": = quotes (2) + name length + colon (1).</summary>
    public int GetPropertyOverhead(string propertyName) => propertyName.Length + 3;

    /// <summary>Object braces { }: 2 bytes.</summary>
    public int GetObjectOverhead() => 2;

    /// <summary>Comma separator: 1 byte.</summary>
    public int GetElementSeparatorOverhead() => 1;

    /// <summary>Array brackets [ ]: 2 bytes.</summary>
    public int GetCollectionOverhead() => 2;
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests --filter "JsonOverheadCalculatorTests"`
Expected: All 6 tests PASS

**Step 5: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/JsonOverheadCalculator.cs \
       libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/JsonOverheadCalculatorTests.cs
git commit -m "feat: add JsonOverheadCalculator implementation"
```

---

### Task 4: BinaryOverheadCalculator Implementation

**Files:**
- Create: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/BinaryOverheadCalculator.cs`
- Test: `libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/BinaryOverheadCalculatorTests.cs`

**Step 1: Write the failing tests**

```csharp
// libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/BinaryOverheadCalculatorTests.cs
using Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;
using Shouldly;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class BinaryOverheadCalculatorTests
{
    private readonly BinaryOverheadCalculator _calculator = new();

    [Fact]
    public void FormatName_ShouldBeBinary()
    {
        _calculator.FormatName.ShouldBe("Binary");
    }

    [Fact]
    public void AllOverheads_ShouldBeZero()
    {
        _calculator.GetStringValueOverhead().ShouldBe(0);
        _calculator.GetPropertyOverhead("AnyName").ShouldBe(0);
        _calculator.GetObjectOverhead().ShouldBe(0);
        _calculator.GetElementSeparatorOverhead().ShouldBe(0);
        _calculator.GetCollectionOverhead().ShouldBe(0);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests --filter "BinaryOverheadCalculatorTests" --no-restore`
Expected: FAIL - `BinaryOverheadCalculator` type not found

**Step 3: Write minimal implementation**

```csharp
// libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/BinaryOverheadCalculator.cs
// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Returns zero overhead for all calculations.
///     Binary serialization formats (Avro, Protobuf) have negligible text-based overhead.
/// </summary>
public sealed class BinaryOverheadCalculator : ISerializationOverheadCalculator
{
    public string FormatName => "Binary";
    public int GetStringValueOverhead() => 0;
    public int GetPropertyOverhead(string propertyName) => 0;
    public int GetObjectOverhead() => 0;
    public int GetElementSeparatorOverhead() => 0;
    public int GetCollectionOverhead() => 0;
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests --filter "BinaryOverheadCalculatorTests"`
Expected: All 2 tests PASS

**Step 5: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/BinaryOverheadCalculator.cs \
       libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/BinaryOverheadCalculatorTests.cs
git commit -m "feat: add BinaryOverheadCalculator implementation"
```

---

### Task 5: Refactor PayloadSizeCalculator to Accept Strategy and StringEncoding

This is the core refactoring task. The `PayloadSizeCalculator` changes from:
- Hardcoded `* 4` for strings → uses `BytesPerChar` from `[StringEncoding]` attribute hierarchy
- No serialization awareness → uses `ISerializationOverheadCalculator` for all overhead

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/PayloadSizeCalculator.cs`
- Test: `libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/PayloadSizeCalculatorTests.cs`

**Step 1: Write the failing tests**

```csharp
// libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/PayloadSizeCalculatorTests.cs
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;
using NSubstitute;
using Shouldly;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class PayloadSizeCalculatorTests
{
    private readonly ISerializationOverheadCalculator _jsonCalc = new JsonOverheadCalculator();
    private readonly ISerializationOverheadCalculator _binaryCalc = new BinaryOverheadCalculator();

    // --- StringEncoding attribute resolution ---

    [Fact]
    public void CalculatePropertySize_StringWithMaxLength_UsesDefaultBytesPerChar()
    {
        // Default is 1 byte/char. Property "Name" with [MaxLength(50)]
        // JSON: (50 * 1) + 2 (string quotes) = 52
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.Name))!;
        var result = PayloadSizeCalculator.CalculatePropertySize(prop, prop.PropertyType, _jsonCalc);
        result.SizeBytes.ShouldBe(52); // 50 * 1 + 2
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_StringWithClassLevelEncoding_UsesClassBytesPerChar()
    {
        // Class has [StringEncoding(BytesPerChar = 2)], property "Name" with [MaxLength(50)]
        // JSON: (50 * 2) + 2 (quotes) = 102
        var prop = typeof(TestEventClassEncoding).GetProperty(nameof(TestEventClassEncoding.Name))!;
        var result = PayloadSizeCalculator.CalculatePropertySize(prop, prop.PropertyType, _jsonCalc);
        result.SizeBytes.ShouldBe(102); // 50 * 2 + 2
    }

    [Fact]
    public void CalculatePropertySize_StringWithPropertyLevelEncoding_OverridesClassLevel()
    {
        // Class has [StringEncoding(BytesPerChar = 2)], property has [StringEncoding(BytesPerChar = 4)]
        // JSON: (50 * 4) + 2 = 202
        var prop = typeof(TestEventClassEncoding).GetProperty(nameof(TestEventClassEncoding.Description))!;
        var result = PayloadSizeCalculator.CalculatePropertySize(prop, prop.PropertyType, _jsonCalc);
        result.SizeBytes.ShouldBe(202); // 50 * 4 + 2
    }

    [Fact]
    public void CalculatePropertySize_BinaryFormat_NoStringOverhead()
    {
        // Binary: (50 * 1) + 0 = 50
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.Name))!;
        var result = PayloadSizeCalculator.CalculatePropertySize(prop, prop.PropertyType, _binaryCalc);
        result.SizeBytes.ShouldBe(50); // 50 * 1 + 0
    }

    // --- Backward compatibility ---

    [Fact]
    public void CalculatePropertySize_WithoutOverheadCalculator_UsesLegacyBehavior()
    {
        // The 2-arg overload still works (backward compat), uses no overhead, 4 bytes/char
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.Name))!;
        var result = PayloadSizeCalculator.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(200); // 50 * 4 (legacy behavior)
    }

    // --- Primitive types unchanged ---

    [Fact]
    public void CalculatePropertySize_Int_UnchangedByFormat()
    {
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.Count))!;
        var result = PayloadSizeCalculator.CalculatePropertySize(prop, prop.PropertyType, _jsonCalc);
        result.SizeBytes.ShouldBe(4);
        result.IsAccurate.ShouldBeTrue();
    }

    // --- Test types ---

    private record TestEventDefault
    {
        [MaxLength(50)]
        public string Name { get; init; } = "";
        public int Count { get; init; }
    }

    [StringEncoding(BytesPerChar = 2)]
    private record TestEventClassEncoding
    {
        [MaxLength(50)]
        public string Name { get; init; } = "";

        [StringEncoding(BytesPerChar = 4)]
        [MaxLength(50)]
        public string Description { get; init; } = "";
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests --filter "PayloadSizeCalculatorTests" --no-restore`
Expected: FAIL - overload `CalculatePropertySize(PropertyInfo, Type, ISerializationOverheadCalculator)` does not exist

**Step 3: Implement the refactored PayloadSizeCalculator**

Key changes to `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/PayloadSizeCalculator.cs`:

1. Add new public overload: `CalculatePropertySize(PropertyInfo property, Type propertyType, ISerializationOverheadCalculator overheadCalculator)`
2. Keep existing 2-arg overload as backward-compatible (calls private with `null` overhead)
3. Add `ResolveStringEncoding` method that walks Property > Class > Assembly
4. In `CalculateStringSize`: use `bytesPerChar` from resolved attribute, add `overheadCalculator.GetStringValueOverhead()`
5. Primitive types: unchanged (no serialization overhead applied to primitives in this phase)

The full replacement for `PayloadSizeCalculator.cs`:

```csharp
// Copyright (c) Momentum .NET. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Calculates estimated payload sizes for event properties based on type analysis,
///     data annotation constraints, string encoding attributes, and serialization format overhead.
/// </summary>
public static class PayloadSizeCalculator
{
    private const int DefaultBytesPerChar = 4;

    /// <summary>
    ///     Calculates the estimated size in bytes using legacy behavior (4 bytes/char, no serialization overhead).
    /// </summary>
    public static PayloadSizeResult CalculatePropertySize(PropertyInfo property, Type propertyType)
    {
        return CalculatePropertySize(property, propertyType, overheadCalculator: null, []);
    }

    /// <summary>
    ///     Calculates the estimated size using string encoding attributes and serialization overhead.
    /// </summary>
    public static PayloadSizeResult CalculatePropertySize(PropertyInfo property, Type propertyType,
        ISerializationOverheadCalculator overheadCalculator)
    {
        return CalculatePropertySize(property, propertyType, overheadCalculator, []);
    }

    private static PayloadSizeResult CalculatePropertySize(PropertyInfo property, Type propertyType,
        ISerializationOverheadCalculator? overheadCalculator, HashSet<Type> visitedTypes)
    {
        try
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (TypeUtils.IsPrimitiveType(underlyingType))
            {
                return new PayloadSizeResult
                {
                    SizeBytes = GetPrimitiveTypeSize(underlyingType),
                    IsAccurate = true,
                    Warning = null
                };
            }

            if (underlyingType == typeof(string))
            {
                return CalculateStringSize(property, overheadCalculator);
            }

            if (TypeUtils.IsCollectionType(underlyingType))
            {
                return CalculateCollectionSize(property, underlyingType, overheadCalculator, visitedTypes);
            }

            return CalculateComplexTypeSize(underlyingType, overheadCalculator, visitedTypes);
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

    private static PayloadSizeResult CalculateStringSize(PropertyInfo property,
        ISerializationOverheadCalculator? overheadCalculator)
    {
        var constraints = GetDataAnnotationConstraints(property);

        if (constraints.MaxLength.HasValue)
        {
            var bytesPerChar = overheadCalculator is not null
                ? ResolveStringEncoding(property)
                : DefaultBytesPerChar;

            var stringOverhead = overheadCalculator?.GetStringValueOverhead() ?? 0;

            return new PayloadSizeResult
            {
                SizeBytes = (constraints.MaxLength.Value * bytesPerChar) + stringOverhead,
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

    private static PayloadSizeResult CalculateCollectionSize(PropertyInfo property, Type collectionType,
        ISerializationOverheadCalculator? overheadCalculator, HashSet<Type> visitedTypes)
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
        var elementSizeResult = CalculateTypeSize(elementType, overheadCalculator, visitedTypes);

        var separatorOverhead = overheadCalculator is not null && estimatedCount > 1
            ? overheadCalculator.GetElementSeparatorOverhead() * (estimatedCount - 1)
            : 0;
        var collectionOverhead = overheadCalculator?.GetCollectionOverhead() ?? 0;

        return new PayloadSizeResult
        {
            SizeBytes = (elementSizeResult.SizeBytes * estimatedCount) + separatorOverhead + collectionOverhead,
            IsAccurate = elementSizeResult.IsAccurate && constraints.MaxRange.HasValue,
            Warning = constraints.MaxRange.HasValue
                ? elementSizeResult.Warning
                : "Collection size estimated (no Range constraint)"
        };
    }

    private static PayloadSizeResult CalculateComplexTypeSize(Type type,
        ISerializationOverheadCalculator? overheadCalculator, HashSet<Type> visitedTypes)
    {
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
            var propertyCount = 0;

            try
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    try
                    {
                        var propertyResult =
                            CalculatePropertySize(property, property.PropertyType, overheadCalculator, visitedTypes);
                        totalSize += propertyResult.SizeBytes;
                        propertyCount++;

                        if (overheadCalculator is not null)
                        {
                            totalSize += overheadCalculator.GetPropertyOverhead(property.Name);
                        }

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
                        warnings.Add(
                            $"{property.Name}: Unable to analyze due to missing dependency ({ex.GetType().Name})");
                        isAccurate = false;
                    }
                }
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

            // Add object overhead and separators
            if (overheadCalculator is not null)
            {
                totalSize += overheadCalculator.GetObjectOverhead();

                if (propertyCount > 1)
                {
                    totalSize += overheadCalculator.GetElementSeparatorOverhead() * (propertyCount - 1);
                }
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

    private static PayloadSizeResult CalculateTypeSize(Type type,
        ISerializationOverheadCalculator? overheadCalculator, HashSet<Type> visitedTypes)
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

            return CalculateComplexTypeSize(type, overheadCalculator, visitedTypes);
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

    /// <summary>
    ///     Resolves the BytesPerChar value from the StringEncoding attribute hierarchy:
    ///     Property > DeclaringType (Class) > Assembly > default (1).
    /// </summary>
    internal static int ResolveStringEncoding(PropertyInfo property)
    {
        // Property-level
        var propAttr = property.GetCustomAttribute<StringEncodingAttribute>();
        if (propAttr is not null)
            return propAttr.BytesPerChar;

        // Class-level
        var classAttr = property.DeclaringType?.GetCustomAttribute<StringEncodingAttribute>();
        if (classAttr is not null)
            return classAttr.BytesPerChar;

        // Assembly-level
        var assemblyAttr = property.DeclaringType?.Assembly.GetCustomAttribute<StringEncodingAttribute>();
        if (assemblyAttr is not null)
            return assemblyAttr.BytesPerChar;

        return 1; // Default: 1 byte per char (UTF-8 ASCII range)
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
            _ => 4
        };
    }

    private sealed record DataAnnotationConstraints
    {
        public int? MaxLength { get; init; }
        public int? MaxRange { get; init; }
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests --filter "PayloadSizeCalculatorTests"`
Expected: All 6 tests PASS

**Step 5: Run all existing tests to verify backward compat**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests`
Expected: All existing tests still pass (the 2-arg overload preserves legacy behavior)

**Step 6: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/PayloadSizeCalculator.cs \
       libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests/PayloadSizeCalculatorTests.cs
git commit -m "feat: refactor PayloadSizeCalculator to support StringEncoding and serialization overhead"
```

---

### Task 6: Wire Up GeneratorOptions and CLI

**Files:**
- Modify: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Models/GeneratorOptions.cs`
- Modify: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/GenerateCommand.cs`
- Modify: `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/AssemblyEventDiscovery.cs`

**Step 1: Add SerializationFormat to GeneratorOptions**

In `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Models/GeneratorOptions.cs`, add:

```csharp
/// <summary>Serialization format for overhead calculation. Default: "json". Options: "json", "binary".</summary>
public string SerializationFormat { get; init; } = "json";
```

**Step 2: Add factory method to resolve format to calculator**

Create a new file `libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/OverheadCalculatorFactory.cs`:

```csharp
// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Resolves a serialization format name to its overhead calculator implementation.
/// </summary>
public static class OverheadCalculatorFactory
{
    public static ISerializationOverheadCalculator Create(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonOverheadCalculator(),
            "binary" => new BinaryOverheadCalculator(),
            _ => throw new ArgumentException($"Unknown serialization format: '{format}'. Supported: json, binary.", nameof(format))
        };
    }
}
```

**Step 3: Add --format CLI option**

In `GenerateCommand.Settings`, add:

```csharp
[CommandOption("--format")]
[Description("Serialization format for payload size calculation (json, binary)")]
[DefaultValue("json")]
public string Format { get; init; } = "json";
```

In `GenerateCommand.ExecuteAsync`, where `GeneratorOptions` is constructed, add:

```csharp
SerializationFormat = settings.Format
```

**Step 4: Thread the overhead calculator through AssemblyEventDiscovery**

In `AssemblyEventDiscovery.cs`, the call site at line 173:
```csharp
var sizeResult = PayloadSizeCalculator.CalculatePropertySize(property, property.PropertyType);
```

Needs to become:
```csharp
var sizeResult = PayloadSizeCalculator.CalculatePropertySize(property, property.PropertyType, overheadCalculator);
```

This requires:
1. Add `ISerializationOverheadCalculator? overheadCalculator = null` parameter to `DiscoverEvents` methods
2. Thread it through `CreateEventMetadata` → `GetEventPropertiesAndPartitionKeys`
3. In `GenerateCommand.GenerateDocumentationAsync`, create the calculator and pass it

**Step 5: Run all tests**

Run: `dotnet test libs/Momentum/tests/Momentum.Extensions.EventMarkdownGenerator.Tests`
Expected: All tests pass. Existing call sites without overhead calculator use legacy behavior.

**Step 6: Commit**

```bash
git add libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Models/GeneratorOptions.cs \
       libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/GenerateCommand.cs \
       libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/AssemblyEventDiscovery.cs \
       libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator/Services/Serialization/OverheadCalculatorFactory.cs
git commit -m "feat: wire serialization format through CLI and generator pipeline"
```

---

### Task 7: Run Full Test Suite and Fix Any Issues

**Step 1: Build the full solution**

Run: `dotnet build libs/Momentum/Momentum.slnx`
Expected: Clean build with no errors

**Step 2: Run all library tests**

Run: `dotnet test libs/Momentum/Momentum.slnx`
Expected: All tests pass

**Step 3: Run the sample app tests (if overhead calculator affects anything)**

Run: `dotnet test`
Expected: All tests pass

**Step 4: Commit any fixes if needed**

```bash
git add -A
git commit -m "fix: resolve any integration issues from serialization overhead changes"
```
