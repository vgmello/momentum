// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.Abstractions.Extensions;

namespace Operations.Extensions.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("already_snake_case", "already_snake_case")]
    [InlineData("PascalCaseString", "pascal_case_string")]
    [InlineData("camelCaseString", "camel_case_string")]
    [InlineData("UPPERCASEString", "uppercase_string")]
    [InlineData("StringWITHAcronym", "string_with_acronym")]
    [InlineData("StartsWithUpper", "starts_with_upper")]
    [InlineData("endsWithUPPER", "ends_with_upper")]
    [InlineData("Multiple___Underscores", "multiple___underscores")]
    [InlineData("Number1InString", "number1_in_string")]
    [InlineData("Number11a2InString", "number11a2_in_string")]
    [InlineData("Number11InString", "number11_in_string")]
    [InlineData("Number11inString", "number11in_string")]
    [InlineData("StringWith1Number", "string_with1_number")]
    [InlineData("HTTPRequest", "http_request")]
    [InlineData("AnotherHTTPRequestExample", "another_http_request_example")]
    [InlineData("LeadingUnderscore", "leading_underscore")]
    [InlineData("MyAPI", "my_api")]
    [InlineData("MyAPIName", "my_api_name")]
    [InlineData("XXMyString", "xx_my_string")]
    [InlineData("A", "a")]
    [InlineData("a", "a")]
    [InlineData("12121_12121", "12121_12121")]
    [InlineData("12121_A12121", "12121_a12121")]
    [InlineData("_", "_")]
    public void ToSnakeCase_ConvertsCorrectly(string input, string expected)
    {
        var result = input.ToSnakeCase();
        result.ShouldBe(expected);
    }
}
