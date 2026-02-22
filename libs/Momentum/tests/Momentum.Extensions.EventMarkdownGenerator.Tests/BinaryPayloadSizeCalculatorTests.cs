// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class BinaryPayloadSizeCalculatorTests
{
    private readonly PayloadSizeCalculator _calculator = PayloadSizeCalculator.Create("binary");

    [Fact]
    public void FormatName_ShouldBeBinary()
    {
        _calculator.FormatName.ShouldBe("Binary");
    }

    [Fact]
    public void CalculatePropertySize_String_ShouldHaveNoOverhead()
    {
        // Binary: (50 * 1) + 0 overhead = 50
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.Name))!;

        var result = _calculator.CalculatePropertySize(prop, prop.PropertyType);

        result.SizeBytes.ShouldBe(50);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_Int_ShouldReturn4Bytes()
    {
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.Count))!;

        var result = _calculator.CalculatePropertySize(prop, prop.PropertyType);

        result.SizeBytes.ShouldBe(4);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_Guid_ShouldReturn16Bytes()
    {
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.Id))!;

        var result = _calculator.CalculatePropertySize(prop, prop.PropertyType);

        result.SizeBytes.ShouldBe(16);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_Bool_ShouldReturn1Byte()
    {
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.IsActive))!;

        var result = _calculator.CalculatePropertySize(prop, prop.PropertyType);

        result.SizeBytes.ShouldBe(1);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_Long_ShouldReturn8Bytes()
    {
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.Timestamp))!;

        var result = _calculator.CalculatePropertySize(prop, prop.PropertyType);

        result.SizeBytes.ShouldBe(8);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_Decimal_ShouldReturn16Bytes()
    {
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.Amount))!;

        var result = _calculator.CalculatePropertySize(prop, prop.PropertyType);

        result.SizeBytes.ShouldBe(16);
        result.IsAccurate.ShouldBeTrue();
    }

    [Fact]
    public void CalculatePropertySize_StringWithNoMaxLength_ShouldReturnInaccurate()
    {
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.Description))!;

        var result = _calculator.CalculatePropertySize(prop, prop.PropertyType);

        result.SizeBytes.ShouldBe(0);
        result.IsAccurate.ShouldBeFalse();
        result.Warning.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void CalculatePropertySize_StringVsJson_BinaryShouldHaveNoOverhead()
    {
        var jsonCalc = PayloadSizeCalculator.Create("json");
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.Name))!;

        var binaryResult = _calculator.CalculatePropertySize(prop, prop.PropertyType);
        var jsonResult = jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // Binary should be smaller: no string quote overhead
        binaryResult.SizeBytes.ShouldBeLessThan(jsonResult.SizeBytes);
        // Binary: 50 bytes, JSON: 52 bytes (50 + 2 for quotes)
        binaryResult.SizeBytes.ShouldBe(50);
        jsonResult.SizeBytes.ShouldBe(52);
    }

    [Fact]
    public void CalculatePropertySize_ComplexType_BinaryShouldHaveNoObjectOverhead()
    {
        var jsonCalc = PayloadSizeCalculator.Create("json");
        var prop = typeof(BinaryTestEventWithNested).GetProperty(nameof(BinaryTestEventWithNested.Nested))!;

        var binaryResult = _calculator.CalculatePropertySize(prop, prop.PropertyType);
        var jsonResult = jsonCalc.CalculatePropertySize(prop, prop.PropertyType);

        // Binary should have no overhead for object wrappers, property names, separators
        binaryResult.SizeBytes.ShouldBeLessThan(jsonResult.SizeBytes);
    }

    [Fact]
    public void Create_BinaryFormat_CaseInsensitive()
    {
        var lower = PayloadSizeCalculator.Create("binary");
        var mixed = PayloadSizeCalculator.Create("Binary");

        lower.FormatName.ShouldBe("Binary");
        mixed.FormatName.ShouldBe("Binary");
    }

    [Fact]
    public void CalculatePropertySize_DateTimeOffset_ShouldReturn10Bytes()
    {
        var prop = typeof(BinaryTestEvent).GetProperty(nameof(BinaryTestEvent.CreatedAt))!;

        var result = _calculator.CalculatePropertySize(prop, prop.PropertyType);

        result.SizeBytes.ShouldBe(10);
        result.IsAccurate.ShouldBeTrue();
    }

    // Test types - properties are accessed via reflection, not direct code usage
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record BinaryTestEvent
    {
        [MaxLength(50)]
        public string Name { get; init; } = "";
        public int Count { get; init; }
        public Guid Id { get; init; }
        public bool IsActive { get; init; }
        public long Timestamp { get; init; }
        public decimal Amount { get; init; }
        public string Description { get; init; } = "";
        public DateTimeOffset CreatedAt { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record BinaryNestedType
    {
        public int Value { get; init; }
        public Guid Id { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private record BinaryTestEventWithNested
    {
        public BinaryNestedType Nested { get; init; } = new();
    }
}
