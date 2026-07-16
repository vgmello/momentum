# Momentum.Extensions.EventMarkdownGenerator

Generates individual markdown documentation files for distributed events from assemblies and XML documentation.

## Overview

The `Momentum.Extensions.EventMarkdownGenerator` provides automatic documentation generation for integration events in .NET applications using C# XML documentation comments. The tool generates comprehensive markdown documentation for events and their schemas, complete with Kafka topic information, partition keys, and structured metadata.

## Key Features

- **Automatic Event Discovery**: Finds events annotated with `EventTopic` attributes through assembly reflection
- **Markdown Documentation Generation**: Creates comprehensive documentation for each event with metadata, payload details, and schema references
- **Schema Documentation**: Generates documentation for complex types referenced by events
- **Sidebar Navigation**: Creates structured sidebar JSON for documentation site integration
- **Template Customization**: Uses Liquid templating engine for fully customizable output format
- **MSBuild Integration**: Automatically generates documentation during build process
- **CLI Tool**: Standalone command-line tool for CI/CD and manual generation
- **GitHub Integration**: Links to source code when GitHub URL is configured

## Installation

### MSBuild Integration (Recommended)

Add the package to your project using the .NET CLI:

```bash
dotnet add package Momentum.Extensions.EventMarkdownGenerator
```

Or add the package reference directly in your `.csproj`:

```xml
<PackageReference Include="Momentum.Extensions.EventMarkdownGenerator" Version="*" />
```

This automatically generates event documentation in the `docs/events/` directory after each build.

### Global CLI Tool

Install as a global dotnet tool for manual execution or CI/CD scenarios:

```bash
dotnet tool install --global Momentum.Extensions.EventMarkdownGenerator
```

Use the CLI tool:

```bash
events-docsgen generate --assemblies "path/to/your.dll" --output "./docs/events/"
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

The CLI tool provides two commands for documentation generation and template management:

#### Generate Command (Default)

Generate markdown documentation from event assemblies:

```bash
events-docsgen generate [OPTIONS]
# or simply (generate is the default command)
events-docsgen [OPTIONS]
```

**Required Options:**

- `-a|--assemblies <ASSEMBLIES>`: Comma-separated list of assembly paths to scan for events

**Optional Options:**

- `--xml-docs <XML_DOCS>`: Comma-separated list of XML documentation file paths (auto-discovered if not provided)
- `-o|--output <OUTPUT>`: Output directory for generated documentation (default: `./docs/events/`)
- `--sidebar-file <SIDEBAR_FILE>`: Filename for sidebar navigation JSON (default: `events-sidebar.json`)
- `--templates <TEMPLATES>`: Directory containing custom Liquid templates
- `--event-attribute <NAME>`: Name (or name prefix) of the attribute used to discover events (default: `EventTopicAttribute`)
- `--partition-key-attribute <NAME>`: Name (or name prefix) of the attribute used to discover partition keys (default: `PartitionKeyAttribute`)
- `--emit-public-visibility`: Render an explicit `public` visibility segment for public events (e.g. `public.domain.subdomain.topic.v1`).
  Default: `false` (public events omit the segment, e.g. `domain.subdomain.topic.v1`). Internal events always render `internal`
  regardless of this flag.
- `-v|--verbose`: Enable verbose output for debugging

**Examples:**

```bash
# Basic usage
events-docsgen --assemblies "MyApp.dll"

# Multiple assemblies with custom output
events-docsgen generate --assemblies "MyApp.dll,MyApp.Contracts.dll" --output "./documentation/events/"

# With custom templates and verbose output
events-docsgen --assemblies "MyApp.dll" --templates "./custom-templates/" --verbose
```

#### Copy Templates Command

Copy default templates to a local directory for customization:

```bash
events-docsgen copy-templates [OPTIONS]
```

**Options:**

- `-o|--output <OUTPUT>`: Output directory for template files (default: `./templates`)
- `-f|--force`: Overwrite existing template files if they exist

**Examples:**

```bash
# Copy templates to default location
events-docsgen copy-templates

# Copy to custom location
events-docsgen copy-templates --output "./my-templates"

# Overwrite existing templates
events-docsgen copy-templates --force

# Use custom templates for generation
events-docsgen copy-templates --output "./my-templates"
# Edit the templates in ./my-templates/ as needed
events-docsgen generate --assemblies "MyApp.dll" --templates "./my-templates/"
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

- **Event Metadata**: Status, version, Kafka topic, entity information
- **Description**: Extracted from XML documentation comments
- **Structured Remarks**: Organized sections from XML `<remarks>` tags
- **Event Payload**: Table showing all properties with types and descriptions
- **Partition Keys**: Information about Kafka partitioning strategy
- **Referenced Schemas**: Links to complex type documentation
- **Technical Details**: Size estimates and deprecation warnings
- **Source Link**: GitHub link to event definition (when configured)

### Schema Documentation

Complex types referenced by events get their own schema documentation:

