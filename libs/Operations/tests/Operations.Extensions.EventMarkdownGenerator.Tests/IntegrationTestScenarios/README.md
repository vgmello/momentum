# Integration Test Scenarios

This directory contains scenario-based integration tests for the EventMarkdownGenerator. Each scenario is a folder containing input XML documentation and expected markdown outputs.

## Scenario Structure

Each test scenario must have the following structure:

```
scenario-name/
├── input.xml           # Input XML documentation file
├── expected/           # Expected output files
│   ├── event-name.md   # Expected event markdown files
│   ├── schemas/        # Expected schema markdown files (optional)
│   │   └── *.md
│   └── sidebar.json    # Expected sidebar JSON (optional)
├── config.json         # Test configuration (optional)
└── assembly.dll        # Custom test assembly (optional, uses default if not provided)
```

## Configuration Options

The `config.json` file supports the following options:

```json
{
  "strictFileMatching": true,        // Enforce exact file name matching
  "ignoreFiles": ["temp.md"],        // Files to ignore during validation
  "customAssertions": {              // Custom validation messages
    "event-name.md": "Should contain specific content"
  },
  "generateSchemas": true,           // Whether to test schema generation
  "generateSidebar": true            // Whether to test sidebar generation
}
```

## Running Tests

The integration tests are automatically discovered by scanning the scenario folders. Each scenario becomes a test case in the `ScenarioBasedIntegrationTests` class.

To run the tests:

```bash
dotnet test --filter "ScenarioBasedIntegrationTests"
```

## Creating New Scenarios

### Manual Creation

1. Create a new folder with a descriptive name
2. Add `input.xml` with the XML documentation
3. Add `expected/` folder with expected markdown outputs
4. Optionally add `config.json` for custom configuration

### Using TestScenarioBuilder

For programmatic creation, use the `TestScenarioBuilder` class:

```csharp
var scenario = new TestScenarioBuilder("my-scenario")
    .WithEvent("My.Events.EventName", "Event description")
    .WithExpectedEventMarkdown("event-name.md", "expected content")
    .WithConfig(generateSchemas: false);

await scenario.BuildAsync();
```

### Using Factory Methods

For common scenarios, use the factory methods:

```csharp
// Basic event
var basicScenario = TestScenarioFactory.CreateBasicEvent(
    "basic-event",
    "Events.UserCreated",
    "User created event",
    new Dictionary<string, string> { {"userId", "User ID"} });

// Complex event with schemas
var complexScenario = TestScenarioFactory.CreateComplexEventWithSchemas(
    "complex-event",
    "Events.OrderCreated", 
    "Order created event",
    "## Details\nOrder processing notes",
    parameters,
    schemas);

// Obsolete event
var obsoleteScenario = TestScenarioFactory.CreateObsoleteEvent(
    "obsolete-event",
    "Events.LegacyEvent",
    "Legacy event",
    "Use NewEvent instead",
    parameters);
```

## Existing Scenarios

### basic-event
Tests simple event generation with basic properties and no schemas.

### complex-event-with-schemas
Tests event generation with complex types, schema generation, and sidebar generation.

### obsolete-event
Tests obsolete event handling with deprecation warnings.

### multiple-partition-keys
Tests events with multiple partition keys and proper ordering.

### minimal-documentation
Tests handling of events with minimal XML documentation (no remarks, no complex structure).

### internal-event
Tests internal event generation with proper topic visibility (internal vs public).

### collection-types
Tests various collection types: arrays, lists, IEnumerable, ICollection, Dictionary.

### nullable-and-enums
Tests nullable reference types, nullable value types, and enum types with proper required/optional field marking.

### nested-namespaces
Tests deep namespace hierarchies and proper domain/subdomain extraction.

### multiple-events-sidebar
Tests sidebar generation with multiple events grouped by subdomain.

### circular-references
Tests circular reference handling in complex types to prevent infinite loops during schema generation.

### missing-documentation
Tests error handling for missing, empty, or malformed XML documentation.

## Validation Features

The test framework validates:

- **Content Matching**: Exact content comparison with detailed diff reporting
- **File Structure**: Correct file names and folder structure
- **Schema Generation**: Complex type schema generation
- **Sidebar Generation**: JSON sidebar structure and content
- **Error Handling**: Proper error messages and troubleshooting info

## Troubleshooting

### Test Failures

When tests fail, the framework:
1. Creates comparison files in temp directory for manual inspection
2. Provides detailed diff output showing exact differences
3. Reports line-by-line comparisons for easier debugging

### Common Issues

1. **Line Ending Differences**: The framework normalizes line endings automatically
2. **Whitespace Differences**: Leading/trailing whitespace is trimmed
3. **File Not Found**: Check that expected files exist and have correct names
4. **Schema Mismatches**: Verify that complex types are properly documented

### Debug Output

Failed tests write comparison files to:
```
/tmp/markdown-test-comparison/{scenario-name}/
├── {filename}-expected.md
└── {filename}-actual.md
```

## Best Practices

1. **Descriptive Names**: Use clear, descriptive scenario names
2. **Minimal Content**: Focus on testing specific features per scenario
3. **Documentation**: Add comments in XML for complex scenarios
4. **Configuration**: Use config.json to customize test behavior
5. **Validation**: Include custom assertions for scenario-specific validation