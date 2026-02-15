// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Returns zero overhead for all calculations.
///     Binary serialization formats (Avro, Protobuf) have negligible text-based overhead.
/// </summary>
public sealed class BinaryPayloadSizeCalculator : PayloadSizeCalculator
{
    public override string FormatName => "Binary";
    public override int GetStringValueOverhead() => 0;
    public override int GetPropertyOverhead(string propertyName) => 0;
    public override int GetObjectOverhead() => 0;
    public override int GetElementSeparatorOverhead() => 0;
    public override int GetCollectionOverhead() => 0;
}
