# Documentation Layout

This guide explains the structure of the documentation system and how to run it locally for development and contribution.

## Documentation Structure

The documentation is organized in the `/docs` folder with the following structure:

```
docs/
├── .vitepress/           # VitePress configuration
│   ├── config.mts        # Main config with sidebar structure
│   ├── mermaidjs.mjs     # Mermaid diagram integration
│   ├── tocParser.js      # Reference API TOC generator
│   ├── adrParser.js      # ADR table generator
│   └── plugins/          # Custom VitePress plugins
├── arch/                 # Architecture documentation
├── events/               # Event documentation
├── guide/                # User guides
│   ├── bills/            # Bills guide (this section)
│   ├── cashiers/         # Cashiers guide
│   ├── invoices/         # Invoices guide
│   └── getting-started.md
├── reference/            # Auto-generated API reference (from DocFX)
├── index.md              # Documentation homepage
├── package.json          # Node.js dependencies and scripts
├── docfx.json           # DocFX configuration for .NET API docs
└── Dockerfile           # Multi-stage build for docs
```

## Technology Stack

-   **VitePress** (v1.6.3) - Static site generator for main documentation
-   **DocFX** - Microsoft's documentation tool for .NET API reference
-   **Mermaid** (v11.9.0) - Diagram and flowchart rendering
-   **Bun** - JavaScript runtime and package manager
-   **TypeScript** - Configuration and custom plugins

## Running the Documentation

### Option 1: Local Development with Bun (Recommended for Template Changes)

**Prerequisites**: Bun installed

```bash
cd docs
bun install
bun run docs:dev
```

This option:

-   ✅ Allows template and configuration changes
-   ✅ Live reload for all changes
-   ✅ Full development capabilities
-   ⚠️ Requires local Bun setup

### Option 2: Via Aspire (Default)

**Prerequisites**: .NET Aspire and Docker

The documentation runs as part of the Aspire application host:

```bash
# From project root
dotnet run --project src/AppDomain.AppHost
```

This option:

-   ✅ Integrated with full application stack
-   ✅ Works out of the box
-   ❌ Limited to content changes only
-   ❌ No template modifications

### Option 3: Docker (Content Changes Only)

**Prerequisites**: Docker and completed .NET build

**Important**: Run `dotnet build` first to generate API reference docs:

```bash
# From project root - build .NET projects first
dotnet build

# Run documentation container
docker run --rm -it \
  -p 3000:5173 \
  -v "$(pwd):/app" \
  -v /app/docs/node_modules \
  AppDomain-docs
```

This option:

-   ✅ Isolated environment
-   ✅ Good for content-only changes
-   ❌ No template or configuration changes
-   ⚠️ Requires prior .NET build for API docs

## Sidebar Configuration

The sidebar is configured in `/docs/.vitepress/config.mts` with the following structure:

### Main Navigation Sections

```typescript
sidebar: {
  "/arch/": {           // Architecture documentation
    base: "/arch",
    items: [
      {
        text: "Architecture",
        items: [
          { text: "Overview", link: "/" },
          { text: "Event-Driven Architecture", link: "/eda" },
          { text: "Database Design", link: "/database" },
          // ... ADR generation
        ]
      }
    ]
  },
  "/guide/": {          // User guides (this section)
    base: "/guide",
    items: [
      {
        text: "Introduction",
        items: [
          { text: "AppDomain Solution", link: "/" },
          { text: "Getting Started", link: "/getting-started" }
        ]
      },
      {
        text: "Cashiers",
        items: [{ text: "Cashiers Overview", link: "/cashiers/" }]
      },
      {
        text: "Invoices",
        items: [{ text: "Invoices Overview", link: "/invoices/" }]
      },
      {
        text: "Bills",
        items: [
          { text: "Bills Overview", link: "/bills/" },
          { text: "Documentation Layout", link: "/bills/documentation-layout" }
        ]
      },
      {
        text: "Developer Guide",
        items: [
          { text: "Running Database Queries", link: "/dbcommand-usage-guide.md" },
          { text: "Debugging Tips", link: "debugging" }
        ]
      }
    ]
  },
  "/reference": {       // Auto-generated API reference
    base: "/reference",
    items: generateReferenceSidebar()
  }
}
```

### Adding New Pages

To add a new page to any section:

1. Create the markdown file in the appropriate directory
2. Add an entry to the relevant sidebar section in `config.mts`
3. Test locally with `bun run docs:dev`

### Sidebar Features

-   **Collapsible sections** with `collapsed: false/true`
-   **Auto-generated content** for API reference and ADR tables
-   **Base path resolution** for clean URL structure
-   **Hierarchical organization** with nested items

## Development Scripts

Available in `/docs/package.json`:

```json
{
    "docs:dev": "bun run docs:prep && vitepress dev",
    "docs:build": "bun run docs:prep && vitepress build",
    "docs:preview": "vitepress preview",
    "docs:prep": "bun run docs:events && bun run docs:dotnet && bun run docs:adr"
}
```

## API Reference Generation

The API reference documentation is auto-generated from .NET XML documentation comments using DocFX:

1. **Build Phase**: `dotnet build` generates XML documentation
2. **DocFX Phase**: `docfx docfx.json` converts XML to markdown
3. **Integration**: Generated files are placed in `/docs/reference/`
4. **Sidebar**: `generateReferenceSidebar()` creates navigation

**Important**: Always run `dotnet build` before generating documentation to ensure API reference is up to date.

## Contributing to Documentation

1. **Content Changes**: Use any of the three running options above
2. **Template Changes**: Use local Bun development only
3. **Configuration Changes**: Edit `.vitepress/config.mts` and test locally
4. **New Sections**: Follow the existing pattern in sidebar configuration

The documentation system automatically handles:

-   Search indexing
-   Syntax highlighting
-   Mermaid diagram rendering
-   Mobile responsiveness
-   Dark/light theme support
