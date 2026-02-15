// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class ScenarioBasedIntegrationTests
{
    private const string ScenariosPath = "IntegrationTestScenarios";

    private static readonly JsonSerializerOptions JsonOptions = System.Text.Json.JsonSerializerOptions.Web;

    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    public static TheoryData<string> GetTestScenarios()
    {
        var theoryData = new TheoryData<string>();
        var scenariosDir = FindScenariosDirectory();

        if (scenariosDir == null || !Directory.Exists(scenariosDir))
        {
            return theoryData;
        }

        var scenarioFolders = Directory.GetDirectories(scenariosDir)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name);

        foreach (var scenarioName in scenarioFolders)
        {
            theoryData.Add(scenarioName!);
        }

        return theoryData;
    }

    private static string? FindScenariosDirectory()
    {
        // Try AppContext.BaseDirectory first (works when running from output directory)
        var baseDir = AppContext.BaseDirectory;
        var scenariosDir = Path.Combine(baseDir, ScenariosPath);

        if (Directory.Exists(scenariosDir))
            return scenariosDir;

        // Try project directory path (go up from bin/Debug/net10.0)
        var projectDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(baseDir)));

        if (projectDir != null)
        {
            scenariosDir = Path.Combine(projectDir, ScenariosPath);

            if (Directory.Exists(scenariosDir))
                return scenariosDir;
        }

        // Try current directory
        var currentDir = Directory.GetCurrentDirectory();
        scenariosDir = Path.Combine(currentDir, ScenariosPath);

        if (Directory.Exists(scenariosDir))
            return scenariosDir;

        return null;
    }

    [Theory(Skip = "Debug test - only run when debugging path issues")]
    [MemberData(nameof(GetTestScenarios))]
    public async Task ScenarioTest_ShouldGenerateExpectedMarkdown(string scenarioName)
    {
        // Arrange
        var scenariosDir = FindScenariosDirectory();
        scenariosDir.ShouldNotBeNull($"Could not find scenarios directory");

        var scenarioPath = Path.Combine(scenariosDir, scenarioName);

        scenarioPath.ShouldSatisfyAllConditions(
            () => Directory.Exists(scenarioPath).ShouldBeTrue($"Scenario folder '{scenarioName}' should exist"),
            () => File.Exists(Path.Combine(scenarioPath, "input.xml")).ShouldBeTrue($"Scenario '{scenarioName}' should have input.xml"),
            () => Directory.Exists(Path.Combine(scenarioPath, "expected"))
                .ShouldBeTrue($"Scenario '{scenarioName}' should have expected folder")
        );

        var scenario = await LoadTestScenario(scenarioPath);
        var outputDir = Path.Combine(Path.GetTempPath(), $"markdown-test-{Guid.NewGuid()}");

        try
        {
            Directory.CreateDirectory(outputDir);

            // Act
            var results = await ExecuteMarkdownGeneration(scenario, outputDir);

            // Assert
            await ValidateResults(scenario, results, scenarioName);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public void TestScenarioDirectories_WhenPresent_ShouldHaveValidStructure()
    {
        var scenariosDir = FindScenariosDirectory();

        if (scenariosDir == null || !Directory.Exists(scenariosDir))
        {
            // Skip if scenarios directory doesn't exist yet
            return;
        }

        var scenarioFolders = Directory.GetDirectories(scenariosDir);

        foreach (var scenarioFolder in scenarioFolders)
        {
            var scenarioName = Path.GetFileName(scenarioFolder);

            // Validate required files exist
            File.Exists(Path.Combine(scenarioFolder, "input.xml"))
                .ShouldBeTrue($"Scenario '{scenarioName}' must have input.xml");

            Directory.Exists(Path.Combine(scenarioFolder, "expected"))
                .ShouldBeTrue($"Scenario '{scenarioName}' must have expected folder");

            // Validate expected folder has at least one markdown file
            var expectedFiles = Directory.GetFiles(Path.Combine(scenarioFolder, "expected"), "*.md");
            expectedFiles.Length.ShouldBeGreaterThan(0, $"Scenario '{scenarioName}' must have at least one expected .md file");

            // Validate optional config file structure
            var configPath = Path.Combine(scenarioFolder, "config.json");

            if (File.Exists(configPath))
            {
                var configContent = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<TestScenarioConfig>(configContent);
                config.ShouldNotBeNull($"Scenario '{scenarioName}' config.json should be valid JSON");
            }
        }
    }

    private static async Task<TestScenario> LoadTestScenario(string scenarioPath)
    {
        var scenario = new TestScenario
        {
            Name = Path.GetFileName(scenarioPath),
            InputXmlPath = Path.Combine(scenarioPath, "input.xml"),
            ExpectedOutputsPath = Path.Combine(scenarioPath, "expected"),
            AssemblyPath = FindTestAssemblyPath(scenarioPath)
        };

        // Load optional configuration
        var configPath = Path.Combine(scenarioPath, "config.json");

        if (File.Exists(configPath))
        {
            var configJson = await File.ReadAllTextAsync(configPath);
            scenario.Config = JsonSerializer.Deserialize<TestScenarioConfig>(configJson, JsonOptions);
        }

        // Load expected files
        scenario.ExpectedFiles = Directory.GetFiles(scenario.ExpectedOutputsPath, "*.md")
            .Select(path => new ExpectedFile
            {
                FileName = Path.GetFileName(path),
                FilePath = path,
                Content = File.ReadAllText(path),
                IsSchemaFile = path.Contains("/schemas/") || Path.GetFileName(path).StartsWith("schema-")
            })
            .ToList();

        return scenario;
    }

    private static string FindTestAssemblyPath(string scenarioPath)
    {
        // Look for assembly.dll in scenario folder, or use default test assembly
        var localAssemblyPath = Path.Combine(scenarioPath, "assembly.dll");

        if (File.Exists(localAssemblyPath))
        {
            return localAssemblyPath;
        }

        // Fallback to default test assembly - search multiple possible paths
        var possiblePaths = new[]
        {
            // Relative to current working directory (when running from Momentum root)
            Path.Combine(Directory.GetCurrentDirectory(), "tests", "TestEvents", "bin", "Debug", "net10.0", "TestEvents.dll"),

            // Relative to test assembly location (when running from test bin directory)
            Path.Combine(Path.GetDirectoryName(typeof(ScenarioBasedIntegrationTests).Assembly.Location)!,
                "..", "..", "..", "..", "TestEvents", "bin", "Debug", "net10.0", "TestEvents.dll"),

            // Relative to solution root
            Path.Combine(Path.GetDirectoryName(typeof(ScenarioBasedIntegrationTests).Assembly.Location)!,
                "..", "..", "..", "..", "..", "..", "tests", "TestEvents", "bin", "Debug", "net10.0", "TestEvents.dll")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException("Could not find TestEvents.dll. Make sure TestEvents project is built.");
    }

    private static async Task<MarkdownGenerationResults> ExecuteMarkdownGeneration(TestScenario scenario, string outputDir)
    {
        // Initialize services
        var xmlParser = new XmlDocumentationParser();
        var fluidGenerator = new FluidMarkdownGenerator();
        // Load XML documentation
        var xmlLoaded = await xmlParser.LoadMultipleDocumentationAsync([scenario.InputXmlPath], TestContext.Current.CancellationToken);

        xmlLoaded.ShouldBeTrue($"Should be able to load XML from {scenario.InputXmlPath}");

        // Load and discover events
        var assembly = Assembly.LoadFrom(scenario.AssemblyPath);
        var discoveredEvents = AssemblyEventDiscovery.DiscoverEvents(assembly, xmlParser).ToList();

        // Generate event documentation
        var eventsWithDocumentation = discoveredEvents.Select(eventMetadata => new EventWithDocumentation
        {
            Metadata = eventMetadata,
            Documentation = xmlParser.GetEventDocumentation(eventMetadata.EventType)
        }).ToList();

        var results = new MarkdownGenerationResults();

        // Generate individual markdown files
        foreach (var eventWithDoc in eventsWithDocumentation)
        {
            // Test FluidMarkdownGenerator
            var fluidResult = fluidGenerator.GenerateMarkdown(eventWithDoc, outputDir);
            results.FluidResults.Add(fluidResult);

            // Generate schemas for complex types (only if enabled in config)
            if (scenario.Config?.GenerateSchemas == true)
            {
                var complexTypes = eventWithDoc.Metadata.Properties
                    .Where(p => p.IsComplexType && !IsCollectionType(p.PropertyType))
                    .Select(p => p.PropertyType)
                    .Distinct();

                foreach (var complexType in complexTypes)
                {
                    var schemaResult = fluidGenerator.GenerateSchemaMarkdown(complexType, outputDir);
                    results.SchemaResults.Add(schemaResult);
                }
            }
        }

        // Generate sidebar
        var sidebarPath = Path.Combine(outputDir, "sidebar.json");
        await JsonSidebarGenerator.WriteSidebarAsync(eventsWithDocumentation, sidebarPath);
        results.SidebarPath = sidebarPath;

        return results;
    }

    private static async Task ValidateResults(TestScenario scenario, MarkdownGenerationResults results, string scenarioName)
    {
        // Validate that we have expected results
        results.ShouldSatisfyAllConditions(
            () => (results.FluidResults.Count + results.SchemaResults.Count)
                .ShouldBeGreaterThan(0, $"Scenario '{scenarioName}' should generate at least one markdown file"),
            () => scenario.ExpectedFiles.Count.ShouldBeGreaterThan(0, $"Scenario '{scenarioName}' should have expected output files")
        );

        // Group all generated results by filename for easier comparison
        var allGeneratedResults = new Dictionary<string, IndividualMarkdownOutput>();

        foreach (var result in results.FluidResults.Concat(results.SchemaResults))
        {
            var fileName = Path.GetFileName(result.FileName);

            allGeneratedResults.TryAdd(fileName, result);
        }

        // Validate each expected file
        foreach (var expectedFile in scenario.ExpectedFiles)
        {
            await ValidateGeneratedFile(expectedFile, allGeneratedResults, scenarioName);
        }

        // Validate sidebar if expected
        var expectedSidebarPath = Path.Combine(scenario.ExpectedOutputsPath, "sidebar.json");

        if (File.Exists(expectedSidebarPath))
        {
            await ValidateSidebar(expectedSidebarPath, results.SidebarPath, scenarioName);
        }

        // Optional: Validate that we didn't generate unexpected files
        if (scenario.Config?.StrictFileMatching == true)
        {
            var expectedFileNames = scenario.ExpectedFiles.Select(f => f.FileName).ToHashSet();
            var generatedFileNames = allGeneratedResults.Keys.ToHashSet();

            var unexpectedFiles = generatedFileNames.Except(expectedFileNames).ToList();
            unexpectedFiles.ShouldBeEmpty($"Scenario '{scenarioName}' generated unexpected files: {string.Join(", ", unexpectedFiles)}");

            var missingFiles = expectedFileNames.Except(generatedFileNames).ToList();
            missingFiles.ShouldBeEmpty($"Scenario '{scenarioName}' missing expected files: {string.Join(", ", missingFiles)}");
        }
    }

    private static async Task ValidateGeneratedFile(ExpectedFile expectedFile,
        Dictionary<string, IndividualMarkdownOutput> generatedResults, string scenarioName)
    {
        generatedResults.ShouldContainKey(expectedFile.FileName,
            $"Scenario '{scenarioName}' should generate file '{expectedFile.FileName}'");

        var actualResult = generatedResults[expectedFile.FileName];
        var actualContent = await File.ReadAllTextAsync(actualResult.FilePath);

        // Normalize line endings for comparison
        var expectedContent = NormalizeContent(expectedFile.Content);
        var normalizedActualContent = NormalizeContent(actualContent);

        // Compare content with detailed error reporting
        if (expectedContent != normalizedActualContent)
        {
            await WriteComparisonFiles(expectedFile.FileName, expectedContent, normalizedActualContent, scenarioName);

            // Provide detailed comparison
            var differences = FindContentDifferences(expectedContent, normalizedActualContent);
            normalizedActualContent.ShouldBe(expectedContent,
                $"Scenario '{scenarioName}' file '{expectedFile.FileName}' content mismatch.\n" +
                $"Differences found:\n{string.Join("\n", differences.Take(5))}" +
                (differences.Count > 5 ? $"\n... and {differences.Count - 5} more differences" : ""));
        }
    }

    private static async Task ValidateSidebar(string expectedSidebarPath, string actualSidebarPath, string scenarioName)
    {
        File.Exists(actualSidebarPath).ShouldBeTrue($"Scenario '{scenarioName}' should generate sidebar.json");

        var expectedSidebar = await File.ReadAllTextAsync(expectedSidebarPath);
        var actualSidebar = await File.ReadAllTextAsync(actualSidebarPath);

        // Parse as JSON to ensure structure is correct
        var expectedJson = JsonDocument.Parse(expectedSidebar);
        var actualJson = JsonDocument.Parse(actualSidebar);

        // Compare JSON structure (this gives better error messages than string comparison)
        JsonSerializer.Serialize(actualJson, IndentedJsonOptions)
            .ShouldBe(JsonSerializer.Serialize(expectedJson, IndentedJsonOptions),
                $"Scenario '{scenarioName}' sidebar.json structure mismatch");
    }

    private static string NormalizeContent(string content)
    {
        return content
            .Replace("\r\n", "\n") // Normalize line endings
            .Replace("\r", "\n") // Handle Mac line endings
            .Trim(); // Remove leading/trailing whitespace
    }

    private static List<string> FindContentDifferences(string expected, string actual)
    {
        var differences = new List<string>();
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');

        var maxLines = Math.Max(expectedLines.Length, actualLines.Length);

        for (var i = 0; i < maxLines; i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "[END OF FILE]";
            var actualLine = i < actualLines.Length ? actualLines[i] : "[END OF FILE]";

            if (expectedLine != actualLine)
            {
                differences.Add($"Line {i + 1}:");
                differences.Add($"  Expected: {expectedLine}");
                differences.Add($"  Actual:   {actualLine}");
                differences.Add("");
            }
        }

        return differences;
    }

    private static async Task WriteComparisonFiles(string fileName, string expectedContent, string actualContent, string scenarioName)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "markdown-test-comparison", scenarioName);
        Directory.CreateDirectory(tempDir);

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        await File.WriteAllTextAsync(Path.Combine(tempDir, $"{baseName}-expected.md"), expectedContent);
        await File.WriteAllTextAsync(Path.Combine(tempDir, $"{baseName}-actual.md"), actualContent);

        Console.WriteLine($"Comparison files written to: {tempDir}");
    }

    private static bool IsCollectionType(Type type)
    {
        return type.IsArray ||
               (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(List<>) ||
                 type.GetGenericTypeDefinition() == typeof(IList<>) ||
                 type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                 type.GetGenericTypeDefinition() == typeof(ICollection<>)));
    }
}

public class TestScenario
{
    public string Name { get; set; } = string.Empty;
    public string InputXmlPath { get; init; } = string.Empty;
    public string ExpectedOutputsPath { get; init; } = string.Empty;
    public string AssemblyPath { get; init; } = string.Empty;
    public TestScenarioConfig? Config { get; set; }
    public List<ExpectedFile> ExpectedFiles { get; set; } = [];
}

public class ExpectedFile
{
    public string FileName { get; init; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsSchemaFile { get; init; }
}

public class TestScenarioConfig
{
    public bool StrictFileMatching { get; init; } = true;
    public List<string> IgnoreFiles { get; init; } = [];
    public Dictionary<string, string> CustomAssertions { get; init; } = [];
    public bool GenerateSchemas { get; init; } = true;
    public bool GenerateSidebar { get; init; } = true;
}

public class MarkdownGenerationResults
{
    public List<IndividualMarkdownOutput> FluidResults { get; set; } = [];
    public List<IndividualMarkdownOutput> SchemaResults { get; set; } = [];
    public string SidebarPath { get; set; } = string.Empty;
}
