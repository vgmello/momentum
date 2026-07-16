---
title: Event Documentation Generator
description: Generate Markdown reference pages for your integration and domain events straight from compiled assemblies and XML doc comments.
date: 2026-07-15
---

# Event Documentation Generator

`Momentum.Extensions.EventMarkdownGenerator` scans your compiled assemblies for events, cross-references their XML documentation comments, and renders a Markdown page per event (plus one per referenced complex type). It ships both as an MSBuild-integrated build step and as a standalone `events-docsgen` CLI tool.

> **Prerequisites**: Events decorated with `[EventTopic]` (see [Integration Events](./integration-events)) and, ideally, `<GenerateDocumentationFile>true</GenerateDocumentationFile>` set on the project so XML doc comments are available to enrich the output.

## Overview

- **Automatic Event Discovery**: Finds types annotated with `EventTopic`-style attributes via reflection
- **XML Doc Enrichment**: Pulls `<summary>`, `<remarks>`, `<example>`, and `<param>` content into the generated pages
- **Schema Documentation**: Generates a separate page for every complex type referenced by an event payload
- **Payload Size Estimation**: Estimates serialized size per property (JSON or binary) using `[MaxLength]`, `[StringLength]`, `[Range]`, and encoding hints
- **Partition Key Detection**: Surfaces Kafka partition keys and their ordering
- **Sidebar Generation**: Emits a VitePress-shaped sidebar JSON file alongside the Markdown
- **Template Customization**: Output is fully controlled by Liquid templates you can copy and edit
- **MSBuild Integration**: Runs automatically after `dotnet build`, or on demand via the CLI

## How It Works

