// Copyright (c) ORG_NAME. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PostSetup.Actions;

public class LibraryRenameAction
{
    public class LibraryRenameResult
    {
        public bool Enabled { get; set; }
        public string LibPrefix { get; set; } = string.Empty;
        public List<string> ImportedTokens { get; set; } = [];
        public int ProcessedFiles { get; set; }
        public int ChangedFiles { get; set; }
        public string SkipReason { get; set; } = string.Empty;
    }

    private class ProcessingConfig
    {
        public string[] ScanRoots { get; set; } = [];
        public string[] FileExtensions { get; set; } = [];
        public string[] ExcludeDirs { get; set; } = [];
        public int MaxBytes { get; set; }
    }

    private class PathReplacement
    {
        public string OldPath { get; set; } = "";
        public string NewPath { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public static LibraryRenameResult ProcessMomentumLibImport(string projectDir, JsonElement config)
    {
        var result = new LibraryRenameResult();

        if (!ValidateConfiguration(config, result))
            return result;

        var (libPrefix, importConfig) = ExtractConfigurationData(config);

        if (!ValidateLibraryPrefix(libPrefix, result))
            return result;

        result.Enabled = true;
        result.LibPrefix = libPrefix;

        Console.WriteLine($"Processing Momentum library imports with prefix: {libPrefix}");

        var importedTokens = BuildImportedTokensWhitelist(projectDir, libPrefix);
        result.ImportedTokens = importedTokens;

        if (importedTokens.Count == 0)
        {
            result.SkipReason = "No imported Momentum libraries found";
            return result;
        }

        Console.WriteLine($"  → Found {importedTokens.Count} imported library tokens: {string.Join(", ", importedTokens)}");

        var processingConfig = BuildProcessingConfig(importConfig);
        var (processedFiles, changedFiles) = ProcessAllFiles(projectDir, libPrefix, importedTokens, processingConfig);

        result.ProcessedFiles = processedFiles;
        result.ChangedFiles = changedFiles;

        return result;
    }

    private static bool ValidateConfiguration(JsonElement config, LibraryRenameResult result)
    {
        if (!config.TryGetProperty("momentumLibImport", out var importConfig))
        {
            result.SkipReason = "No momentumLibImport configuration found";
            return false;
        }

        if (!importConfig.TryGetProperty("enabled", out var enabledElement) || !enabledElement.GetBoolean())
        {
            result.SkipReason = "Momentum library import is disabled";
            return false;
        }

        return true;
    }

    private static (string libPrefix, JsonElement importConfig) ExtractConfigurationData(JsonElement config)
    {
        config.TryGetProperty("momentumLibImport", out var importConfig);

        var libPrefix = "";
        if (importConfig.TryGetProperty("libName", out var libNameElement))
        {
            libPrefix = libNameElement.GetString() ?? "";
        }

        return (libPrefix, importConfig);
    }

    private static bool ValidateLibraryPrefix(string libPrefix, LibraryRenameResult result)
    {
        if (string.IsNullOrEmpty(libPrefix))
        {
            result.SkipReason = "libName not specified in momentumLibImport config";
            return false;
        }

        if (libPrefix == "Momentum")
        {
            result.SkipReason = "Library prefix is empty or unchanged (Momentum)";
            return false;
        }

        return true;
    }

    private static ProcessingConfig BuildProcessingConfig(JsonElement importConfig) => new()
    {
        ScanRoots = GetStringArrayFromConfig(importConfig, "scanRoots", ["src", "infra", "libs", "tests"]),
        FileExtensions = GetStringArrayFromConfig(importConfig, "fileExtensions", [".cs", ".csproj"]),
        ExcludeDirs = GetStringArrayFromConfig(importConfig, "excludeDirs", [".git", "bin", "obj"]),
        MaxBytes = GetIntFromConfig(importConfig, "maxBytes", 2097152)
    };

    private static (int processedFiles, int changedFiles) ProcessAllFiles(
        string projectDir,
        string libPrefix,
        List<string> importedTokens,
        ProcessingConfig config)
    {
        var processedFiles = 0;
        var changedFiles = 0;

        // Process regular files
        var regex = BuildMomentumLibraryRegex(importedTokens);
        var (regularProcessed, regularChanged) = ProcessRegularFiles(
            projectDir, libPrefix, importedTokens, config, regex);

        processedFiles += regularProcessed;
        changedFiles += regularChanged;

        // Process solution files
        var (solutionProcessed, solutionChanged) = ProcessSolutionFiles(
            projectDir, libPrefix, importedTokens);

        processedFiles += solutionProcessed;
        changedFiles += solutionChanged;

        return (processedFiles, changedFiles);
    }

    private static (int processed, int changed) ProcessRegularFiles(
        string projectDir,
        string libPrefix,
        List<string> importedTokens,
        ProcessingConfig config,
        Regex regex)
    {
        var processedFiles = 0;
        var changedFiles = 0;

        foreach (var root in config.ScanRoots)
        {
            var rootPath = Path.Combine(projectDir, root);
            if (!Directory.Exists(rootPath)) continue;

            var files = FindFilesToProcess(rootPath, config.FileExtensions, config.ExcludeDirs, config.MaxBytes);

            foreach (var filePath in files)
            {
                processedFiles++;
                if (ProcessFileContent(filePath, regex, libPrefix, projectDir))
                {
                    changedFiles++;
                }
            }
        }

        return (processedFiles, changedFiles);
    }

    private static (int processed, int changed) ProcessSolutionFiles(
        string projectDir,
        string libPrefix,
        List<string> importedTokens)
    {
        var processedFiles = 0;
        var changedFiles = 0;

        var solutionFiles = Directory.GetFiles(projectDir, "*.slnx", SearchOption.TopDirectoryOnly);
        foreach (var slnFile in solutionFiles)
        {
            processedFiles++;
            if (UpdateSolutionReferences(slnFile, libPrefix, projectDir, importedTokens))
            {
                changedFiles++;
            }
        }

        return (processedFiles, changedFiles);
    }

    private static List<string> BuildImportedTokensWhitelist(string projectDir, string libPrefix)
    {
        var tokens = new HashSet<string>();
        var libsPath = Path.Combine(projectDir, "libs");

        if (!Directory.Exists(libsPath))
            return tokens;

        var prefixedLibsPath = Path.Combine(libsPath, libPrefix);
        var searchPath = Directory.Exists(prefixedLibsPath) ? prefixedLibsPath : libsPath;

        var allItems = GetLibraryItems(searchPath, libPrefix);

        foreach (var item in allItems)
        {
            var token = ExtractTokenFromItem(item, libPrefix);
            if (!string.IsNullOrEmpty(token))
            {
                tokens.Add(token);
            }
        }

        return tokens.OrderByDescending(t => t.Length).ToList();
    }

    private static IEnumerable<string> GetLibraryItems(string searchPath, string libPrefix)
    {
        var directories = Directory.GetDirectories(searchPath, "*", SearchOption.AllDirectories)
            .Where(d => Path.GetFileName(d).StartsWith($"{libPrefix}."));

        var projectFiles = Directory.GetFiles(searchPath, "*.csproj", SearchOption.AllDirectories)
            .Where(f => Path.GetFileNameWithoutExtension(f).StartsWith($"{libPrefix}."));

        return directories
            .Concat(projectFiles)
            .Select(Path.GetFileNameWithoutExtension)
            .Distinct()
            .Where(i => i is not null)!;
    }

    private static string ExtractTokenFromItem(string item, string libPrefix)
    {
        if (item.StartsWith($"{libPrefix}.") && item.Length > libPrefix.Length + 1)
        {
            return item[(libPrefix.Length + 1)..];
        }
        return "";
    }

    private static Regex BuildMomentumLibraryRegex(List<string> tokens)
    {
        var escapedTokens = tokens.Select(Regex.Escape);
        var alternation = string.Join("|", escapedTokens);
        var pattern = $@"\bMomentum\.(?<token>{alternation})(?!\.|\w)";

        return new Regex(pattern, RegexOptions.Compiled);
    }

    private static List<string> FindFilesToProcess(string rootPath, string[] extensions, string[] excludeDirs, int maxBytes)
    {
        var files = new List<string>();

        foreach (var ext in extensions)
        {
            try
            {
                var pattern = "*" + ext;
                var foundFiles = Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories)
                    .Where(f => !IsExcludedFile(f, excludeDirs) && new FileInfo(f).Length <= maxBytes)
                    .ToArray();
                files.AddRange(foundFiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Warning: Error searching for {ext} files: {ex.Message}");
            }
        }

        return files;
    }

    private static bool IsExcludedFile(string filePath, string[] excludeDirs)
    {
        return excludeDirs.Any(dir => filePath.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}") ||
                                      filePath.Contains($"{Path.DirectorySeparatorChar}{dir}"));
    }

