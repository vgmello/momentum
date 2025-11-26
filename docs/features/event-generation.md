---
title: Event Documentation Generation System
description: Comprehensive guide to the Momentum event generation system using EventMarkdownGenerator with Liquid templating
date: 2025-01-10
---

# Event Documentation Generation System

The Momentum platform includes an automated event documentation generation system that creates comprehensive markdown documentation for integration and domain events. This system uses the **Momentum.Extensions.EventMarkdownGenerator** tool with Liquid templating to generate structured documentation from .NET assemblies and XML documentation.

## Overview

The event generation system automatically discovers distributed events in your .NET assemblies, extracts their metadata, and generates well-structured markdown documentation. The system supports:

- **Automatic Event Discovery**: Finds events decorated with `EventTopic` attributes
- **Schema Documentation**: Generates documentation for complex types referenced by events
- **Liquid Template Customization**: Fully customizable output using the Liquid templating engine
- **MSBuild Integration**: Automatic generation during build process
- **CLI Support**: Standalone command-line tool for CI/CD and manual generation
- **VitePress Integration**: Structured sidebar generation for documentation sites

## How It Works with EventMarkdownGenerator

### Discovery Process

The EventMarkdownGenerator uses reflection to scan .NET assemblies and discovers events through the following process:

1. **Assembly Loading**: Loads target assemblies with dependency resolution
2. **Event Discovery**: Finds classes/records decorated with `[EventTopic]` attributes
3. **Metadata Extraction**: Extracts event properties, partition keys, and topic information
4. **Documentation Parsing**: Loads XML documentation comments for descriptions and examples
5. **Schema Analysis**: Identifies complex types referenced by events for schema generation
6. **Template Rendering**: Uses Liquid templates to generate markdown output

### Integration in Documentation Workflow

The system integrates seamlessly with the Momentum documentation workflow:

```bash
# Executed as part of pnpm docs:events
tsx .vitepress/scripts/generate-events-docs.ts "../src/AppDomain.BackOffice/bin/Debug/net10.0/AppDomain.BackOffice.dll,../src/AppDomain/bin/Debug/net10.0/AppDomain.dll"
```

This script:
- Locates the pre-built EventMarkdownGenerator tool
- Passes assembly paths to the generator
- Configures GitHub URL linking for source code references
- Outputs documentation to `docs/events/` directory

### Generated Output Structure

The system generates a structured documentation layout:

```
docs/events/
├── AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated.md
├── AppDomain.Invoices.Contracts.IntegrationEvents.InvoiceCreated.md
├── schemas/
│   ├── AppDomain.Cashiers.Contracts.Models.Cashier.md
│   └── AppDomain.Invoices.Contracts.Models.Invoice.md
└── events-sidebar.json
```

## Liquid Templating Capabilities

