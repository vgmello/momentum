// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Returns zero overhead for all calculations.
///     Binary serialization formats (Avro, Protobuf) have negligible text-based overhead.
/// </summary>
public sealed class BinaryOverheadCalculator : ISerializationOverheadCalculator
{
    public string FormatName => "Binary";
    public int GetStringValueOverhead() => 0;
    public int GetPropertyOverhead(string propertyName) => 0;
    public int GetObjectOverhead() => 0;
    public int GetElementSeparatorOverhead() => 0;
    public int GetCollectionOverhead() => 0;
}
