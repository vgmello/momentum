// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Services.Serialization;

/// <summary>
///     Calculates serialization format overhead for payload size estimation.
///     Implement this interface to support additional formats (Avro, Protobuf, etc.).
/// </summary>
public interface ISerializationOverheadCalculator
{
    /// <summary>Display name of the serialization format (e.g., "JSON", "Binary").</summary>
    string FormatName { get; }

    /// <summary>Overhead bytes for a serialized string value (e.g., JSON quotes: 2 bytes).</summary>
    int GetStringValueOverhead();

    /// <summary>Overhead bytes for a property entry including key and delimiters (e.g., JSON: "key": adds key.Length + 3).</summary>
    int GetPropertyOverhead(string propertyName);

    /// <summary>Overhead bytes for object wrappers (e.g., JSON { } adds 2 bytes).</summary>
    int GetObjectOverhead();

    /// <summary>Overhead bytes per element separator (e.g., JSON comma: 1 byte).</summary>
    int GetElementSeparatorOverhead();

    /// <summary>Overhead bytes for collection wrappers (e.g., JSON [ ] adds 2 bytes).</summary>
    int GetCollectionOverhead();
}
