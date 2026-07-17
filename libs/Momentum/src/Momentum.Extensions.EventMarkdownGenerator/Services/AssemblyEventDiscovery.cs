// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

public static class AssemblyEventDiscovery
{
    private const string DefaultAttributeNamePrefix = nameof(EventTopicAttribute);
    private const string DefaultPartitionKeyAttributeNamePrefix = nameof(PartitionKeyAttribute);

    public static IEnumerable<EventWithDocumentation> DiscoverEvents(Assembly assembly, XmlDocumentationParser? xmlParser,
        PayloadSizeCalculator calculator, string attributeNamePrefix = DefaultAttributeNamePrefix,
        string partitionKeyAttributeNamePrefix = DefaultPartitionKeyAttributeNamePrefix, bool emitPublicVisibility = false)
    {
        var defaultDomain = GetMainDomainName(assembly);
        var integrationEventTypes = GetEventTypes(assembly, attributeNamePrefix);

        return integrationEventTypes.Select(type =>
        {
            var metadata =
                EventMetadataBuilder.Build(type, defaultDomain, xmlParser, calculator, attributeNamePrefix,
                    partitionKeyAttributeNamePrefix, emitPublicVisibility);

            return new EventWithDocumentation
            {
                Metadata = metadata,
                Documentation = xmlParser?.GetEventDocumentation(type) ?? new EventDocumentation { Summary = string.Empty }
            };
        });
    }

    private static IEnumerable<Type> GetEventTypes(Assembly assembly, string attributeNamePrefix)
    {
        try
        {
            return assembly.GetTypes().Where(t => IsEventType(t, attributeNamePrefix));
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle missing dependencies gracefully - return only the types that loaded successfully
            var loadedTypes = ex.Types.Where(t => t != null).Cast<Type>();

            return loadedTypes.Where(t => IsEventType(t, attributeNamePrefix));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or TypeLoadException)
        {
            // Return empty collection if we can't load any types due to missing dependencies
            return [];
        }
    }

    private static bool IsEventType(Type type, string attributeNamePrefix)
    {
        // Check for the attribute by name prefix only, to work across assembly load contexts
        return type.GetCustomAttributes()
            .Any(attr => attr.GetType().Name.StartsWith(attributeNamePrefix));
    }

    private static string GetMainDomainName(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? "Unknown";
        var assemblyParts = assemblyName.Split('.');

        return assemblyParts[0];
    }
}