    private static bool ProcessFileContent(string filePath, Regex regex, string libPrefix, string projectDir)
    {
        try
        {
            var (originalContent, hasBom) = ReadFileWithBomDetection(filePath);

            var modifiedContent = regex.Replace(originalContent, match =>
            {
                var token = match.Groups["token"].Value;

                if (ShouldSkipMatch(originalContent, match.Index))
                {
                    return match.Value; // Keep original
                }

                return $"{libPrefix}.{token}";
            });

            if (modifiedContent != originalContent)
            {
                WriteFileWithBomHandling(filePath, modifiedContent, hasBom);
                Console.WriteLine($"  → Updated library references in: {Path.GetRelativePath(projectDir, filePath)}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Warning: Could not process file {filePath}: {ex.Message}");
        }

        return false;
    }

    private static (string content, bool hasBom) ReadFileWithBomDetection(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        bool hasBom = bytes is [0xEF, 0xBB, 0xBF, ..];

        string content;
        if (hasBom)
        {
            content = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        else
        {
            content = Encoding.UTF8.GetString(bytes);
        }

        return (content, hasBom);
    }

    private static void WriteFileWithBomHandling(string filePath, string content, bool hasBom)
    {
        var tempFile = filePath + ".tmp";
        var encoding = hasBom ? new UTF8Encoding(true) : new UTF8Encoding(false);

        File.WriteAllText(tempFile, content, encoding);
        File.Move(tempFile, filePath, overwrite: true);
    }

    private static bool ShouldSkipMatch(string originalContent, int matchIndex)
    {
        var beforeMatch = originalContent.Substring(0, matchIndex);
        var lineStart = beforeMatch.LastIndexOf('\n') + 1;
        var lineEndIndex = originalContent.IndexOfAny(['\n', '\r'], matchIndex);
        if (lineEndIndex == -1) lineEndIndex = originalContent.Length;

        var currentLine = originalContent.Substring(lineStart, lineEndIndex - lineStart);

        return currentLine.TrimStart().StartsWith("<PackageReference") && currentLine.Contains("Include=");
    }

    private static bool UpdateSolutionReferences(string slnFile, string libPrefix, string projectDir, List<string> importedTokens)
    {
        try
        {
            var originalContent = File.ReadAllText(slnFile);
            var modifiedContent = originalContent;

            foreach (var token in importedTokens)
            {
                var replacements = BuildPathReplacements(token, libPrefix);

                foreach (var replacement in replacements)
                {
                    modifiedContent = modifiedContent.Replace(replacement.OldPath, replacement.NewPath);
                }
            }

            if (modifiedContent != originalContent)
            {
                File.WriteAllText(slnFile, modifiedContent);
                Console.WriteLine($"  → Updated solution references in: {Path.GetRelativePath(projectDir, slnFile)}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Warning: Could not update solution file {slnFile}: {ex.Message}");
        }

        return false;
    }

    private static List<PathReplacement> BuildPathReplacements(string token, string libPrefix)
    {
        return new List<PathReplacement>
        {
            // Project file name replacements
            new() {
                OldPath = $"Momentum.{token}.csproj",
                NewPath = $"{libPrefix}.{token}.csproj",
                Description = "Project file name"
            },

            // Full path replacements with original structure
            new() {
                OldPath = $"libs\\Momentum\\src\\Momentum.{token}\\Momentum.{token}.csproj",
                NewPath = $"libs\\{libPrefix}\\src\\{libPrefix}.{token}\\{libPrefix}.{token}.csproj",
                Description = "Full backslash path (original structure)"
            },
            new() {
                OldPath = $"libs/Momentum/src/Momentum.{token}/Momentum.{token}.csproj",
                NewPath = $"libs/{libPrefix}/src/{libPrefix}.{token}/{libPrefix}.{token}.csproj",
                Description = "Full forward slash path (original structure)"
            },

            // Mixed path replacements (renamed files but old directory structure)
            new() {
                OldPath = $"libs\\Momentum\\src\\{libPrefix}.{token}\\{libPrefix}.{token}.csproj",
                NewPath = $"libs\\{libPrefix}\\src\\{libPrefix}.{token}\\{libPrefix}.{token}.csproj",
                Description = "Mixed backslash path (renamed files, old dirs)"
            },
            new() {
                OldPath = $"libs/Momentum/src/{libPrefix}.{token}/{libPrefix}.{token}.csproj",
                NewPath = $"libs/{libPrefix}/src/{libPrefix}.{token}/{libPrefix}.{token}.csproj",
                Description = "Mixed forward slash path (renamed files, old dirs)"
            },

            // Partial path replacements
            new() {
                OldPath = $"Momentum.{token}/Momentum.{token}.csproj",
                NewPath = $"{libPrefix}.{token}/{libPrefix}.{token}.csproj",
                Description = "Partial path"
            },
            new() {
                OldPath = $"Momentum.{token}\\Momentum.{token}.csproj",
                NewPath = $"{libPrefix}.{token}\\{libPrefix}.{token}.csproj",
                Description = "Partial backslash path"
            }
        };
    }

    private static string[] GetStringArrayFromConfig(JsonElement config, string propertyName, string[] defaultValue)
    {
        if (config.TryGetProperty(propertyName, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
            return arrayElement.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();

        return defaultValue;
    }

    private static int GetIntFromConfig(JsonElement config, string propertyName, int defaultValue)
    {
        if (config.TryGetProperty(propertyName, out var intElement) && intElement.TryGetInt32(out var value))
            return value;

        return defaultValue;
    }
}