1. Loads each target assembly in an isolated, collectible `AssemblyLoadContext` (keeps analyzed assemblies from colliding with the generator's own dependencies)
2. Reflects over public types to find event types — matched by **attribute name prefix**, not exact type, so both `EventTopicAttribute` and generic variants like `EventTopicAttribute<TEntity>` are discovered
3. Parses the matching `.xml` documentation file(s), auto-discovered next to each assembly (`{assembly}.xml`) unless overridden with `--xml-docs`
4. Computes an estimated payload size per property and flags properties whose size can't be determined accurately (e.g. dynamic collections)
5. Renders `event.liquid` for each event and `schema.liquid` for each referenced complex type, then writes a sidebar JSON describing the resulting page tree

## Installation

::: code-group

```bash [MSBuild integration]
dotnet add package Momentum.Extensions.EventMarkdownGenerator
```

```bash [Global CLI tool]
dotnet tool install --global Momentum.Extensions.EventMarkdownGenerator
```

:::

### MSBuild Integration (recommended)

Adding the package reference is enough — `Momentum.Extensions.EventMarkdownGenerator.props` is packed into the NuGet package's `build/` folder and is auto-imported by any project that references it. It hooks a target into `AfterTargets="Build"` that invokes the generator against the project's build output (`$(TargetPath)`), and a companion target that cleans generated files `BeforeTargets="Clean"`.

Generation runs with `ContinueOnError="true"`, so a documentation-generation failure never breaks your build.

Configure it with MSBuild properties in your `.csproj`:

```xml
<PropertyGroup>
  <EventMarkdownOutput>$(MSBuildProjectDirectory)/docs/events</EventMarkdownOutput>
  <EventSidebarFileName>events-sidebar.json</EventSidebarFileName>
  <GenerateEventMarkdown>true</GenerateEventMarkdown>
  <EventMarkdownVerbose>false</EventMarkdownVerbose>
</PropertyGroup>
```

| Property                | Description                             | Default                                  |
| ----------------------- | --------------------------------------- | ---------------------------------------- |
| `GenerateEventMarkdown` | Enable/disable generation on build      | `true`                                   |
| `EventMarkdownOutput`   | Output directory for generated Markdown | `$(MSBuildProjectDirectory)/docs/events` |
| `EventSidebarFileName`  | Filename for the sidebar JSON           | `events-sidebar.json`                    |
| `EventMarkdownVerbose`  | Pass `--verbose` to the generator       | `false`                                  |

If the project sets `<GenerateDocumentationFile>true</GenerateDocumentationFile>`, its `.xml` doc file is passed to the generator automatically via `--xml-docs`.

> [!WARNING]
> Setting `<RepositoryUrl>` does **not** automatically enable GitHub source links in the MSBuild-integrated path — the `.props` file does not forward it as `--github-url`. To get source links, invoke the CLI directly with `--github-url` (see [GitHub Source Links](#github-source-links)).

### CLI Tool

Once installed as a global (or local) tool, the executable is available as `events-docsgen`:

```bash
events-docsgen generate --assemblies "path/to/YourService.Contracts.dll" --output "./docs/events/"
```

## Command Reference

`events-docsgen` exposes two subcommands. Both must be specified explicitly — there is no default command.

### `generate`

Generates Markdown documentation from one or more assemblies.

```bash
events-docsgen generate [OPTIONS]
```

| Option                                         | Description                                                                        | Default                               |
| ---------------------------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------- |
| `-a, --assemblies <ASSEMBLIES>` **(required)** | Comma-separated list of assembly paths to analyze                                  | —                                     |
| `--xml-docs <XML_DOCS>`                        | Comma-separated list of XML documentation file paths                               | auto-discovered next to each assembly |
| `-o, --output <OUTPUT>`                        | Output directory for generated Markdown files                                      | `./docs/events/`                      |
| `--sidebar-file <SIDEBAR_FILE>`                | Filename for the sidebar JSON                                                      | `events-sidebar.json`                 |
| `--templates <TEMPLATES>`                      | Custom templates directory (falls back to defaults per file)                       | none                                  |
| `--github-url <GITHUB_URL>`                    | Base GitHub URL for source links, e.g. `https://github.com/org/repo/blob/main/src` | none                                  |
| `--format <FORMAT>`                            | Payload size calculation format: `json` or `binary`                                | `json`                                |
| `--event-attribute <NAME>`                     | Attribute name (or prefix) used to discover events                                 | `EventTopicAttribute`                 |
| `--partition-key-attribute <NAME>`             | Attribute name (or prefix) used to discover partition keys                         | `PartitionKeyAttribute`               |
| `-v, --verbose`                                | Print stack traces on error                                                        | `false`                               |

```bash
# Multiple assemblies, custom output directory
events-docsgen generate \
  --assemblies "YourService.Contracts.dll,YourService.dll" \
  --output "./documentation/events/"

# With GitHub source links and verbose output
events-docsgen generate \
  --assemblies "YourService.Contracts.dll" \
  --github-url "https://github.com/your-org/your-service/blob/main/src" \
  --verbose
```

An assembly that can't be loaded (missing dependency, load timeout after 30s) logs a warning and is skipped rather than failing the whole run. If no events are found across all assemblies, an empty sidebar file is still written.

### `templates`

Copies the default Liquid templates to a local directory so you can customize them.

```bash
events-docsgen templates [OPTIONS]
```

| Option                  | Description                         | Default       |
| ----------------------- | ----------------------------------- | ------------- |
| `-o, --output <OUTPUT>` | Output directory for template files | `./templates` |
| `-f, --force`           | Overwrite existing template files   | `false`       |

```bash
events-docsgen templates --output "./my-templates"
```

If the target directory already contains `.liquid` files, the command refuses to overwrite them unless `--force` is passed.

## What Gets Discovered

- **Events**: any type carrying an attribute whose runtime type name starts with `--event-attribute` (default `EventTopicAttribute`), including generic variants like `EventTopic<Cashier>`
- **Partition keys**: properties (or record constructor parameters) carrying an attribute matching `--partition-key-attribute` (default `PartitionKeyAttribute`); an `Order` property on the attribute controls display order
- **Deprecation**: standard `[Obsolete]` marks the event `Deprecated` and surfaces the obsolete message
- **Required fields**: `[Required]`, or non-nullable reference types via nullability metadata
- **Payload size hints**: `[MaxLength]`, `[StringLength]`, `[Range]`, and a custom `StringEncoding` attribute (checked at property, class, then assembly level)
- **Descriptions**: XML `<summary>` and `<remarks>` for events/schemas, `<example>` for sample payloads, `<param>` for record constructor parameters without a property-level doc comment

## Generated Output

```
docs/events/
├── YourService.Contracts.CashierCreated.md
├── YourService.Contracts.CashierDeleted.md
├── schemas/
│   ├── YourService.Contracts.Cashier.md
│   └── YourService.Contracts.Address.md
└── events-sidebar.json
```

Each event's Markdown filename is its sanitized full type name; schema pages live under `schemas/` using the same convention.

### Event page sections

1. Deprecation callout (if `[Obsolete]`)
2. Metadata: status, version, entity, domain vs. integration event, topic, fully qualified topic, estimated payload size, partition keys
3. `## Description` — from XML `<summary>` and `<remarks>`
4. `### Example` — from XML `<example>`, if present
5. `## Event Payload` — property table (name, type, required, estimated size, description); complex-type properties link to their schema page
6. `### Partition Keys`, if any
7. `### Reference Schemas` — embeds each referenced schema page via a VitePress `<!--@include: -->` directive
8. `## Technical Details` — full type name (linked to GitHub when `--github-url` is set), namespace, and the attribute used to discover the event

### Schema page sections

1. `## Description`
2. `## Schema` — property table, wrapped in a named region so event pages can `@include` just that section
3. `### Reference Schemas` — nested includes for any properties that are themselves complex types

## Customizing Templates

Output is rendered with [Fluid](https://github.com/sebastienros/fluid), a .NET Liquid engine. Copy the defaults, edit them, then point `generate` at the copy:

```bash
events-docsgen templates --output "./my-templates"
# edit ./my-templates/event.liquid and/or schema.liquid
events-docsgen generate --assemblies "YourService.Contracts.dll" --templates "./my-templates/"
```

A custom templates directory only needs to contain the file(s) you want to override — anything missing falls back to the built-in default.

### `event.liquid` variables

| Variable                          | Type    | Description                                                                                                                                            |
| --------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `event.EventName`                 | string  | Documented event name — an `EventName` attribute override if set, else the CLR type name                                                               |
| `event.EventNameKebab`            | string  | Kebab-cased form of `EventName` (same override precedence), for URL-safe/topic-adjacent uses                                                           |
| `event.EventTypeName`             | string  | The event's CLR type name, always as-is (never overridden)                                                                                             |
| `event.FullTypeName`              | string  | Full type name with namespace                                                                                                                          |
| `event.Namespace`                 | string  | Event namespace                                                                                                                                        |
| `event.Topic`                     | string  | Kafka topic name                                                                                                                                       |
| `event.FullyQualifiedTopicName`   | string  | `{domain}.{visibility}.{topic}.{version}`, domain kebab-cased                                                                                          |
| `event.Domain`                    | string  | Resolved domain — explicit attribute `Domain` override, then namespace-derived, then default                                                           |
| `event.Version`                   | string  | Event schema version                                                                                                                                   |
| `event.Status`                    | string  | `Active` / `Deprecated`                                                                                                                                |
| `event.Entity`                    | string  | Kebab-cased entity name — from a generic `EventTopic<T>`, or a stripped event-name suffix (`Created`, `Updated`, ...) when the attribute isn't generic |
| `event.IsInternal`                | boolean | Domain event vs. integration event                                                                                                                     |
| `event.IsObsolete`                | boolean | Whether `[Obsolete]` is present                                                                                                                        |
| `event.ObsoleteMessage`           | string  | Deprecation message, if any                                                                                                                            |
| `event.Description`               | string  | From XML `<summary>`, with a fallback message when absent                                                                                              |
| `event.Summary`                   | string  | Raw XML `<summary>` text                                                                                                                               |
| `event.Remarks`                   | string  | From XML `<remarks>`                                                                                                                                   |
| `event.Example`                   | string  | From XML `<example>`                                                                                                                                   |
| `event.Properties`                | array   | See below                                                                                                                                              |
| `event.PartitionKeys`             | array   | `Name`, `TypeName`, `Description`, `Order` per key                                                                                                     |
| `event.AttributeProperties`       | array   | Every public property of the topic attribute, reflected into `Key`/`Value` pairs                                                                       |
| `event.TotalEstimatedSizeBytes`   | number  | Sum of estimated property sizes                                                                                                                        |
| `event.HasInaccurateEstimates`    | boolean | True when a property's size can't be reliably estimated                                                                                                |
| `event.GithubUrl`                 | string  | Source link, when `--github-url` is configured                                                                                                         |
| `event.TopicAttributeDisplayName` | string  | e.g. `[EventTopic<Cashier>]`                                                                                                                           |

Each item in `event.Properties`:

| Field                                  | Description                                                                       |
| -------------------------------------- | --------------------------------------------------------------------------------- |
| `Name`, `TypeName`                     | Property name and type                                                            |
| `IsRequired`                           | Required flag                                                                     |
| `IsComplexType`, `IsCollectionType`    | Whether the property links to a schema page, and whether it's a collection of one |
| `Description`                          | From XML docs                                                                     |
| `SchemaLink`, `SchemaPath`             | Relative link/path to the property's schema page                                  |
| `ElementTypeName`, `ElementSchemaPath` | Used for collection properties                                                    |
| `EstimatedSizeDisplay`                 | Human-readable size, e.g. `"42 bytes"`                                            |

### `schema.liquid` variables

| Variable             | Type   | Description                                                                                                      |
| -------------------- | ------ | ---------------------------------------------------------------------------------------------------------------- |
| `schema.name`        | string | Type name                                                                                                        |
| `schema.description` | string | From XML `<summary>`                                                                                             |
| `schema.properties`  | array  | `name`, `typeName`, `isRequired`, `isComplexType`, `isCollectionType`, `description`, `schemaLink`, `schemaPath` |

Note the casing difference: `event.*` fields are PascalCase, `schema.*` fields are camelCase — Fluid's `UnsafeMemberAccessStrategy` resolves either, but templates must match the model exactly since Liquid property access is case-sensitive.

```liquid
{% if event.IsObsolete %}
**Deprecated:** {{ event.ObsoleteMessage }}
{% endif %}

{% for property in event.Properties %}
- **{{ property.Name }}**: `{{ property.TypeName }}`
{% endfor %}
```

## GitHub Source Links

Pass `--github-url` on the CLI to have the "Technical Details" section on each event page link back to its source file:

```bash
events-docsgen generate \
  --assemblies "YourService.Contracts.dll" \
  --github-url "https://github.com/your-org/your-service/blob/main/src"
```

This only takes effect via the explicit CLI flag — the MSBuild-integrated build step does not currently forward `<RepositoryUrl>` to it.

## Troubleshooting

**No events found** — the assembly was scanned but nothing matched the event attribute. Confirm the type carries `[EventTopic]` (or your `--event-attribute` override):

```csharp
[EventTopic("user-events")]
public record UserCreated(Guid UserId, string Name);
```

**Missing or empty descriptions** — the generator couldn't find XML docs. Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` on the analyzed project, or pass `--xml-docs` explicitly if the `.xml` file isn't next to the assembly.

**Assembly loading failures** — usually a missing dependency next to the target DLL. Point `--assemblies` at the build output directory (not a copy elsewhere) so sibling dependencies resolve, and prefer absolute paths.

**Custom template errors** — validate Liquid syntax and check that every referenced variable exists in the model tables above; the safest starting point is always the copied default template.

## Next Steps

- [Integration Events](./integration-events) — how `EventTopic` and `PartitionKey` attributes are defined
- [Kafka Configuration](./kafka) — topic naming and broker setup that the generated docs describe
- [DbCommand Pattern](../database/dbcommand) — another source-generation-driven Momentum tool
