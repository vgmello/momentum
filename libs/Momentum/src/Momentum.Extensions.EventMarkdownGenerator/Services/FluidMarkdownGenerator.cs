// Copyright (c) ABCDEG. All rights reserved.

using System.Reflection;
using Fluid;
using Momentum.Extensions.EventMarkdownGenerator.Extensions;
using Momentum.Extensions.EventMarkdownGenerator.Models;

namespace Momentum.Extensions.EventMarkdownGenerator.Services;

public class FluidMarkdownGenerator
{
    private readonly IFluidTemplate _eventTemplate;
    private readonly IFluidTemplate _schemaTemplate;

    public FluidMarkdownGenerator(string? customTemplatesDirectory = null)
    {
        var parser = new FluidParser();

        var eventTemplateSource = GetTemplate("event.liquid", customTemplatesDirectory);
        var schemaTemplateSource = GetTemplate("schema.liquid", customTemplatesDirectory);

        _eventTemplate = parser.Parse(eventTemplateSource);
        _schemaTemplate = parser.Parse(schemaTemplateSource);
    }

    public IndividualMarkdownOutput GenerateMarkdown(EventWithDocumentation eventWithDoc, string outputDirectory,
        GeneratorOptions? options = null)
    {
        var metadata = eventWithDoc.Metadata;
        var documentation = eventWithDoc.Documentation;

        var fileName = metadata.GetFileName();
        var filePath = GenerateFilePath(outputDirectory, fileName);

        var context = CreateTemplateContext();
        var eventModel = EventViewModelFactory.CreateEventModel(metadata, documentation, options);
        context.SetValue("event", eventModel);

        var content = _eventTemplate.Render(context);

        WriteMarkdownFile(filePath, content);

        return new IndividualMarkdownOutput
        {
            FileName = fileName,
            Content = content,
            FilePath = filePath
        };
    }

    public IEnumerable<IndividualMarkdownOutput> GenerateAllMarkdown(IEnumerable<EventWithDocumentation> events, string outputDirectory,
        GeneratorOptions? options = null)
    {
        return events.Select(eventWithDoc => GenerateMarkdown(eventWithDoc, outputDirectory, options));
    }

    public IndividualMarkdownOutput GenerateSchemaMarkdown(Type schemaType, string outputDirectory)
    {
        var fileName = $"{(schemaType.FullName ?? "UnknownType").ToSafeFileName()}.md";
        var filePath = GenerateFilePath(outputDirectory, fileName, "schemas");

        var context = CreateTemplateContext();
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

    public IEnumerable<IndividualMarkdownOutput> GenerateAllSchemas(IEnumerable<Type> schemaTypes, string outputDirectory)
    {
        return schemaTypes.Select(schemaType => GenerateSchemaMarkdown(schemaType, outputDirectory));
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

    private static string GetEmbeddedTemplate(string templateName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Momentum.Extensions.EventMarkdownGenerator.Templates.{templateName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                // Fallback to reading from filesystem during development
                var assemblyLocation = assembly.Location;

                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    throw new InvalidOperationException($"Cannot determine assembly location for template '{templateName}'.");
                }

                var assemblyDir = Path.GetDirectoryName(assemblyLocation);

                if (string.IsNullOrEmpty(assemblyDir))
                {
                    throw new InvalidOperationException($"Cannot determine assembly directory for template '{templateName}'.");
                }

                var templatePath = Path.Combine(assemblyDir, "Templates", Path.GetFileName(templateName));

                if (File.Exists(templatePath))
                {
                    return File.ReadAllText(templatePath);
                }

                throw new FileNotFoundException(
                    $"Template '{templateName}' not found as embedded resource or file. Searched in: {resourceName} and {templatePath}");
            }

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to load template '{templateName}': {ex.Message}", ex);
        }
    }

    private static TemplateContext CreateTemplateContext()
    {
        return new TemplateContext
        {
            Options =
            {
                // Using UnsafeMemberAccessStrategy for now - TODO: Configure SafeMemberAccessStrategy with explicit type registration
                MemberAccessStrategy = new UnsafeMemberAccessStrategy()
            }
        };
    }

    private static string GenerateFilePath(string outputDirectory, string fileName, string? subdirectory = null)
    {
        var sanitizedFileName = Path.GetFileName(fileName);

        return subdirectory != null
            ? Path.Combine(outputDirectory, subdirectory, sanitizedFileName)
            : Path.Combine(outputDirectory, sanitizedFileName);
    }

    private static void WriteMarkdownFile(string filePath, string content)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);
    }
}
