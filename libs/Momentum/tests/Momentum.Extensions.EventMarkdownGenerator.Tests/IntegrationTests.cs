// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class IntegrationTests
{
    private static string TestAssemblyPath => GetTestEventsAssemblyPath();
    private static string TestXmlPath => TestPathHelper.GetTestEventsXmlPath();
    private static string ReferenceMarkdownPath => FindReferenceMarkdownPath();

    private static string GetTestEventsAssemblyPath()
    {
        var xmlPath = TestPathHelper.GetTestEventsXmlPath();

        return Path.ChangeExtension(xmlPath, ".dll");
    }

    private static string FindReferenceMarkdownPath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "docs", "events", "cashier-created.md"),
            Path.Combine(Path.GetDirectoryName(typeof(IntegrationTests).Assembly.Location)!,
                "..", "..", "..", "..", "..", "..", "..", "docs", "events", "cashier-created.md")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);

            if (File.Exists(fullPath))
                return fullPath;
        }

        // Return a dummy path if reference doesn't exist
        return Path.Combine(Path.GetTempPath(), "dummy-cashier-created.md");
    }

    [Fact]
    public async Task GenerateMarkdown_ShouldMatchReferenceFormat()
    {
        // Arrange
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        try
        {
            // Initialize services
            var xmlParser = new XmlDocumentationParser();
            var markdownGenerator = await FluidMarkdownGenerator.CreateAsync();

            // Load XML documentation
            await xmlParser.LoadMultipleDocumentationAsync([TestXmlPath], TestContext.Current.CancellationToken);

            // Load and discover events
            var assembly = Assembly.LoadFrom(TestAssemblyPath);
            var events = AssemblyEventDiscovery.DiscoverEvents(assembly, xmlParser, PayloadSizeCalculator.Create("json")).ToList();

            events.Count.ShouldBeGreaterThan(0);
            var cashierCreatedEvent = events.FirstOrDefault(e => e.Metadata.EventTypeName == "CashierCreated");
            cashierCreatedEvent.ShouldNotBeNull();

            var generatedMarkdown = markdownGenerator.GenerateMarkdown(cashierCreatedEvent, outputDir);

            // Act - Use generated content directly (GenerateMarkdown returns content without writing to disk)
            var generatedContent = generatedMarkdown.Content;

            // Assert - Compare with reference (if exists)
            if (File.Exists(ReferenceMarkdownPath))
            {
                var referenceContent = await File.ReadAllTextAsync(ReferenceMarkdownPath, TestContext.Current.CancellationToken);

                // Compare key sections. Status is rendered as a badge next to the title, not its own
                // section, so it's covered by the title comparison rather than a separate one.
                CompareMarkdownSection(generatedContent, referenceContent, "# CashierCreated", "Title should match");
                CompareMarkdownSection(generatedContent, referenceContent, "**Topic:**", "Topic should match");
                CompareMarkdownSection(generatedContent, referenceContent, "**Type:**", "Type should match");
            }

            // Validate specific content regardless of reference
            ValidateGeneratedContent(generatedContent);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public void EventDiscovery_ShouldDetectPartitionKeys()
    {
        // Arrange
        var assembly = Assembly.LoadFrom(TestAssemblyPath);

        // Act
        var xmlParser = new XmlDocumentationParser();
        var events = AssemblyEventDiscovery.DiscoverEvents(assembly, xmlParser, PayloadSizeCalculator.Create("json")).Select(e => e.Metadata).ToList();

        // Assert
        events.Count.ShouldBeGreaterThan(0);
        var cashierEvent = events.FirstOrDefault(e => e.EventTypeName == "CashierCreated");
        cashierEvent.ShouldNotBeNull();

        cashierEvent.PartitionKeys.Count.ShouldBe(2);
        cashierEvent.PartitionKeys.ShouldContain(pk => pk.Name == "TenantId");
        cashierEvent.PartitionKeys.ShouldContain(pk => pk.Name == "PartitionKeyTest");

        // Validate partition key ordering
        var orderedKeys = cashierEvent.PartitionKeys.OrderBy(pk => pk.Order).ToList();
        orderedKeys[0].Name.ShouldBe("TenantId");
        orderedKeys[1].Name.ShouldBe("PartitionKeyTest");
    }

    [Fact]
    public void EventDiscovery_ShouldDetectSubdomainFromNamespace()
    {
        // Arrange
        var assembly = Assembly.LoadFrom(TestAssemblyPath);

        // Act
        var xmlParser = new XmlDocumentationParser();
        var events = AssemblyEventDiscovery.DiscoverEvents(assembly, xmlParser, PayloadSizeCalculator.Create("json")).Select(e => e.Metadata).ToList();

        // Assert
        events.Count.ShouldBeGreaterThan(0);
        var cashierEvent = events.FirstOrDefault(e => e.EventTypeName == "CashierCreated");
        cashierEvent.ShouldNotBeNull();
        cashierEvent.Domain.ShouldBe("TestEvents"); // Assembly-level default domain
        cashierEvent.Subdomain.ShouldBe("Cashiers"); // Namespace-convention subdomain
    }

    [Fact]
    public async Task XmlDocumentationParser_ShouldParsePropertyDescriptions()
    {
        // Arrange
        var parser = new XmlDocumentationParser();
        await parser.LoadMultipleDocumentationAsync([TestXmlPath], TestContext.Current.CancellationToken);

        var assembly = Assembly.LoadFrom(TestAssemblyPath);
        var cashierCreatedType = assembly.GetType("TestEvents.AppDomain.Cashiers.Contracts.IntegrationEvents.CashierCreated");

        // Act
        var documentation = parser.GetEventDocumentation(cashierCreatedType!);

        // Assert
        documentation.Summary.ShouldContain("Published when a new cashier is successfully created");

        // Verify property descriptions
        documentation.PropertyDescriptions.ShouldContainKey("TenantId");
        documentation.PropertyDescriptions["TenantId"].ShouldBe("Identifier of the tenant that owns the cashier");

        documentation.PropertyDescriptions.ShouldContainKey("PartitionKeyTest");
        documentation.PropertyDescriptions["PartitionKeyTest"].ShouldBe("Additional partition key for message routing");

        documentation.PropertyDescriptions.ShouldContainKey("Cashier");
        documentation.PropertyDescriptions["Cashier"].ShouldBe("Complete cashier object containing all cashier data and configuration");
    }

    private static void CompareMarkdownSection(string generated, string reference, string sectionMarker, string message)
    {
        var generatedLine = FindLineContaining(generated, sectionMarker);
        var referenceLine = FindLineContaining(reference, sectionMarker);

        if (generatedLine != null && referenceLine != null)
        {
            generatedLine.ShouldBe(referenceLine, message);
        }
    }

    private static string? FindLineContaining(string content, string marker)
    {
        return content.Split('\n')
            .FirstOrDefault(line => line.Contains(marker))?.Trim();
    }

    [Fact]
    public void JsonSidebarGenerator_ShouldGenerateCorrectStructure()
    {
        // Arrange
        var assembly = Assembly.LoadFrom(TestAssemblyPath);
        var xmlParser = new XmlDocumentationParser();
        var events = AssemblyEventDiscovery.DiscoverEvents(assembly, xmlParser, PayloadSizeCalculator.Create("json")).ToList();
        var eventsWithDoc = events.Select(e => e with { Documentation = new EventDocumentation { Summary = "Test" } }).ToList();

        // Act
        var sidebarItems = JsonSidebarGenerator.GenerateSidebarItems(eventsWithDoc);

        // Assert
        var cashierCreatedEvent = events.FirstOrDefault(e => e.Metadata.EventTypeName == "CashierCreated");
        sidebarItems.Count.ShouldBe(7); // Multiple domain sections + Schemas section

        // Validate AppDomain section exists (contains CashierCreated)
        var appDomainSection = sidebarItems.FirstOrDefault(s => s.Text == "AppDomain");
        appDomainSection.ShouldNotBeNull();
        appDomainSection.Items.Count.ShouldBe(7);

        // Find the Cashiers subsection within AppDomain section
        var cashiersSubsection = appDomainSection.Items.FirstOrDefault(i => i.Text == "Cashiers");
        cashiersSubsection.ShouldNotBeNull();
        cashiersSubsection.Items.ShouldNotBeNull();
        cashiersSubsection.Items.Count.ShouldBe(1);

        // Check that CashierCreated is in the Cashiers subsection
        var cashierCreatedItem = cashiersSubsection.Items[0];
        cashierCreatedItem.Text.ShouldBe("Cashier Created");
        cashierCreatedItem.Link.ShouldBe($"/integration_events/{cashierCreatedEvent!.Metadata.FullTypeName}");

        // Validate schemas section exists
        var schemasSection = sidebarItems.FirstOrDefault(s => s.Text == "Schemas");
        schemasSection.ShouldNotBeNull();
        schemasSection.Items.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateMarkdown_WhenEntityPropertyPresent_LinksEntityToGeneratedSchema()
    {
        // CashierCreated is [EventTopic<Cashier>] AND carries a `Cashier Cashier` property, so the
        // entity type is both reflectable (generic arg) and present in the payload — the two
        // conditions EventViewModelFactory needs to link Entity to a schema page.
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        try
        {
            var xmlParser = new XmlDocumentationParser();
            var markdownGenerator = await FluidMarkdownGenerator.CreateAsync();

            await xmlParser.LoadMultipleDocumentationAsync([TestXmlPath], TestContext.Current.CancellationToken);

            var assembly = Assembly.LoadFrom(TestAssemblyPath);
            var events = AssemblyEventDiscovery.DiscoverEvents(assembly, xmlParser, PayloadSizeCalculator.Create("json")).ToList();

            var cashierCreatedEvent = events.First(e => e.Metadata.EventTypeName == "CashierCreated");
            var entityType = cashierCreatedEvent.Metadata.EntityType;
            entityType.ShouldNotBeNull();

            // Mirrors GenerateCommand.CollectAllSchemaTypes: schema types come from the event's own
            // complex-type properties, not from EntityType directly.
            var schemaTypes = TypeUtils.CollectComplexTypesFromProperties(cashierCreatedEvent.Metadata.Properties);
            schemaTypes.ShouldContain(entityType);

            var generatedMarkdown = markdownGenerator.GenerateMarkdown(cashierCreatedEvent, outputDir, schemaTypes: schemaTypes);

            generatedMarkdown.Content.ShouldContain($"**Entity:** [Cashier](/events/schemas/{entityType.FullName}.md)");
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    private static void ValidateGeneratedContent(string content)
    {
        // Validate basic structure
        content.ShouldContain("# CashierCreated");
        content.ShouldContain("<Badge type=\"tip\" text=\"Active\" />");
        content.ShouldContain("**Version:**");
        content.ShouldContain("**Topic:**");
        content.ShouldContain("**Type:** Integration Event");

        // Validate sections
        content.ShouldContain("## Description");
        content.ShouldContain("## Event Payload");
        content.ShouldContain("### Partition Keys");
        content.ShouldContain("### Reference Schemas");
        content.ShouldContain("## Technical Details");

        // Validate partition keys are mentioned
        content.ShouldContain("TenantId");
        content.ShouldContain("PartitionKeyTest");

        // Validate frontmatter
        content.ShouldContain("---\neditLink: false\n---");

        // Validate topic format
        // Public events omit the visibility segment by default (emitPublicVisibility defaults to false).
        content.ShouldContain("**Topic:** `Cashiers`");
        content.ShouldContain("**Fully Qualified Topic:** `test-events.cashiers.cashiers.v1`");

        // Validate entity field (PascalCase, not linked since Cashier has no generated schema in this test)
        content.ShouldContain("**Entity:** `Cashier`");

        // Validate domain detection
        content.ShouldContain("AppDomain.Cashiers.Contracts.IntegrationEvents");
    }
}
