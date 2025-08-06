// Copyright (c) ABCDEG. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Templates.Models;

public class PartitionKeyViewModel
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
}
