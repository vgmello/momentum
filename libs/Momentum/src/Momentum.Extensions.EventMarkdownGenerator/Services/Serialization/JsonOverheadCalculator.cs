// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Calculates JSON serialization overhead for payload size estimation.
///     Accounts for quotes, property keys, colons, commas, braces, and brackets.
/// </summary>
public sealed class JsonOverheadCalculator : ISerializationOverheadCalculator
{
    public string FormatName => "JSON";

    /// <summary>Opening and closing double quotes around string values: 2 bytes.</summary>
    public int GetStringValueOverhead() => 2;

    /// <summary>"propertyName": = quotes (2) + name length + colon (1).</summary>
    public int GetPropertyOverhead(string propertyName) => propertyName.Length + 3;

    /// <summary>Object braces { }: 2 bytes.</summary>
    public int GetObjectOverhead() => 2;

    /// <summary>Comma separator: 1 byte.</summary>
    public int GetElementSeparatorOverhead() => 1;

    /// <summary>Array brackets [ ]: 2 bytes.</summary>
    public int GetCollectionOverhead() => 2;
}
