// Copyright (c) Momentum .NET. All rights reserved.

using System.Text.Json;
using Momentum.Extensions.EventMarkdownGenerator.Extensions;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Generates JSON sidebar navigation structure for documentation sites.
///     Groups events by subdomain and section, with separate schemas section.
/// </summary>
public static class JsonSidebarGenerator
{
    private const string UnknownValue = "Unknown";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    ///     Generates a JSON string containing the sidebar navigation structure.
    /// </summary>
    /// <param name="events">The events to include in the sidebar.</param>
    /// <returns>A formatted JSON string representing the sidebar structure.</returns>
    public static string GenerateSidebar(ICollection<EventWithDocumentation> events)
    {
        var sidebarItems = GenerateSidebarItems(events);

        return JsonSerializer.Serialize(sidebarItems, JsonOptions);
    }

    public static List<SidebarItem> GenerateSidebarItems(ICollection<EventWithDocumentation> events)
    {
        var eventGroups = GroupEventsBySubdomainAndSection(events);
        var sidebarItems = BuildSidebarStructure(eventGroups);

        var schemasSection = GenerateSchemasSection(events);
        if (schemasSection != null)
        {
            sidebarItems.Add(schemasSection);
        }

        return sidebarItems;
    }

    private static Dictionary<string, Dictionary<string, List<EventWithDocumentation>>> GroupEventsBySubdomainAndSection(
        ICollection<EventWithDocumentation> events)
    {
        var integrationEvents = events.Where(e => !e.Metadata.IsInternal);
        var domainEvents = events.Where(e => e.Metadata.IsInternal);

        var eventGroups = new Dictionary<string, Dictionary<string, List<EventWithDocumentation>>>();

        GroupEventsByType(integrationEvents, eventGroups, e => ParseNamespaceHierarchy(e.Metadata.Namespace).section);
        GroupEventsByType(domainEvents, eventGroups, _ => "Domain Events");

        return eventGroups;
    }

    private static void GroupEventsByType(
        IEnumerable<EventWithDocumentation> events,
        Dictionary<string, Dictionary<string, List<EventWithDocumentation>>> eventGroups,
        Func<EventWithDocumentation, string> sectionSelector)
    {
        foreach (var eventWithDoc in events)
        {
            var (subdomain, _) = ParseNamespaceHierarchy(eventWithDoc.Metadata.Namespace);
            var section = sectionSelector(eventWithDoc);

            AddEventToGroup(eventGroups, subdomain, section, eventWithDoc);
        }
    }

    private static void AddEventToGroup(
        Dictionary<string, Dictionary<string, List<EventWithDocumentation>>> eventGroups,
        string subdomain,
        string section,
        EventWithDocumentation eventWithDoc)
    {
        if (!eventGroups.TryGetValue(subdomain, out var sections))
        {
            sections = [];
            eventGroups[subdomain] = sections;
        }

        if (!sections.TryGetValue(section, out var list))
        {
            list = [];
            sections[section] = list;
        }

        list.Add(eventWithDoc);
    }

    private static List<SidebarItem> BuildSidebarStructure(
        Dictionary<string, Dictionary<string, List<EventWithDocumentation>>> eventGroups)
    {
        List<SidebarItem> sidebarItems = [];

        foreach (var (subdomain, sections) in eventGroups.OrderBy(x => x.Key))
        {
            var subdomainItem = CreateSubdomainItem(subdomain, sections);
            sidebarItems.Add(subdomainItem);
        }

        return sidebarItems;
    }

    private static SidebarItem CreateSubdomainItem(
        string subdomain,
        Dictionary<string, List<EventWithDocumentation>> sections)
    {
        var subdomainItem = new SidebarItem
        {
            Text = CapitalizeDomain(subdomain),
            Link = null,
            Collapsed = false,
            Items = []
        };

        if (HasMultipleSectionsOrNamedSection(sections))
        {
            AddSectionsToSubdomain(subdomainItem, sections);
        }
        else if (sections.Count > 0)
        {
            AddEventsDirectlyToSubdomain(subdomainItem, sections.Values.First());
        }

        return subdomainItem;
    }

    private static bool HasMultipleSectionsOrNamedSection(Dictionary<string, List<EventWithDocumentation>> sections)
    {
        return sections.Count > 1 || (sections.Count == 1 && !string.IsNullOrEmpty(sections.Keys.First()));
    }

    private static void AddSectionsToSubdomain(
        SidebarItem subdomainItem,
        Dictionary<string, List<EventWithDocumentation>> sections)
    {
        foreach (var (section, sectionEvents) in sections.OrderBy(x => x.Key))
        {
            if (!string.IsNullOrEmpty(section))
            {
                var sectionItem = CreateSectionItem(section, sectionEvents);
                subdomainItem.Items.Add(sectionItem);
            }
            else
            {
                AddEventsDirectlyToSubdomain(subdomainItem, sectionEvents);
            }
        }
    }

