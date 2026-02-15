// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Returns zero overhead for all calculations.
///     Binary serialization formats (Avro, Protobuf) have negligible text-based overhead.
/// </summary>
internal sealed class BinaryPayloadSizeCalculator : PayloadSizeCalculator
{
    public override string FormatName => "Binary";
    protected override int GetStringValueOverhead() => 0;
    protected override int GetPropertyOverhead(string propertyName) => 0;
    protected override int GetObjectOverhead() => 0;
    protected override int GetElementSeparatorOverhead() => 0;
    protected override int GetCollectionOverhead() => 0;
}
