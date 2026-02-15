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
