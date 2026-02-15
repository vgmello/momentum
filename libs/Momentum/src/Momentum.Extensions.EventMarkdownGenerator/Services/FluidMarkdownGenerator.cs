// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;
using Fluid;
using Momentum.Extensions.EventMarkdownGenerator.Extensions;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

/// <summary>
///     Generates markdown documentation from event metadata using Fluid (Liquid) templates.
///     Supports custom template directories and generates both event and schema documentation.
/// </summary>
public class FluidMarkdownGenerator
{
    private static readonly FluidParser Parser = new();
    private static readonly TemplateOptions TemplateOptions = new()
    {
        MemberAccessStrategy = new UnsafeMemberAccessStrategy()
    };

    private readonly IFluidTemplate _eventTemplate;
    private readonly IFluidTemplate _schemaTemplate;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FluidMarkdownGenerator"/> class.
    /// </summary>
    /// <param name="customTemplatesDirectory">Optional directory containing custom Liquid templates to override defaults.</param>
    public FluidMarkdownGenerator(string? customTemplatesDirectory = null)
    {
        var eventTemplateSource = GetTemplate("event.liquid", customTemplatesDirectory);
        var schemaTemplateSource = GetTemplate("schema.liquid", customTemplatesDirectory);

        _eventTemplate = Parser.Parse(eventTemplateSource);
        _schemaTemplate = Parser.Parse(schemaTemplateSource);
    }

    private FluidMarkdownGenerator(string eventTemplateSource, string schemaTemplateSource)
    {
        _eventTemplate = Parser.Parse(eventTemplateSource);
        _schemaTemplate = Parser.Parse(schemaTemplateSource);
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="FluidMarkdownGenerator"/> class asynchronously.
    /// </summary>
    /// <param name="customTemplatesDirectory">Optional directory containing custom Liquid templates to override defaults.</param>
    /// <returns>A new <see cref="FluidMarkdownGenerator"/> instance.</returns>
    public static async Task<FluidMarkdownGenerator> CreateAsync(string? customTemplatesDirectory = null)
    {
        var eventTemplateSource = await GetTemplateAsync("event.liquid", customTemplatesDirectory);
        var schemaTemplateSource = await GetTemplateAsync("schema.liquid", customTemplatesDirectory);

        return new FluidMarkdownGenerator(eventTemplateSource, schemaTemplateSource);
    }

    /// <summary>
    ///     Generates markdown content for a single event.
    /// </summary>
    /// <param name="eventWithDoc">The event metadata and documentation to render.</param>
    /// <param name="outputDirectory">The base output directory for generated files.</param>
    /// <param name="options">Optional generator options for customization.</param>
    /// <returns>The generated markdown output containing content and file path.</returns>
    public IndividualMarkdownOutput GenerateMarkdown(EventWithDocumentation eventWithDoc, string outputDirectory,
        GeneratorOptions? options = null)
    {
        var metadata = eventWithDoc.Metadata;
        var documentation = eventWithDoc.Documentation;

        var fileName = metadata.GetFileName();
        var filePath = GenerateFilePath(outputDirectory, fileName);

        var context = new TemplateContext(TemplateOptions);
        var eventModel = EventViewModelFactory.CreateEventModel(metadata, documentation, options);
        context.SetValue("event", eventModel);

        var content = _eventTemplate.Render(context);

        return new IndividualMarkdownOutput
        {
            FileName = fileName,
            Content = content,
            FilePath = filePath
        };
    }

    /// <summary>
    ///     Generates markdown content for multiple events.
    /// </summary>
    /// <param name="events">The events to render.</param>
    /// <param name="outputDirectory">The base output directory for generated files.</param>
    /// <param name="options">Optional generator options for customization.</param>
    /// <returns>An enumerable of generated markdown outputs.</returns>
    public IEnumerable<IndividualMarkdownOutput> GenerateAllMarkdown(IEnumerable<EventWithDocumentation> events, string outputDirectory,
        GeneratorOptions? options = null)
    {
        return events.Select(eventWithDoc => GenerateMarkdown(eventWithDoc, outputDirectory, options));
    }

    /// <summary>
    ///     Generates markdown content for a complex type schema.
    /// </summary>
    /// <param name="schemaType">The type to generate schema documentation for.</param>
    /// <param name="outputDirectory">The base output directory for generated files.</param>
    /// <returns>The generated markdown output containing content and file path.</returns>
    public IndividualMarkdownOutput GenerateSchemaMarkdown(Type schemaType, string outputDirectory)
    {
        var fileName = $"{TypeUtils.GetCleanTypeName(schemaType).ToSafeFileName()}.md";
        var filePath = GenerateFilePath(outputDirectory, fileName, "schemas");

        var context = new TemplateContext(TemplateOptions);
        var schemaModel = EventViewModelFactory.CreateSchemaModel(schemaType);
        context.SetValue("schema", schemaModel);

        var content = _schemaTemplate.Render(context);

        return new IndividualMarkdownOutput
        {
            FileName = fileName,
            Content = content,
            FilePath = filePath
        };
    }

    /// <summary>
    ///     Generates markdown content for multiple complex type schemas.
    /// </summary>
    /// <param name="schemaTypes">The types to generate schema documentation for.</param>
    /// <param name="outputDirectory">The base output directory for generated files.</param>
    /// <returns>An enumerable of generated markdown outputs.</returns>
    public IEnumerable<IndividualMarkdownOutput> GenerateAllSchemas(IEnumerable<Type> schemaTypes, string outputDirectory)
    {
        return schemaTypes.Select(schemaType => GenerateSchemaMarkdown(schemaType, outputDirectory));
    }

    /// <summary>
    ///     Copies the default Liquid templates to a target directory for customization.
    /// </summary>
    /// <param name="targetDirectory">The directory to copy templates to.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when targetDirectory is null or empty.</exception>
    public static async Task CopyDefaultTemplatesToDirectoryAsync(string targetDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(targetDirectory))
        {
            throw new ArgumentException("Target directory cannot be null or empty", nameof(targetDirectory));
        }

        Directory.CreateDirectory(targetDirectory);

        string[] templateNames = ["event.liquid", "schema.liquid"];

        foreach (var templateName in templateNames)
        {
            var templateContent = await GetEmbeddedTemplateAsync(templateName);
            var targetPath = Path.Combine(targetDirectory, templateName);
            await File.WriteAllTextAsync(targetPath, templateContent, cancellationToken);
        }
    }

