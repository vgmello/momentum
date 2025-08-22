// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

/// <summary>
///     Helper class for resolving test file paths in a working directory independent manner.
/// </summary>
internal static class TestPathHelper
{
    /// <summary>
    ///     Gets the path to the TestEvents.xml file using assembly location-based path resolution.
    ///     This method searches multiple possible locations to find the XML documentation file.
    /// </summary>
    /// <returns>The full path to the TestEvents.xml file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the TestEvents.xml file cannot be found in any expected location.</exception>
    public static string GetTestEventsXmlPath()
    {
        // Get the location of the current test assembly
        var testAssemblyLocation = Assembly.GetExecutingAssembly().Location;
        var testAssemblyDirectory = Path.GetDirectoryName(testAssemblyLocation)!;

        // Try multiple possible paths for the TestEvents XML file
        var possiblePaths = new[]
        {
            // When running from test project directory
            Path.Combine(testAssemblyDirectory, "..", "TestEvents", "bin", "Debug", "net9.0", "TestEvents.xml"),
            Path.Combine(testAssemblyDirectory, "..", "TestEvents", "bin", "Release", "net9.0", "TestEvents.xml"),

            // When running from solution root
            Path.Combine(testAssemblyDirectory, "..", "..", "..", "TestEvents", "bin", "Debug", "net9.0", "TestEvents.xml"),
            Path.Combine(testAssemblyDirectory, "..", "..", "..", "TestEvents", "bin", "Release", "net9.0", "TestEvents.xml"),

            // When running in CI/CD or different contexts
            Path.Combine(testAssemblyDirectory, "TestEvents.xml"),

            // Search upward for the TestEvents project and its output
            FindTestEventsXmlInParentDirectories(testAssemblyDirectory)
        }.Where(p => !string.IsNullOrEmpty(p)).ToArray();

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException(
            $"Could not find TestEvents.xml in any of the expected locations. Searched paths: {string.Join(", ", possiblePaths.Select(Path.GetFullPath))}");
    }

    private static string FindTestEventsXmlInParentDirectories(string startDirectory)
    {
        var currentDir = startDirectory;

        while (currentDir != null && Path.GetDirectoryName(currentDir) != currentDir)
        {
            var testEventsDir = Path.Combine(currentDir, "TestEvents");

            if (Directory.Exists(testEventsDir))
            {
                var xmlPaths = new[]
                {
                    Path.Combine(testEventsDir, "bin", "Debug", "net9.0", "TestEvents.xml"),
                    Path.Combine(testEventsDir, "bin", "Release", "net9.0", "TestEvents.xml")
                };

                foreach (var xmlPath in xmlPaths)
                {
                    if (File.Exists(xmlPath))
                    {
                        return xmlPath;
                    }
                }
            }

            currentDir = Path.GetDirectoryName(currentDir);
        }

        return string.Empty;
    }
}
