// Copyright (c) OrgName. All rights reserved.

using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PostSetup.Actions;

public static class LocalPackageConfigurationAction
{
    public class LocalPackageResult
    {
        public bool Enabled { get; set; }
        public string Version { get; set; } = string.Empty;
        public string FeedPath { get; set; } = string.Empty;
        public bool VersionUpdated { get; set; }
        public bool NugetConfigCreated { get; set; }
        public string SkipReason { get; set; } = string.Empty;
    }

    public static LocalPackageResult ConfigureLocalPackages(string projectDir, JsonElement config)
    {
        var result = new LocalPackageResult();

        if (!ValidateConfiguration(config, result))
            return result;

        var (versionFileName, feedPathFileName) = ExtractConfigurationData(config);

        result.Enabled = true;

        var versionFilePath = Path.Combine(projectDir, versionFileName);
        var feedPathFilePath = Path.Combine(projectDir, feedPathFileName);

        if (!File.Exists(versionFilePath))
        {
            result.SkipReason = $"Version file not found: {versionFileName}";
            result.Enabled = false;
            return result;
        }

        if (!File.Exists(feedPathFilePath))
        {
            result.SkipReason = $"Feed path file not found: {feedPathFileName}";
            result.Enabled = false;
            return result;
        }

        var version = File.ReadAllText(versionFilePath).Trim();
        var feedPath = File.ReadAllText(feedPathFilePath).Trim();

        if (string.IsNullOrEmpty(version))
        {
            result.SkipReason = $"Version file is empty: {versionFileName}";
            result.Enabled = false;
            return result;
        }

        if (string.IsNullOrEmpty(feedPath))
        {
            result.SkipReason = $"Feed path file is empty: {feedPathFileName}";
            result.Enabled = false;
            return result;
        }

        result.Version = version;
        result.FeedPath = feedPath;

        Console.WriteLine($"  Local package version: {version}");
        Console.WriteLine($"  Local feed path: {feedPath}");

        UpdateMomentumVersion(projectDir, version, result);
        CreateNugetConfig(projectDir, feedPath, result);
        CleanupFiles(versionFilePath, feedPathFilePath);

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

    private static (string versionFile, string feedPathFile) ExtractConfigurationData(JsonElement config)
    {
        config.TryGetProperty("localPackages", out var localConfig);

        var versionFile = "local-mmt-version.txt";
        var feedPathFile = "local-feed-path.txt";

        if (localConfig.TryGetProperty("versionFile", out var vf))
            versionFile = vf.GetString() ?? versionFile;

        if (localConfig.TryGetProperty("feedPathFile", out var fp))
            feedPathFile = fp.GetString() ?? feedPathFile;

        return (versionFile, feedPathFile);
    }

    private static void UpdateMomentumVersion(string projectDir, string version, LocalPackageResult result)
    {
        var packagesPropsPath = Path.Combine(projectDir, "Directory.Packages.props");

        if (!File.Exists(packagesPropsPath))
        {
            Console.WriteLine("  ⚠️  Directory.Packages.props not found, skipping version update");
            return;
        }

        var content = File.ReadAllText(packagesPropsPath);
        var pattern = @"<MomentumVersion>[^<]+</MomentumVersion>";
        var replacement = $"<MomentumVersion>{version}</MomentumVersion>";

        var updated = Regex.Replace(content, pattern, replacement, RegexOptions.None, TimeSpan.FromSeconds(1));

        if (updated != content)
        {
            File.WriteAllText(packagesPropsPath, updated);
            result.VersionUpdated = true;
            Console.WriteLine($"  → Updated MomentumVersion to {version} in Directory.Packages.props");
        }
        else
        {
            Console.WriteLine("  ⚠️  No MomentumVersion element found in Directory.Packages.props");
        }
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

    private static void CleanupFiles(string versionFilePath, string feedPathFilePath)
    {
        try
        {
            if (File.Exists(versionFilePath))
                File.Delete(versionFilePath);

            if (File.Exists(feedPathFilePath))
                File.Delete(feedPathFilePath);

            Console.WriteLine("  → Cleaned up local package configuration files");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️  Could not clean up configuration files: {ex.Message}");
        }
    }
}