The EventMarkdownGenerator uses the [Liquid templating engine](https://shopify.github.io/liquid/) to provide complete control over documentation output format.

### Default Templates

The system includes two embedded templates:

- **`event.liquid`**: Template for individual event documentation
- **`schema.liquid`**: Template for complex type schema documentation

### Template Override Mechanism

You can override default templates by:

1. Creating a custom templates directory
2. Adding `event.liquid` and/or `schema.liquid` files
3. Specifying the templates directory using the `--templates` option

```bash
# Install the EventMarkdownGenerator as a global tool
dotnet tool install --global Momentum.Extensions.EventMarkdownGenerator --prerelease

# Use the tool with custom templates
events-docsgen --assemblies "path/to/assembly.dll" \
  --templates "./custom-templates/" \
  --output "./docs/events/"
```

### Available Template Variables

#### Event Template Variables

The `event.liquid` template has access to a comprehensive event model:

| Variable                          | Type    | Description                                              |
| --------------------------------- | ------- | -------------------------------------------------------- |
| `event.EventName`                 | string  | Event class name (e.g., "CashierCreated")                |
| `event.FullTypeName`              | string  | Full type name with namespace                            |
| `event.Namespace`                 | string  | Event namespace                                          |
| `event.TopicName`                 | string  | Kafka topic name pattern                                 |
| `event.Version`                   | string  | Event version (e.g., "v1")                               |
| `event.Status`                    | string  | Event status ("Active" or "Deprecated")                  |
| `event.Entity`                    | string  | Extracted entity name (e.g., "cashier")                  |
| `event.IsInternal`                | boolean | `true` for domain events, `false` for integration events |
| `event.IsObsolete`                | boolean | Whether event is marked with `[Obsolete]`                |
| `event.ObsoleteMessage`           | string  | Deprecation message if obsolete                          |
| `event.Description`               | string  | Event description from XML `<summary>`                   |
| `event.Summary`                   | string  | XML summary content                                      |
| `event.Remarks`                   | string  | XML remarks content                                      |
| `event.Example`                   | string  | XML example content                                      |
| `event.Properties`                | array   | Array of event property objects                          |
| `event.PartitionKeys`             | array   | Array of partition key objects                           |
| `event.TotalEstimatedSizeBytes`   | number  | Total estimated payload size in bytes                    |
| `event.HasInaccurateEstimates`    | boolean | Warning flag for dynamic size properties                 |
| `event.GithubUrl`                 | string  | Link to source code on GitHub                            |
| `event.TopicAttributeDisplayName` | string  | Display name for topic attribute                         |

#### Property Objects (event.Properties)

Each property object in the `event.Properties` array contains:

| Property               | Type    | Description                                 |
| ---------------------- | ------- | ------------------------------------------- |
| `Name`                 | string  | Property name                               |
| `TypeName`             | string  | Property type display name                  |
| `IsRequired`           | boolean | Whether property is required (non-nullable) |
| `IsComplexType`        | boolean | Whether property is a complex object type   |
| `IsCollectionType`     | boolean | Whether property is a collection/array      |
| `Description`          | string  | Property description from XML documentation |
| `SchemaLink`           | string  | Markdown link fragment for complex types    |
| `SchemaPath`           | string  | File path to schema documentation           |
| `ElementTypeName`      | string  | For collections, the element type name      |
| `ElementSchemaPath`    | string  | For collections, path to element schema     |
| `EstimatedSizeBytes`   | number  | Estimated size in bytes                     |
| `IsAccurate`           | boolean | Whether size estimate is accurate           |
| `SizeWarning`          | string  | Warning message for dynamic sizing          |
| `EstimatedSizeDisplay` | string  | Formatted size display with warnings        |

#### Partition Key Objects (event.PartitionKeys)

Each partition key object contains:

| Property      | Type   | Description                                 |
| ------------- | ------ | ------------------------------------------- |
| `Name`        | string | Partition key property name                 |
| `Description` | string | Description from XML documentation          |
| `Order`       | number | Order specified in `PartitionKey` attribute |

#### Schema Template Variables

The `schema.liquid` template has access to schema model:

| Variable             | Type   | Description                             |
| -------------------- | ------ | --------------------------------------- |
| `schema.name`        | string | Schema type name                        |
| `schema.description` | string | Type description from XML documentation |
| `schema.properties`  | array  | Array of schema property objects        |

Schema property objects have the same structure as event property objects listed above.

## Configuration Settings

### Command Line Options

The EventMarkdownGenerator tool supports comprehensive configuration through command-line options:

| Option             | Required | Default               | Description                                          |
| ------------------ | -------- | --------------------- | ---------------------------------------------------- |
| `-a, --assemblies` | Yes      | -                     | Comma-separated list of assembly paths to analyze    |
| `--xml-docs`       | No       | Auto-discovered       | Comma-separated list of XML documentation file paths |
| `-o, --output`     | No       | Current directory     | Output directory for generated markdown files        |
| `--sidebar-file`   | No       | `events-sidebar.json` | Name of the JSON sidebar file                        |
| `--templates`      | No       | Embedded templates    | Custom templates directory path                      |
| `--github-url`     | No       | -                     | Base GitHub URL for source code links                |
| `-v, --verbose`    | No       | `false`               | Enable verbose output for debugging                  |

### MSBuild Integration Properties

When using MSBuild integration, configure generation using these properties:

| Property                | Default                                  | Description                                   |
| ----------------------- | ---------------------------------------- | --------------------------------------------- |
| `EventMarkdownOutput`   | `$(MSBuildProjectDirectory)/docs/events` | Output directory for generated documentation  |
| `EventSidebarFileName`  | `events-sidebar.json`                    | Filename for sidebar navigation JSON          |
| `GenerateEventMarkdown` | `true`                                   | Enable/disable documentation generation       |
| `EventMarkdownVerbose`  | `false`                                  | Enable verbose output during generation       |
| `RepositoryUrl`         | -                                        | GitHub repository URL for source code linking |

### GeneratorOptions Configuration Object

The internal `GeneratorOptions` record supports these properties:

| Property                | Type           | Required | Description                          |
| ----------------------- | -------------- | -------- | ------------------------------------ |
| `AssemblyPaths`         | `List<string>` | Yes      | List of assembly file paths to scan  |
| `XmlDocumentationPaths` | `List<string>` | No       | List of XML documentation file paths |
| `OutputDirectory`       | `string`       | Yes      | Target directory for generated files |
| `SidebarFileName`       | `string`       | Yes      | Sidebar JSON filename                |
| `TemplatesDirectory`    | `string`       | No       | Custom templates directory path      |
| `GitHubBaseUrl`         | `string`       | No       | Base GitHub URL for source linking   |

## Advanced Usage Scenarios

### Multi-Assembly Processing

Generate documentation for multiple assemblies in a single run:

```bash
# Install the tool globally first
dotnet tool install --global Momentum.Extensions.EventMarkdownGenerator --prerelease

# Generate documentation for multiple assemblies
events-docsgen --assemblies "App.Core.dll,App.Contracts.dll,App.Events.dll" \
  --output "./docs/events/" \
  --verbose
```

### GitHub Actions Integration

Integrate event documentation generation into CI/CD pipeline:

```yaml
name: Generate Event Documentation

on:
  push:
    branches: [main]
    paths: ['src/**/*.cs']

jobs:
  generate-docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Build Projects
        run: dotnet build

      - name: Install EventMarkdownGenerator
        run: dotnet tool install --global Momentum.Extensions.EventMarkdownGenerator --prerelease

      - name: Generate Event Documentation
        run: |
          events-docsgen --assemblies "src/App.Core/bin/Debug/net10.0/App.Core.dll" \
            --output "./docs/events/" \
            --github-url "https://github.com/$&#123;&#123; github.repository &#125;&#125;/blob/main/src" \
            --verbose

      - name: Commit Documentation
        run: |
          git config --local user.email "action@github.com"
          git config --local user.name "GitHub Action"
          git add docs/events/
          git diff --staged --quiet || git commit -m "docs: update event documentation"
          git push
```

### Development Workflow Integration

For development workflows, create a script that rebuilds and regenerates documentation:

```bash
#!/bin/bash
# scripts/update-event-docs.sh

echo "Installing/updating EventMarkdownGenerator..."
dotnet tool install --global Momentum.Extensions.EventMarkdownGenerator --prerelease

echo "Building projects..."
dotnet build

echo "Generating event documentation..."
events-docsgen --assemblies "$(find src -name '*.dll' -path '*/bin/Debug/net10.0/*' | tr '\n' ',')" \
  --output "./docs/events/" \
  --github-url "https://github.com/your-org/your-repo/blob/main/src" \
  --verbose

echo "Starting documentation server..."
cd docs && pnpm dev
```

## Troubleshooting and Best Practices

### Event Discovery Issues

**Problem**: Events not being discovered despite having `EventTopic` attributes.

**Solutions**:
- Ensure events implement `IDistributedEvent` interface
- Verify `EventTopic` attribute is properly applied
- Check that assemblies are built and accessible
- Use `--verbose` flag to see discovery process details

### XML Documentation Missing

**Problem**: Generated documentation lacks descriptions.

**Solutions**:
- Enable XML documentation in project files:
  ```xml
  <PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  ```
- Ensure XML documentation files are in assembly directory
- Manually specify XML paths using `--xml-docs` option

### Template Customization Best Practices

1. **Start with Default Templates**: Copy embedded templates as starting point
2. **Use Liquid Filters**: Leverage built-in filters like `upcase`, `downcase`, `default`
3. **Test Template Changes**: Use small test assemblies to validate template modifications
4. **Version Control Templates**: Keep custom templates in source control
5. **Document Template Variables**: Maintain documentation for custom template variables

### Performance Optimization

- **Assembly Filtering**: Only include assemblies that contain events
- **XML Documentation**: Place XML files in same directory as assemblies for faster discovery
- **Template Simplification**: Avoid complex logic in templates; use simple conditionals and loops
- **Batch Processing**: Process multiple assemblies in single run rather than multiple executions

The event generation system provides powerful automation for maintaining up-to-date event documentation, ensuring your integration events are well-documented and easily discoverable by development teams.