- **Type Information**: Full type name and description
- **Properties Table**: All properties with types, requirements, and descriptions
- **Nested Schema Links**: References to other complex types

## Template Customization

The tool uses the Liquid templating engine with default templates included as content files. You can customize the output by providing your own templates.

### Getting Started with Custom Templates

The easiest way to customize templates is to copy the default templates and modify them:

```bash
# Copy default templates to a local directory
events-docsgen copy-templates --output "./my-templates"

# Edit the templates as needed
# Then use them for generation
events-docsgen generate --assemblies "MyApp.dll" --templates "./my-templates/"
```

### Template Variables

#### Event Template (event.liquid)

The following variables are available in event templates, bound to the top-level `event` object:

| Variable                          | Type    | Description                                                                                                                                                                                                                                                                                                                   |
| --------------------------------- | ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `event.EventName`                 | string  | Documented event name — the `EventName` attribute override if set, otherwise the CLR type name                                                                                                                                                                                                                                |
| `event.EventNameKebab`            | string  | Kebab-cased form of `EventName` (same override precedence), for topic-adjacent/URL-safe uses                                                                                                                                                                                                                                  |
| `event.EventTypeName`             | string  | The event's CLR type name, always as-is (never overridden)                                                                                                                                                                                                                                                                    |
| `event.FullTypeName`              | string  | Full type name with namespace                                                                                                                                                                                                                                                                                                 |
| `event.Namespace`                 | string  | Event namespace                                                                                                                                                                                                                                                                                                               |
| `event.Topic`                     | string  | Plain topic / event hub name (e.g. `cashiers`)                                                                                                                                                                                                                                                                                |
| `event.FullyQualifiedTopicName`   | string  | Composed `{visibility}.{domain}.{subdomain}.{topic}.{version}` convention string (domain/subdomain kebab-cased). The `visibility` segment is `internal` for internal events, `public` for public events only when `--emit-public-visibility` is set, otherwise omitted. The `subdomain` segment is omitted when not resolved. |
| `event.Domain`                    | string  | Resolved domain — explicit attribute `Domain` override, else assembly default                                                                                                                                                                                                                                                 |
| `event.Subdomain`                 | string? | Resolved subdomain — explicit attribute `Subdomain` override, else namespace-derived (segment before `Contracts`), else `null`                                                                                                                                                                                                |
| `event.Version`                   | string  | Event version (default `v1`)                                                                                                                                                                                                                                                                                                  |
| `event.Status`                    | string  | `Active` or `Deprecated`                                                                                                                                                                                                                                                                                                      |
| `event.Entity`                    | string  | Kebab-cased entity name — from a generic topic attribute's `TEntity`, or a stripped event-name suffix                                                                                                                                                                                                                         |
| `event.IsInternal`                | boolean | Whether the event is a domain event (internal) vs. an integration event (public)                                                                                                                                                                                                                                              |
| `event.IsObsolete`                | boolean | Whether the event is marked obsolete                                                                                                                                                                                                                                                                                          |
| `event.ObsoleteMessage`           | string? | Deprecation message if obsolete                                                                                                                                                                                                                                                                                               |
| `event.GithubUrl`                 | string  | Link to source code on GitHub (built from `EventTypeName` + `Namespace`; `#` if no base URL configured)                                                                                                                                                                                                                       |
| `event.TopicAttributeDisplayName` | string  | Display name for the topic attribute, e.g. `[EventTopic<Cashier>]`                                                                                                                                                                                                                                                            |
| `event.AttributeProperties`       | array   | Every public property of the topic attribute, reflected into `{ Key, Value }` pairs                                                                                                                                                                                                                                           |
| `event.Description`               | string  | Event description — `Summary` if set, else a fallback message                                                                                                                                                                                                                                                                 |
| `event.Summary`                   | string  | Summary from the XML doc `<summary>` tag                                                                                                                                                                                                                                                                                      |
| `event.Remarks`                   | string? | Content from the XML doc `<remarks>` tag                                                                                                                                                                                                                                                                                      |
| `event.Example`                   | string? | Content from the XML doc `<example>` tag                                                                                                                                                                                                                                                                                      |
| `event.Properties`                | array   | Array of event properties (shape below)                                                                                                                                                                                                                                                                                       |
| `event.PartitionKeys`             | array   | Array of partition key definitions (shape below), ordered by `Order`                                                                                                                                                                                                                                                          |
| `event.TotalEstimatedSizeBytes`   | number  | Total estimated payload size                                                                                                                                                                                                                                                                                                  |
| `event.HasInaccurateEstimates`    | boolean | Warning flag — true if any property's size estimate is dynamic (no fixed constraint)                                                                                                                                                                                                                                          |

**Property Objects** (in `event.Properties` array):

