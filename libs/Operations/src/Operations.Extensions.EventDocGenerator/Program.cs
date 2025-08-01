using System.CommandLine;
using Operations.Extensions.EventDocGenerator.Models;
using Operations.Extensions.EventDocGenerator.Services;

namespace Operations.Extensions.EventDocGenerator;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var assemblyOption = new Option<string>(
            name: "--assembly",
            description: "Path to the assembly to analyze")
        {
            IsRequired = true
        };

        var xmlDocsOption = new Option<string?>(
            name: "--xml-docs",
            description: "Path to the XML documentation file");

        var outputOption = new Option<string>(
            name: "--output",
            description: "Output directory for generated documentation")
        {
            IsRequired = true
        };

        var filenameOption = new Option<string>(
            name: "--filename",
            description: "Name of the output markdown file",
            getDefaultValue: () => "events.md");

        var environmentOption = new Option<string>(
            name: "--environment",
            description: "Environment for topic name generation",
            getDefaultValue: () => "dev");

        var verboseOption = new Option<bool>(
            name: "--verbose",
            description: "Enable verbose output");

        var rootCommand = new RootCommand("Generate VitePress documentation for integration events")
        {
            assemblyOption,
            xmlDocsOption,
            outputOption,
            filenameOption,
            environmentOption,
            verboseOption
        };

        rootCommand.SetHandler(async (assemblyPath, xmlDocsPath, outputDir, filename, environment, verbose) =>
        {
            try
            {
                var options = new GeneratorOptions
                {
                    AssemblyPath = assemblyPath,
                    XmlDocumentationPath = xmlDocsPath,
                    OutputDirectory = outputDir,
                    FileName = filename,
                    Environment = environment,
                    Verbose = verbose
                };

                await GenerateDocumentationAsync(options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (verbose)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                Environment.Exit(1);
            }
        }, assemblyOption, xmlDocsOption, outputOption, filenameOption, environmentOption, verboseOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task GenerateDocumentationAsync(GeneratorOptions options)
    {
        if (options.Verbose)
        {
            Console.WriteLine($"Analyzing assembly: {options.AssemblyPath}");
            Console.WriteLine($"XML documentation: {options.XmlDocumentationPath ?? "Not provided"}");
            Console.WriteLine($"Output directory: {options.OutputDirectory}");
        }

        // Ensure output directory exists
        Directory.CreateDirectory(options.OutputDirectory);

        // Initialize services
        var xmlParser = new XmlDocumentationParser();
        var eventDiscovery = new AssemblyEventDiscovery();
        var generator = new EventDocumentationGenerator(xmlParser, eventDiscovery);

        // Generate documentation
        var documentation = await generator.GenerateAsync(options);

        // Generate output files
        if (options.GenerateMarkdown)
        {
            var markdownGenerator = new MarkdownGenerator();
            var markdownContent = markdownGenerator.Generate(documentation);
            await File.WriteAllTextAsync(options.GetMarkdownPath(), markdownContent);
            
            if (options.Verbose)
            {
                Console.WriteLine($"Generated markdown: {options.GetMarkdownPath()}");
            }
        }

        if (options.GenerateJsonSidebar)
        {
            var sidebarGenerator = new JsonSidebarGenerator();
            var sidebarContent = sidebarGenerator.Generate(documentation, options.FileName);
            await File.WriteAllTextAsync(options.GetJsonSidebarPath(), sidebarContent);
            
            if (options.Verbose)
            {
                Console.WriteLine($"Generated sidebar JSON: {options.GetJsonSidebarPath()}");
            }
        }

        Console.WriteLine($"Documentation generated successfully for {documentation.AllEvents.Count()} events");
    }
}