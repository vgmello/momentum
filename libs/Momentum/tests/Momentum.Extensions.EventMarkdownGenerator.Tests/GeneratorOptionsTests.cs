// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.EventMarkdownGenerator.Models;
using Shouldly;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class GeneratorOptionsTests
{
    [Fact]
    public void GetSidebarPath_ShouldCombineOutputDirectoryAndFileName()
    {
        // Arrange
        var options = new GeneratorOptions
        {
            OutputDirectory = "/output/events",
            SidebarFileName = "sidebar.json"
        };

        // Act
        var result = options.GetSidebarPath();

        // Assert
        result.ShouldBe(Path.Combine("/output/events", "sidebar.json"));
    }

    [Fact]
    public void GetSidebarPath_WithNestedSidebarPath_ShouldExtractFileName()
    {
        // Arrange
        var options = new GeneratorOptions
        {
            OutputDirectory = "/output/events",
            SidebarFileName = "/some/other/path/sidebar.json"
        };

        // Act
        var result = options.GetSidebarPath();

        // Assert
        result.ShouldBe(Path.Combine("/output/events", "sidebar.json"));
    }

    [Fact]
    public void EnsureOutputDirectoryExists_WhenDirectoryDoesNotExist_ShouldCreateIt()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"momentum-test-{Guid.NewGuid()}");
        var options = new GeneratorOptions
        {
            OutputDirectory = tempDir,
            SidebarFileName = "sidebar.json"
        };

        try
        {
            // Act
            options.EnsureOutputDirectoryExists();

            // Assert
            Directory.Exists(tempDir).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void EnsureOutputDirectoryExists_WhenDirectoryAlreadyExists_ShouldNotThrow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"momentum-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var options = new GeneratorOptions
        {
            OutputDirectory = tempDir,
            SidebarFileName = "sidebar.json"
        };

        try
        {
            // Act & Assert
            Should.NotThrow(() => options.EnsureOutputDirectoryExists());
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void DefaultValues_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/output",
            SidebarFileName = "sidebar.json"
        };

        // Assert
        options.AssemblyPaths.ShouldBeEmpty();
        options.XmlDocumentationPaths.ShouldBeEmpty();
        options.TemplatesDirectory.ShouldBeNull();
        options.GitHubBaseUrl.ShouldBeNull();
        options.SerializationFormat.ShouldBe("json");
    }

    [Fact]
    public void SerializationFormat_CanBeSetToBinary()
    {
        // Arrange & Act
        var options = new GeneratorOptions
        {
            OutputDirectory = "/output",
            SidebarFileName = "sidebar.json",
            SerializationFormat = "binary"
        };

        // Assert
        options.SerializationFormat.ShouldBe("binary");
    }
}
