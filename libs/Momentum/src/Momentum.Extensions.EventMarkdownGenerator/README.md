# Momentum.Extensions.EventMarkdownGenerator

This project provides automatic documentation generation for integration events in .NET applications using C# XML documentation comments. The tool generates markdown documentation for events and their schemas, complete with Kafka topic information, partition keys, and structured metadata.

## Features

-   **Automatic Event Discovery**: Finds events annotated with `EventTopic` attributes through assembly reflection
-   **Markdown Documentation Generation**: Creates comprehensive documentation for each event with metadata, payload details, and schema references
-   **Schema Documentation**: Generates documentation for complex types referenced by events
-   **Sidebar Navigation**: Creates structured sidebar JSON for documentation site integration
-   **Template Customization**: Uses Liquid templating engine for fully customizable output format
-   **MSBuild Integration**: Automatically generates documentation during build process
-   **CLI Tool**: Standalone command-line tool for CI/CD and manual generation
-   **GitHub Integration**: Links to source code when GitHub URL is configured

## Installation

### MSBuild Integration (Recommended)

Add the NuGet package to your project for automatic documentation generation during builds:

```xml
<PackageReference Include="Momentum.Extensions.EventMarkdownGenerator" Version="*" />
```

This will automatically generate event documentation in the `docs/events/` directory after each build.

### Global CLI Tool

Install as a global dotnet tool for manual execution or CI/CD scenarios:

```bash
dotnet tool install --global Momentum.Extensions.EventMarkdownGenerator
```

Use the CLI tool:

```bash
events-docsgen --assemblies "path/to/your.dll" --output "./docs/events/"
```

## Usage

### MSBuild Integration

After adding the NuGet package, documentation is generated automatically during build. Configure generation using MSBuild properties in your `.csproj`:

```xml
<PropertyGroup>
  <EventMarkdownOutput>$(MSBuildProjectDirectory)/docs/events</EventMarkdownOutput>
  <EventSidebarFileName>events-sidebar.json</EventSidebarFileName>
  <GenerateEventMarkdown>true</GenerateEventMarkdown>
</PropertyGroup>
```

### Command Line Interface

The CLI tool provides several options for customizing documentation generation:

```bash
events-docsgen [OPTIONS]
```

**Required Options:**

-   `-a|--assemblies <ASSEMBLIES>`: Comma-separated list of assembly paths to scan for events

**Optional Options:**

-   `--xml-docs <XML_DOCS>`: Comma-separated list of XML documentation file paths (auto-discovered if not provided)
-   `-o|--output <OUTPUT>`: Output directory for generated documentation (default: `./docs/events/`)
-   `--sidebar-file <SIDEBAR_FILE>`: Filename for sidebar navigation JSON (default: `events-sidebar.json`)
-   `--templates <TEMPLATES>`: Directory containing custom Liquid templates
-   `-v|--verbose`: Enable verbose output for debugging

**Examples:**

```bash
# Basic usage
events-docsgen --assemblies "MyApp.dll"

# Multiple assemblies with custom output
events-docsgen --assemblies "MyApp.dll,MyApp.Contracts.dll" --output "./documentation/events/"

# With custom templates and verbose output
events-docsgen --assemblies "MyApp.dll" --templates "./custom-templates/" --verbose
```

## Configuration

### MSBuild Properties

Configure the tool behavior using these MSBuild properties in your `.csproj` file:

| Property                | Description                                  | Default                                  |
| ----------------------- | -------------------------------------------- | ---------------------------------------- |
| `EventMarkdownOutput`   | Output directory for generated documentation | `$(MSBuildProjectDirectory)/docs/events` |
| `EventSidebarFileName`  | Filename for sidebar navigation JSON         | `events-sidebar.json`                    |
| `GenerateEventMarkdown` | Enable/disable documentation generation      | `true`                                   |
| `EventMarkdownVerbose`  | Enable verbose output during generation      | `false`                                  |

### GitHub Source Linking

Configure GitHub URL for source code links in generated documentation:

```xml
<PropertyGroup>
  <RepositoryUrl>https://github.com/your-org/your-repo</RepositoryUrl>
</PropertyGroup>
```

When configured, each event will include a link to its source code on GitHub.

## Sample Output

The tool generates documentation with the following structure:

```
docs/events/
├── Your.Namespace.Events.UserCreated.md
├── Your.Namespace.Events.UserUpdated.md
├── schemas/
│   ├── Your.Namespace.Models.User.md
│   └── Your.Namespace.Models.Address.md
└── events-sidebar.json
```

### Event Documentation Sections

Each generated event documentation includes:

-   **Event Metadata**: Status, version, Kafka topic, entity information
-   **Description**: Extracted from XML documentation comments
-   **Structured Remarks**: Organized sections from XML `<remarks>` tags
-   **Event Payload**: Table showing all properties with types and descriptions
-   **Partition Keys**: Information about Kafka partitioning strategy
-   **Referenced Schemas**: Links to complex type documentation
-   **Technical Details**: Size estimates and deprecation warnings
-   **Source Link**: GitHub link to event definition (when configured)

### Schema Documentation

Complex types referenced by events get their own schema documentation:

-   **Type Information**: Full type name and description
-   **Properties Table**: All properties with types, requirements, and descriptions
-   **Nested Schema Links**: References to other complex types

