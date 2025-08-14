using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PostSetup;

public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("Running Momentum .NET post-setup tasks...");
            
            // Get the target project directory (where template was instantiated)
            var projectDir = Environment.CurrentDirectory;
            Console.WriteLine($"Project directory: {projectDir}");
            
            // Parse the base port from template parameters (default 8100)
            var basePort = GetBasePortFromArgs(args);
            Console.WriteLine($"Configuring ports with base port: {basePort}");
            
            // Post-setup tasks
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
            
            // Clean up - remove the tools directory after successful setup
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
        // Look for base port in template parameters
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--basePort" && int.TryParse(args[i + 1], out var port))
            {
                return port;
            }
        }
        
        // Default base port
        return 8100;
    }
    
    private static void ConfigurePorts(string projectDir, int basePort)
    {
        Console.WriteLine("Configuring ports in project files...");
        
        // Define port mappings based on the pattern
        var portMappings = new Dictionary<string, int>
        {
            // Default placeholders to actual ports
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
        
        // Find all relevant files to update
        var filesToUpdate = FindFilesToUpdate(projectDir);
        
        foreach (var filePath in filesToUpdate)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var originalContent = content;
                
                // Replace each port mapping
                foreach (var mapping in portMappings)
                {
                    // Use regex to replace port numbers in various contexts
                    var patterns = new[]
                    {
                        $@"\b{mapping.Key}\b",                    // Exact port number
                        $@":{mapping.Key}\b",                     // Port after colon
                        $@"localhost:{mapping.Key}\b",            // localhost:port
                        $@"https?://[^:\s]+:{mapping.Key}\b"      // Full URL with port
                    };
                    
                    foreach (var pattern in patterns)
                    {
                        content = Regex.Replace(content, pattern, 
                            match => match.Value.Replace(mapping.Key.ToString(), mapping.Value.ToString()));
                    }
                }
                
                // Only write if content changed
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
        
        // Search recursively for relevant files
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
            
            if (Directory.Exists(toolsDir))
            {
                Console.WriteLine("Cleaning up post-setup tools...");
                
                // Give a small delay to ensure any file handles are released
                System.Threading.Thread.Sleep(100);
                
                // Remove the tools directory
                Directory.Delete(toolsDir, recursive: true);
                Console.WriteLine("Post-setup tools removed.");
                
                // If .local directory is now empty, remove it too
                if (Directory.Exists(localDir) && !Directory.EnumerateFileSystemEntries(localDir).Any())
                {
                    Directory.Delete(localDir);
                    Console.WriteLine(".local directory removed (was empty).");
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