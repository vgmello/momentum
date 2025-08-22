// Copyright (c) ORG_NAME. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using PostSetup.Actions;

try
{
    Console.WriteLine("🚀 Running Momentum .NET post-setup tasks...");
    Console.WriteLine();

    var projectDir = Environment.CurrentDirectory;
    Console.WriteLine($"📁 Project directory: {projectDir}");

    var config = LoadConfig(projectDir);

    config.TryGetProperty("projectName", out var projectNameProp);
    var domainName = projectNameProp.GetString() ?? "{Domain}";

    // Action 1: Port Configuration
    Console.WriteLine();
    Console.WriteLine("🔧 Action 1: Port Configuration");
    Console.WriteLine("═══════════════════════════════");

    var portAction = new PortConfigurationAction();
    var portResult = portAction.ConfigurePorts(projectDir, config, args);

    Console.WriteLine(portResult.ChangedFiles > 0
        ? $"✅ Port configuration completed: {portResult.ChangedFiles}/{portResult.ProcessedFiles} files updated"
        : "ℹ️  No port configuration changes required");

    // Action 2: Library Rename
    Console.WriteLine();
    Console.WriteLine("📚 Action 2: Library Rename");
    Console.WriteLine("═══════════════════════════════");

    var libraryResult = LibraryRenameAction.ProcessMomentumLibImport(projectDir, config);

    if (!libraryResult.Enabled)
    {
        Console.WriteLine($"⏭️  Skipped: {libraryResult.SkipReason}");
    }
    else if (libraryResult.ChangedFiles > 0)
    {
        Console.WriteLine($"✅ Library rename completed: {libraryResult.ChangedFiles}/{libraryResult.ProcessedFiles} files updated");
        Console.WriteLine(
            $"   → Renamed {libraryResult.ImportedTokens.Count} library tokens to use '{libraryResult.LibPrefix}' prefix");
    }
    else
    {
        Console.WriteLine("ℹ️  Library rename enabled but no changes required");
    }

    // Final summary
    Console.WriteLine();
    Console.WriteLine("🎉 Post-setup Actions Summary");
    Console.WriteLine("═══════════════════════════════");

    var totalFilesChanged = portResult.ChangedFiles + libraryResult.ChangedFiles;
    var totalFilesProcessed = portResult.ProcessedFiles + libraryResult.ProcessedFiles;

    Console.WriteLine($"Total files processed: {totalFilesProcessed}");
    Console.WriteLine($"Total files changed: {totalFilesChanged}");

    if (portResult.ChangedFiles > 0)
    {
        Console.WriteLine();
        Console.WriteLine("🌐 Port Configuration:");
        Console.WriteLine($"   Base port: {portResult.BasePort}");
        Console.WriteLine($"   - Aspire Dashboard: {portResult.BasePort + 10000} (HTTP) / {portResult.BasePort + 10010} (HTTPS)");
        Console.WriteLine($"   - Aspire Resource Service: {portResult.BasePort} (HTTP) / {portResult.BasePort + 10} (HTTPS)");
        Console.WriteLine(
            $"   - Main API: {portResult.BasePort + 1} (HTTP) / {portResult.BasePort + 11} (HTTPS) / {portResult.BasePort + 2} (gRPC)");
        Console.WriteLine($"   - BackOffice: {portResult.BasePort + 3} (HTTP) / {portResult.BasePort + 13} (HTTPS)");
        Console.WriteLine($"   - Orleans: {portResult.BasePort + 4} (HTTP) / {portResult.BasePort + 14} (HTTPS)");
        Console.WriteLine($"   - UI/Frontend: {portResult.BasePort + 5} (HTTP) / {portResult.BasePort + 15} (HTTPS)");
        Console.WriteLine($"   - Documentation: {portResult.BasePort + 19}");
    }

    if (libraryResult is { Enabled: true, ImportedTokens.Count: > 0 })
    {
        Console.WriteLine();
        Console.WriteLine($"📦 Library Configuration:");
        Console.WriteLine($"   Prefix: {libraryResult.LibPrefix}");
        Console.WriteLine($"   Imported libraries: {string.Join(", ", libraryResult.ImportedTokens)}");
    }

    Console.WriteLine();
    Console.WriteLine("📋 Next steps:");
    Console.WriteLine("   1. Run 'dotnet build' to verify the solution builds correctly");
    Console.WriteLine($"   2. For Aspire projects, run 'dotnet run --project src/{domainName}.AppHost'");

    CleanupPostSetupTools(projectDir);

    Console.WriteLine();
    Console.WriteLine("✨ All post-setup actions completed successfully!");

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"❌ Post-setup failed: {ex.Message}");

    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
    }

    return 1;
}

static JsonElement LoadConfig(string projectDir)
{
    var configFile = Path.Combine(projectDir, ".local", "tools", "post-setup-config.json");

    if (!File.Exists(configFile))
    {
        throw new InvalidOperationException($"Post-setup config file not found: {configFile}");
    }

    try
    {
        var configContent = File.ReadAllText(configFile);
        var configDoc = JsonDocument.Parse(configContent);

        return configDoc.RootElement;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to parse post-setup config: {ex.Message}", ex);
    }
}

static void CleanupPostSetupTools(string projectDir)
{
    try
    {
        var localDir = Path.Combine(projectDir, ".local");
        var toolsDir = Path.Combine(localDir, "tools");
        var postSetupDir = Path.Combine(toolsDir, "post-setup");

        if (Directory.Exists(postSetupDir))
        {
            Console.WriteLine();
            Console.WriteLine("🧹 Cleaning up post-setup tools...");

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
        Console.WriteLine($"⚠️  Warning: Could not remove post-setup tools: {ex.Message}");
        Console.WriteLine("   You can safely delete the '.local/tools' directory manually.");
    }
}
