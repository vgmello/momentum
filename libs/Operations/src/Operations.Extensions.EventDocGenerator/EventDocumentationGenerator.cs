using System.Reflection;
using Operations.Extensions.EventDocGenerator.Models;
using Operations.Extensions.EventDocGenerator.Services;

namespace Operations.Extensions.EventDocGenerator;

public class EventDocumentationGenerator
{
    private readonly XmlDocumentationParser _xmlParser;
    private readonly AssemblyEventDiscovery _eventDiscovery;

    public EventDocumentationGenerator(XmlDocumentationParser xmlParser, AssemblyEventDiscovery eventDiscovery)
    {
        _xmlParser = xmlParser;
        _eventDiscovery = eventDiscovery;
    }

    public async Task<AssemblyEventDocumentation> GenerateAsync(GeneratorOptions options)
    {
        // Load the assembly
        var assembly = await LoadAssemblyAsync(options.AssemblyPath);
        
        // Load XML documentation if available
        await _xmlParser.LoadDocumentationAsync(options.XmlDocumentationPath);

        // Discover events in the assembly
        var eventMetadata = _eventDiscovery.DiscoverEvents(assembly, options.Environment);

        // Generate documentation for each event
        var eventsWithDocumentation = eventMetadata.Select(metadata => 
            new EventWithDocumentation
            {
                Metadata = metadata,
                Documentation = _xmlParser.GetEventDocumentation(metadata.EventType)
            }).ToList();

        // Separate public and internal events
        var publicEvents = eventsWithDocumentation
            .Where(e => !e.Metadata.IsInternal)
            .OrderBy(e => e.Metadata.EventName)
            .ThenBy(e => e.Metadata.Version)
            .ToList();

        var internalEvents = eventsWithDocumentation
            .Where(e => e.Metadata.IsInternal)
            .OrderBy(e => e.Metadata.EventName)
            .ThenBy(e => e.Metadata.Version)
            .ToList();

        return new AssemblyEventDocumentation
        {
            AssemblyName = assembly.GetName().Name ?? "Unknown",
            AssemblyPath = options.AssemblyPath,
            DefaultDomain = GetDefaultDomain(assembly),
            PublicEvents = publicEvents,
            InternalEvents = internalEvents
        };
    }

    private static async Task<Assembly> LoadAssemblyAsync(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
        }

        // Use LoadFrom to handle dependencies properly
        return Assembly.LoadFrom(assemblyPath);
    }

    private static string GetDefaultDomain(Assembly assembly)
    {
        var domainAttribute = assembly.GetCustomAttribute<Operations.Extensions.Abstractions.Messaging.DefaultDomainAttribute>();
        return domainAttribute?.Domain ?? "unknown";
    }
}