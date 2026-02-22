// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Messaging;
using Momentum.Extensions.EventMarkdownGenerator.Models;
using Momentum.Extensions.EventMarkdownGenerator.Services;
using Shouldly;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class FluidMarkdownGeneratorTests
{
    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"momentum-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);

        return dir;
    }

    private static EventWithDocumentation CreateTestEventWithDocumentation()
    {
        var eventType = typeof(TestEvent);
        var topicAttribute = eventType.GetCustomAttributes(false)
            .First(a => a.GetType().Name.Contains("EventTopic"));

        return new EventWithDocumentation
        {
            Metadata = new EventMetadata
            {
                EventName = "TestEvent",
                FullTypeName = eventType.FullName!,
                Namespace = eventType.Namespace!,
                TopicName = "{env}.test.public.tests.v1",
                Domain = "Tests",
                Version = "v1",
                IsInternal = false,
                EventType = eventType,
                TopicAttribute = (Attribute)topicAttribute,
                Properties =
                [
                    new EventPropertyMetadata
                    {
                        Name = "Id",
                        TypeName = "Guid",
                        PropertyType = typeof(Guid),
                        IsRequired = true,
                        IsComplexType = false,
                        IsPartitionKey = true,
                        EstimatedSizeBytes = 16,
                        IsAccurate = true
                    },
                    new EventPropertyMetadata
                    {
                        Name = "Name",
                        TypeName = "string",
                        PropertyType = typeof(string),
                        IsRequired = true,
                        IsComplexType = false,
                        IsPartitionKey = false,
                        EstimatedSizeBytes = 0,
                        IsAccurate = false,
                        SizeWarning = "Dynamic size - no MaxLength constraint"
                    }
                ],
                PartitionKeys =
                [
                    new PartitionKeyMetadata
                    {
                        Name = "Id",
                        TypeName = "Guid",
                        Order = 0,
                        IsFromParameter = true
                    }
                ]
            },
            Documentation = new EventDocumentation
            {
                Summary = "Test event for unit testing",
                Remarks = "Used in FluidMarkdownGenerator tests",
                PropertyDescriptions = new Dictionary<string, string>
                {
                    ["Id"] = "The unique identifier",
                    ["Name"] = "The name"
                }
            }
        };
    }

    [Fact]
    public async Task CreateAsync_WithNoCustomDirectory_ShouldCreateInstance()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();

        generator.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithCustomDirectory_ShouldUseCustomTemplates()
    {
        var customDir = CreateTempDirectory();

        try
        {
            await FluidMarkdownGenerator.CopyDefaultTemplatesToDirectoryAsync(customDir, TestContext.Current.CancellationToken);

            var generator = await FluidMarkdownGenerator.CreateAsync(customDir);

            generator.ShouldNotBeNull();
        }
        finally
        {
            Directory.Delete(customDir, true);
        }
    }

    [Fact]
    public async Task GenerateMarkdown_ShouldReturnOutputWithContent()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var eventWithDoc = CreateTestEventWithDocumentation();
        var outputDir = CreateTempDirectory();

        try
        {
            var result = generator.GenerateMarkdown(eventWithDoc, outputDir);

            result.ShouldNotBeNull();
            result.Content.ShouldNotBeNullOrEmpty();
            result.FileName.ShouldNotBeNullOrEmpty();
            result.FilePath.ShouldNotBeNullOrEmpty();
            result.Content.ShouldContain("TestEvent");
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task GenerateMarkdown_ShouldContainEventMetadata()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var eventWithDoc = CreateTestEventWithDocumentation();
        var outputDir = CreateTempDirectory();

        try
        {
            var result = generator.GenerateMarkdown(eventWithDoc, outputDir);

            result.Content.ShouldContain("Test event for unit testing");
            result.Content.ShouldContain("{env}.test.public.tests.v1");
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task GenerateAllMarkdown_ShouldReturnOutputsForAllEvents()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var event1 = CreateTestEventWithDocumentation();
        var event2 = CreateTestEventWithDocumentation();
        var outputDir = CreateTempDirectory();

        try
        {
            var results = generator.GenerateAllMarkdown([event1, event2], outputDir).ToList();

            results.Count.ShouldBe(2);
            results.ShouldAllBe(r => !string.IsNullOrEmpty(r.Content));
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task GenerateAllMarkdown_WithEmptyList_ShouldReturnEmpty()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var outputDir = CreateTempDirectory();

        try
        {
            var results = generator.GenerateAllMarkdown([], outputDir).ToList();

            results.Count.ShouldBe(0);
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task CopyDefaultTemplatesToDirectoryAsync_ShouldCopyTemplates()
    {
        var targetDir = CreateTempDirectory();

        try
        {
            await FluidMarkdownGenerator.CopyDefaultTemplatesToDirectoryAsync(targetDir, TestContext.Current.CancellationToken);

            File.Exists(Path.Combine(targetDir, "event.liquid")).ShouldBeTrue();
            File.Exists(Path.Combine(targetDir, "schema.liquid")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(targetDir, true);
        }
    }

    [Fact]
    public async Task CopyDefaultTemplatesToDirectoryAsync_WithNullDirectory_ShouldThrow()
    {
        await Should.ThrowAsync<ArgumentException>(
            () => FluidMarkdownGenerator.CopyDefaultTemplatesToDirectoryAsync(null!));
    }

    [Fact]
    public async Task CopyDefaultTemplatesToDirectoryAsync_WithEmptyDirectory_ShouldThrow()
    {
        await Should.ThrowAsync<ArgumentException>(
            () => FluidMarkdownGenerator.CopyDefaultTemplatesToDirectoryAsync(""));
    }

    [Fact]
    public async Task GenerateSchemaMarkdown_ShouldReturnOutputWithContent()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var outputDir = CreateTempDirectory();

        try
        {
            var result = generator.GenerateSchemaMarkdown(typeof(TestSchemaType), outputDir);

            result.ShouldNotBeNull();
            result.Content.ShouldNotBeNullOrEmpty();
            result.FileName.ShouldNotBeNullOrEmpty();
            result.FilePath.ShouldContain("schemas");
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task GenerateSchemaMarkdown_ShouldContainTypeName()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var outputDir = CreateTempDirectory();

        try
        {
            var result = generator.GenerateSchemaMarkdown(typeof(TestSchemaType), outputDir);

            result.Content.ShouldContain("TestSchemaType");
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task GenerateAllSchemas_ShouldReturnOutputsForAllTypes()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var outputDir = CreateTempDirectory();

        try
        {
            var results = generator.GenerateAllSchemas([typeof(TestSchemaType), typeof(AnotherSchemaType)], outputDir)
                .ToList();

            results.Count.ShouldBe(2);
            results.ShouldAllBe(r => !string.IsNullOrEmpty(r.Content));
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task GenerateAllSchemas_WithEmptyList_ShouldReturnEmpty()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var outputDir = CreateTempDirectory();

        try
        {
            var results = generator.GenerateAllSchemas([], outputDir).ToList();

            results.Count.ShouldBe(0);
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task GenerateMarkdown_WithOptions_ShouldApplyOptions()
    {
        var generator = await FluidMarkdownGenerator.CreateAsync();
        var eventWithDoc = CreateTestEventWithDocumentation();
        var outputDir = CreateTempDirectory();

        try
        {
            var options = new GeneratorOptions
            {
                OutputDirectory = outputDir,
                SidebarFileName = "sidebar.json",
                GitHubBaseUrl = "https://github.com/test/repo"
            };

            var result = generator.GenerateMarkdown(eventWithDoc, outputDir, options);

            result.ShouldNotBeNull();
            result.Content.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public async Task CopyDefaultTemplatesToDirectoryAsync_ShouldCreateDirectoryIfNotExists()
    {
        var parentDir = CreateTempDirectory();
        var targetDir = Path.Combine(parentDir, "nested", "templates");

        try
        {
            await FluidMarkdownGenerator.CopyDefaultTemplatesToDirectoryAsync(targetDir, TestContext.Current.CancellationToken);

            Directory.Exists(targetDir).ShouldBeTrue();
            File.Exists(Path.Combine(targetDir, "event.liquid")).ShouldBeTrue();
            File.Exists(Path.Combine(targetDir, "schema.liquid")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(parentDir, true);
        }
    }

    // Test types
    [EventTopic("{env}.test.public.tests.v1")]
    public record TestEvent(Guid Id, string Name);

    public class TestSchemaType
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public class AnotherSchemaType
    {
        public string Code { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
