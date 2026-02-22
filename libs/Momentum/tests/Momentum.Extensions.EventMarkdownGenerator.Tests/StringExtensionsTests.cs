// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Extensions;
using Shouldly;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class StringExtensionsTests
{
    // --- ToSafeFileName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToSafeFileName_WithNullOrEmpty_ShouldReturnDefault(string? input)
    {
        var result = input!.ToSafeFileName();

        result.ShouldBe("default");
    }

    [Fact]
    public void ToSafeFileName_WithGenericType_ShouldReplaceAngleBrackets()
    {
        var result = "List<string>".ToSafeFileName();

        result.ShouldBe("List_string_");
    }

    [Fact]
    public void ToSafeFileName_WithDictionaryType_ShouldReplaceMultipleInvalidChars()
    {
        var result = "Dictionary<string,int>".ToSafeFileName();

        result.ShouldBe("Dictionary_string_int_");
    }

    [Fact]
    public void ToSafeFileName_WithNormalString_ShouldReturnSame()
    {
        var result = "MyEventName".ToSafeFileName();

        result.ShouldBe("MyEventName");
    }

    [Fact]
    public void ToSafeFileName_WithSpaces_ShouldReplaceWithUnderscores()
    {
        var result = "My Event Name".ToSafeFileName();

        result.ShouldBe("My_Event_Name");
    }

    [Fact]
    public void ToSafeFileName_WithBacktick_ShouldReplaceWithUnderscore()
    {
        var result = "Dictionary`2".ToSafeFileName();

        result.ShouldBe("Dictionary_2");
    }

    [Fact]
    public void ToSafeFileName_WithBrackets_ShouldReplaceWithUnderscores()
    {
        var result = "int[]".ToSafeFileName();

        result.ShouldBe("int__");
    }

    // --- ToDisplayName ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToDisplayName_WithNullOrEmpty_ShouldReturnSame(string? input)
    {
        var result = input!.ToDisplayName();

        result.ShouldBe(input);
    }

    [Fact]
    public void ToDisplayName_WithPascalCase_ShouldAddSpaces()
    {
        var result = "CashierCreated".ToDisplayName();

        result.ShouldBe("Cashier Created");
    }

    [Fact]
    public void ToDisplayName_WithSingleWord_ShouldReturnSame()
    {
        var result = "Cashier".ToDisplayName();

        result.ShouldBe("Cashier");
    }

    [Fact]
    public void ToDisplayName_WithMultipleWords_ShouldSeparateAll()
    {
        var result = "CustomerOrderCreated".ToDisplayName();

        result.ShouldBe("Customer Order Created");
    }

    [Fact]
    public void ToDisplayName_WithLowercaseString_ShouldReturnSame()
    {
        var result = "lowercase".ToDisplayName();

        result.ShouldBe("lowercase");
    }

    // --- CapitalizeFirst ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CapitalizeFirst_WithNullOrEmpty_ShouldReturnSame(string? input)
    {
        var result = input!.CapitalizeFirst();

        result.ShouldBe(input);
    }

    [Fact]
    public void CapitalizeFirst_WithLowercase_ShouldCapitalize()
    {
        var result = "hello".CapitalizeFirst();

        result.ShouldBe("Hello");
    }

    [Fact]
    public void CapitalizeFirst_WithAlreadyCapitalized_ShouldReturnSame()
    {
        var result = "Hello".CapitalizeFirst();

        result.ShouldBe("Hello");
    }

    [Fact]
    public void CapitalizeFirst_WithSingleLowercaseChar_ShouldCapitalize()
    {
        var result = "a".CapitalizeFirst();

        result.ShouldBe("A");
    }

    [Fact]
    public void CapitalizeFirst_WithMixedCase_ShouldOnlyCapitalizeFirst()
    {
        var result = "helloWorld".CapitalizeFirst();

        result.ShouldBe("HelloWorld");
    }
}
