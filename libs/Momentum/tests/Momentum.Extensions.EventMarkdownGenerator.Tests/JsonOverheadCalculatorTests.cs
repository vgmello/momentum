using Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;
using Shouldly;
using Xunit;

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
        _calculator.GetStringValueOverhead().ShouldBe(2);
    }

    [Theory]
    [InlineData("Id", 5)]
    [InlineData("Name", 7)]
    [InlineData("X", 4)]
    public void GetPropertyOverhead_ShouldIncludeKeyQuotesAndColon(string name, int expected)
    {
        _calculator.GetPropertyOverhead(name).ShouldBe(expected);
    }

    [Fact]
    public void GetObjectOverhead_ShouldReturn2ForBraces()
    {
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
        _calculator.GetCollectionOverhead().ShouldBe(2);
    }
}
