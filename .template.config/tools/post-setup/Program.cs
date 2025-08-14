using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PostSetup;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("Running Momentum .NET post-setup tasks...");

            var projectDir = Environment.CurrentDirectory;
            Console.WriteLine($"Project directory: {projectDir}");

            var basePort = GetBasePortFromArgs(args);
            Console.WriteLine($"Configuring ports with base port: {basePort}");

            ConfigurePorts(projectDir, basePort);

            Console.WriteLine("Momentum .NET post-setup completed successfully!");
            Console.WriteLine();
            Console.WriteLine("Port Configuration:");
            Console.WriteLine($"- Aspire Dashboard: {basePort + 10000} (HTTP) / {basePort + 10010} (HTTPS)");
            Console.WriteLine($"- Aspire Resource Service: {basePort} (HTTP) / {basePort + 10} (HTTPS)");
            Console.WriteLine($"- Main API: {basePort + 1} (HTTP) / {basePort + 11} (HTTPS) / {basePort + 2} (gRPC)");
            Console.WriteLine($"- BackOffice: {basePort + 3} (HTTP) / {basePort + 13} (HTTPS)");
            Console.WriteLine($"- Orleans: {basePort + 4} (HTTP) / {basePort + 14} (HTTPS)");
            Console.WriteLine($"- UI/Frontend: {basePort + 5} (HTTP) / {basePort + 15} (HTTPS)");
            Console.WriteLine($"- Documentation: {basePort + 19}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("1. Run 'dotnet build' to verify the solution builds correctly");
            Console.WriteLine("2. For Aspire projects, run 'dotnet run --project src/{YourProjectName}.AppHost'");

            CleanupPostSetupTools(projectDir);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Post-setup failed: {ex.Message}");
            return 1;
        }
    }

    private static int GetBasePortFromArgs(string[] args)
    {
        // Check command line arguments first
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--basePort" && int.TryParse(args[i + 1], out var port))
            {
                return port;
            }
        }

        // Try to read from config file (created by template with substituted values)
        try
        {
            var configFile = Path.Combine(Environment.CurrentDirectory, ".local", "tools", "post-setup-config.json");
            if (File.Exists(configFile))
            {
                var configContent = File.ReadAllText(configFile);
                var configJson = System.Text.Json.JsonDocument.Parse(configContent);
                if (configJson.RootElement.TryGetProperty("basePort", out var basePortElement) &&
                    basePortElement.TryGetInt32(out var configPort))
                {
                    return configPort;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not read post-setup config: {ex.Message}");
        }

        return 8100;
    }

    private static void ConfigurePorts(string projectDir, int basePort)
    {
        Console.WriteLine("Configuring ports in project files...");

        var portMappings = new Dictionary<string, int>
        {
            {"8100", basePort},                 // Aspire Resource Service HTTP
            {"8110", basePort + 10},            // Aspire Resource Service HTTPS
            {"8101", basePort + 1},             // Main API HTTP
            {"8111", basePort + 11},            // Main API HTTPS
            {"8102", basePort + 2},             // Main API gRPC
            {"8103", basePort + 3},             // BackOffice HTTP
            {"8113", basePort + 13},            // BackOffice HTTPS
            {"8104", basePort + 4},             // Orleans HTTP
            {"8114", basePort + 14},            // Orleans HTTPS
            {"8105", basePort + 5},             // UI/Frontend HTTP
            {"8115", basePort + 15},            // UI/Frontend HTTPS
            {"8119", basePort + 19},            // Documentation
            {"18100", basePort + 10000},        // Aspire Dashboard HTTP
            {"18110", basePort + 10010}         // Aspire Dashboard HTTPS
        };

        var filesToUpdate = FindFilesToUpdate(projectDir);

        foreach (var filePath in filesToUpdate)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var originalContent = content;

                foreach (var mapping in portMappings)
                {
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
                            match => match.Value.Replace(mapping.Key.ToString(), mapping.Value.ToString()));
                    }
                }

                if (content != originalContent)
                {
                    File.WriteAllText(filePath, content);
                    Console.WriteLine($"Updated ports in: {Path.GetRelativePath(projectDir, filePath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not update ports in {filePath}: {ex.Message}");
            }
        }
    }

    private static List<string> FindFilesToUpdate(string projectDir)
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

    private static void CleanupPostSetupTools(string projectDir)
    {
        try
        {
            var localDir = Path.Combine(projectDir, ".local");
            var toolsDir = Path.Combine(localDir, "tools");
            var postSetupDir = Path.Combine(toolsDir, "post-setup");

            if (Directory.Exists(postSetupDir))
            {
                Console.WriteLine("Cleaning up post-setup tools...");

                System.Threading.Thread.Sleep(100);

                Directory.Delete(postSetupDir, recursive: true);
                
                // Delete config file if it exists
                var configFile = Path.Combine(toolsDir, "post-setup-config.json");
                if (File.Exists(configFile))
                {
                    File.Delete(configFile);
                }

                if (Directory.Exists(toolsDir) && !Directory.EnumerateFileSystemEntries(toolsDir).Any())
                {
                    Directory.Delete(toolsDir);
                }

                if (Directory.Exists(localDir) && !Directory.EnumerateFileSystemEntries(localDir).Any())
                {
                    Directory.Delete(localDir);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not remove post-setup tools: {ex.Message}");
            Console.WriteLine("You can safely delete the '.local/tools' directory manually.");
        }
    }
}