    private static SidebarItem CreateSectionItem(
        string section,
        List<EventWithDocumentation> sectionEvents)
    {
        return new SidebarItem
        {
            Text = CapitalizeDomain(section),
            Link = null,
            Collapsed = false,
            Items = CreateEventItems(sectionEvents)
        };
    }

    private static void AddEventsDirectlyToSubdomain(
        SidebarItem subdomainItem,
        List<EventWithDocumentation> events)
    {
        subdomainItem.Items.AddRange(CreateEventItems(events));
    }

    private static List<SidebarItem> CreateEventItems(List<EventWithDocumentation> events)
    {
        return events
            .OrderBy(e => e.Metadata.EventName)
            .Select(CreateEventSidebarItem)
            .ToList();
    }

    private static (string subdomain, string section) ParseNamespaceHierarchy(string namespaceName)
    {
        // For namespaces like "AppDomain.Cashiers.Contracts.IntegrationEvents" or "AppDomain.Invoices.Contracts.DomainEvents", extract subdomain and section
        // Pattern: Domain.Subdomain.[Section].Contracts.IntegrationEvents|DomainEvents
        var parts = namespaceName.Split('.');

        // Find the index of "Contracts", "IntegrationEvents", or "DomainEvents"
        var contractsIndex = Array.IndexOf(parts, NamespaceConstants.Contracts);
        var integrationEventsIndex = Array.IndexOf(parts, NamespaceConstants.IntegrationEvents);
        var domainEventsIndex = Array.IndexOf(parts, NamespaceConstants.DomainEvents);

        int endIndex;

        if (contractsIndex != -1)
        {
            endIndex = contractsIndex;
        }
        else if (integrationEventsIndex != -1)
        {
            endIndex = integrationEventsIndex;
        }
        else
        {
            endIndex = domainEventsIndex;
        }

        if (endIndex == -1)
        {
            endIndex = parts.Length;
        }

        // Extract subdomain (second part) and section (if exists between subdomain and Contracts)
        var subdomain = parts.Length > 1 ? parts[1] : UnknownValue;
        var section = "";

        // If there are parts between subdomain and Contracts/IntegrationEvents, it's a section
        if (endIndex > 2)
        {
            section = string.Join(".", parts.Skip(2).Take(endIndex - 2));
        }

        return (subdomain, section);
    }

    private static SidebarItem? GenerateSchemasSection(IEnumerable<EventWithDocumentation> events)
    {
        // Collect all unique complex types from all events
        var complexTypes = new HashSet<Type>();

        foreach (var eventWithDoc in events)
        {
            var eventComplexTypes = TypeUtils.CollectComplexTypesFromProperties(eventWithDoc.Metadata.Properties);
            complexTypes.UnionWith(eventComplexTypes);
        }

        if (complexTypes.Count == 0)
            return null;

        List<SidebarItem> schemaItems = [];

        // Group schemas by namespace (subdomain)
        var schemasByNamespace = complexTypes
            .GroupBy(ExtractSubdomainFromType)
            .OrderBy(g => g.Key);

        foreach (var namespaceGroup in schemasByNamespace)
        {
            var subdomainSchemas = namespaceGroup
                .OrderBy(t => t.Name)
                .Select(t => new SidebarItem
                {
                    Text = t.Name,
                    Link = $"/schemas/{TypeUtils.GetCleanTypeName(t).ToSafeFileName()}"
                })
                .ToList();

            // Always group schemas under subdomain
            schemaItems.Add(new SidebarItem
            {
                Text = CapitalizeDomain(namespaceGroup.Key),
                Collapsed = false,
                Items = subdomainSchemas
            });
        }

        return new SidebarItem
        {
            Text = "Schemas",
            Collapsed = false,
            Items = schemaItems
        };
    }

    private static string ExtractSubdomainFromType(Type type)
    {
        if (type.Namespace == null)
            return UnknownValue;

        var parts = type.Namespace.Split('.');

        return parts.Length > 1 ? parts[1] : parts[0];
    }



    /// <summary>
    ///     Generates and writes the sidebar JSON to a file asynchronously.
    /// </summary>
    /// <param name="events">The events to include in the sidebar.</param>
    /// <param name="filePath">The file path to write the sidebar JSON to.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task WriteSidebarAsync(ICollection<EventWithDocumentation> events, string filePath, CancellationToken cancellationToken = default)
    {
        var sidebarJson = GenerateSidebar(events);
        await File.WriteAllTextAsync(filePath, sidebarJson, cancellationToken);
    }

    private static SidebarItem CreateEventSidebarItem(EventWithDocumentation eventWithDoc)
    {
        var metadata = eventWithDoc.Metadata;
        var displayName = metadata.EventName.ToDisplayName();
        var link = "/" + metadata.GetFileName().Replace(".md", "");

        return new SidebarItem
        {
            Text = displayName,
            Link = link
        };
    }

    private static string CapitalizeDomain(string domain)
    {
        if (string.IsNullOrEmpty(domain))
            return UnknownValue;

        // Handle special cases
        if (domain.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            return UnknownValue;

        return domain.CapitalizeFirst();
    }
}
