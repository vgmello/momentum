// Copyright (c) ABCDEG. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Models;

public record SidebarItem
{
    public required string Text { get; init; }
    public string? Link { get; init; }
    public List<SidebarItem> Items { get; init; } = [];
    public bool Collapsed { get; init; }
}
