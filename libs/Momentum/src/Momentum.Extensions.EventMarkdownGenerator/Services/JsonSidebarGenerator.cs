// Copyright (c) Momentum .NET. All rights reserved.

using System.Text.Json;
using Momentum.Extensions.EventMarkdownGenerator.Extensions;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

public class JsonSidebarGenerator
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string GenerateSidebar(ICollection<EventWithDocumentation> events)
    {
        var sidebarItems = GenerateSidebarItems(events);

        return JsonSerializer.Serialize(sidebarItems, _jsonOptions);
    }

    public List<SidebarItem> GenerateSidebarItems(ICollection<EventWithDocumentation> events)
    {
        var sidebarItems = new List<SidebarItem>();

        // Separate integration events and domain events
        var integrationEvents = events.Where(e => !e.Metadata.IsInternal).ToList();
        var domainEvents = events.Where(e => e.Metadata.IsInternal).ToList();

        // Group all events by subdomain
        var allEventGroups = new Dictionary<string, Dictionary<string, List<EventWithDocumentation>>>();

        // Process integration events
        foreach (var eventWithDoc in integrationEvents)
        {
            var namespaceParts = ParseNamespaceHierarchy(eventWithDoc.Metadata.Namespace);
            var subdomain = namespaceParts.subdomain;
            var section = namespaceParts.section;

            if (!allEventGroups.ContainsKey(subdomain))
            {
                allEventGroups[subdomain] = [];
            }

            if (!allEventGroups[subdomain].ContainsKey(section))
            {
                allEventGroups[subdomain][section] = [];
            }

            allEventGroups[subdomain][section].Add(eventWithDoc);
        }

        // Process domain events - add them under their respective subdomains
        foreach (var eventWithDoc in domainEvents)
        {
            var namespaceParts = ParseNamespaceHierarchy(eventWithDoc.Metadata.Namespace);
            var subdomain = namespaceParts.subdomain;
            var section = "Domain Events"; // Fixed section name for domain events

            if (!allEventGroups.ContainsKey(subdomain))
            {
                allEventGroups[subdomain] = [];
            }

            if (!allEventGroups[subdomain].ContainsKey(section))
            {
                allEventGroups[subdomain][section] = [];
            }

            allEventGroups[subdomain][section].Add(eventWithDoc);
        }

        // Build sidebar structure
        foreach (var (subdomain, sections) in allEventGroups.OrderBy(x => x.Key))
        {
            var subdomainItem = new SidebarItem
            {
                Text = CapitalizeDomain(subdomain),
                Link = null,
                Collapsed = false,
                Items = []
            };

            // If there are multiple sections, create nested structure
            if (sections.Count > 1 || (sections.Count == 1 && sections.Keys.First() != ""))
            {
                foreach (var (section, sectionEvents) in sections.OrderBy(x => x.Key))
                {
                    if (!string.IsNullOrEmpty(section))
                    {
                        var sectionItem = new SidebarItem
                        {
                            Text = CapitalizeDomain(section),
                            Link = null,
                            Collapsed = false,
                            Items = sectionEvents
                                .OrderBy(e => e.Metadata.EventName)
                                .Select(CreateEventSidebarItem)
                                .ToList()
                        };
                        subdomainItem.Items.Add(sectionItem);
                    }
                    else
                    {
                        // Add events directly to subdomain if no section
                        subdomainItem.Items.AddRange(sectionEvents
                            .OrderBy(e => e.Metadata.EventName)
                            .Select(CreateEventSidebarItem));
                    }
                }
            }
            else
            {
                var sectionEvents = sections.Values.First();
                subdomainItem.Items.AddRange(sectionEvents
                    .OrderBy(e => e.Metadata.EventName)
                    .Select(CreateEventSidebarItem));
            }

            sidebarItems.Add(subdomainItem);
        }

        var schemasSection = GenerateSchemasSection(events);

        if (schemasSection != null)
        {
            sidebarItems.Add(schemasSection);
        }

        return sidebarItems;
    }

    private static (string subdomain, string section) ParseNamespaceHierarchy(string namespaceName)
    {
        // For namespaces like "AppDomain.Cashiers.Contracts.IntegrationEvents" or "AppDomain.Invoices.Contracts.DomainEvents", extract subdomain and section
        // Pattern: Domain.Subdomain.[Section].Contracts.IntegrationEvents|DomainEvents
        var parts = namespaceName.Split('.');

        // Find the index of "Contracts", "IntegrationEvents", or "DomainEvents"
        var contractsIndex = Array.IndexOf(parts, "Contracts");
        var integrationEventsIndex = Array.IndexOf(parts, "IntegrationEvents");
        var domainEventsIndex = Array.IndexOf(parts, "DomainEvents");

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
        var subdomain = parts.Length > 1 ? parts[1] : "Unknown";
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

        if (!complexTypes.Any())
            return null;

        var schemaItems = new List<SidebarItem>();

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
                    Link = $"/schemas/{(t.FullName ?? "UnknownType").ToSafeFileName()}"
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
            return "Unknown";

        var parts = type.Namespace.Split('.');

        return parts.Length > 1 ? parts[1] : parts[0];
    }


    public async Task WriteSidebarAsync(ICollection<EventWithDocumentation> events, string filePath)
    {
        var sidebarJson = GenerateSidebar(events);
        await File.WriteAllTextAsync(filePath, sidebarJson);
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
            return "Unknown";

        // Handle special cases
        if (domain.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            return "Unknown";

        return domain.CapitalizeFirst();
    }
}
