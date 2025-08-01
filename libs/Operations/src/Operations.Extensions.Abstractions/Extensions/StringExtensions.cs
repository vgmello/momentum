// Copyright (c) ABCDEG. All rights reserved.

namespace Operations.Extensions.Abstractions.Extensions;

/// <summary>
///     Provides extension methods for string manipulation and case conversion.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Converts a string to snake_case format.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The string converted to snake_case.</returns>
    /// <example>
    ///     "HelloWorld" becomes "hello_world"
    ///     "APIName" becomes "api_name"
    ///     "IOController" becomes "io_controller"
    /// </example>
    public static string ToSnakeCase(this string input) => ToLowerCaseWithSeparator(input, '_');

    /// <summary>
    ///     Converts a string to kebab-case format.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The string converted to kebab-case.</returns>
    /// <example>
    ///     "HelloWorld" becomes "hello-world"
    ///     "APIName" becomes "api-name"
    ///     "IOController" becomes "io-controller"
    /// </example>
    public static string ToKebabCase(this string input) => ToLowerCaseWithSeparator(input, '-');

    /// <summary>
    ///     Converts a camelCase/PascalCase string to a lowercase with a specified separator between words.
    /// </summary>
    /// <param name="input">The string.</param>
    /// <param name="separator">The character to use as a separator between words.</param>
    /// <returns>The string converted to lowercase with separators.</returns>
    public static string ToLowerCaseWithSeparator(this string input, char separator)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var inputSpan = input.AsSpan();
        var needsChange = false;
        var separatorCount = 0;

        // First Pass: Check for changes and calculate the total length
        for (var i = 0; i < inputSpan.Length; i++)
        {
            if (char.IsUpper(inputSpan[i]))
            {
                needsChange = true;

                if (i > 0 && IsWordBoundary(inputSpan, i))
                {
                    separatorCount++;
                }
            }
        }

        if (!needsChange)
        {
            return input;
        }

        var state = (Source: input, Separator: separator);

        // Second Pass: Build the new string
        return string.Create(inputSpan.Length + separatorCount, state, static (destinationSpan, state) =>
        {
            var sourceSpan = state.Source.AsSpan();
            var separatorChar = state.Separator;
            var writeIndex = 0;

            for (var readIndex = 0; readIndex < sourceSpan.Length; readIndex++)
            {
                var currentChar = sourceSpan[readIndex];

                if (char.IsUpper(currentChar))
                {
                    if (readIndex > 0 && IsWordBoundary(sourceSpan, readIndex))
                    {
                        destinationSpan[writeIndex++] = separatorChar;
                    }

                    destinationSpan[writeIndex++] = char.ToLowerInvariant(currentChar);
                }
                else
                {
                    destinationSpan[writeIndex++] = currentChar;
                }
            }
        });
    }

    private static bool IsWordBoundary(ReadOnlySpan<char> source, int currentIndex)
    {
        var prevChar = source[currentIndex - 1];

        // Boundary 1 & 2: Lowercase or digit followed by uppercase.
        if (char.IsLower(prevChar) || char.IsDigit(prevChar))
        {
            return true;
        }

        // Boundary 3: An acronym followed by a regular word (e.g., "APIName").
        if (char.IsUpper(prevChar) && currentIndex + 1 < source.Length && char.IsLower(source[currentIndex + 1]))
        {
            return true;
        }

        return false;
    }
}