    private static string GetTemplate(string templateName, string? customTemplatesDirectory)
    {
        try
        {
            // Check if override templates exists
            if (!string.IsNullOrEmpty(customTemplatesDirectory))
            {
                var customTemplatePath = Path.Combine(customTemplatesDirectory, templateName);

                if (File.Exists(customTemplatePath))
                    return File.ReadAllText(customTemplatePath);
            }

            return GetEmbeddedTemplate(templateName);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to load template '{templateName}': {ex.Message}", ex);
        }
    }

    private static async Task<string> GetTemplateAsync(string templateName, string? customTemplatesDirectory)
    {
        try
        {
            if (!string.IsNullOrEmpty(customTemplatesDirectory))
            {
                var customTemplatePath = Path.Combine(customTemplatesDirectory, templateName);

                if (File.Exists(customTemplatePath))
                    return await File.ReadAllTextAsync(customTemplatePath);
            }

            return await GetEmbeddedTemplateAsync(templateName);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to load template '{templateName}': {ex.Message}", ex);
        }
    }

    private static async Task<string> GetEmbeddedTemplateAsync(string templateName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyLocation = assembly.Location;

            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);

                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    var templatePath = Path.Combine(assemblyDir, "Templates", Path.GetFileName(templateName));

                    if (File.Exists(templatePath))
                    {
                        return await File.ReadAllTextAsync(templatePath);
                    }
                }
            }

            var resourceName = $"Momentum.Extensions.EventMarkdownGenerator.Templates.{templateName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new FileNotFoundException(
                    $"Template '{templateName}' not found as content file or embedded resource. Expected location: Templates/{templateName}");
            }

            using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync();
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to load template '{templateName}': {ex.Message}", ex);
        }
    }

    private static string GetEmbeddedTemplate(string templateName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Try to read from filesystem first (as Content files)
            var assemblyLocation = assembly.Location;

            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);

                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    var templatePath = Path.Combine(assemblyDir, "Templates", Path.GetFileName(templateName));

                    if (File.Exists(templatePath))
                    {
                        return File.ReadAllText(templatePath);
                    }
                }
            }

            // Fallback to embedded resource for backward compatibility
            var resourceName = $"Momentum.Extensions.EventMarkdownGenerator.Templates.{templateName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new FileNotFoundException(
                    $"Template '{templateName}' not found as content file or embedded resource. Expected location: Templates/{templateName}");
            }

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to load template '{templateName}': {ex.Message}", ex);
        }
    }

    private static string GenerateFilePath(string outputDirectory, string fileName, string? subdirectory = null)
    {
        var sanitizedFileName = Path.GetFileName(fileName);

        return subdirectory != null
            ? Path.Combine(outputDirectory, subdirectory, sanitizedFileName)
            : Path.Combine(outputDirectory, sanitizedFileName);
    }
}
