using System.Text.Json;
using Operations.Extensions.EventDocGenerator.Models;

namespace Operations.Extensions.EventDocGenerator.Services;

public class JsonSidebarGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string Generate(AssemblyEventDocumentation documentation, string markdownFileName)
    {
        var sidebarRoot = CreateSidebarStructure(documentation, markdownFileName);
        return JsonSerializer.Serialize(sidebarRoot, JsonOptions);
    }

    private static SidebarItem CreateSidebarStructure(AssemblyEventDocumentation documentation, string markdownFileName)
    {
        var markdownPath = "/" + Path.ChangeExtension(markdownFileName, null);
        
        var rootItems = new List<SidebarItem>();

        // Public Events Section
        if (documentation.PublicEvents.Any())
        {
            var publicEventsSection = CreateEventsSection(
                "Public Events", 
                documentation.PublicEvents, 
                markdownPath, 
                collapsed: false);
            
            rootItems.Add(publicEventsSection);
        }

        // Internal Events Section
        if (documentation.InternalEvents.Any())
        {
            var internalEventsSection = CreateEventsSection(
                "Internal Events", 
                documentation.InternalEvents, 
                markdownPath, 
                collapsed: true);
            
            rootItems.Add(internalEventsSection);
        }

        return new SidebarItem
        {
            Text = $"Integration Events - {documentation.AssemblyName}",
            Items = rootItems
        };
    }

    private static SidebarItem CreateEventsSection(
        string sectionTitle, 
        List<EventWithDocumentation> events, 
        string markdownPath, 
        bool collapsed)
    {
        var eventsByTopic = events
            .GroupBy(e => e.Metadata.EventName)
            .OrderBy(g => g.Key)
            .ToList();

        var sectionItems = new List<SidebarItem>();

        foreach (var eventGroup in eventsByTopic)
        {
            var eventName = eventGroup.Key;
            var versions = eventGroup.OrderBy(e => e.Metadata.Version).ToList();

            if (versions.Count == 1)
            {
                // Single version - direct link
                var singleEvent = versions.First();
                sectionItems.Add(new SidebarItem
                {
                    Text = singleEvent.Metadata.GetDisplayName(),
                    Link = $"{markdownPath}#{singleEvent.Metadata.GetAnchorId()}"
                });
            }
            else
            {
                // Multiple versions - group with sub-items
                var versionItems = versions.Select(eventWithDoc => new SidebarItem
                {
                    Text = eventWithDoc.Metadata.Version,
                    Link = $"{markdownPath}#{eventWithDoc.Metadata.GetAnchorId()}"
                }).ToList();

                sectionItems.Add(new SidebarItem
                {
                    Text = eventName,
                    Items = versionItems
                });
            }
        }

        return new SidebarItem
        {
            Text = sectionTitle,
            Items = sectionItems,
            Collapsed = collapsed
        };
    }
}