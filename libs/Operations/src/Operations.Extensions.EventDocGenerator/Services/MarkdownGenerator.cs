using System.Text;
using Operations.Extensions.EventDocGenerator.Models;

namespace Operations.Extensions.EventDocGenerator.Services;

public class MarkdownGenerator
{
    public string Generate(AssemblyEventDocumentation documentation)
    {
        var markdown = new StringBuilder();

        // Header
        markdown.AppendLine($"# Integration Events - {documentation.AssemblyName}");
        markdown.AppendLine();

        // Summary statistics
        var totalEvents = documentation.AllEvents.Count();
        var publicEventCount = documentation.PublicEvents.Count;
        var internalEventCount = documentation.InternalEvents.Count;

        markdown.AppendLine($"**Total Events:** {totalEvents} ({publicEventCount} public, {internalEventCount} internal)");
        markdown.AppendLine($"**Default Domain:** {documentation.DefaultDomain}");
        markdown.AppendLine();

        // Public Events Section
        if (documentation.PublicEvents.Any())
        {
            markdown.AppendLine("## Public Events");
            markdown.AppendLine();
            GenerateEventsSection(markdown, documentation.PublicEvents);
        }

        // Internal Events Section
        if (documentation.InternalEvents.Any())
        {
            markdown.AppendLine("## Internal Events");
            markdown.AppendLine();
            GenerateEventsSection(markdown, documentation.InternalEvents);
        }

        return markdown.ToString();
    }

    private static void GenerateEventsSection(StringBuilder markdown, List<EventWithDocumentation> events)
    {
        var eventsByTopic = events
            .GroupBy(e => e.Metadata.EventName)
            .OrderBy(g => g.Key);

        foreach (var eventGroup in eventsByTopic)
        {
            var eventName = eventGroup.Key;
            var versions = eventGroup.OrderBy(e => e.Metadata.Version).ToList();

            markdown.AppendLine($"### {eventName}");
            markdown.AppendLine();

            foreach (var eventWithDoc in versions)
            {
                GenerateEventVersionSection(markdown, eventWithDoc);
            }

            markdown.AppendLine("---");
            markdown.AppendLine();
        }
    }

    private static void GenerateEventVersionSection(StringBuilder markdown, EventWithDocumentation eventWithDoc)
    {
        var metadata = eventWithDoc.Metadata;
        var documentation = eventWithDoc.Documentation;

        // Version header
        markdown.AppendLine($"#### Version {metadata.Version}");
        markdown.AppendLine();

        // Anchor for linking
        markdown.AppendLine($"<div id=\"{metadata.GetAnchorId()}\"></div>");
        markdown.AppendLine();

        // Topic information
        markdown.AppendLine($"**Topic:** `{metadata.TopicName}`");
        markdown.AppendLine();

        // Summary
        if (!string.IsNullOrEmpty(documentation.Summary))
        {
            markdown.AppendLine(documentation.Summary);
            markdown.AppendLine();
        }

        // Trigger scenario
        var triggerScenario = documentation.GetTriggerScenario();
        markdown.AppendLine($"**Trigger Scenario:** {triggerScenario}");
        markdown.AppendLine();

        // Properties table
        if (metadata.Properties.Any())
        {
            markdown.AppendLine("**Properties:**");
            markdown.AppendLine();
            markdown.AppendLine("| Name | Type | Required | Description |");
            markdown.AppendLine("|------|------|----------|-------------|");

            foreach (var property in metadata.Properties.OrderBy(p => p.Name))
            {
                var description = documentation.PropertyDescriptions.GetValueOrDefault(property.Name, "No description");
                var required = property.IsRequired ? "✓" : "";
                
                markdown.AppendLine($"| {property.Name} | `{property.TypeName}` | {required} | {description} |");
            }

            markdown.AppendLine();
        }

        // Example if available
        if (!string.IsNullOrEmpty(documentation.Example))
        {
            markdown.AppendLine("**Example:**");
            markdown.AppendLine();
            markdown.AppendLine("```json");
            markdown.AppendLine(documentation.Example);
            markdown.AppendLine("```");
            markdown.AppendLine();
        }

        // Metadata information
        markdown.AppendLine("**Metadata:**");
        markdown.AppendLine($"- **Full Type:** `{metadata.FullTypeName}`");
        markdown.AppendLine($"- **Namespace:** `{metadata.Namespace}`");
        markdown.AppendLine($"- **Domain:** `{metadata.Domain}`");
        markdown.AppendLine($"- **Internal:** {(metadata.IsInternal ? "Yes" : "No")}");
        markdown.AppendLine();
    }
}