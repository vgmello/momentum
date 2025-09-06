// Copyright (c) ORG_NAME. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PostSetup.Actions;

public class PortConfigurationAction
{
    public class PortConfigurationResult
    {
        public int ProcessedFiles { get; set; }
        public int ChangedFiles { get; set; }
        public int BasePort { get; set; }
        public Dictionary<string, int> PortMappings { get; set; } = [];
    }

    public PortConfigurationResult ConfigurePorts(string projectDir, JsonElement config)
    {
        var basePort = GetBasePortFromConfig(config);
        var portMappings = BuildPortMappings(basePort);

        Console.WriteLine($"Configuring ports with base port: {basePort}");

        var filesToUpdate = FindFilesToUpdate(projectDir);
        var processedFiles = 0;
        var changedFiles = 0;

        foreach (var filePath in filesToUpdate)
        {
            processedFiles++;

            if (UpdatePortsInFile(filePath, portMappings, projectDir))
            {
                changedFiles++;
            }
        }

        return new PortConfigurationResult
        {
            ProcessedFiles = processedFiles,
            ChangedFiles = changedFiles,
            BasePort = basePort,
            PortMappings = portMappings
        };
    }

    private static int GetBasePortFromConfig(JsonElement config)
    {
        if (config.TryGetProperty("basePort", out var basePortElement) && basePortElement.TryGetInt32(out var configPort))
        {
            return configPort;
        }

        return 8100;
    }

    private static Dictionary<string, int> BuildPortMappings(int basePort) =>
        new()
        {
            { "8100", basePort }, // Aspire Resource Service HTTP
            { "8110", basePort + 10 }, // Aspire Resource Service HTTPS
            { "8101", basePort + 1 }, // Main API HTTP
            { "8111", basePort + 11 }, // Main API HTTPS
            { "8102", basePort + 2 }, // Main API gRPC
            { "8103", basePort + 3 }, // BackOffice HTTP
            { "8113", basePort + 13 }, // BackOffice HTTPS
            { "8104", basePort + 4 }, // Orleans HTTP
            { "8114", basePort + 14 }, // Orleans HTTPS
            { "8105", basePort + 5 }, // UI/Frontend HTTP
            { "8115", basePort + 15 }, // UI/Frontend HTTPS
            { "8119", basePort + 19 }, // Documentation
            { "18100", basePort + 10000 }, // Aspire Dashboard HTTP
            { "18110", basePort + 10010 } // Aspire Dashboard HTTPS
        };

    private List<string> FindFilesToUpdate(string projectDir)
    {
        var files = new List<string>();
        var extensions = new[] { ".cs", ".json", ".yml", ".yaml", ".xml", ".config" };

        foreach (var extension in extensions)
        {
            try
            {
                var pattern = "*" + extension;
                var foundFiles = Directory.GetFiles(projectDir, pattern, SearchOption.AllDirectories)
                    .Where(f => !IsExcludedPath(f))
                    .ToArray();
                files.AddRange(foundFiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error searching for {extension} files: {ex.Message}");
            }
        }

        return files;
    }

    private static bool IsExcludedPath(string filePath)
    {
        var excludedDirs = new[] { "bin", "obj", ".git", ".vs", "node_modules", ".local" };

        return excludedDirs.Any(dir => filePath.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}") ||
                                       filePath.Contains($"{Path.DirectorySeparatorChar}{dir}"));
    }

    private static bool UpdatePortsInFile(string filePath, Dictionary<string, int> portMappings, string projectDir)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var originalContent = content;

            foreach (var mapping in portMappings)
            {
                var mappingValueStr = mapping.Value.ToString();
                var patterns = new[]
                {
                    $@"\b{mapping.Key}\b",
                    $@":{mapping.Key}\b",
                    $@"localhost:{mapping.Key}\b",
                    $@"https?://[^:\s]+:{mapping.Key}\b"
                };

                foreach (var pattern in patterns)
                {
                    content = Regex.Replace(content, pattern,
                        match => match.Value.Replace(mapping.Key, mappingValueStr));
                }
            }

            if (content != originalContent)
            {
                File.WriteAllText(filePath, content);
                Console.WriteLine($"  → Updated ports in: {Path.GetRelativePath(projectDir, filePath)}");
                Console.WriteLine();

                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Warning: Could not update ports in {filePath}: {ex.Message}");
        }

        return false;
    }
}
