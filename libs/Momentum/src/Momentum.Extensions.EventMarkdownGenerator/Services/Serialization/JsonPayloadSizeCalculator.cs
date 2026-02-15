// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Calculates JSON serialization overhead for payload size estimation.
///     Accounts for quotes, property keys, colons, commas, braces, and brackets.
/// </summary>
internal sealed class JsonPayloadSizeCalculator : PayloadSizeCalculator
{
    public override string FormatName => "JSON";

    /// <summary>Opening and closing double quotes around string values: 2 bytes.</summary>
    protected override int GetStringValueOverhead() => 2;

    /// <summary>"propertyName": = quotes (2) + name length + colon (1).</summary>
    protected override int GetPropertyOverhead(string propertyName) => propertyName.Length + 3;

    /// <summary>Object braces { }: 2 bytes.</summary>
    protected override int GetObjectOverhead() => 2;

    /// <summary>Comma separator: 1 byte.</summary>
    protected override int GetElementSeparatorOverhead() => 1;

    /// <summary>Array brackets [ ]: 2 bytes.</summary>
    protected override int GetCollectionOverhead() => 2;
}
