// Copyright (c) Momentum .NET. All rights reserved.

using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class DebugTests
{
    [Fact(Skip = "Debug test - only run when debugging path issues")]
    public void Debug_ShowPaths_ShouldDisplayCurrentPaths()
    {
        var baseDir = AppContext.BaseDirectory;
        var scenariosPath = Path.Combine(baseDir, "IntegrationTestScenarios");
        var currentDir = Directory.GetCurrentDirectory();

        // Let's check if the scenarios exist relative to the project file
        var projectDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(baseDir))); // Go up from bin/Debug/net10.0

        if (projectDir != null)
        {
            var altScenariosPath = Path.Combine(projectDir, "IntegrationTestScenarios");

            if (Directory.Exists(altScenariosPath))
            {
                Assert.True(true, $"Found scenarios at: {altScenariosPath}");

                return;
            }
        }

        // Try current directory
        var currentScenariosPath = Path.Combine(currentDir, "IntegrationTestScenarios");

        if (Directory.Exists(currentScenariosPath))
        {
            Assert.True(true, $"Found scenarios at current dir: {currentScenariosPath}");

            return;
        }

        Assert.Fail(
            $"BaseDir: {baseDir}, Current: {currentDir}, Scenarios: {scenariosPath}, Exists: {Directory.Exists(scenariosPath)}");
    }

    [Fact]
    public void Debug_EventDiscovery_ShouldShowDiscoveredEvents()
    {
        // Test event discovery with the actual TestEvents assembly
        var testEventsPath = FindTestEventsAssembly();
        Assert.True(File.Exists(testEventsPath), $"TestEvents.dll should exist at: {testEventsPath}");

        var assembly = System.Reflection.Assembly.LoadFrom(testEventsPath);
        var discoveredEvents = Services.AssemblyEventDiscovery.DiscoverEvents(assembly, null, Services.PayloadSizeCalculator.Create("json")).ToList();

        Assert.True(discoveredEvents.Count > 0,
            $"Should discover events. Found {discoveredEvents.Count} events. Assembly types: {string.Join(", ", assembly.GetTypes().Select(t => t.FullName))}");

        foreach (var evt in discoveredEvents)
        {
            Console.WriteLine($"Discovered event: {evt.EventName} in namespace {evt.Namespace}");
        }
    }

    private static string FindTestEventsAssembly()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "tests", "TestEvents", "bin", "Debug", "net10.0", "TestEvents.dll"),
            Path.Combine(Path.GetDirectoryName(typeof(DebugTests).Assembly.Location)!, "..", "..", "..", "..", "TestEvents", "bin", "Debug",
                "net10.0", "TestEvents.dll"),
            Path.Combine(Path.GetDirectoryName(typeof(DebugTests).Assembly.Location)!, "..", "..", "..", "..", "..", "..", "tests",
                "TestEvents", "bin", "Debug", "net10.0", "TestEvents.dll")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException("Could not find TestEvents.dll");
    }
}
