// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Loader;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Momentum.Extensions.EventMarkdownGenerator;

public sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-a|--assemblies")]
        [Description("Comma-separated list of assembly paths to analyze")]
        public required string Assemblies { get; init; }

        [CommandOption("--xml-docs")]
        [Description("Comma-separated list of XML documentation file paths (auto-discovered if not provided)")]
        public string? XmlDocs { get; init; }

        [CommandOption("-o|--output")]
        [Description("Output directory for generated markdown files")]
        [DefaultValue("./docs/events/")]
        public string Output { get; init; } = "./docs/events/";

        [CommandOption("--sidebar-file")]
        [Description("Name of the JSON sidebar file")]
        [DefaultValue("events-sidebar.json")]
        public string SidebarFile { get; init; } = "events-sidebar.json";

        [CommandOption("--templates")]
        [Description("Custom templates directory to override default templates")]
        public string? Templates { get; init; }

        [CommandOption("--github-url")]
        [Description("Base GitHub URL for source code links (e.g., https://github.com/org/repo/blob/main/src)")]
        public string? GitHubUrl { get; init; }

        [CommandOption("-v|--verbose")]
        [Description("Enable verbose output")]
        public bool Verbose { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Validate required arguments
            if (string.IsNullOrWhiteSpace(settings.Assemblies))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] The --assemblies argument is required. Please specify one or more assembly paths.");
                AnsiConsole.MarkupLine(
                    "Usage: events-docsgen generate --assemblies [yellow]<path1>[/][gray],[/][yellow]<path2>[/]... --output [yellow]<output-path>[/]");

                return 1;
            }

            var assemblyPaths = settings.Assemblies.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            var xmlDocPaths = string.IsNullOrEmpty(settings.XmlDocs)
                ? []
                : settings.XmlDocs.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToList();

            var options = new GeneratorOptions
            {
                AssemblyPaths = assemblyPaths,
                XmlDocumentationPaths = xmlDocPaths,
                OutputDirectory = settings.Output,
                SidebarFileName = settings.SidebarFile,
                TemplatesDirectory = settings.Templates,
                GitHubBaseUrl = settings.GitHubUrl
            };

            await GenerateDocumentationAsync(options, cancellationToken);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");

            if (settings.Verbose)
            {
                AnsiConsole.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return 1;
        }
    }

    private static async Task GenerateDocumentationAsync(GeneratorOptions options, CancellationToken cancellationToken = default)
    {
        if (options.AssemblyPaths.Count == 0)
            throw new ArgumentException("At least one assembly path must be provided", nameof(options));

        // Validate assembly paths
        var missingAssembly = options.AssemblyPaths.FirstOrDefault(path => !File.Exists(path));

        if (missingAssembly is not null)
        {
            throw new FileNotFoundException($"Assembly not found: {missingAssembly}");
        }

        options.EnsureOutputDirectoryExists();

        var xmlParser = new XmlDocumentationParser();
        var markdownGenerator = await FluidMarkdownGenerator.CreateAsync(options.TemplatesDirectory);
        var xmlDocumentationPaths = DiscoverXmlDocumentationFiles(options.AssemblyPaths, options.XmlDocumentationPaths);

        if (xmlDocumentationPaths.Count > 0)
        {
            await xmlParser.LoadMultipleDocumentationAsync(xmlDocumentationPaths, cancellationToken);
        }

        var allEvents = new List<EventWithDocumentation>();
        var processedAssemblies = 0;

        foreach (var assemblyPath in options.AssemblyPaths)
        {
            IsolatedAssemblyLoadContext? loadContext = null;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                var assembly = await Task.Run(
                    () => LoadAssemblyWithDependencyResolution(assemblyPath, out loadContext),
                    cts.Token);

                var events = AssemblyEventDiscovery.DiscoverEvents(assembly, xmlParser);

                foreach (var eventMetadata in events)
                {
                    var documentation = xmlParser.GetEventDocumentation(eventMetadata.EventType);
                    var eventWithDoc = new EventWithDocumentation
                    {
                        Metadata = eventMetadata,
                        Documentation = documentation
                    };

                    allEvents.Add(eventWithDoc);
                }

                processedAssemblies++;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Timed out loading assembly {assemblyPath} (30s limit)");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to process assembly {assemblyPath}: {ex.Message.EscapeMarkup()}");
            }
            finally
            {
                loadContext?.Dispose();
            }
        }

        if (allEvents.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] No distributed events found in the provided assemblies.");
            AnsiConsole.WriteLine($"Processed {processedAssemblies} assemblies but found no types with EventTopic attributes.");

            // Still generate empty sidebar for consistency
            await JsonSidebarGenerator.WriteSidebarAsync([], options.GetSidebarPath(), cancellationToken);
            AnsiConsole.MarkupLine($"[green]✓[/] Generated empty sidebar file: {options.SidebarFileName}");

            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Generate individual markdown files
        var markdownFiles = markdownGenerator.GenerateAllMarkdown(allEvents, options.OutputDirectory, options).ToList();

        // Write markdown files
        await WriteMarkdownFilesAsync(markdownFiles, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Extract and generate schema files
        var schemaTypes = CollectAllSchemaTypes(allEvents);
        var schemaFiles = markdownGenerator.GenerateAllSchemas(schemaTypes, options.OutputDirectory).ToList();

        // Write schema files
        await WriteMarkdownFilesAsync(schemaFiles, cancellationToken);

        // Generate and write sidebar JSON
        await JsonSidebarGenerator.WriteSidebarAsync(allEvents, options.GetSidebarPath(), cancellationToken);

        // Summary
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[green]✓[/] Successfully generated documentation for [bold]{allEvents.Count}[/] events from [bold]{processedAssemblies}[/] assemblies");
        AnsiConsole.MarkupLine($"[green]✓[/] Created [bold]{markdownFiles.Count}[/] markdown files in: {options.OutputDirectory}");

        if (schemaFiles.Count > 0)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Created [bold]{schemaFiles.Count}[/] schema files");
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Generated sidebar file: {options.SidebarFileName}");
    }

    private static async Task WriteMarkdownFilesAsync(List<IndividualMarkdownOutput> files, CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            var directory = Path.GetDirectoryName(file.FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(file.FilePath, file.Content, cancellationToken);
        }
    }

    private static HashSet<Type> CollectAllSchemaTypes(List<EventWithDocumentation> allEvents)
    {
        var schemaTypes = new HashSet<Type>();

        foreach (var eventWithDoc in allEvents)
        {
            var eventSchemaTypes = TypeUtils.CollectComplexTypesFromProperties(eventWithDoc.Metadata.Properties);

            foreach (var schemaType in eventSchemaTypes)
            {
                schemaTypes.Add(schemaType);
                TypeUtils.CollectNestedComplexTypes(schemaType, schemaTypes);
            }
        }

        return schemaTypes;
    }

    private static HashSet<string> DiscoverXmlDocumentationFiles(List<string> assemblyPaths, List<string> explicitXmlPaths)
    {
        var explicitPath = explicitXmlPaths.Where(File.Exists);
        var autoDiscoveredPath = assemblyPaths.Select(GetExpectedXmlDocumentationPath).Where(File.Exists);

        return explicitPath.Concat(autoDiscoveredPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string GetExpectedXmlDocumentationPath(string assemblyPath)
    {
        var directory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyPath);

        return Path.Combine(directory, $"{fileNameWithoutExtension}.xml");
    }


    private static Assembly LoadAssemblyWithDependencyResolution(string assemblyPath, out IsolatedAssemblyLoadContext loadContext)
    {
        loadContext = new IsolatedAssemblyLoadContext(assemblyPath);
        return loadContext.LoadFromAssemblyPath(assemblyPath);
    }

    /// <summary>
    /// Isolated assembly load context to prevent version conflicts between the host app
    /// and the assemblies being analyzed for documentation generation.
    /// </summary>
    private sealed class IsolatedAssemblyLoadContext : AssemblyLoadContext, IDisposable
    {
        private readonly string _assemblyDirectory;
        private readonly AssemblyDependencyResolver _resolver;
        private bool _disposed;

        public IsolatedAssemblyLoadContext(string assemblyPath) : base(isCollectible: true)
        {
            _assemblyDirectory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
            _resolver = new AssemblyDependencyResolver(assemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            var localPath = Path.Combine(_assemblyDirectory, assemblyName.Name + ".dll");
            if (File.Exists(localPath))
            {
                return LoadFromAssemblyPath(localPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return 0;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Unload();
                _disposed = true;
            }
        }
    }
}
