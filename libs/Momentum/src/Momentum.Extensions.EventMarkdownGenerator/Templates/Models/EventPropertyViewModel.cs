// Copyright (c) ABCDEG. All rights reserved.

namespace Momentum.Extensions.EventMarkdownGenerator.Templates.Models;

public class EventPropertyViewModel
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsComplexType { get; set; }
    public bool IsCollectionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SchemaLink { get; set; }
    public string? SchemaPath { get; set; }
    public string? ElementTypeName { get; set; }
    public string? ElementSchemaPath { get; set; }
    public int EstimatedSizeBytes { get; set; }
    public bool IsAccurate { get; set; } = true;
    public string? SizeWarning { get; set; }

    public string EstimatedSizeDisplay
    {
        get
        {
            if (IsAccurate)
            {
                return $"{EstimatedSizeBytes} bytes";
            }

            var warningText = string.IsNullOrEmpty(SizeWarning) ? "dynamic" : SizeWarning;

            return $"{EstimatedSizeBytes} bytes ({warningText})";
        }
    }
}