## Template Customization

The tool uses the Liquid templating engine with embedded default templates. You can customize the output by providing your own templates.

### Template Variables

#### Event Template (event.liquid)

The following variables are available in event templates:

| Variable                                | Type    | Description                           |
| --------------------------------------- | ------- | ------------------------------------- |
| `event.EventName`                       | string  | Event class name                      |
| `event.FullTypeName`                    | string  | Full type name with namespace         |
| `event.Namespace`                       | string  | Event namespace                       |
| `event.TopicName`                       | string  | Kafka topic name                      |
| `event.Version`                         | string  | Event version                         |
| `event.Status`                          | string  | Active/Deprecated status              |
| `event.Entity`                          | string  | Entity name extracted from event type |
| `event.IsInternal`                      | boolean | Whether event is domain or int        |
| `event.IsObsolete`                      | boolean | Whether event is marked obsolete      |
| `event.ObsoleteMessage`                 | string  | Deprecation message if obsolete       |
| `event.Documentation.Description`       | string  | Event description from XML docs       |
| `event.Documentation.StructuredRemarks` | object  | Key-value pairs from XML remarks      |
| `event.Properties`                      | array   | Array of event properties             |
| `event.PartitionKeys`                   | array   | Array of partition key definitions    |
| `event.TotalEstimatedSizeBytes`         | number  | Total estimated payload size          |
| `event.HasInaccurateEstimates`          | boolean | Warning flag for dynamic size         |
| `event.GithubUrl`                       | string  | Link to source code on GitHub         |
| `event.TopicAttributeDisplayName`       | string  | Display name for topic attribute      |

#### Schema Template (schema.liquid)

The following variables are available in schema templates:

| Variable             | Type   | Description                    |
| -------------------- | ------ | ------------------------------ |
| `schema.name`        | string | Type name                      |
| `schema.description` | string | Type description from XML docs |
| `schema.properties`  | array  | Array of type properties       |

**Property Objects** (in `schema.properties` array):

| Property        | Type    | Description                          |
| --------------- | ------- | ------------------------------------ |
| `name`          | string  | Property name                        |
| `typeName`      | string  | Property type                        |
| `isRequired`    | boolean | Whether property is required         |
| `isComplexType` | boolean | Whether property is a complex type   |
| `description`   | string  | Property description from XML docs   |
| `schemaLink`    | string  | Markdown link to complex type schema |
| `schemaPath`    | string  | File path to schema documentation    |

### Custom Template Usage

Override the default templates by creating custom templates and specifying the templates directory:

1. Create a directory for your custom templates
2. Add `event.liquid` and/or `schema.liquid` files
3. Use the `--templates` option to specify your custom templates directory

```bash
events-docsgen --assemblies "MyApp.dll" --templates "./my-templates/"
```

### Liquid Syntax Basics

The tool uses the Liquid templating language. Common syntax patterns:

```liquid
<!-- Output variables -->
{{ event.EventName }}

<!-- Conditional logic -->
{% if event.IsObsolete %}
**Deprecated:** {{ event.ObsoleteMessage }}
{% endif %}

<!-- Loops -->
{% for property in event.Properties %}
- **{{ property.Name }}**: {{ property.TypeName }}
{% endfor %}

<!-- Filters -->
{{ event.EventName | upcase }}
```

## Troubleshooting

### No Events Found

**Problem**: The tool reports "No events found" even though your assembly contains events.

**Solution**: Ensure your events are decorated with `EventTopic` attributes. The tool only discovers events that have this attribute.

```csharp
[EventTopic("user-events")]
public record UserCreated(Guid UserId, string Name);
```

### XML Documentation Not Found

**Problem**: Generated documentation lacks descriptions or shows empty descriptions.

**Solutions**:

-   Enable XML documentation generation in your project: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
-   Manually specify XML documentation paths: `--xml-docs "path/to/MyApp.xml"`
-   Ensure XML documentation files are in the same directory as assemblies

### Assembly Loading Issues

**Problem**: Tool fails to load assemblies with `FileNotFoundException` or similar errors.

**Solutions**:

-   Ensure all dependent assemblies are in the same directory as the target assembly
-   Use absolute paths when specifying assembly locations
-   Check that the assembly was built for a compatible .NET framework

### Template Errors

**Problem**: Custom templates cause parsing or rendering errors.

**Solutions**:

-   Validate Liquid syntax using online Liquid template validators
-   Check that all referenced variables exist in the template context
-   Review default templates as reference for correct syntax and available variables

### Common Error Messages

-   **"Assembly not found"**: Check assembly paths and ensure files exist
-   **"No XML documentation found"**: Enable XML documentation generation or specify paths manually
-   **"Template parsing failed"**: Verify Liquid template syntax is correct
-   **"No events discovered"**: Ensure events have `EventTopic` attributes

## Architecture Notes

The EventMarkdownGenerator tool needs to be in a separate project because:

1. **Tool Distribution**: Can be packaged and distributed as both a NuGet package and global dotnet tool
2. **MSBuild Integration**: Requires specific packaging for MSBuild targets and props files
3. **Dependency Isolation**: Keeps documentation generation dependencies separate from your application
4. **Reflection Requirements**: Needs to load and analyze assemblies at build time or via CLI

This design allows the generator to be reused across multiple projects and provides flexible integration options for different development workflows.
