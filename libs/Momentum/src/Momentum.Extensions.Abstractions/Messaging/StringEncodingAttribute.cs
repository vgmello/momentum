// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.Abstractions.Messaging;

/// <summary>
///     Controls string byte-size estimation for payload size calculations.
///     Apply at assembly, class, or property level. Resolution order: Property > Class > Assembly > default (1).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Property)]
public class StringEncodingAttribute : Attribute
{
    /// <summary>
    ///     Bytes per character for string size estimation.
    ///     Common values: 1 (UTF-8 ASCII/Latin), 2 (UTF-16), 4 (worst-case UTF-8/UTF-32).
    /// </summary>
    public int BytesPerChar { get; set; } = 1;
}
