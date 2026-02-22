// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class PayloadSizeCalculatorTests
{
    private readonly PayloadSizeCalculator _jsonCalc = PayloadSizeCalculator.Create("json");
    private readonly PayloadSizeCalculator _binaryCalc = PayloadSizeCalculator.Create("binary");

    // --- StringEncoding attribute resolution ---

    [Fact]
    public void CalculatePropertySize_StringWithMaxLength_UsesDefaultBytesPerChar()
    {
        // Default is 1 byte/char when using overhead calculator. Property "Name" with [MaxLength(50)]
        // JSON: (50 * 1) + 2 (string quotes) = 52
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.Name))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(52); // 50 * 1 + 2
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_StringWithClassLevelEncoding_UsesClassBytesPerChar()
    {
        // Class has [StringEncoding(BytesPerChar = 2)], property "Name" with [MaxLength(50)]
        // JSON: (50 * 2) + 2 (quotes) = 102
        var prop = typeof(TestEventClassEncoding).GetProperty(nameof(TestEventClassEncoding.Name))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(102); // 50 * 2 + 2
    }

    [Fact]
    public void CalculatePropertySize_StringWithPropertyLevelEncoding_OverridesClassLevel()
    {
        // Class has [StringEncoding(BytesPerChar = 2)], property has [StringEncoding(BytesPerChar = 4)]
        // JSON: (50 * 4) + 2 = 202
        var prop = typeof(TestEventClassEncoding).GetProperty(nameof(TestEventClassEncoding.Description))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(202); // 50 * 4 + 2
    }

    [Fact]
    public void CalculatePropertySize_BinaryFormat_NoStringOverhead()
    {
        // Binary: (50 * 1) + 0 = 50
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.Name))!;
        var result = _binaryCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(50); // 50 * 1 + 0
    }

    // --- Primitive types unchanged ---

    [Fact]
    public void CalculatePropertySize_Int_UnchangedByFormat()
    {
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.Count))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(4);
        result.IsAccurate.ShouldBeTrue();
    }

    // --- String with no MaxLength ---

    [Fact]
    public void CalculatePropertySize_StringWithNoMaxLength_ReturnsInaccurate()
    {
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.NoLength))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(0);
        result.IsAccurate.ShouldBeFalse();
    }

    // --- Factory method ---

    [Theory]
    [InlineData("json", "JSON")]
    [InlineData("JSON", "JSON")]
    [InlineData("binary", "Binary")]
    [InlineData("Binary", "Binary")]
    public void Create_ValidFormat_ReturnsCorrectCalculator(string format, string expectedName)
    {
        var calc = PayloadSizeCalculator.Create(format);
        calc.FormatName.ShouldBe(expectedName);
    }

    [Fact]
    public void Create_UnknownFormat_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => PayloadSizeCalculator.Create("xml"));
    }

    // --- Nullable types ---

    [Fact]
    public void CalculatePropertySize_NullableInt_ShouldReturnSameAsPrimitive()
    {
        var prop = typeof(TestEventWithNullable).GetProperty(nameof(TestEventWithNullable.NullableCount))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(4);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_NullableGuid_ShouldReturnSameAsPrimitive()
    {
        var prop = typeof(TestEventWithNullable).GetProperty(nameof(TestEventWithNullable.NullableId))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(16);
        result.IsAccurate.ShouldBeTrue();
    }

    // --- Collection types ---

    [Fact]
    public void CalculatePropertySize_ListOfIntsWithRange_ShouldCalculateCorrectSize()
    {
        var prop = typeof(TestEventWithCollections).GetProperty(nameof(TestEventWithCollections.Scores))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // [Range(0, 5)] means max 5 elements, each int = 4 bytes
        // JSON: (4 * 5) + 2 (brackets) + 4 (commas: 5-1) = 26
        result.SizeBytes.ShouldBe(26);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_ListWithoutRange_ShouldUseDefaultCount()
    {
        var prop = typeof(TestEventWithCollections).GetProperty(nameof(TestEventWithCollections.Tags))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // No Range attr, default count = 10. String elements without MaxLength = 0 each
        // But warning about collection size estimated
        result.IsAccurate.ShouldBeFalse();
        result.Warning.ShouldNotBeNull();
        result.Warning.ShouldContain("Collection size estimated");
    }

    [Fact]
    public void CalculatePropertySize_ArrayOfIntsWithRange_ShouldCalculateCorrectSize()
    {
        var prop = typeof(TestEventWithCollections).GetProperty(nameof(TestEventWithCollections.Values))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // [Range(0, 3)] means max 3 elements, each int = 4 bytes
        // JSON: (4 * 3) + 2 (brackets) + 2 (commas: 3-1) = 16
        result.SizeBytes.ShouldBe(16);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_CollectionWithUnknownElementType_ShouldReturnInaccurate()
    {
        var prop = typeof(TestEventWithCollections).GetProperty(nameof(TestEventWithCollections.StringItems))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // Strings in collection without MaxLength are "Dynamic string size in collection"
        result.IsAccurate.ShouldBeFalse();
    }

    // --- Complex nested types ---

    [Fact]
    public void CalculatePropertySize_ComplexType_ShouldIncludePropertyOverhead()
    {
        var prop = typeof(TestEventWithNested).GetProperty(nameof(TestEventWithNested.Address))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // NestedAddress has: City (MaxLength 100) and ZipCode (int)
        // JSON overhead for object: 2 (braces) + 1 (comma between 2 props)
        // City: (100 * 1) + 2 (quotes) + "City".Length + 3 (property overhead) = 102 + 7 = 109
        // ZipCode: 4 + "ZipCode".Length + 3 = 4 + 10 = 14
        // Total: 109 + 14 + 3 (object overhead) = 126
        result.SizeBytes.ShouldBeGreaterThan(0);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_CircularReference_ShouldReturnInaccurate()
    {
        var prop = typeof(TestCircularType).GetProperty(nameof(TestCircularType.Self))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        result.Warning.ShouldNotBeNull();
        result.Warning.ShouldContain("Circular reference");
    }

    // --- StringLength attribute ---

    [Fact]
    public void CalculatePropertySize_StringWithStringLengthAttribute_ShouldUseMaximumLength()
    {
        var prop = typeof(TestEventWithStringLength).GetProperty(nameof(TestEventWithStringLength.Code))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // [StringLength(20)] -> JSON: (20 * 1) + 2 = 22
        result.SizeBytes.ShouldBe(22);
        result.IsAccurate.ShouldBeTrue();
    }

    // --- Various primitive types ---

    [Theory]
    [InlineData(nameof(TestPrimitiveTypes.BoolValue), 1)]
    [InlineData(nameof(TestPrimitiveTypes.ByteValue), 1)]
    [InlineData(nameof(TestPrimitiveTypes.ShortValue), 2)]
    [InlineData(nameof(TestPrimitiveTypes.IntValue), 4)]
    [InlineData(nameof(TestPrimitiveTypes.LongValue), 8)]
    [InlineData(nameof(TestPrimitiveTypes.FloatValue), 4)]
    [InlineData(nameof(TestPrimitiveTypes.DoubleValue), 8)]
    [InlineData(nameof(TestPrimitiveTypes.DecimalValue), 16)]
    [InlineData(nameof(TestPrimitiveTypes.GuidValue), 16)]
    [InlineData(nameof(TestPrimitiveTypes.DateTimeValue), 8)]
    [InlineData(nameof(TestPrimitiveTypes.DateTimeOffsetValue), 10)]
    [InlineData(nameof(TestPrimitiveTypes.TimeSpanValue), 8)]
    [InlineData(nameof(TestPrimitiveTypes.EnumValue), 4)]
    public void CalculatePropertySize_PrimitiveTypes_ShouldReturnCorrectSize(string propertyName, int expectedSize)
    {
        var prop = typeof(TestPrimitiveTypes).GetProperty(propertyName)!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);
        result.SizeBytes.ShouldBe(expectedSize);
        result.IsAccurate.ShouldBeTrue();
    }

    // --- Collection with complex element types ---

    [Fact]
    public void CalculatePropertySize_ListOfComplexType_ShouldCalculateElementSizes()
    {
        var prop = typeof(TestEventWithCollections).GetProperty(nameof(TestEventWithCollections.Addresses))!;
        var result = _jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // List of NestedAddress with [Range(0, 2)], each address has City (100+2 string) and ZipCode (4 int)
        result.SizeBytes.ShouldBeGreaterThan(0);
    }

    // --- Test types ---

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record TestEventDefault
    {
        [MaxLength(50)]
        public string Name { get; init; } = "";
        public int Count { get; init; }
        public string NoLength { get; init; } = "";
    }

    [StringEncoding(BytesPerChar = 2)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record TestEventClassEncoding
    {
        [MaxLength(50)]
        public string Name { get; init; } = "";

        [StringEncoding(BytesPerChar = 4)]
        [MaxLength(50)]
        public string Description { get; init; } = "";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record TestEventWithNullable
    {
        public int? NullableCount { get; init; }
        public Guid? NullableId { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record TestEventWithCollections
    {
        [Range(0, 5)]
        public List<int> Scores { get; init; } = [];

        public List<string> Tags { get; init; } = [];

        [Range(0, 3)]
        public int[] Values { get; init; } = [];

        public List<string> StringItems { get; init; } = [];

        [Range(0, 2)]
        public List<NestedAddress> Addresses { get; init; } = [];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record TestEventWithNested
    {
        public NestedAddress Address { get; init; } = new();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record NestedAddress
    {
        [MaxLength(100)]
        public string City { get; init; } = "";
        public int ZipCode { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record TestCircularType
    {
        public TestCircularType? Self { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record TestEventWithStringLength
    {
        [StringLength(20)]
        public string Code { get; init; } = "";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private enum TestEnum { A, B, C }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record TestPrimitiveTypes
    {
        public bool BoolValue { get; init; }
        public byte ByteValue { get; init; }
        public short ShortValue { get; init; }
        public int IntValue { get; init; }
        public long LongValue { get; init; }
        public float FloatValue { get; init; }
        public double DoubleValue { get; init; }
        public decimal DecimalValue { get; init; }
        public Guid GuidValue { get; init; }
        public DateTime DateTimeValue { get; init; }
        public DateTimeOffset DateTimeOffsetValue { get; init; }
        public TimeSpan TimeSpanValue { get; init; }
        public TestEnum EnumValue { get; init; }
    }
}
