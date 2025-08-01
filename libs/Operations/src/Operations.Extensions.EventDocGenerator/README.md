# Operations.Extensions.EventDocGenerator

Command-line tool for generating VitePress documentation from integration events defined in .NET assemblies. This tool analyzes your assemblies and their XML documentation to create comprehensive, searchable documentation for your Operations platform integration events.

## Installation

Install as a global tool:
```bash
dotnet tool install -g Operations.Extensions.EventDocGenerator
```

Or add to your project:
```bash
dotnet add package Operations.Extensions.EventDocGenerator
```

## Usage

Once installed, use the `eventdocgen` command:

```bash
eventdocgen --assembly MyApp.dll --output ./docs/events
```

### Command Line Options

- `--assembly` - Path to the assembly containing integration events
- `--output` - Output directory for generated VitePress documentation
- `--xml-docs` - Path to XML documentation file (optional)

## Features

- **VitePress Integration**: Generates documentation compatible with VitePress static site generator
- **XML Documentation**: Extracts rich documentation from XML doc comments
- **Event Discovery**: Automatically discovers integration events in assemblies
- **Structured Output**: Creates organized documentation with proper navigation

## Generated Documentation

The tool generates:
- Individual pages for each integration event
- Index pages with event listings
- VitePress configuration files
- Proper cross-references between events

## Requirements

- .NET 9.0 or later
- Assemblies with XML documentation (recommended)

## Dependencies

- System.CommandLine - CLI framework
- System.Text.Json - JSON processing
- Operations.Extensions.Abstractions - Core abstractions
- Operations.ServiceDefaults - Service configurations

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/vgmello/momentum-sample/blob/main/LICENSE) file for details.

## Contributing

For more information about the Operations platform and contribution guidelines, please visit the [main repository](https://github.com/vgmello/momentum-sample).