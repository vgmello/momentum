// Copyright (c) Momentum .NET. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Models;

public record PayloadSizeResult
{
    public int SizeBytes { get; init; }
    public bool IsAccurate { get; init; }
    public string? Warning { get; init; }
}
