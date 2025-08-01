// Copyright (c) ABCDEG. All rights reserved.

namespace Operations.Extensions.EventMarkdownGenerator.Extensions;

/// <summary>
///     String extension methods for common formatting operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Converts a string to a safe filename by replacing invalid characters.
    /// </summary>
    public static string ToSafeFileName(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return "default";

        // Handle generic types by replacing angle brackets with underscores
        // e.g., List<string> becomes List_string_
        // e.g., Dictionary<string,int> becomes Dictionary_string_int_
        var result = value
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace(',', '_')
            .Replace(' ', '_');

        // Remove any other unsafe file name characters
        var invalidChars = Path.GetInvalidFileNameChars();
        result = new string(result.Where(c => !invalidChars.Contains(c)).ToArray());

        if (string.IsNullOrWhiteSpace(result))
            return "default";

        return result;
    }

    /// <summary>
    ///     Converts PascalCase to space-separated words for display.
    /// </summary>
    public static string ToDisplayName(this string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
            return eventName;

        // Convert PascalCase to space-separated words
        var result = string.Concat(
            eventName.Select((x, i) => i > 0 && char.IsUpper(x) && char.IsLower(eventName[i - 1])
                ? " " + x
                : x.ToString())
        );

        return result;
    }

    /// <summary>
    ///     Capitalizes the first letter of a string while preserving the rest.
    /// </summary>
    public static string CapitalizeFirst(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        if (char.IsUpper(input[0]))
            return input;

        return char.ToUpperInvariant(input[0]) + input[1..];
    }
}
