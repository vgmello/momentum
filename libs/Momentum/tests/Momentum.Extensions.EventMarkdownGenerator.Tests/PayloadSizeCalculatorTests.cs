// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;
using Shouldly;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class PayloadSizeCalculatorTests
{
    private readonly ISerializationOverheadCalculator _jsonCalc = new JsonOverheadCalculator();
    private readonly ISerializationOverheadCalculator _binaryCalc = new BinaryOverheadCalculator();

    // --- StringEncoding attribute resolution ---

    [Fact]
    public void CalculatePropertySize_StringWithMaxLength_UsesDefaultBytesPerChar()
    {
        // Default is 1 byte/char when using overhead calculator. Property "Name" with [MaxLength(50)]
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

    // --- String with no MaxLength ---

    [Fact]
    public void CalculatePropertySize_StringWithNoMaxLength_ReturnsInaccurate()
    {
        var prop = typeof(TestEventDefault).GetProperty(nameof(TestEventDefault.NoLength))!;
        var result = PayloadSizeCalculator.CalculatePropertySize(prop, prop.PropertyType, _jsonCalc);
        result.SizeBytes.ShouldBe(0);
        result.IsAccurate.ShouldBeFalse();
    }

    // --- Test types (inner classes so attribute resolution works) ---

    private record TestEventDefault
    {
        [MaxLength(50)]
        public string Name { get; init; } = "";
        public int Count { get; init; }
        public string NoLength { get; init; } = "";
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
