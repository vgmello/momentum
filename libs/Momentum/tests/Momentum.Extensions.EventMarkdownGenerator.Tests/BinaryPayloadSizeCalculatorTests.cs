// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;
using Shouldly;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class BinaryPayloadSizeCalculatorTests
{
    private readonly BinaryPayloadSizeCalculator _calculator = new();

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
