// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Resolves a serialization format name to its overhead calculator implementation.
/// </summary>
public static class OverheadCalculatorFactory
{
    public static ISerializationOverheadCalculator Create(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonOverheadCalculator(),
            "binary" => new BinaryOverheadCalculator(),
            _ => throw new ArgumentException(
                $"Unknown serialization format: '{format}'. Supported: json, binary.",
                nameof(format))
        };
    }
}