| Property               | Type    | Description                                                           |
| ---------------------- | ------- | --------------------------------------------------------------------- |
| `Name`                 | string  | Property name                                                         |
| `TypeName`             | string  | Friendly type name (e.g. `string`, `Guid`, `List<Cashier>`)           |
| `IsRequired`           | boolean | Whether the property is required                                      |
| `IsComplexType`        | boolean | Whether the property is a complex (non-primitive) type                |
| `IsCollectionType`     | boolean | Whether the property is a collection type                             |
| `Description`          | string  | Property description from XML docs (whitespace-collapsed to one line) |
| `SchemaLink`           | string? | Markdown link path to the complex type's schema, if applicable        |
| `SchemaPath`           | string? | File path to the schema documentation, if applicable                  |
| `ElementTypeName`      | string? | Element type name, for collection properties                          |
| `ElementSchemaPath`    | string? | Schema path for the collection's element type, if complex             |
| `EstimatedSizeBytes`   | number  | Estimated payload size in bytes                                       |
| `IsAccurate`           | boolean | Whether the size estimate is a fixed/accurate value                   |
| `SizeWarning`          | string? | Explanation when the estimate is dynamic (e.g. no `MaxLength`)        |
| `EstimatedSizeDisplay` | string  | Human-readable size, e.g. `16 bytes` or `0 bytes (dynamic)`           |

**Partition Key Objects** (in `event.PartitionKeys` array):

| Property      | Type   | Description                             |
| ------------- | ------ | --------------------------------------- |
| `Name`        | string | Property name used as the partition key |
| `TypeName`    | string | Friendly type name                      |
| `Description` | string | Description from XML docs               |
| `Order`       | number | Ordering among multiple partition keys  |

**Attribute Property Objects** (in `event.AttributeProperties` array):

| Property | Type   | Description                         |
| -------- | ------ | ----------------------------------- |
| `Key`    | string | The topic attribute's property name |
| `Value`  | string | The property's stringified value    |

#### Schema Template (schema.liquid)

The following variables are available in schema templates:

| Variable             | Type   | Description                    |
| -------------------- | ------ | ------------------------------ |
| `schema.name`        | string | Type name                      |
| `schema.fullName`    | string | Full type name with namespace  |
| `schema.description` | string | Type description from XML docs |
| `schema.properties`  | array  | Array of type properties       |

**Property Objects** (in `schema.properties` array):

| Property           | Type    | Description                           |
| ------------------ | ------- | ------------------------------------- |
| `name`             | string  | Property name                         |
| `typeName`         | string  | Property type                         |
| `isRequired`       | boolean | Whether property is required          |
| `isComplexType`    | boolean | Whether property is a complex type    |
| `isCollectionType` | boolean | Whether property is a collection type |
| `description`      | string  | Property description from XML docs    |
| `schemaLink`       | string  | Markdown link to complex type schema  |
| `schemaPath`       | string  | File path to schema documentation     |

### Custom Template Usage

Override the default templates by creating custom templates and specifying the templates directory:

**Method 1: Copy and Modify Default Templates (Recommended)**

```bash
# Copy default templates
events-docsgen copy-templates --output "./my-templates"
# Modify the templates as needed
# Use them for generation
events-docsgen generate --assemblies "MyApp.dll" --templates "./my-templates/"
```

**Method 2: Create Templates from Scratch**

1. Create a directory for your custom templates
2. Add `event.liquid` and/or `schema.liquid` files
3. Use the `--templates` option to specify your custom templates directory

```bash
events-docsgen generate --assemblies "MyApp.dll" --templates "./my-templates/"
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

- Enable XML documentation generation in your project: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- Manually specify XML documentation paths: `--xml-docs "path/to/MyApp.xml"`
- Ensure XML documentation files are in the same directory as assemblies

### Assembly Loading Issues

**Problem**: Tool fails to load assemblies with `FileNotFoundException` or similar errors.

**Solutions**:

- Ensure all dependent assemblies are in the same directory as the target assembly
- Use absolute paths when specifying assembly locations
- Check that the assembly was built for a compatible .NET framework

### Template Errors

**Problem**: Custom templates cause parsing or rendering errors.

**Solutions**:

- Validate Liquid syntax using online Liquid template validators
- Check that all referenced variables exist in the template context
- Review default templates as reference for correct syntax and available variables

### Common Error Messages

- **"Assembly not found"**: Check assembly paths and ensure files exist
- **"No XML documentation found"**: Enable XML documentation generation or specify paths manually
- **"Template parsing failed"**: Verify Liquid template syntax is correct
- **"No events discovered"**: Ensure events have `EventTopic` attributes

## Architecture Notes

The EventMarkdownGenerator tool needs to be in a separate project because:

1. **Tool Distribution**: Can be packaged and distributed as both a NuGet package and global dotnet tool
2. **MSBuild Integration**: Requires specific packaging for MSBuild targets and props files
3. **Dependency Isolation**: Keeps documentation generation dependencies separate from your application
4. **Reflection Requirements**: Needs to load and analyze assemblies at build time or via CLI

This design allows the generator to be reused across multiple projects and provides flexible integration options for different development workflows.
