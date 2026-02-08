// Copyright (c) OrgName. All rights reserved.

using System;
using System.IO;
using System.Text.Json;

namespace PostSetup.Actions;

public static class LocalPackageConfigurationAction
{
    private const string FeedPathFileName = "local-feed-path.txt";

    public class LocalPackageResult
    {
        public bool Enabled { get; set; }
        public string FeedPath { get; set; } = string.Empty;
        public bool NugetConfigCreated { get; set; }
        public string SkipReason { get; set; } = string.Empty;
    }

    public static LocalPackageResult ConfigureLocalPackages(string projectDir, JsonElement config)
    {
        var result = new LocalPackageResult();

        if (!ValidateConfiguration(config, result))
            return result;

        result.Enabled = true;

        var feedPathFilePath = Path.Combine(projectDir, FeedPathFileName);

        if (!File.Exists(feedPathFilePath))
        {
            result.SkipReason = $"Feed path file not found: {FeedPathFileName}. Build the Momentum libraries first (dotnet build libs/Momentum/Momentum.slnx).";
            result.Enabled = false;
            return result;
        }

        var feedPath = File.ReadAllText(feedPathFilePath).Trim();

        if (string.IsNullOrEmpty(feedPath))
        {
            result.SkipReason = $"Feed path file is empty: {FeedPathFileName}";
            result.Enabled = false;
            return result;
        }

        result.FeedPath = feedPath;

        Console.WriteLine($"  Local feed path: {feedPath}");

        CreateNugetConfig(projectDir, feedPath, result);
        CleanupFeedPathFile(feedPathFilePath);

        return result;
    }

    private static bool ValidateConfiguration(JsonElement config, LocalPackageResult result)
    {
        if (!config.TryGetProperty("localPackages", out var localConfig))
        {
            result.SkipReason = "No localPackages configuration found";
            return false;
        }

        if (!localConfig.TryGetProperty("enabled", out var enabledElement) || !enabledElement.GetBoolean())
        {
            result.SkipReason = "Local package configuration is disabled";
            return false;
        }

        return true;
    }

    private static void CreateNugetConfig(string projectDir, string feedPath, LocalPackageResult result)
    {
        var nugetConfigPath = Path.Combine(projectDir, "nuget.config");

        var content = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                <add key="local-momentum" value="{feedPath}" />
              </packageSources>
            </configuration>
            """;

        File.WriteAllText(nugetConfigPath, content);
        result.NugetConfigCreated = true;
        Console.WriteLine($"  → Created nuget.config with local feed: {feedPath}");
    }

    private static void CleanupFeedPathFile(string feedPathFilePath)
    {
        try
        {
            File.Delete(feedPathFilePath);
            Console.WriteLine("  → Cleaned up local feed path file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️  Could not clean up feed path file: {ex.Message}");
        }
    }
}
