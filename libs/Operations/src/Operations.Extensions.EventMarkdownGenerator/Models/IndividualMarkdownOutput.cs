// Copyright (c) ABCDEG. All rights reserved.

namespace Operations.Extensions.EventMarkdownGenerator.Models;

public record IndividualMarkdownOutput
{
    public required string FileName { get; init; }
    public required string Content { get; init; }
    public required string FilePath { get; init; }
}
